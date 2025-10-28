using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace ZipPeek
{
    public static class RemoteZipExtractor
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<long> GetRemoteFileSizeAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
                return response.Content.Headers.ContentLength.Value;

            return 0;
        }

        public static async Task ExtractRemoteEntryAsync(string url, ZipEntry entry, string outputFolder, string password = null)
        {
            const int LocalHeaderFixedSize = 30;
            long headerStart = entry.LocalHeaderOffset;
            long headerEnd = headerStart + LocalHeaderFixedSize + 2048;

            // تحميل Local Header
            var headerRequest = new HttpRequestMessage(HttpMethod.Get, url);
            headerRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(headerStart, headerEnd);
            var headerResponse = await client.SendAsync(headerRequest, HttpCompletionOption.ResponseHeadersRead);
            byte[] headerData = await headerResponse.Content.ReadAsByteArrayAsync();

            int sigOffset = FindSignature(headerData, 0x04034b50);
            if (sigOffset < 0)
                throw new Exception("Local file header not found in downloaded range.");

            if (headerData.Length < sigOffset + 30)
                throw new Exception("Incomplete local header.");

            ushort generalPurposeFlag = BitConverter.ToUInt16(headerData, sigOffset + 6);
            ushort fileNameLength = BitConverter.ToUInt16(headerData, sigOffset + 26);
            ushort extraFieldLength = BitConverter.ToUInt16(headerData, sigOffset + 28);
            int headerTotalLength = 30 + fileNameLength + extraFieldLength;

            string fileName = Encoding.UTF8.GetString(headerData, sigOffset + 30, fileNameLength);
            string outputPath = Path.Combine(outputFolder, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            bool hasDataDescriptor = (generalPurposeFlag & 0x0008) != 0;

            if (entry.IsEncrypted)
            {
                if (string.IsNullOrEmpty(password))
                    throw new Exception($"⛔ File '{fileName}' is encrypted. Password is required.");

                const int encryptionHeaderSize = 12;
                long fullStart = headerStart;
                long extraBytes = 64; // تحسبًا لوجود Data Descriptor

                long fullEnd = fullStart + headerTotalLength + encryptionHeaderSize + entry.CompressedSize + extraBytes - 1;

                var dataRequest = new HttpRequestMessage(HttpMethod.Get, url);
                dataRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(fullStart, fullEnd);
                var dataResponse = await client.SendAsync(dataRequest, HttpCompletionOption.ResponseHeadersRead);
                byte[] fullEncryptedData = await dataResponse.Content.ReadAsByteArrayAsync();

                if (fullEncryptedData.Length < sigOffset + headerTotalLength + encryptionHeaderSize)
                    throw new Exception("Downloaded encrypted data is smaller than expected.");

                // مهم جدًا: لا تقص البيانات، مرر الـ stream بالكامل من بداية الـ LocalHeader
                using (var inputStream = new MemoryStream(fullEncryptedData, sigOffset, fullEncryptedData.Length - sigOffset))
                using (var zipStream = new ZipInputStream(inputStream))
                {
                    zipStream.Password = password;
                    var zipEntry = zipStream.GetNextEntry() ?? throw new Exception($"⚠️ Could not open encrypted file '{fileName}'. Possibly AES encryption (not supported).");
                    using (var output = new MemoryStream())
                    {
                        await zipStream.CopyToAsync(output);
                        File.WriteAllBytes(outputPath, output.ToArray());
                        File.SetLastWriteTime(outputPath, entry.LastModified);
                    }
                }

                return;
            }

            // تحميل الملف غير المشفر
            long fullDataStart = headerStart + sigOffset;
            long fullDataEnd = fullDataStart + headerTotalLength + entry.CompressedSize + (hasDataDescriptor ? 20 : 0) + 32 - 1;

            var normalRequest = new HttpRequestMessage(HttpMethod.Get, url);
            normalRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(fullDataStart, fullDataEnd);
            var normalResponse = await client.SendAsync(normalRequest, HttpCompletionOption.ResponseHeadersRead);
            byte[] fullFileData = await normalResponse.Content.ReadAsByteArrayAsync();

            if (fullFileData.Length < headerTotalLength + entry.CompressedSize)
                throw new Exception("Downloaded data is smaller than expected.");

            byte[] outputData;

            if (entry.CompressionMethod == 0) // Stored
            {
                outputData = new byte[entry.CompressedSize];
                Array.Copy(fullFileData, headerTotalLength, outputData, 0, entry.CompressedSize);
            }
            else if (entry.CompressionMethod == 8) // Deflate
            {
                using (var input = new MemoryStream(fullFileData, headerTotalLength, (int)entry.CompressedSize))
                using (var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    await deflate.CopyToAsync(output);
                    outputData = output.ToArray();
                }
            }
            else
            {
                throw new Exception($"⚠️ Skipping unsupported compression method {entry.CompressionMethod} for file {fileName}");
            }

            File.WriteAllBytes(outputPath, outputData);
            File.SetLastWriteTime(outputPath, entry.LastModified);
        }

        private static int FindSignature(byte[] data, uint signature)
        {
            for (int i = 0; i <= data.Length - 4; i++)
            {
                if (BitConverter.ToUInt32(data, i) == signature)
                    return i;
            }
            return -1;
        }
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ZipPeek
{

    public static class RemoteZipExtractor
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task ExtractRemoteEntryAsync(string url, ZipEntry entry, string outputFolder)
        {
            const int LocalHeaderFixedSize = 30;
            long headerStart = entry.LocalHeaderOffset;
            long headerEnd = headerStart + LocalHeaderFixedSize + 256;

            byte[] headerData;

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(headerStart, headerEnd);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            headerData = await response.Content.ReadAsByteArrayAsync();

            if (BitConverter.ToUInt32(headerData, 0) != 0x04034b50)
                throw new Exception("Invalid local file header signature.");

            ushort fileNameLength = BitConverter.ToUInt16(headerData, 26);
            ushort extraFieldLength = BitConverter.ToUInt16(headerData, 28);

            string fileName = Encoding.UTF8.GetString(headerData, 30, fileNameLength);
            string outputPath = Path.Combine(outputFolder, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            int headerTotalLength = 30 + fileNameLength + extraFieldLength;
            long dataStart = headerStart + headerTotalLength;
            long dataEnd = dataStart + entry.CompressedSize - 1;

            byte[] compressedData;

            request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(dataStart, dataEnd);
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            compressedData = await response.Content.ReadAsByteArrayAsync();

            byte[] outputData;

            if (entry.CompressionMethod == 0) // Stored (no compression)
            {
                outputData = compressedData;
            }
            else if (entry.CompressionMethod == 8) // Deflate
            {
                using (var input = new MemoryStream(compressedData))
                using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    await deflate.CopyToAsync(output);
                    outputData = output.ToArray();
                }
            }
            else
            {
                throw new NotSupportedException($"Compression method {entry.CompressionMethod} not supported.");
            }

            File.WriteAllBytes(outputPath, outputData);
        }
    }
}

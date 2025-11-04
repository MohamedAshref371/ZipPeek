using System;
using System.Diagnostics;
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
            using (var request = new HttpRequestMessage(HttpMethod.Head, url))
            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
                    return response.Content.Headers.ContentLength.Value;

                return 0;
            }
        }

        public static async Task ExtractRemoteEntryAsync(string url, ZipEntry entry, IProgress<long> progress, string password = null)
        {
            const int LocalHeaderFixedSize = 30;
            long headerStart = entry.LocalHeaderOffset;
            long headerEnd = headerStart + LocalHeaderFixedSize + 2048;

            byte[] headerData;
            // تحميل Local Header
            using (var headerRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                headerRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(headerStart, headerEnd);
                using (var headerResponse = await client.SendAsync(headerRequest, HttpCompletionOption.ResponseHeadersRead))
                    headerData = await headerResponse.Content.ReadAsByteArrayAsync();
            }

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
            string outputPath = Path.Combine("Download", fileName);
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

                byte[] fullEncryptedData;
                if (progress is null)
                {
                    using (var dataRequest = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        dataRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(fullStart, fullEnd);
                        using (var dataResponse = await client.SendAsync(dataRequest, HttpCompletionOption.ResponseHeadersRead))
                            fullEncryptedData = await dataResponse.Content.ReadAsByteArrayAsync();
                    }
                }
                else
                    fullEncryptedData = await DownloadManager.FetchRangeAsync(url, fullStart, fullEnd, progress);

                if (fullEncryptedData.Length < sigOffset + headerTotalLength + encryptionHeaderSize)
                    throw new Exception("Downloaded encrypted data is smaller than expected.");

                if (entry.IsAesEncrypted)
                {
                    // لن يحدث ابدا
                }
                else
                {
                    using (var inputStream = new MemoryStream(fullEncryptedData, sigOffset, fullEncryptedData.Length - sigOffset))
                    using (var zipStream = new ZipInputStream(inputStream))
                    {
                        zipStream.Password = password;
                        var zipEntry = zipStream.GetNextEntry() ?? throw new Exception($"⚠️ Could not open encrypted file '{fileName}'.");
                        using (var output = new MemoryStream())
                        {
                            await zipStream.CopyToAsync(output);
                            File.WriteAllBytes(outputPath, output.ToArray());
                            File.SetLastWriteTime(outputPath, entry.LastModified);
                        }
                    }
                }
                return;
            }

            // تحميل الملف غير المشفر
            long fullDataStart = headerStart + sigOffset;
            long fullDataEnd = fullDataStart + headerTotalLength + entry.CompressedSize + (hasDataDescriptor ? 20 : 0) + 32 - 1;

            byte[] fullFileData;
            if (progress is null)
            {
                using (var normalRequest = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    normalRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(fullDataStart, fullDataEnd);
                    using (var normalResponse = await client.SendAsync(normalRequest, HttpCompletionOption.ResponseHeadersRead))
                        fullFileData = await normalResponse.Content.ReadAsByteArrayAsync();
                }
            }
            else
                fullFileData = await DownloadManager.FetchRangeAsync(url, fullDataStart, fullDataEnd, progress);

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

        public static async Task<long> ExtractRemoteEntry2Async(string url, ZipEntry entry, IProgress<long> progress, string password = null)
        {
            const int LocalHeaderFixedSize = 30;
            long headerStart = entry.LocalHeaderOffset;
            long headerEnd = headerStart + LocalHeaderFixedSize + 2048;

            // تحميل Local Header إلى مصفوفة بايت صغيرة أولاً
            byte[] headerData;
            using (var headerRequest = new HttpRequestMessage(HttpMethod.Get, url))
            {
                headerRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(headerStart, headerEnd);
                using (var headerResponse = await client.SendAsync(headerRequest, HttpCompletionOption.ResponseHeadersRead))
                    headerData = await headerResponse.Content.ReadAsByteArrayAsync();
            }
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
            string outputPath = Path.Combine("Download", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            bool hasDataDescriptor = (generalPurposeFlag & 0x0008) != 0;

            // نحدد نطاق التحميل الكامل اعتماداً على ما إذا كان مشفراً أم لا
            // للحالة المشفرة سنبدأ من بداية LocalHeader (headerStart) لنتأكد من وجود كل شيء
            // للحالة غير المشفرة يمكننا البدء من headerStart + sigOffset لاقتصاد نطاق التحميل
            string tempPath = Path.Combine("_temp", fileName + ".zip");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

            int bytesRead;
            long totalRead = 0;
            byte[] buffer = new byte[8192];
            if (entry.IsEncrypted)
            {
                if (string.IsNullOrEmpty(password))
                    throw new Exception($"⛔ File '{fileName}' is encrypted. Password is required.");

                const int encryptionHeaderSize = 12;
                long fullStart = headerStart; // مهم: نبدأ من بداية الـ LocalHeader
                long extraBytes = 64; // تحسباً لوجود Data Descriptor أو اختلافات
                long fullEnd = fullStart + headerTotalLength + encryptionHeaderSize + entry.CompressedSize + extraBytes - 1;

                // تحميل إلى ملف مؤقت على الهارد
                await DownloadManager.DownloadRangeToFileAsync(url, tempPath, fullStart, fullEnd, progress);

                if (entry.IsAesEncrypted)
                {
                    ExtractSingleFile(tempPath, entry.FileName, "Download", password);
                    if (File.Exists(outputPath))
                        totalRead = new FileInfo(outputPath).Length;
                }
                else
                {
                    // افتح الملف المؤقت ومرره إلى ZipInputStream
                    using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true))
                    {
                        // نضع مؤشر القراءة عند بداية local header داخل الملف المؤقت
                        // داخل الملف المؤقت الموضع الصحيح للـ local header هو (sigOffset) لأننا بدأنا التحميل من headerStart
                        fs.Seek(sigOffset, SeekOrigin.Begin);

                        using (var zipStream = new ZipInputStream(fs))
                        {
                            zipStream.Password = password;

                            var zipEntryFromStream = zipStream.GetNextEntry() ?? throw new Exception($"⚠️ Could not open encrypted file '{fileName}'.");

                            // كتابة المخرجات مباشرة إلى ملف (من الهارد إلى الهارد)
                            using (var outFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                            {
                                while ((bytesRead = await zipStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await outFs.WriteAsync(buffer, 0, bytesRead);
                                    totalRead += bytesRead;
                                }
                            }

                            File.SetLastWriteTime(outputPath, entry.LastModified);
                        }
                    }
                }
                return totalRead;
            }

            // الحالة غير المشفرة
            long fullDataStart = headerStart + sigOffset;
            long fullDataEnd = fullDataStart + headerTotalLength + entry.CompressedSize + (hasDataDescriptor ? 20 : 0) + 32 - 1;

            // تحميل إلى ملف مؤقت
            await DownloadManager.DownloadRangeToFileAsync(url, tempPath, fullDataStart, fullDataEnd, progress);

            // الآن افتح الملف المؤقت واستخدمه لفك الضغط مباشرة إلى القرص
            using (var input = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true))
            {
                // داخل هذا الملف المؤقت، رأس الملف المحلي يبدأ عند موضع 0 لأننا حملنا من fullDataStart (= headerStart + sigOffset)
                // لذلك نضع Seek إلى headerTotalLength داخل الملف المؤقت لنبدأ بنهاية الهيدر حيث يبدأ البيانات المضغوطة
                input.Seek(headerTotalLength, SeekOrigin.Begin);

                if (entry.CompressionMethod == 0) // Stored
                {
                    using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                    {
                        while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;
                        }
                    }
                }
                else if (entry.CompressionMethod == 8) // Deflate
                {
                    using (var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress, leaveOpen: true))
                    using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                    {
                        while ((bytesRead = await deflate.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;
                        }
                    }
                }
                else
                {
                    throw new Exception($"⚠️ Skipping unsupported compression method {entry.CompressionMethod} for file {fileName}");
                }

                File.SetLastWriteTime(outputPath, entry.LastModified);
            }
            return totalRead;
        }

        public static string SevenZipPath = @"7zip.org\7z.exe";
        public static void ExtractSingleFile(string archivePath, string fileInsideArchive, string outputDir, string password)
        {
            string args = $"x \"{archivePath}\" -o\"{outputDir}\" -p\"{password}\" \"{fileInsideArchive}\" -y";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = SevenZipPath, Arguments = args,
                    RedirectStandardOutput = false, RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
        }

    }
}

using System;

namespace ZipPeek
{
    public class ZipEntry
    {
        public string FileName { get; set; }
        public long LocalHeaderOffset { get; set; }
        public long CompressedSize { get; set; }
        public long UncompressedSize { get; set; }
        public ushort CompressionMethod { get; set; }
        public bool IsEncrypted { get; set; }
        public bool IsAesEncrypted { get; set; }
        public DateTime LastModified { get; set; }
    }
}

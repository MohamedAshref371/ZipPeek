using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ZipPeek
{
    public static class TreeViewHelper
    {
        public static TreeView TreeView;
        private static readonly Dictionary<string, TreeNode> NodeCache = new Dictionary<string, TreeNode>();

        public static void AddToTree(ZipEntry entry)
        {
            var parts = entry.FileName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            TreeNodeCollection current = TreeView.Nodes;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                bool isLast = (i == parts.Length - 1);
                bool isFolder = entry.FileName.EndsWith("/") && isLast;
                string pathSoFar = string.Join("/", parts, 0, i + 1);

                if (NodeCache.TryGetValue(pathSoFar, out TreeNode found))
                {
                    current = found.Nodes;
                    continue;
                }

                string displayName;

                if (isFolder)
                {
                    displayName = $"📁 {part} (empty)";
                }
                else if (isLast)
                {
                    string compressed = FormatSize(entry.CompressedSize);
                    string uncompressed = FormatSize(entry.UncompressedSize);
                    string modified = entry.LastModified.ToString("yyyy-MM-dd HH:mm");
                    string icon = entry.IsEncrypted ? "🔒📄" : "📄";
                    displayName = $"{icon} {part} ({compressed} / {uncompressed}) | {modified}";
                }
                else
                {
                    displayName = $"📁 {part}";
                }

                TreeNode newNode = new TreeNode(displayName)
                {
                    Name = part,
                    Tag = (isLast && !isFolder) ? entry : null
                };

                current.Add(newNode);
                NodeCache[pathSoFar] = newNode;
                current = newNode.Nodes;
            }
        }

        public static string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

    }
}

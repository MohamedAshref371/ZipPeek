using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ZipPeek
{
    public static class TreeViewHelper
    {
        public static TreeView TreeView;

        public static void AddToTree(ZipEntry entry)
        {
            string[] parts = entry.FileName.Split('/');
            TreeNodeCollection current = TreeView.Nodes;
            TreeNode node = null;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (string.IsNullOrWhiteSpace(part)) continue;

                bool isLastPart = (i == parts.Length - 1);
                string nodeText = part;

                if (isLastPart && entry.CompressedSize > 0)
                {
                    string compressed = FormatSize(entry.CompressedSize);
                    string uncompressed = FormatSize(entry.UncompressedSize);
                    nodeText = $"{(entry.IsEncrypted ? "🔒" : "")} {part} ({compressed} / {uncompressed})";
                }

                TreeNode found = null;
                foreach (TreeNode n in current)
                {
                    if (n.Text.StartsWith(part))
                    {
                        found = n;
                        break;
                    }
                }

                if (found == null)
                {
                    found = new TreeNode(nodeText);
                    current.Add(found);
                }

                node = found;
                current = node.Nodes;
            }

            if (node != null)
                node.Tag = entry;
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

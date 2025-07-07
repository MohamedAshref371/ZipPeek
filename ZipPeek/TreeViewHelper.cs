using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ZipPeek
{
    public static class TreeViewHelper
    {
        public static TreeView TreeView;
        private static readonly Dictionary<string, TreeNode> NodeCache = new Dictionary<string, TreeNode>();

        public static void Reset()
        {
            NodeCache.Clear();
            TreeView?.Nodes.Clear();
        }

        public static void AddToTree(ZipEntry entry)
        {
            var parts = entry.FileName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            TreeNodeCollection current = TreeView.Nodes;
            string pathSoFar = "", displayName;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                bool isLast = (i == parts.Length - 1);
                bool isFolder = entry.FileName.EndsWith("/") && isLast;
                pathSoFar = (pathSoFar == "") ? part : $"{pathSoFar}/{part}";

                if (NodeCache.TryGetValue(pathSoFar, out TreeNode found))
                {
                    current = found.Nodes;
                    continue;
                }

                if (!isFolder && isLast)
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

        public static void MarkEmptyFolders(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag == null || node.Tag is NodeMetadata)
                {
                    if (node.Nodes.Count == 0 && !node.Text.EndsWith("(empty)"))
                        node.Text += " (empty)";
                    else
                        MarkEmptyFolders(node.Nodes);
                }
            }
        }

        private static string FormatSize(long bytes)
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

        public static void SearchByName(List<TreeNode> matches, string keyword, bool ignoreCase = true)
        {
            matches.Clear();
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            void Search(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Text.IndexOf(keyword, comparison) >= 0)
                        matches.Add(node);

                    if (node.Nodes.Count > 0)
                        Search(node.Nodes);
                }
            }

            Search(TreeView.Nodes);
        }

        public enum SortCriteria
        {
            Name,
            ModifiedTime,
            UncompressedSize,
            CompressedSize
        }

        private class NodeMetadata
        {
            public DateTime? MinModified;
            public long? MaxSize;
            public long? MaxCompressedSize;
        }

        public static void ComputeMetadata(TreeNode node)
        {
            var metadata = new NodeMetadata();

            foreach (TreeNode child in node.Nodes)
            {
                if (child.Tag is ZipEntry fileEntry)
                {
                    if (!metadata.MinModified.HasValue || fileEntry.LastModified < metadata.MinModified)
                        metadata.MinModified = fileEntry.LastModified;

                    if (!metadata.MaxSize.HasValue || fileEntry.UncompressedSize > metadata.MaxSize)
                        metadata.MaxSize = fileEntry.UncompressedSize;

                    if (!metadata.MaxCompressedSize.HasValue || fileEntry.CompressedSize > metadata.MaxCompressedSize)
                        metadata.MaxCompressedSize = fileEntry.CompressedSize;
                }
                else
                {
                    ComputeMetadata(child);
                    if (child.Tag is NodeMetadata childMeta)
                    {
                        if (childMeta.MinModified.HasValue &&
                            (!metadata.MinModified.HasValue || childMeta.MinModified < metadata.MinModified))
                            metadata.MinModified = childMeta.MinModified;

                        if (childMeta.MaxSize.HasValue &&
                            (!metadata.MaxSize.HasValue || childMeta.MaxSize > metadata.MaxSize))
                            metadata.MaxSize = childMeta.MaxSize;

                        if (childMeta.MaxCompressedSize.HasValue &&
                            (!metadata.MaxCompressedSize.HasValue || childMeta.MaxCompressedSize > metadata.MaxCompressedSize))
                            metadata.MaxCompressedSize = childMeta.MaxCompressedSize;
                    }
                }
            }

            // خزّن فقط لو مش ملف (أي: مجلد أو مجلد افتراضي)
            if (!(node.Tag is ZipEntry))
            {
                node.Tag = metadata;
            }
        }

        public static void SortNodes(TreeNodeCollection nodes, SortCriteria criteria, bool ascending = true)
        {
            var sorted = new List<TreeNode>(nodes.Cast<TreeNode>());

            sorted.Sort((a, b) =>
            {
                int cmp = 0;

                switch (criteria)
                {
                    case SortCriteria.Name:
                        cmp = string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase);
                        break;

                    case SortCriteria.ModifiedTime:
                        DateTime aTime = DateTime.MaxValue;
                        DateTime bTime = DateTime.MaxValue;

                        if (a.Tag is ZipEntry zeA)
                            aTime = zeA.LastModified;
                        else if (a.Tag is NodeMetadata mdA && mdA.MinModified.HasValue)
                            aTime = mdA.MinModified.Value;

                        if (b.Tag is ZipEntry zeB)
                            bTime = zeB.LastModified;
                        else if (b.Tag is NodeMetadata mdB && mdB.MinModified.HasValue)
                            bTime = mdB.MinModified.Value;

                        cmp = aTime.CompareTo(bTime);
                        break;

                    case SortCriteria.UncompressedSize:
                        long aSize = long.MinValue;
                        long bSize = long.MinValue;

                        if (a.Tag is ZipEntry zeA1)
                            aSize = zeA1.UncompressedSize;
                        else if (a.Tag is NodeMetadata mdA1 && mdA1.MaxSize.HasValue)
                            aSize = mdA1.MaxSize.Value;

                        if (b.Tag is ZipEntry zeB1)
                            bSize = zeB1.UncompressedSize;
                        else if (b.Tag is NodeMetadata mdB1 && mdB1.MaxSize.HasValue)
                            bSize = mdB1.MaxSize.Value;

                        cmp = aSize.CompareTo(bSize);
                        break;

                    case SortCriteria.CompressedSize:
                        long aComp = long.MinValue;
                        long bComp = long.MinValue;

                        if (a.Tag is ZipEntry zeA2)
                            aComp = zeA2.CompressedSize;
                        else if (a.Tag is NodeMetadata mdA2 && mdA2.MaxCompressedSize.HasValue)
                            aComp = mdA2.MaxCompressedSize.Value;

                        if (b.Tag is ZipEntry zeB2)
                            bComp = zeB2.CompressedSize;
                        else if (b.Tag is NodeMetadata mdB2 && mdB2.MaxCompressedSize.HasValue)
                            bComp = mdB2.MaxCompressedSize.Value;

                        cmp = aComp.CompareTo(bComp);
                        break;
                }

                return ascending ? cmp : -cmp;
            });

            nodes.Clear();
            foreach (var node in sorted)
            {
                nodes.Add(node);
                if (node.Nodes.Count > 0)
                    SortNodes(node.Nodes, criteria, ascending);
            }
        }
    }
}

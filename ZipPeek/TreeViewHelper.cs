using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ZipPeek
{
    public static class TreeViewHelper
    {
        private static TreeView treeView;
        private static readonly Dictionary<string, TreeNode> nodeCache = new Dictionary<string, TreeNode>();
        private static readonly List<TreeNode> mainNode = new List<TreeNode>();

        public static void Start(TreeView tv)
        {
            treeView = tv;
            treeView.BeforeExpand -= TreeView_BeforeExpand;
            treeView.BeforeExpand += TreeView_BeforeExpand;
        }

        public static void Reset()
        {
            zipEntries.Clear();
            paths.Clear();
            treeView?.Nodes.Clear();
        }

        private static void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            if (node == null || node.Tag == null) return;

            node.Nodes.Clear();
            List<TreeNode> list = (List<TreeNode>)node.Tag;
            node.Nodes.AddRange(list.ToArray());
            node.Tag = null;

            if (list.Count == 0 && !node.Text.EndsWith("(empty)"))
                node.Text += " (empty)";
        }

        private static List<ZipEntry> zipEntries = new List<ZipEntry>();
        public static void AddToTree(List<ZipEntry> entries)
        {
            treeView.BeginUpdate();

            foreach (var entry in entries)
                AddToTree(entry);

            for (int i = 0; i < mainNode.Count; i++)
                treeView.Nodes.Add(mainNode[i]);

            treeView.EndUpdate();
            zipEntries = entries;
            nodeCache.Clear();
            mainNode.Clear();
        }

        private static void AddToTree(ZipEntry entry)
        {
            var parts = entry.FileName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            List<TreeNode> current = mainNode;
            string pathSoFar = "", displayName;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                bool isLast = (i == parts.Length - 1);
                bool isFolder = entry.FileName.EndsWith("/") && isLast;
                pathSoFar = (pathSoFar == "") ? part : $"{pathSoFar}/{part}";

                if (nodeCache.TryGetValue(pathSoFar, out TreeNode found))
                {
                    current = (List<TreeNode>)found.Tag;
                    continue;
                }

                if (!isFolder && isLast)
                {
                    string compressed = Form1.FormatSize(entry.CompressedSize);
                    string uncompressed = Form1.FormatSize(entry.UncompressedSize);
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
                nodeCache[pathSoFar] = newNode;

                if (!isLast || isFolder)
                {
                    newNode.Tag = new List<TreeNode>();
                    newNode.Nodes.Add(new TreeNode("Loading..."));
                    current = (List<TreeNode>)newNode.Tag;
                }
            }
        }

        #region Search
        private static void CollectPaths()
        {
            foreach (ZipEntry entry in zipEntries)
            {
                string path = entry.FileName.TrimEnd('/');
                int index = path.IndexOf('/');

                while (index >= 0)
                {
                    string folder = path.Substring(0, index);
                    paths.Add(folder);

                    index = path.IndexOf('/', index + 1);
                }

                paths.Add(path);
            }
        }

        static readonly HashSet<string> paths = new HashSet<string>();
        public static void SearchByName(List<string> matches, string keyword, bool ignoreCase = true)
        {
            if (paths.Count == 0) CollectPaths();

            matches.Clear();
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            foreach (string path in paths)
                if (path.IndexOf(keyword, path.LastIndexOf('/') + 1, comparison) >= 0)
                    matches.Add(path);
        }

        public static void SelectNode(string path)
        {
            var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            TreeNode node = null;

            TreeNodeCollection nodes = treeView.Nodes;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                node = FindNode(nodes, parts[i]);
                nodes = node.Nodes;
            }

            foreach (TreeNode n in nodes)
                if (n.Name == parts[parts.Length - 1])
                    node = n;

            treeView.SelectedNode = node;
            node.EnsureVisible();
            treeView.Focus();
        }

        private static TreeNode FindNode(TreeNodeCollection nodes, string name)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name == name)
                {
                    node.Expand();
                    return node;
                }
            }
            return null;
        }
        #endregion

        #region Sorting
        public enum SortCriteria
        {
            Name,
            ModifiedTime,
            UncompressedSize,
            CompressedSize
        }

        public static void SortNodes(SortCriteria criteria, bool ascending = true)
        {
            paths.Clear();
            treeView?.Nodes.Clear();

            int asc = ascending ? 1 : -1;
            switch (criteria)
            {
                case SortCriteria.Name:
                    zipEntries.Sort((a, b) => a.FileName.Split('/').CompareTo(b.FileName.Split('/')) * asc);
                    break;

                case SortCriteria.ModifiedTime:
                    zipEntries.Sort((a, b) => a.LastModified.CompareTo(b.LastModified) * asc);
                    break;

                case SortCriteria.UncompressedSize:
                    zipEntries.Sort((a, b) => a.UncompressedSize.CompareTo(b.UncompressedSize) * asc);
                    break;

                case SortCriteria.CompressedSize:
                    zipEntries.Sort((a, b) => a.CompressedSize.CompareTo(b.CompressedSize) * asc);
                    break;
            }

            AddToTree(zipEntries);
        }

        private static int CompareTo(this string[] a, string[] b)
        {
            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++)
            {
                int cmp = string.Compare(a[i], b[i], StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
            }
            return a.Length.CompareTo(b.Length);
        }
        #endregion
    }
}

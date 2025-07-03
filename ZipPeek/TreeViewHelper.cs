using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ZipPeek
{
    public static class TreeViewHelper
    {
        public static TreeView TreeView;

        public static void AddToTree(string path, ZipEntry entry)
        {
            string[] parts = path.Split('/');
            TreeNodeCollection current = TreeView.Nodes;
            TreeNode node = null;

            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;

                TreeNode found = null;
                foreach (TreeNode n in current)
                {
                    if (n.Text == part)
                    {
                        found = n;
                        break;
                    }
                }

                if (found == null)
                {
                    found = new TreeNode(part);
                    current.Add(found);
                }

                node = found;
                current = node.Nodes;
            }

            if (node != null)
                node.Tag = entry;
        }

        #region TreeZip_NodeMouseClick
        public static List<TreeNode> SelectedNodes = new List<TreeNode>();
        public static TreeNode LastNode;

        public static void ToggleSelectNode(TreeNode node)
        {
            if (SelectedNodes.Contains(node))
            {
                SelectedNodes.Remove(node);
                node.BackColor = TreeView.BackColor;
                node.ForeColor = TreeView.ForeColor;
            }
            else
            {
                AddToSelection(node);
            }
        }

        public static void AddToSelection(TreeNode node)
        {
            SelectedNodes.Add(node);
            node.BackColor = SystemColors.Highlight;
            node.ForeColor = SystemColors.HighlightText;
        }

        public static void ClearSelection()
        {
            foreach (var node in SelectedNodes)
            {
                node.BackColor = TreeView.BackColor;
                node.ForeColor = TreeView.ForeColor;
            }
            SelectedNodes.Clear();
        }

        public static void SelectRange(TreeNode end)
        {
            ClearSelection();
            bool found = false;

            foreach (TreeNode node in TraverseAllNodes(TreeView.Nodes))
            {
                if (node == LastNode || node == end)
                {
                    if (found || LastNode == end)
                    {
                        AddToSelection(node);
                        break;
                    }
                    else
                    {
                        found = true;
                        AddToSelection(node);
                    }
                }
                else if (found)
                {
                    AddToSelection(node);
                }
            }
        }

        private static IEnumerable<TreeNode> TraverseAllNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                yield return node;
                foreach (TreeNode child in TraverseAllNodes(node.Nodes))
                    yield return child;
            }
        }
        #endregion

    }
}

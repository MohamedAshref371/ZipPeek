using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPeek
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            TreeViewHelper.TreeView = treeZip;
        }

        private void LoadBtn_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog() != DialogResult.OK) return;

            treeZip.Nodes.Clear();
            List<ZipEntry> entries;
            try
            {
                entries = ZipReader.ReadZipEntries(ofd.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading ZIP file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var entry in entries)
                TreeViewHelper.AddToTree(entry.Path, entry.LocalHeaderOffset);
        }

        private async void OnlineLoadBtn_Click(object sender, EventArgs e)
        {
            treeZip.Nodes.Clear();
            List<ZipEntry> entries;
            try
            {
                entries = await ZipReader.ReadZipEntriesAsync(urlTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading ZIP file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var entry in entries)
                TreeViewHelper.AddToTree(entry.Path, entry.LocalHeaderOffset);
        }

        private void TreeZip_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //treeZip.SelectedNode = e.Node;

            //if (ModifierKeys == Keys.Control)
            //{
            //    TreeViewHelper.ToggleSelectNode(e.Node);
            //}
            //else if (ModifierKeys == Keys.Shift && TreeViewHelper.LastNode != null)
            //{
            //    TreeViewHelper.SelectRange(e.Node);
            //}
            //else
            //{
            //    TreeViewHelper.ClearSelection();
            //    TreeViewHelper.AddToSelection(e.Node);
            //}

            //TreeViewHelper.LastNode = e.Node;
        }
    }
}

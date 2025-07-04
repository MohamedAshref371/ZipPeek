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
                TreeViewHelper.AddToTree(entry);
        }

        private async void DownloadBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (treeZip.SelectedNode?.Tag is ZipEntry entry)
                    await RemoteZipExtractor.ExtractRemoteEntryAsync(urlTextBox.Text, entry, "down");
            
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting entry from remote ZIP file: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void TreeZip_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
        }

    }
}

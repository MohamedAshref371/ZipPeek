using System;
using System.Collections.Generic;
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
            if (string.IsNullOrWhiteSpace(urlTextBox.Text))
                return;

            treeZip.Nodes.Clear();
            List<ZipEntry> entries;
            onlineLoadBtn.Enabled = false; downloadBtn.Enabled = false;
            try
            {
                entries = await RemoteZipReader.ReadAsync(urlTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading ZIP file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                onlineLoadBtn.Enabled = true;
                downloadBtn.Enabled = true;
            }

            foreach (var entry in entries)
                TreeViewHelper.AddToTree(entry);
        }

        private async void DownloadBtn_Click(object sender, EventArgs e)
        {
            onlineLoadBtn.Enabled = false; downloadBtn.Enabled = false;
            try
            {
                if (treeZip.SelectedNode?.Tag is ZipEntry entry)
                    await RemoteZipExtractor.ExtractRemoteEntryAsync(urlTextBox.Text, entry, "downloadFolder");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting entry from remote ZIP file: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            onlineLoadBtn.Enabled = true; downloadBtn.Enabled = true;
        }

        private void TreeZip_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
        }

    }
}

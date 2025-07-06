using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ZipPeek
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            TreeViewHelper.TreeView = treeZip;

            string[] versionParts = Application.ProductVersion.Split('.');
            Array.Resize(ref versionParts, 2);
            Text = $"{Application.ProductName} v{string.Join(".", versionParts)} - View ZIP Files Online";

            statusLabel.Text = "Ready.";
        }

        private async void OnlineLoadBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(urlTextBox.Text))
            {
                statusLabel.Text = "⚠️ Please enter a valid URL.";
                return;
            }

            treeZip.Nodes.Clear();
            onlineLoadBtn.Enabled = false;
            downloadBtn.Enabled = false;
            statusLabel.Text = "📡 Connecting... downloading central directory...";

            List<ZipEntry> entries;
            try
            {
                entries = await RemoteZipReader.ReadAsync(urlTextBox.Text);
                entries = entries.OrderByDescending(ze => ze.FileName, StringComparer.OrdinalIgnoreCase).ToList();

                foreach (var entry in entries)
                    TreeViewHelper.AddToTree(entry);

                statusLabel.Text = $"✅ Loaded {entries.Count} entries successfully.";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "❌ Failed to load ZIP file.";
                MessageBox.Show($"Error reading ZIP file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                onlineLoadBtn.Enabled = true;
                downloadBtn.Enabled = true;
            }
        }

        private async void DownloadBtn_Click(object sender, EventArgs e)
        {
            var node = treeZip.SelectedNode;
            if (node == null || !(node.Tag is ZipEntry))
            {
                statusLabel.Text = "⚠️ Please select a file to download.";
                MessageBox.Show("Please select a ZIP entry to download.", "Selection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ZipEntry entry = (ZipEntry)node.Tag;

            onlineLoadBtn.Enabled = false;
            downloadBtn.Enabled = false;

            try
            {
                if (entry.IsEncrypted && string.IsNullOrWhiteSpace(passwordTextBox.Text))
                {
                    statusLabel.Text = "🔒 Password required to extract encrypted file.";
                    MessageBox.Show("This file is encrypted. Please enter the password.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    statusLabel.Text = $"⬇️ Downloading: {entry.FileName} ...";
                    await RemoteZipExtractor.ExtractRemoteEntryAsync( urlTextBox.Text, entry, "downloadFolder", entry.IsEncrypted ? passwordTextBox.Text : null );
                    statusLabel.Text = $"✅ Downloaded: {entry.FileName}";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"❌ Failed to extract {entry.FileName}";
                MessageBox.Show($"Error extracting entry from remote ZIP file: {ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                onlineLoadBtn.Enabled = true;
                downloadBtn.Enabled = true;
            }
        }

        private void TreeZip_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            
        }
    }
}

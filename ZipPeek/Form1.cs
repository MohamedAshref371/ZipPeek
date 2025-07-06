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

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                statusLabel.Text = "⚠️ No internet connection.";
                return;
            }

            treeZip.Nodes.Clear();
            SetUiState(false);
            statusLabel.Text = "📡 Connecting... downloading central directory...";

            List<ZipEntry> entries;
            try
            {
                entries = await RemoteZipReader.ReadAsync(urlTextBox.Text);
                entries = entries.OrderByDescending(ze => ze.FileName, StringComparer.OrdinalIgnoreCase).ToList();

                if (entries.Count == 0)
                {
                    statusLabel.Text = "ℹ️ ZIP archive is empty.";
                    return;
                }

                statusLabel.Text = "🌲 Building file tree...";
                Application.DoEvents();

                treeZip.BeginUpdate();
                foreach (var entry in entries)
                    TreeViewHelper.AddToTree(entry);
                treeZip.EndUpdate();

                statusLabel.Text = $"✅ Loaded {entries.Count:N0} file{(entries.Count == 1 ? "" : "s")}.";
            }
            catch (Exception ex)
            {
                statusLabel.Text = "❌ Failed to load ZIP file.";
                MessageBox.Show($"Error reading ZIP file:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUiState(true);
            }
        }

        private async void DownloadBtn_Click(object sender, EventArgs e)
        {
            var node = treeZip.SelectedNode;
            if (node == null || !(node.Tag is ZipEntry))
            {
                statusLabel.Text = "⚠️ Please select a file to download.";
                return;
            }

            ZipEntry entry = (ZipEntry)node.Tag;
            string shortName = entry.FileName.Split('/').Last();

            SetUiState(false);

            try
            {
                if (entry.IsEncrypted && string.IsNullOrWhiteSpace(passwordTextBox.Text))
                {
                    statusLabel.Text = "🔒 Password required to extract encrypted file.";
                    MessageBox.Show("This file is encrypted. Please enter the password.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    statusLabel.Text = $"⬇️ Downloading: {shortName} ...";
                    await RemoteZipExtractor.ExtractRemoteEntryAsync( urlTextBox.Text, entry, "downloadFolder", entry.IsEncrypted ? passwordTextBox.Text : null );
                    statusLabel.Text = $"✅ Downloaded: {shortName}";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"❌ Failed to extract {shortName}";
                MessageBox.Show($"Error extracting entry:\n{ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUiState(true);
            }
        }

        private void SetUiState(bool enabled)
        {
            onlineLoadBtn.Enabled = enabled;
            downloadBtn.Enabled = enabled;
        }

        private void TreeZip_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {

        }
    }
}

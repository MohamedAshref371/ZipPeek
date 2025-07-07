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

            TreeViewHelper.Reset();
            sortingFeatureEnabled = false;
            sortList.SelectedIndex = -1;
            SetUiState(false);
            statusLabel.Text = "📡 Connecting... downloading central directory...";

            List<ZipEntry> entries;
            try
            {
                entries = await RemoteZipReader.ReadAsync(urlTextBox.Text);

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
                TreeViewHelper.MarkEmptyFolders(treeZip.Nodes);
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

        private void StatusLabel_DoubleClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/MohamedAshref371/ZipPeek/releases/latest");
        }

        string currentKeyword = "";
        private readonly List<TreeNode> treeNodeList = new List<TreeNode>();
        int index;
        private void Search(bool up)
        {
            string keyword = searchTextBox.Text;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                statusLabel.Text = "🔍 Please enter a search term.";
                return;
            }

            if (currentKeyword != keyword)
            {
                currentKeyword = keyword;
                TreeViewHelper.SearchByName(treeNodeList, keyword);
                index = up ? 1 : -1;

                if (treeNodeList.Count == 0)
                {
                    statusLabel.Text = "🔍 No matches found.";
                    return;
                }

                statusLabel.Text = $"🔍 Found {treeNodeList.Count} match{(treeNodeList.Count == 1 ? "" : "es")}.";
            }

            if (treeNodeList.Count == 0) return;

            // تحريك الفهرس
            index += up ? -1 : 1;

            // تدوير الفهرس
            if (index < 0) index = treeNodeList.Count - 1;
            if (index >= treeNodeList.Count) index = 0;

            // تحديد النود
            var selectedNode = treeNodeList[index];
            treeZip.SelectedNode = selectedNode;
            selectedNode.EnsureVisible();

            statusLabel.Text = $"🔍 Match {index + 1} of {treeNodeList.Count}";
            treeZip.Focus();
        }

        private void UpBtn_Click(object sender, EventArgs e)
        {
            Search(true);
        }

        private void DownBtn_Click(object sender, EventArgs e)
        {
            Search(false);
        }

        bool sortingFeatureEnabled = false;
        private void SortList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = sortList.SelectedIndex;
            if (idx < 0) return;

            if (!sortingFeatureEnabled)
            {
                foreach (TreeNode root in treeZip.Nodes)
                    TreeViewHelper.ComputeMetadata(root);
                sortingFeatureEnabled = true;
            }

            // نحدد معيار الفرز حسب العنصر المحدد
            TreeViewHelper.SortCriteria criteria;
            
            switch (idx)
            {
                case 2:
                case 3:
                    criteria = TreeViewHelper.SortCriteria.CompressedSize;
                    break;
                case 4:
                case 5:
                    criteria = TreeViewHelper.SortCriteria.UncompressedSize;
                    break;
                case 6:
                case 7:
                    criteria = TreeViewHelper.SortCriteria.ModifiedTime;
                    break;
                default:
                    criteria = TreeViewHelper.SortCriteria.Name;
                    break;
            }

            // تنفيذ الفرز
            treeZip.BeginUpdate();
            TreeViewHelper.SortNodes(treeZip.Nodes, criteria, ascending: idx % 2 == 0);
            treeZip.EndUpdate();

            statusLabel.Text = $"🔃 Sorted by {sortList.SelectedItem}";
        }
    }
}

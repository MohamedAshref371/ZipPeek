using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPeek
{
    public partial class Form1 : Form
    {
        private readonly FolderSetting folderSetting = new FolderSetting();

        public Form1()
        {
            InitializeComponent();
            TreeViewHelper.TreeView = treeZip;

            string[] versionParts = Application.ProductVersion.Split('.');
            Array.Resize(ref versionParts, 2);
            Text = $"{Application.ProductName} v{string.Join(".", versionParts)} - View ZIP Files Online";

            FolderSetting.FailedSkip = Properties.Settings.Default.FailedSkip;
            FolderSetting.ExistsFileOption = Properties.Settings.Default.ExistsFileOption;
            FolderSetting.SubfolderOption = Properties.Settings.Default.SubfolderOption;
            folderSetting.SetValues();

            Controls.Add(folderSetting);
            folderSetting.BringToFront();
            folderSetting.Location = new System.Drawing.Point(treeZip.Location.X + (treeZip.Size.Width - folderSetting.Size.Width) / 2, treeZip.Location.Y + 4);
            folderSetting.Visible = false;
            folderBtn.Click += (s, e) =>
            {
                if (folderSetting.Visible)
                {
                    folderSetting.Visible = false;
                    folderBtn.BackgroundImage = Properties.Resources.settingsIcon;
                }
                else
                {
                    folderBtn.BackgroundImage = Properties.Resources.xIcon;
                    folderSetting.Visible = true;
                }
            };

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
            sortList.SelectedIndex = -1;
            sortingFeatureEnabled = false;
            currentKeyword = "";
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
            if (node == null)
            {
                statusLabel.Text = "⚠️ Please select a file to download.";
                return;
            }

            SetUiState(false);
            if (node.Tag is ZipEntry entry)
                await DownloadZipEntry(entry, entry.FileName.Split('/').Last());
            else if (node.Nodes.Count == 0)
                statusLabel.Text = "⚠️ Selected folder is empty.";
            else if (MessageBox.Show("Do you want to download the folder according to the current settings?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                statusLabel.Text = "📂 Downloading selected folder...";
                cancelAll = false;
                cancelBtn.Visible = true;
                await Download(node);
                cancelBtn.Visible = false;
            }
            SetUiState(true);
        }

        bool cancelAll = false;
        private void CancelBtn_Click(object sender, EventArgs e)
        {
            cancelAll = true;
            cancelBtn.Visible = false;
        }

        private async Task Download(TreeNode node)
        {
            string shortName = "";
            for (int i = 0; i < node.Nodes.Count; i++)
            {
                if (cancelAll)
                {
                    statusLabel.Text = "🛑 Download cancelled by user.";
                    return;
                }

                if (node.Nodes[i].Tag is ZipEntry entry)
                {
                    shortName = entry.FileName.Split('/').Last();
                    #region File Exists
                    if (FolderSetting.ExistsFileOption != 1 && File.Exists(Path.Combine("Download", entry.FileName.Replace('/', '\\'))))
                    {
                        if (FolderSetting.ExistsFileOption == 0)
                        {
                            statusLabel.Text = $"⚠️ Skipped existing file: {shortName}";
                            continue;
                        }
                        else if (FolderSetting.ExistsFileOption == 2)
                        {
                            DialogResult res = MessageBox.Show($"File '{shortName}' already exists.\nDo you want to overwrite it?", "File Exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                            if (res == DialogResult.No)
                            {
                                statusLabel.Text = $"⚠️ Skipped existing file: {shortName}";
                                continue;
                            }
                            else if (res == DialogResult.Cancel)
                            {
                                CancelBtn_Click(null, null);
                                continue;
                            }
                        }
                    }
                    #endregion

                    await DownloadZipEntry(entry, shortName, false);

                    #region Download Failed
                    if (!FolderSetting.FailedSkip && statusLabel.Text.StartsWith("❌ Failed"))
                    {
                        DialogResult res = MessageBox.Show($"The '{shortName}' file download failed. Do you want to try again?", "Download Failed", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                        if (res == DialogResult.Yes)
                        {
                            await DownloadZipEntry(entry, shortName, false);
                            if (statusLabel.Text.StartsWith("❌ Failed") && MessageBox.Show($"The file download failed for the second time.\nSkipping file: {shortName}", "Skipping", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1) == DialogResult.Cancel)
                                CancelBtn_Click(null, null);
                        }
                        else if (res == DialogResult.Cancel)
                            CancelBtn_Click(null, null);
                    }
                    #endregion
                }
                #region Subfolder Handling
                else if (FolderSetting.SubfolderOption == 1)
                    await Download(node.Nodes[i]);
                else if (FolderSetting.SubfolderOption == 2 && node.Nodes[i].Nodes.Count > 0)
                {
                    DialogResult res = MessageBox.Show($"Do you want to download the folder: '{node.Nodes[i].Text}' ?", "Question", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                    if (res == DialogResult.Yes) {
                        await Download(node.Nodes[i]);
                    }
                    else if (res == DialogResult.Cancel)
                        CancelBtn_Click(null, null);
                }
                #endregion
            }
        }

        private async Task DownloadZipEntry(ZipEntry entry, string shortName, bool showMessages = true)
        {
            try
            {
                if (entry.IsEncrypted && string.IsNullOrWhiteSpace(passwordTextBox.Text))
                {
                    statusLabel.Text = "🔒 Password required to extract encrypted file.";
                    if (showMessages) MessageBox.Show("This file is encrypted. Please enter the password.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    statusLabel.Text = $"⬇️ Downloading: {shortName} ...";
                    await RemoteZipExtractor.ExtractRemoteEntryAsync(urlTextBox.Text, entry, "Download", entry.IsEncrypted ? passwordTextBox.Text : null);
                    statusLabel.Text = $"✅ Downloaded: {shortName}";
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"❌ Failed to extract {shortName}";
                if (showMessages) MessageBox.Show($"Error extracting entry:\n{ex.Message}", "Extraction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetUiState(bool enabled)
        {
            onlineLoadBtn.Enabled = enabled;
            downloadBtn.Enabled = enabled;
            sortList.Enabled = enabled;
            upBtn.Enabled = enabled;
            downBtn.Enabled = enabled;
            folderBtn.Enabled = enabled;
            if (!enabled && folderSetting.Visible)
            {
                folderSetting.Visible = false;
                folderBtn.BackgroundImage = Properties.Resources.settingsIcon;
            }
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.FailedSkip = FolderSetting.FailedSkip;
            Properties.Settings.Default.ExistsFileOption = FolderSetting.ExistsFileOption;
            Properties.Settings.Default.SubfolderOption = FolderSetting.SubfolderOption;
            Properties.Settings.Default.Save();
        }

        private void TreeZip_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode < Keys.F1 || e.KeyCode > Keys.F4)
                return;

            var node = treeZip.SelectedNode;
            if (!downBtn.Enabled || node == null || node.Tag is ZipEntry)
                return;

            bool compressed = e.KeyCode == Keys.F1 || e.KeyCode == Keys.F2;
            long totalSize = GetSize(node, compressed, e.KeyCode == Keys.F1 || e.KeyCode == Keys.F3);
            string txt = $"{(compressed ? "C" : "Unc")}ompressed Size";
            MessageBox.Show($"Total {txt}: {TreeViewHelper.FormatSize(totalSize)}", txt, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private long GetSize(TreeNode node, bool compressed, bool withSubfolders)
        {
            long totalSize = 0;
            for (int i = 0; i < node.Nodes.Count; i++)
            {
                if (node.Nodes[i].Tag is ZipEntry entry)
                    totalSize += compressed ? entry.CompressedSize : entry.UncompressedSize;
                else if (withSubfolders)
                    totalSize += GetSize(node.Nodes[i], compressed, true);
            }
            return totalSize;
        }
    }
}

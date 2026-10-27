using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZipPeek.SevenZip;

namespace ZipPeek
{
    public partial class Form1
    {
        private async Task ReadSevenZipAsync()
        {
            sevenZipArchive?.Dispose();
            sevenZipArchive = null;
            TreeViewHelper.Reset();
            zipEntryList.Clear();
            currentKeyword = "";
            sortList.SelectedIndex = -1;
            SetUiState(false);
            isDownload = true;
            cancelBtn.Visible = true;
            statusLabel.Text = "Reading 7z archive metadata...";
            using (var cancellation = new CancellationTokenSource())
            {
                sevenZipCancellation = cancellation;
                try
                {
                    var progress = new Progress<long>(bytes => statusLabel.Text = $"Reading 7z metadata... {FormatSize(bytes)} downloaded");
                    string password = passwordTextBox.Text.Length == 0 ? null : passwordTextBox.Text;
                    sevenZipArchive = await RemoteSevenZipArchive.OpenAsync(urlTextBox.Text.Trim(), password, cancellation.Token, progress);
                    var entries = sevenZipArchive.Entries.Cast<ZipEntry>().ToList();
                    RemoveNonEmptyFolders(entries);
                    TreeViewHelper.AddToTree(entries);
                    int files = sevenZipArchive.Entries.Count(x => !x.IsDirectory);
                    statusLabel.Text = $"Loaded {files:N0} files from 7z. Metadata: {FormatSize(sevenZipArchive.DownloadedBytes)}.";
                }
                catch (OperationCanceledException)
                {
                    statusLabel.Text = "7z metadata download canceled.";
                }
                catch (Exception error)
                {
                    sevenZipArchive?.Dispose();
                    sevenZipArchive = null;
                    statusLabel.Text = "Failed to read 7z archive.";
                    MessageBox.Show(this, error.Message, "7z archive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    sevenZipCancellation = null;
                    SetUiState(true);
                }
            }
        }

        private bool CollectSevenZipSelection(TreeNode node, List<SevenZipArchiveEntry> selected)
        {
            if (node.Tag is SevenZipArchiveEntry entry)
            {
                selected.Add(entry);
                return true;
            }
            IEnumerable<TreeNode> children = node.Tag is List<TreeNode> list ? list : node.Nodes.Cast<TreeNode>();
            foreach (var child in children)
            {
                if (child.Tag is SevenZipArchiveEntry file)
                {
                    selected.Add(file);
                    continue;
                }
                if (FolderSetting.SubfolderOption == 0) continue;
                if (FolderSetting.SubfolderOption == 2)
                {
                    var answer = MessageBox.Show(this, $"Include subfolder '{child.Name}'?", "7z folder download",
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                    if (answer == DialogResult.Cancel) return false;
                    if (answer == DialogResult.No) continue;
                }
                if (!CollectSevenZipSelection(child, selected)) return false;
            }
            return true;
        }

        private async Task DownloadSevenZipSelectionAsync(TreeNode node)
        {
            if (sevenZipArchive == null || sevenZipArchive.Url != urlTextBox.Text.Trim())
            {
                statusLabel.Text = "Read this 7z URL before downloading files.";
                return;
            }
            SetUiState(false);
            try
            {
                var selected = new List<SevenZipArchiveEntry>();
                if (!CollectSevenZipSelection(node, selected)) { statusLabel.Text = "7z download canceled."; return; }
                if (selected.Count == 0) { statusLabel.Text = "No files selected with the current folder settings."; return; }
                var plan = sevenZipArchive.Plan(selected);
                string confirmation = null;
                if (plan.DownloadsWholeArchive)
                    confirmation = $"WARNING: This selection requires downloading the ENTIRE 7z archive ({FormatSize(sevenZipArchive.Length)}).\n\n" +
                        $"All {plan.Entries.Length:N0} files will be included for extraction.\n" +
                        "Previously cached bytes will be reused. Existing-file settings still apply.\n\nContinue?";
                else if (plan.Entries.Length > plan.RequestedFileCount)
                    confirmation = $"The selected files share solid compression blocks with other files.\n\n" +
                        $"ZipPeek will download the required blocks ({FormatSize(plan.CompressedBytes)}) and include all {plan.Entries.Length:N0} files in them for extraction.\n" +
                        "This can include files outside the selected folder. Existing-file settings still apply.\n\nContinue?";
                else if (!(node.Tag is SevenZipArchiveEntry))
                    confirmation = $"Download {plan.Entries.Length:N0} files using the current folder settings?";
                if (confirmation != null && MessageBox.Show(this, confirmation, plan.DownloadsWholeArchive ? "Entire archive download" : "7z download",
                    MessageBoxButtons.YesNo, plan.DownloadsWholeArchive ? MessageBoxIcon.Warning : MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    statusLabel.Text = "7z download canceled.";
                    return;
                }

                var filesToWrite = new HashSet<uint>();
                foreach (var file in plan.Entries)
                {
                    string target = ArchiveOutputPath.Resolve("Download", file.FileName);
                    if (File.Exists(target) && FolderSetting.ExistsFileOption != 1)
                    {
                        if (FolderSetting.ExistsFileOption == 0) continue;
                        var answer = MessageBox.Show(this, $"Overwrite existing file '{file.FileName}'?", "File exists",
                            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                        if (answer == DialogResult.Cancel) { statusLabel.Text = "7z download canceled."; return; }
                        if (answer == DialogResult.No) continue;
                    }
                    filesToWrite.Add(file.Index);
                }
                if (filesToWrite.Count == 0) { statusLabel.Text = "Skipped all existing files."; return; }
                if (plan.Entries.Any(x => x.IsEncrypted) && passwordTextBox.Text.Length == 0)
                {
                    statusLabel.Text = "Enter the password before extracting encrypted 7z files.";
                    return;
                }

                using (var cancellation = new CancellationTokenSource())
                {
                    sevenZipCancellation = cancellation;
                    isDownload = true;
                    cancelBtn.Visible = true;
                    long before = sevenZipArchive.DownloadedBytes;
                    long outputSize = plan.Entries.Where(x => filesToWrite.Contains(x.Index)).Sum(x => x.UncompressedSize);
                    var downloaded = new Progress<long>(bytes => statusLabel.Text = $"Downloading 7z data... {FormatSize(bytes - before)} transferred");
                    var extracted = new Progress<long>(bytes => statusLabel.Text = $"Extracting 7z... {FormatSize(bytes)} processed");
                    string password = passwordTextBox.Text.Length == 0 ? null : passwordTextBox.Text;
                    statusLabel.Text = "Preparing 7z download...";
                    while (true)
                    {
                        try
                        {
                            await sevenZipArchive.ExtractAsync(plan, filesToWrite, "Download", password, cancellation.Token, downloaded, extracted, FolderSetting.FailedSkip);
                            break;
                        }
                        catch (Exception error) when (!FolderSetting.FailedSkip && !(error is OperationCanceledException))
                        {
                            if (MessageBox.Show(this, error.Message + "\n\nRetry this 7z selection? Cached data will be reused.", "7z extraction failed",
                                MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Retry)
                            {
                                statusLabel.Text = "7z extraction stopped. Completed files have been kept.";
                                return;
                            }
                        }
                    }
                    statusLabel.Text = $"Extracted {filesToWrite.Count:N0} files ({FormatSize(outputSize)}). Downloaded {FormatSize(sevenZipArchive.DownloadedBytes - before)}.";
                }
            }
            catch (OperationCanceledException) { statusLabel.Text = "7z download canceled. Cached data can be reused."; }
            catch (Exception error)
            {
                statusLabel.Text = "Failed to extract 7z files.";
                MessageBox.Show(this, error.Message, "7z extraction", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                sevenZipCancellation = null;
                SetUiState(true);
            }
        }
    }
}

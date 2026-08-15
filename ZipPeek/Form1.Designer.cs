namespace ZipPeek
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.treeZip = new System.Windows.Forms.TreeView();
            this.urlTextBox = new System.Windows.Forms.TextBox();
            this.ofd = new System.Windows.Forms.OpenFileDialog();
            this.onlineLoadBtn = new System.Windows.Forms.Button();
            this.downloadBtn = new System.Windows.Forms.Button();
            this.passLabel = new System.Windows.Forms.Label();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.urlLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.searchLabel = new System.Windows.Forms.Label();
            this.sortList = new System.Windows.Forms.ComboBox();
            this.sortLabel = new System.Windows.Forms.Label();
            this.folderBtn = new System.Windows.Forms.Button();
            this.downBtn = new System.Windows.Forms.Button();
            this.upBtn = new System.Windows.Forms.Button();
            this.cancelBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // treeZip
            // 
            this.treeZip.Font = new System.Drawing.Font("Tahoma", 10F);
            this.treeZip.Location = new System.Drawing.Point(12, 40);
            this.treeZip.Name = "treeZip";
            this.treeZip.PathSeparator = "/";
            this.treeZip.Size = new System.Drawing.Size(776, 455);
            this.treeZip.TabIndex = 0;
            this.treeZip.KeyUp += new System.Windows.Forms.KeyEventHandler(this.TreeZip_KeyUp);
            // 
            // urlTextBox
            // 
            this.urlTextBox.Location = new System.Drawing.Point(98, 507);
            this.urlTextBox.Name = "urlTextBox";
            this.urlTextBox.Size = new System.Drawing.Size(520, 20);
            this.urlTextBox.TabIndex = 2;
            this.urlTextBox.Text = "https://github.com/MohamedAshref371/ZipPeek/releases/latest/download/ZipPeek.zip";
            // 
            // ofd
            // 
            this.ofd.FileName = "cookies.txt";
            this.ofd.Filter = "Text Files|*.txt";
            // 
            // onlineLoadBtn
            // 
            this.onlineLoadBtn.Font = new System.Drawing.Font("Tahoma", 10F);
            this.onlineLoadBtn.Location = new System.Drawing.Point(624, 501);
            this.onlineLoadBtn.Name = "onlineLoadBtn";
            this.onlineLoadBtn.Size = new System.Drawing.Size(75, 32);
            this.onlineLoadBtn.TabIndex = 3;
            this.onlineLoadBtn.Text = "Read";
            this.onlineLoadBtn.UseVisualStyleBackColor = true;
            this.onlineLoadBtn.Click += new System.EventHandler(this.OnlineLoadBtn_Click);
            // 
            // downloadBtn
            // 
            this.downloadBtn.Font = new System.Drawing.Font("Tahoma", 10F);
            this.downloadBtn.Location = new System.Drawing.Point(705, 501);
            this.downloadBtn.Name = "downloadBtn";
            this.downloadBtn.Size = new System.Drawing.Size(83, 32);
            this.downloadBtn.TabIndex = 4;
            this.downloadBtn.Text = "Download";
            this.downloadBtn.UseVisualStyleBackColor = true;
            this.downloadBtn.Click += new System.EventHandler(this.DownloadBtn_Click);
            // 
            // passLabel
            // 
            this.passLabel.AutoSize = true;
            this.passLabel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.passLabel.Location = new System.Drawing.Point(12, 538);
            this.passLabel.Name = "passLabel";
            this.passLabel.Size = new System.Drawing.Size(83, 17);
            this.passLabel.TabIndex = 5;
            this.passLabel.Text = "PassWord : ";
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(98, 537);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(144, 20);
            this.passwordTextBox.TabIndex = 6;
            // 
            // urlLabel
            // 
            this.urlLabel.AutoSize = true;
            this.urlLabel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.urlLabel.Location = new System.Drawing.Point(12, 510);
            this.urlLabel.Name = "urlLabel";
            this.urlLabel.Size = new System.Drawing.Size(80, 17);
            this.urlLabel.TabIndex = 7;
            this.urlLabel.Text = "Direct link : ";
            // 
            // statusLabel
            // 
            this.statusLabel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.statusLabel.Location = new System.Drawing.Point(243, 537);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(556, 23);
            this.statusLabel.TabIndex = 8;
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.statusLabel.DoubleClick += new System.EventHandler(this.StatusLabel_DoubleClick);
            // 
            // searchTextBox
            // 
            this.searchTextBox.Font = new System.Drawing.Font("Tahoma", 10F);
            this.searchTextBox.Location = new System.Drawing.Point(81, 8);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(266, 24);
            this.searchTextBox.TabIndex = 10;
            // 
            // searchLabel
            // 
            this.searchLabel.AutoSize = true;
            this.searchLabel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.searchLabel.Location = new System.Drawing.Point(12, 13);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(63, 17);
            this.searchLabel.TabIndex = 13;
            this.searchLabel.Text = "Search : ";
            // 
            // sortList
            // 
            this.sortList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sortList.Font = new System.Drawing.Font("Tahoma", 10F);
            this.sortList.FormattingEnabled = true;
            this.sortList.Items.AddRange(new object[] {
            "Name (asc)",
            "Name (desc)",
            "Compressed Size (asc)",
            "Compressed Size (desc)",
            "Uncompressed Size (asc)",
            "Uncompressed Size (desc)",
            "Last Modified (asc)",
            "Last Modified (desc)"});
            this.sortList.Location = new System.Drawing.Point(550, 8);
            this.sortList.Name = "sortList";
            this.sortList.Size = new System.Drawing.Size(238, 24);
            this.sortList.TabIndex = 14;
            this.sortList.SelectedIndexChanged += new System.EventHandler(this.SortList_SelectedIndexChanged);
            // 
            // sortLabel
            // 
            this.sortLabel.AutoSize = true;
            this.sortLabel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.sortLabel.Location = new System.Drawing.Point(497, 13);
            this.sortLabel.Name = "sortLabel";
            this.sortLabel.Size = new System.Drawing.Size(47, 17);
            this.sortLabel.TabIndex = 15;
            this.sortLabel.Text = "Sort : ";
            // 
            // folderBtn
            // 
            this.folderBtn.BackgroundImage = global::ZipPeek.Properties.Resources.settingsIcon;
            this.folderBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.folderBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.folderBtn.Font = new System.Drawing.Font("Tahoma", 10F);
            this.folderBtn.ForeColor = System.Drawing.SystemColors.Control;
            this.folderBtn.Location = new System.Drawing.Point(438, 6);
            this.folderBtn.Name = "folderBtn";
            this.folderBtn.Size = new System.Drawing.Size(32, 28);
            this.folderBtn.TabIndex = 16;
            this.folderBtn.UseVisualStyleBackColor = false;
            this.folderBtn.Click += new System.EventHandler(this.FolderBtn_Click);
            // 
            // downBtn
            // 
            this.downBtn.BackgroundImage = global::ZipPeek.Properties.Resources.down_arrow;
            this.downBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.downBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.downBtn.Font = new System.Drawing.Font("Tahoma", 10F);
            this.downBtn.ForeColor = System.Drawing.SystemColors.Control;
            this.downBtn.Location = new System.Drawing.Point(385, 6);
            this.downBtn.Name = "downBtn";
            this.downBtn.Size = new System.Drawing.Size(32, 28);
            this.downBtn.TabIndex = 12;
            this.downBtn.UseVisualStyleBackColor = false;
            this.downBtn.Click += new System.EventHandler(this.DownBtn_Click);
            // 
            // upBtn
            // 
            this.upBtn.BackgroundImage = global::ZipPeek.Properties.Resources.up_arrow;
            this.upBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.upBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.upBtn.Font = new System.Drawing.Font("Tahoma", 10F);
            this.upBtn.ForeColor = System.Drawing.SystemColors.Control;
            this.upBtn.Location = new System.Drawing.Point(350, 6);
            this.upBtn.Name = "upBtn";
            this.upBtn.Size = new System.Drawing.Size(32, 28);
            this.upBtn.TabIndex = 11;
            this.upBtn.UseVisualStyleBackColor = false;
            this.upBtn.Click += new System.EventHandler(this.UpBtn_Click);
            // 
            // cancelBtn
            // 
            this.cancelBtn.Font = new System.Drawing.Font("Tahoma", 10F);
            this.cancelBtn.Location = new System.Drawing.Point(705, 501);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size(83, 32);
            this.cancelBtn.TabIndex = 17;
            this.cancelBtn.Text = "Cancel";
            this.cancelBtn.UseVisualStyleBackColor = true;
            this.cancelBtn.Visible = false;
            this.cancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 569);
            this.Controls.Add(this.cancelBtn);
            this.Controls.Add(this.folderBtn);
            this.Controls.Add(this.sortLabel);
            this.Controls.Add(this.sortList);
            this.Controls.Add(this.searchLabel);
            this.Controls.Add(this.downBtn);
            this.Controls.Add(this.upBtn);
            this.Controls.Add(this.searchTextBox);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.urlLabel);
            this.Controls.Add(this.passwordTextBox);
            this.Controls.Add(this.passLabel);
            this.Controls.Add(this.downloadBtn);
            this.Controls.Add(this.onlineLoadBtn);
            this.Controls.Add(this.urlTextBox);
            this.Controls.Add(this.treeZip);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "ZipPeek";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView treeZip;
        private System.Windows.Forms.TextBox urlTextBox;
        private System.Windows.Forms.OpenFileDialog ofd;
        private System.Windows.Forms.Button onlineLoadBtn;
        private System.Windows.Forms.Button downloadBtn;
        private System.Windows.Forms.Label passLabel;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.Label urlLabel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.Button upBtn;
        private System.Windows.Forms.Button downBtn;
        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.ComboBox sortList;
        private System.Windows.Forms.Label sortLabel;
        private System.Windows.Forms.Button folderBtn;
        private System.Windows.Forms.Button cancelBtn;
    }
}


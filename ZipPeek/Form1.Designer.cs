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
            this.label1 = new System.Windows.Forms.Label();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.upBtn = new System.Windows.Forms.Button();
            this.downBtn = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.sortList = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
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
            // 
            // urlTextBox
            // 
            this.urlTextBox.Location = new System.Drawing.Point(98, 507);
            this.urlTextBox.Name = "urlTextBox";
            this.urlTextBox.Size = new System.Drawing.Size(520, 20);
            this.urlTextBox.TabIndex = 2;
            // 
            // ofd
            // 
            this.ofd.Filter = "ZIP Files|*.zip";
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
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 10F);
            this.label1.Location = new System.Drawing.Point(9, 538);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 17);
            this.label1.TabIndex = 5;
            this.label1.Text = "PassWord : ";
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(98, 537);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(189, 20);
            this.passwordTextBox.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Tahoma", 10F);
            this.label2.Location = new System.Drawing.Point(12, 510);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 17);
            this.label2.TabIndex = 7;
            this.label2.Text = "Link : ";
            // 
            // statusLabel
            // 
            this.statusLabel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.statusLabel.Location = new System.Drawing.Point(293, 537);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(495, 23);
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
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Tahoma", 10F);
            this.label3.Location = new System.Drawing.Point(12, 13);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 17);
            this.label3.TabIndex = 13;
            this.label3.Text = "Search : ";
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
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Tahoma", 10F);
            this.label4.Location = new System.Drawing.Point(497, 13);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 17);
            this.label4.TabIndex = 15;
            this.label4.Text = "Sort : ";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 569);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.sortList);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.downBtn);
            this.Controls.Add(this.upBtn);
            this.Controls.Add(this.searchTextBox);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.passwordTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.downloadBtn);
            this.Controls.Add(this.onlineLoadBtn);
            this.Controls.Add(this.urlTextBox);
            this.Controls.Add(this.treeZip);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "ZipPeek";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView treeZip;
        private System.Windows.Forms.TextBox urlTextBox;
        private System.Windows.Forms.OpenFileDialog ofd;
        private System.Windows.Forms.Button onlineLoadBtn;
        private System.Windows.Forms.Button downloadBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.Button upBtn;
        private System.Windows.Forms.Button downBtn;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox sortList;
        private System.Windows.Forms.Label label4;
    }
}


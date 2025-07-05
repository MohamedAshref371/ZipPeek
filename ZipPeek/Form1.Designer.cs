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
            this.SuspendLayout();
            // 
            // treeZip
            // 
            this.treeZip.Location = new System.Drawing.Point(12, 12);
            this.treeZip.Name = "treeZip";
            this.treeZip.Size = new System.Drawing.Size(776, 400);
            this.treeZip.TabIndex = 0;
            this.treeZip.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.TreeZip_NodeMouseClick);
            // 
            // urlTextBox
            // 
            this.urlTextBox.Location = new System.Drawing.Point(83, 421);
            this.urlTextBox.Name = "urlTextBox";
            this.urlTextBox.Size = new System.Drawing.Size(705, 20);
            this.urlTextBox.TabIndex = 2;
            // 
            // ofd
            // 
            this.ofd.Filter = "ZIP Files|*.zip";
            // 
            // onlineLoadBtn
            // 
            this.onlineLoadBtn.Location = new System.Drawing.Point(632, 447);
            this.onlineLoadBtn.Name = "onlineLoadBtn";
            this.onlineLoadBtn.Size = new System.Drawing.Size(75, 23);
            this.onlineLoadBtn.TabIndex = 3;
            this.onlineLoadBtn.Text = "read";
            this.onlineLoadBtn.UseVisualStyleBackColor = true;
            this.onlineLoadBtn.Click += new System.EventHandler(this.OnlineLoadBtn_Click);
            // 
            // downloadBtn
            // 
            this.downloadBtn.Location = new System.Drawing.Point(713, 447);
            this.downloadBtn.Name = "downloadBtn";
            this.downloadBtn.Size = new System.Drawing.Size(75, 23);
            this.downloadBtn.TabIndex = 4;
            this.downloadBtn.Text = "download";
            this.downloadBtn.UseVisualStyleBackColor = true;
            this.downloadBtn.Click += new System.EventHandler(this.DownloadBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 452);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "PassWord : ";
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.Location = new System.Drawing.Point(83, 449);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(269, 20);
            this.passwordTextBox.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 424);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 7;
            this.label2.Text = "Link : ";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 481);
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
            this.Text = "ZipPeek v1.0";
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
    }
}


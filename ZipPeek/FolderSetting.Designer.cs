namespace ZipPeek
{
    partial class FolderSetting
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.failedMessage = new System.Windows.Forms.RadioButton();
            this.failedSkip = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.existMessage = new System.Windows.Forms.RadioButton();
            this.existDownload = new System.Windows.Forms.RadioButton();
            this.existSkip = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.subfolderMessage = new System.Windows.Forms.RadioButton();
            this.subfolderYes = new System.Windows.Forms.RadioButton();
            this.subfolderNo = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.failedMessage);
            this.groupBox1.Controls.Add(this.failedSkip);
            this.groupBox1.Font = new System.Drawing.Font("Tahoma", 12F);
            this.groupBox1.Location = new System.Drawing.Point(19, 48);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(430, 91);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "2. The \'download failed\' event.";
            // 
            // failedMessage
            // 
            this.failedMessage.AutoSize = true;
            this.failedMessage.Location = new System.Drawing.Point(19, 55);
            this.failedMessage.Name = "failedMessage";
            this.failedMessage.Size = new System.Drawing.Size(290, 23);
            this.failedMessage.TabIndex = 3;
            this.failedMessage.Text = "Display the \'retry or ignore\' message.";
            this.failedMessage.UseVisualStyleBackColor = true;
            // 
            // failedSkip
            // 
            this.failedSkip.AutoSize = true;
            this.failedSkip.Checked = true;
            this.failedSkip.Location = new System.Drawing.Point(19, 26);
            this.failedSkip.Name = "failedSkip";
            this.failedSkip.Size = new System.Drawing.Size(88, 23);
            this.failedSkip.TabIndex = 2;
            this.failedSkip.TabStop = true;
            this.failedSkip.Text = "Skip file.";
            this.failedSkip.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 12F);
            this.label1.Location = new System.Drawing.Point(18, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(639, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "1. Tip: Sort the files by compressed size in ascending order before downloading a" +
    " folder.";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.existMessage);
            this.groupBox2.Controls.Add(this.existDownload);
            this.groupBox2.Controls.Add(this.existSkip);
            this.groupBox2.Font = new System.Drawing.Font("Tahoma", 12F);
            this.groupBox2.Location = new System.Drawing.Point(19, 153);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(430, 118);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "3. File already exists before download.";
            // 
            // existMessage
            // 
            this.existMessage.AutoSize = true;
            this.existMessage.Location = new System.Drawing.Point(19, 84);
            this.existMessage.Name = "existMessage";
            this.existMessage.Size = new System.Drawing.Size(326, 23);
            this.existMessage.TabIndex = 7;
            this.existMessage.Text = "Display the \'download or ignore\' message.";
            this.existMessage.UseVisualStyleBackColor = true;
            // 
            // existDownload
            // 
            this.existDownload.AutoSize = true;
            this.existDownload.Location = new System.Drawing.Point(19, 55);
            this.existDownload.Name = "existDownload";
            this.existDownload.Size = new System.Drawing.Size(146, 23);
            this.existDownload.TabIndex = 6;
            this.existDownload.Text = "Download again.";
            this.existDownload.UseVisualStyleBackColor = true;
            // 
            // existSkip
            // 
            this.existSkip.AutoSize = true;
            this.existSkip.Checked = true;
            this.existSkip.Location = new System.Drawing.Point(19, 26);
            this.existSkip.Name = "existSkip";
            this.existSkip.Size = new System.Drawing.Size(88, 23);
            this.existSkip.TabIndex = 5;
            this.existSkip.TabStop = true;
            this.existSkip.Text = "Skip file.";
            this.existSkip.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.subfolderMessage);
            this.groupBox3.Controls.Add(this.subfolderYes);
            this.groupBox3.Controls.Add(this.subfolderNo);
            this.groupBox3.Font = new System.Drawing.Font("Tahoma", 12F);
            this.groupBox3.Location = new System.Drawing.Point(19, 285);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(430, 118);
            this.groupBox3.TabIndex = 8;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "4. Download subfolders.";
            // 
            // subfolderMessage
            // 
            this.subfolderMessage.AutoSize = true;
            this.subfolderMessage.Location = new System.Drawing.Point(19, 84);
            this.subfolderMessage.Name = "subfolderMessage";
            this.subfolderMessage.Size = new System.Drawing.Size(211, 23);
            this.subfolderMessage.TabIndex = 11;
            this.subfolderMessage.Text = "Ask about each subfolder.";
            this.subfolderMessage.UseVisualStyleBackColor = true;
            // 
            // subfolderYes
            // 
            this.subfolderYes.AutoSize = true;
            this.subfolderYes.Checked = true;
            this.subfolderYes.Location = new System.Drawing.Point(19, 55);
            this.subfolderYes.Name = "subfolderYes";
            this.subfolderYes.Size = new System.Drawing.Size(57, 23);
            this.subfolderYes.TabIndex = 10;
            this.subfolderYes.TabStop = true;
            this.subfolderYes.Text = "Yes.";
            this.subfolderYes.UseVisualStyleBackColor = true;
            // 
            // subfolderNo
            // 
            this.subfolderNo.AutoSize = true;
            this.subfolderNo.Location = new System.Drawing.Point(19, 26);
            this.subfolderNo.Name = "subfolderNo";
            this.subfolderNo.Size = new System.Drawing.Size(52, 23);
            this.subfolderNo.TabIndex = 9;
            this.subfolderNo.Text = "No.";
            this.subfolderNo.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Tahoma", 12F);
            this.label2.Location = new System.Drawing.Point(18, 419);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(579, 19);
            this.label2.TabIndex = 12;
            this.label2.Text = "5. Warning: If you do not enter the password, all encrypted files will be ignored" +
    ".";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Tahoma", 20F);
            this.label3.ForeColor = System.Drawing.Color.Maroon;
            this.label3.Location = new System.Drawing.Point(471, 115);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(215, 66);
            this.label3.TabIndex = 13;
            this.label3.Text = "Folder Download\r\nSettings";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Tahoma", 14F);
            this.label4.ForeColor = System.Drawing.Color.Navy;
            this.label4.Location = new System.Drawing.Point(469, 239);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(223, 92);
            this.label4.TabIndex = 14;
            this.label4.Text = "F1 / F2 : Compressed  \r\nF3 / F4 : Uncompressed  \r\nSize of the folder  \r\nWith / wi" +
    "thout subfolders";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FolderSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Azure;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label4);
            this.Name = "FolderSetting";
            this.Size = new System.Drawing.Size(710, 447);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton failedMessage;
        private System.Windows.Forms.RadioButton failedSkip;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton existDownload;
        private System.Windows.Forms.RadioButton existSkip;
        private System.Windows.Forms.RadioButton existMessage;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton subfolderMessage;
        private System.Windows.Forms.RadioButton subfolderYes;
        private System.Windows.Forms.RadioButton subfolderNo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
    }
}

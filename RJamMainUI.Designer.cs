namespace RJam
{
    partial class RJamMainUI
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
            this.ButtonStartDoc = new System.Windows.Forms.Button();
            this.labelDocumentName = new System.Windows.Forms.Label();
            this.labelStatus = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.IPSegment4 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.IPSegment1 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.IPSegment2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.IPSegment3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.partnerIPTextbox = new System.Windows.Forms.MaskedTextBox();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonStartDoc
            // 
            this.ButtonStartDoc.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ButtonStartDoc.Location = new System.Drawing.Point(10, 94);
            this.ButtonStartDoc.Margin = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.ButtonStartDoc.Name = "ButtonStartDoc";
            this.ButtonStartDoc.Size = new System.Drawing.Size(230, 35);
            this.ButtonStartDoc.TabIndex = 0;
            this.ButtonStartDoc.Text = "Start Session";
            this.ButtonStartDoc.UseVisualStyleBackColor = true;
            this.ButtonStartDoc.Click += new System.EventHandler(this.ButtonStartDoc_Click);
            // 
            // labelDocumentName
            // 
            this.labelDocumentName.AutoSize = true;
            this.labelDocumentName.Location = new System.Drawing.Point(10, 12);
            this.labelDocumentName.Margin = new System.Windows.Forms.Padding(10, 0, 5, 10);
            this.labelDocumentName.Name = "labelDocumentName";
            this.labelDocumentName.Size = new System.Drawing.Size(105, 13);
            this.labelDocumentName.TabIndex = 1;
            this.labelDocumentName.Text = "Unnamed Document";
            // 
            // labelStatus
            // 
            this.labelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(181, 12);
            this.labelStatus.Margin = new System.Windows.Forms.Padding(10, 0, 5, 10);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(59, 13);
            this.labelStatus.TabIndex = 2;
            this.labelStatus.Text = "Not started";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.panel1);
            this.groupBox1.Location = new System.Drawing.Point(10, 35);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(230, 54);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Local Address";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.IPSegment4);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.IPSegment1);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.IPSegment2);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.IPSegment3);
            this.panel1.Location = new System.Drawing.Point(22, 19);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(186, 22);
            this.panel1.TabIndex = 7;
            // 
            // IPSegment4
            // 
            this.IPSegment4.AutoSize = true;
            this.IPSegment4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IPSegment4.Location = new System.Drawing.Point(150, 0);
            this.IPSegment4.Margin = new System.Windows.Forms.Padding(0);
            this.IPSegment4.Name = "IPSegment4";
            this.IPSegment4.Size = new System.Drawing.Size(36, 20);
            this.IPSegment4.TabIndex = 3;
            this.IPSegment4.Text = "999";
            this.IPSegment4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(136, 0);
            this.label7.Margin = new System.Windows.Forms.Padding(0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(14, 20);
            this.label7.TabIndex = 6;
            this.label7.Text = ".";
            // 
            // IPSegment1
            // 
            this.IPSegment1.AutoSize = true;
            this.IPSegment1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IPSegment1.Location = new System.Drawing.Point(0, 0);
            this.IPSegment1.Margin = new System.Windows.Forms.Padding(0);
            this.IPSegment1.Name = "IPSegment1";
            this.IPSegment1.Size = new System.Drawing.Size(36, 20);
            this.IPSegment1.TabIndex = 0;
            this.IPSegment1.Text = "999";
            this.IPSegment1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(86, 0);
            this.label6.Margin = new System.Windows.Forms.Padding(0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(14, 20);
            this.label6.TabIndex = 5;
            this.label6.Text = ".";
            // 
            // IPSegment2
            // 
            this.IPSegment2.AutoSize = true;
            this.IPSegment2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IPSegment2.Location = new System.Drawing.Point(50, 0);
            this.IPSegment2.Margin = new System.Windows.Forms.Padding(0);
            this.IPSegment2.Name = "IPSegment2";
            this.IPSegment2.Size = new System.Drawing.Size(36, 20);
            this.IPSegment2.TabIndex = 1;
            this.IPSegment2.Text = "999";
            this.IPSegment2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(36, 0);
            this.label5.Margin = new System.Windows.Forms.Padding(0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(14, 20);
            this.label5.TabIndex = 4;
            this.label5.Text = ".";
            // 
            // IPSegment3
            // 
            this.IPSegment3.AutoSize = true;
            this.IPSegment3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IPSegment3.Location = new System.Drawing.Point(100, 0);
            this.IPSegment3.Margin = new System.Windows.Forms.Padding(0);
            this.IPSegment3.Name = "IPSegment3";
            this.IPSegment3.Size = new System.Drawing.Size(36, 20);
            this.IPSegment3.TabIndex = 2;
            this.IPSegment3.Text = "999";
            this.IPSegment3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.partnerIPTextbox);
            this.groupBox2.Location = new System.Drawing.Point(10, 141);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(230, 45);
            this.groupBox2.TabIndex = 8;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Connect To Partner";
            // 
            // partnerIPTextbox
            // 
            this.partnerIPTextbox.Location = new System.Drawing.Point(6, 19);
            this.partnerIPTextbox.Mask = "099.099.099.099";
            this.partnerIPTextbox.Name = "partnerIPTextbox";
            this.partnerIPTextbox.Size = new System.Drawing.Size(218, 20);
            this.partnerIPTextbox.TabIndex = 0;
            this.partnerIPTextbox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // buttonConnect
            // 
            this.buttonConnect.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonConnect.Location = new System.Drawing.Point(10, 189);
            this.buttonConnect.Margin = new System.Windows.Forms.Padding(10, 0, 10, 0);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(230, 35);
            this.buttonConnect.TabIndex = 9;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // RJamMainUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonConnect);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.labelDocumentName);
            this.Controls.Add(this.ButtonStartDoc);
            this.Name = "RJamMainUI";
            this.Size = new System.Drawing.Size(250, 233);
            this.groupBox1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ButtonStartDoc;
        private System.Windows.Forms.Label labelDocumentName;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label IPSegment4;
        private System.Windows.Forms.Label IPSegment3;
        private System.Windows.Forms.Label IPSegment2;
        private System.Windows.Forms.Label IPSegment1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.MaskedTextBox partnerIPTextbox;
    }
}

namespace ZurfurGui.WinForms
{
    partial class FormZurfurGui
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            pictureMain = new PictureBox();
            timer1 = new System.Windows.Forms.Timer(components);
            labelInfo = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureMain).BeginInit();
            SuspendLayout();
            // 
            // pictureMain
            // 
            pictureMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureMain.Location = new System.Drawing.Point(12, 12);
            pictureMain.Name = "pictureMain";
            pictureMain.Size = new System.Drawing.Size(954, 601);
            pictureMain.TabIndex = 1;
            pictureMain.TabStop = false;
            // 
            // timer1
            // 
            timer1.Enabled = true;
            timer1.Interval = 15;
            timer1.Tick += timer1_Tick;
            // 
            // labelMag
            // 
            labelInfo.AutoSize = true;
            labelInfo.Font = new Font("Segoe UI", 16F);
            labelInfo.ImageAlign = ContentAlignment.MiddleRight;
            labelInfo.Location = new System.Drawing.Point(21, 22);
            labelInfo.Name = "labelMag";
            labelInfo.Size = new System.Drawing.Size(209, 30);
            labelInfo.TabIndex = 2;
            labelInfo.Text = "Magnification: 100%";
            labelInfo.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // FormZurfurGui
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(978, 625);
            Controls.Add(labelInfo);
            Controls.Add(pictureMain);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormZurfurGui";
            Text = "Zurfur Gui";
            ((System.ComponentModel.ISupportInitialize)pictureMain).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private PictureBox pictureMain;
        private System.Windows.Forms.Timer timer1;
        private Label labelInfo;
    }
}

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
            button1 = new Button();
            pictureMain = new PictureBox();
            timer1 = new System.Windows.Forms.Timer(components);
            labelMag = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureMain).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(12, 12);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(75, 23);
            button1.TabIndex = 0;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // pictureMain
            // 
            pictureMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureMain.Location = new System.Drawing.Point(12, 41);
            pictureMain.Name = "pictureMain";
            pictureMain.Size = new System.Drawing.Size(954, 572);
            pictureMain.TabIndex = 1;
            pictureMain.TabStop = false;
            pictureMain.Paint += pictureMain_Paint;
            // 
            // timer1
            // 
            timer1.Enabled = true;
            timer1.Interval = 15;
            timer1.Tick += timer1_Tick;
            // 
            // labelMag
            // 
            labelMag.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelMag.AutoSize = true;
            labelMag.ImageAlign = ContentAlignment.MiddleRight;
            labelMag.Location = new System.Drawing.Point(851, 16);
            labelMag.Name = "labelMag";
            labelMag.Size = new System.Drawing.Size(115, 15);
            labelMag.TabIndex = 2;
            labelMag.Text = "Magnification: 100%";
            // 
            // FormZurfurGui
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(978, 625);
            Controls.Add(labelMag);
            Controls.Add(pictureMain);
            Controls.Add(button1);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormZurfurGui";
            Text = "Zurfur Gui";
            Load += FormZurfurGui_Load;
            ((System.ComponentModel.ISupportInitialize)pictureMain).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private PictureBox pictureMain;
        private System.Windows.Forms.Timer timer1;
        private Label labelMag;
    }
}

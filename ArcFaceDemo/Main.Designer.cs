namespace FaceRecognization
{
    partial class Main
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
            this.label2 = new System.Windows.Forms.Label();
            this.TextBoxID = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ButtonRegister = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.VideoPlayer = new AForge.Controls.VideoSourcePlayer();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label2.Location = new System.Drawing.Point(60, 445);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(223, 14);
            this.label2.TabIndex = 11;
            this.label2.Text = "人脸注册：点击视频，输入ID，点击注册";
            // 
            // TextBoxID
            // 
            this.TextBoxID.Location = new System.Drawing.Point(44, 291);
            this.TextBoxID.Multiline = true;
            this.TextBoxID.Name = "TextBoxID";
            this.TextBoxID.Size = new System.Drawing.Size(290, 27);
            this.TextBoxID.TabIndex = 9;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 294);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(17, 12);
            this.label1.TabIndex = 8;
            this.label1.Text = "ID";
            // 
            // ButtonRegister
            // 
            this.ButtonRegister.Location = new System.Drawing.Point(14, 339);
            this.ButtonRegister.Name = "ButtonRegister";
            this.ButtonRegister.Size = new System.Drawing.Size(320, 23);
            this.ButtonRegister.TabIndex = 6;
            this.ButtonRegister.Text = "注册";
            this.ButtonRegister.UseVisualStyleBackColor = true;
            this.ButtonRegister.Click += new System.EventHandler(this.ButtonRegister_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(14, 20);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(320, 240);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 10;
            this.pictureBox1.TabStop = false;
            // 
            // VideoPlayer
            // 
            this.VideoPlayer.Location = new System.Drawing.Point(12, 12);
            this.VideoPlayer.Name = "VideoPlayer";
            this.VideoPlayer.Size = new System.Drawing.Size(640, 480);
            this.VideoPlayer.TabIndex = 7;
            this.VideoPlayer.Text = "videoSourcePlayer1";
            this.VideoPlayer.VideoSource = null;
            this.VideoPlayer.Click += new System.EventHandler(this.VideoPlayer_Click);
            this.VideoPlayer.Paint += new System.Windows.Forms.PaintEventHandler(this.VideoPlayer_Paint);
            this.VideoPlayer.MouseMove += new System.Windows.Forms.MouseEventHandler(this.VideoPlayer_MouseMove);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pictureBox1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.TextBoxID);
            this.groupBox1.Controls.Add(this.ButtonRegister);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(671, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(350, 480);
            this.groupBox1.TabIndex = 12;
            this.groupBox1.TabStop = false;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1046, 511);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.VideoPlayer);
            this.Name = "Main";
            this.Text = "ArcFace人脸识别+注册";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TextBoxID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button ButtonRegister;
        private System.Windows.Forms.PictureBox pictureBox1;
        private AForge.Controls.VideoSourcePlayer VideoPlayer;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}
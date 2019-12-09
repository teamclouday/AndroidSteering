namespace SteeringWheel
{
    partial class MonitorUI
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MonitorUI));
            this.BthStatusLabel = new System.Windows.Forms.Label();
            this.BthStatus = new System.Windows.Forms.Label();
            this.BthDetail = new System.Windows.Forms.TextBox();
            this.BthServiceControl = new System.Windows.Forms.Button();
            this.WheelPanel = new System.Windows.Forms.Panel();
            this.SteerLeftMinBar = new System.Windows.Forms.TrackBar();
            this.SteerLeftMinLabel = new System.Windows.Forms.Label();
            this.SteerLeftMaxBar = new System.Windows.Forms.TrackBar();
            this.SteerLeftMaxLabel = new System.Windows.Forms.Label();
            this.WheelControl = new System.Windows.Forms.Button();
            this.WheelTip = new System.Windows.Forms.ToolTip(this.components);
            this.SteerRightMaxLabel = new System.Windows.Forms.Label();
            this.SteerRightMinLabel = new System.Windows.Forms.Label();
            this.SteerRightMaxBar = new System.Windows.Forms.TrackBar();
            this.SteerRightMinBar = new System.Windows.Forms.TrackBar();
            this.ForwardMaxLabel = new System.Windows.Forms.Label();
            this.ForwardMinLabel = new System.Windows.Forms.Label();
            this.ForwardMaxBar = new System.Windows.Forms.TrackBar();
            this.ForwardMinBar = new System.Windows.Forms.TrackBar();
            this.BackwardMaxLabel = new System.Windows.Forms.Label();
            this.BackwardMinLabel = new System.Windows.Forms.Label();
            this.BackwardMaxBar = new System.Windows.Forms.TrackBar();
            this.BackwardMinBar = new System.Windows.Forms.TrackBar();
            this.AccInvertCheckBox = new System.Windows.Forms.CheckBox();
            this.WheelPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SteerLeftMinBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SteerLeftMaxBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SteerRightMaxBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SteerRightMinBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ForwardMaxBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ForwardMinBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.BackwardMaxBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.BackwardMinBar)).BeginInit();
            this.SuspendLayout();
            // 
            // BthStatusLabel
            // 
            this.BthStatusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BthStatusLabel.Location = new System.Drawing.Point(20, 20);
            this.BthStatusLabel.Name = "BthStatusLabel";
            this.BthStatusLabel.Size = new System.Drawing.Size(120, 20);
            this.BthStatusLabel.TabIndex = 0;
            this.BthStatusLabel.Text = "Bluetooth Status";
            // 
            // BthStatus
            // 
            this.BthStatus.BackColor = System.Drawing.Color.DarkGray;
            this.BthStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BthStatus.Location = new System.Drawing.Point(170, 20);
            this.BthStatus.Name = "BthStatus";
            this.BthStatus.Size = new System.Drawing.Size(80, 20);
            this.BthStatus.TabIndex = 2;
            this.BthStatus.Text = "STATUS";
            this.BthStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BthDetail
            // 
            this.BthDetail.BackColor = System.Drawing.Color.White;
            this.BthDetail.Cursor = System.Windows.Forms.Cursors.Default;
            this.BthDetail.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BthDetail.Location = new System.Drawing.Point(20, 50);
            this.BthDetail.Multiline = true;
            this.BthDetail.Name = "BthDetail";
            this.BthDetail.ReadOnly = true;
            this.BthDetail.Size = new System.Drawing.Size(240, 240);
            this.BthDetail.TabIndex = 3;
            this.BthDetail.TabStop = false;
            // 
            // BthServiceControl
            // 
            this.BthServiceControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BthServiceControl.Location = new System.Drawing.Point(65, 310);
            this.BthServiceControl.Name = "BthServiceControl";
            this.BthServiceControl.Size = new System.Drawing.Size(150, 30);
            this.BthServiceControl.TabIndex = 4;
            this.BthServiceControl.Text = "Pause Bluetooth";
            this.BthServiceControl.UseVisualStyleBackColor = true;
            this.BthServiceControl.Click += new System.EventHandler(this.BthServiceControl_Click);
            // 
            // WheelPanel
            // 
            this.WheelPanel.BackColor = System.Drawing.Color.White;
            this.WheelPanel.Controls.Add(this.AccInvertCheckBox);
            this.WheelPanel.Controls.Add(this.BackwardMinBar);
            this.WheelPanel.Controls.Add(this.BackwardMaxBar);
            this.WheelPanel.Controls.Add(this.BackwardMinLabel);
            this.WheelPanel.Controls.Add(this.BackwardMaxLabel);
            this.WheelPanel.Controls.Add(this.ForwardMinBar);
            this.WheelPanel.Controls.Add(this.ForwardMaxBar);
            this.WheelPanel.Controls.Add(this.ForwardMinLabel);
            this.WheelPanel.Controls.Add(this.ForwardMaxLabel);
            this.WheelPanel.Controls.Add(this.SteerRightMinBar);
            this.WheelPanel.Controls.Add(this.SteerRightMaxBar);
            this.WheelPanel.Controls.Add(this.SteerRightMinLabel);
            this.WheelPanel.Controls.Add(this.SteerRightMaxLabel);
            this.WheelPanel.Controls.Add(this.SteerLeftMinBar);
            this.WheelPanel.Controls.Add(this.SteerLeftMinLabel);
            this.WheelPanel.Controls.Add(this.SteerLeftMaxBar);
            this.WheelPanel.Controls.Add(this.SteerLeftMaxLabel);
            this.WheelPanel.Location = new System.Drawing.Point(280, 20);
            this.WheelPanel.Name = "WheelPanel";
            this.WheelPanel.Size = new System.Drawing.Size(280, 270);
            this.WheelPanel.TabIndex = 5;
            // 
            // SteerLeftMinBar
            // 
            this.SteerLeftMinBar.AutoSize = false;
            this.SteerLeftMinBar.Location = new System.Drawing.Point(145, 30);
            this.SteerLeftMinBar.Maximum = 40;
            this.SteerLeftMinBar.Minimum = 2;
            this.SteerLeftMinBar.Name = "SteerLeftMinBar";
            this.SteerLeftMinBar.Size = new System.Drawing.Size(120, 20);
            this.SteerLeftMinBar.TabIndex = 3;
            this.SteerLeftMinBar.Value = 10;
            this.SteerLeftMinBar.Scroll += new System.EventHandler(this.SteerLeftMinBar_Scroll);
            this.SteerLeftMinBar.MouseHover += new System.EventHandler(this.SteerLeftMinBar_MouseHover);
            // 
            // SteerLeftMinLabel
            // 
            this.SteerLeftMinLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SteerLeftMinLabel.Location = new System.Drawing.Point(145, 10);
            this.SteerLeftMinLabel.Name = "SteerLeftMinLabel";
            this.SteerLeftMinLabel.Size = new System.Drawing.Size(130, 20);
            this.SteerLeftMinLabel.TabIndex = 2;
            this.SteerLeftMinLabel.Text = "Steering Left Min";
            this.SteerLeftMinLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // SteerLeftMaxBar
            // 
            this.SteerLeftMaxBar.AutoSize = false;
            this.SteerLeftMaxBar.Location = new System.Drawing.Point(10, 30);
            this.SteerLeftMaxBar.Maximum = 85;
            this.SteerLeftMaxBar.Minimum = 45;
            this.SteerLeftMaxBar.Name = "SteerLeftMaxBar";
            this.SteerLeftMaxBar.Size = new System.Drawing.Size(120, 20);
            this.SteerLeftMaxBar.TabIndex = 1;
            this.SteerLeftMaxBar.Value = 45;
            this.SteerLeftMaxBar.Scroll += new System.EventHandler(this.SteerLeftMaxBar_Scroll);
            this.SteerLeftMaxBar.MouseHover += new System.EventHandler(this.SteerLeftMaxBar_MouseHover);
            // 
            // SteerLeftMaxLabel
            // 
            this.SteerLeftMaxLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SteerLeftMaxLabel.Location = new System.Drawing.Point(10, 10);
            this.SteerLeftMaxLabel.Name = "SteerLeftMaxLabel";
            this.SteerLeftMaxLabel.Size = new System.Drawing.Size(130, 20);
            this.SteerLeftMaxLabel.TabIndex = 0;
            this.SteerLeftMaxLabel.Text = "Steering Left Max";
            this.SteerLeftMaxLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // WheelControl
            // 
            this.WheelControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WheelControl.Location = new System.Drawing.Point(350, 310);
            this.WheelControl.Name = "WheelControl";
            this.WheelControl.Size = new System.Drawing.Size(150, 30);
            this.WheelControl.TabIndex = 6;
            this.WheelControl.Text = "Pause Wheel";
            this.WheelControl.UseVisualStyleBackColor = true;
            this.WheelControl.Click += new System.EventHandler(this.WheelControl_Click);
            // 
            // SteerRightMaxLabel
            // 
            this.SteerRightMaxLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SteerRightMaxLabel.Location = new System.Drawing.Point(10, 60);
            this.SteerRightMaxLabel.Name = "SteerRightMaxLabel";
            this.SteerRightMaxLabel.Size = new System.Drawing.Size(130, 20);
            this.SteerRightMaxLabel.TabIndex = 4;
            this.SteerRightMaxLabel.Text = "Steering Right Max";
            this.SteerRightMaxLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // SteerRightMinLabel
            // 
            this.SteerRightMinLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SteerRightMinLabel.Location = new System.Drawing.Point(145, 60);
            this.SteerRightMinLabel.Name = "SteerRightMinLabel";
            this.SteerRightMinLabel.Size = new System.Drawing.Size(130, 20);
            this.SteerRightMinLabel.TabIndex = 5;
            this.SteerRightMinLabel.Text = "Steering Right Min";
            this.SteerRightMinLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // SteerRightMaxBar
            // 
            this.SteerRightMaxBar.AutoSize = false;
            this.SteerRightMaxBar.Location = new System.Drawing.Point(10, 80);
            this.SteerRightMaxBar.Maximum = 85;
            this.SteerRightMaxBar.Minimum = 45;
            this.SteerRightMaxBar.Name = "SteerRightMaxBar";
            this.SteerRightMaxBar.Size = new System.Drawing.Size(120, 20);
            this.SteerRightMaxBar.TabIndex = 6;
            this.SteerRightMaxBar.Value = 45;
            this.SteerRightMaxBar.Scroll += new System.EventHandler(this.SteerRightMaxBar_Scroll);
            this.SteerRightMaxBar.MouseHover += new System.EventHandler(this.SteerRightMaxBar_MouseHover);
            // 
            // SteerRightMinBar
            // 
            this.SteerRightMinBar.AutoSize = false;
            this.SteerRightMinBar.Location = new System.Drawing.Point(145, 80);
            this.SteerRightMinBar.Maximum = 40;
            this.SteerRightMinBar.Minimum = 2;
            this.SteerRightMinBar.Name = "SteerRightMinBar";
            this.SteerRightMinBar.Size = new System.Drawing.Size(120, 20);
            this.SteerRightMinBar.TabIndex = 7;
            this.SteerRightMinBar.Value = 10;
            this.SteerRightMinBar.Scroll += new System.EventHandler(this.SteerRightMinBar_Scroll);
            this.SteerRightMinBar.MouseHover += new System.EventHandler(this.SteerRightMinBar_MouseHover);
            // 
            // ForwardMaxLabel
            // 
            this.ForwardMaxLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForwardMaxLabel.Location = new System.Drawing.Point(10, 110);
            this.ForwardMaxLabel.Name = "ForwardMaxLabel";
            this.ForwardMaxLabel.Size = new System.Drawing.Size(130, 20);
            this.ForwardMaxLabel.TabIndex = 8;
            this.ForwardMaxLabel.Text = "Forward Max";
            this.ForwardMaxLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // ForwardMinLabel
            // 
            this.ForwardMinLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForwardMinLabel.Location = new System.Drawing.Point(145, 110);
            this.ForwardMinLabel.Name = "ForwardMinLabel";
            this.ForwardMinLabel.Size = new System.Drawing.Size(130, 20);
            this.ForwardMinLabel.TabIndex = 9;
            this.ForwardMinLabel.Text = "Forward Min";
            this.ForwardMinLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // ForwardMaxBar
            // 
            this.ForwardMaxBar.AutoSize = false;
            this.ForwardMaxBar.Location = new System.Drawing.Point(10, 130);
            this.ForwardMaxBar.Maximum = 85;
            this.ForwardMaxBar.Minimum = 55;
            this.ForwardMaxBar.Name = "ForwardMaxBar";
            this.ForwardMaxBar.Size = new System.Drawing.Size(120, 20);
            this.ForwardMaxBar.TabIndex = 10;
            this.ForwardMaxBar.Value = 55;
            this.ForwardMaxBar.Scroll += new System.EventHandler(this.ForwardMaxBar_Scroll);
            this.ForwardMaxBar.MouseHover += new System.EventHandler(this.ForwardMaxBar_MouseHover);
            // 
            // ForwardMinBar
            // 
            this.ForwardMinBar.AutoSize = false;
            this.ForwardMinBar.Location = new System.Drawing.Point(145, 130);
            this.ForwardMinBar.Maximum = 50;
            this.ForwardMinBar.Minimum = 5;
            this.ForwardMinBar.Name = "ForwardMinBar";
            this.ForwardMinBar.Size = new System.Drawing.Size(120, 20);
            this.ForwardMinBar.TabIndex = 11;
            this.ForwardMinBar.Value = 25;
            this.ForwardMinBar.Scroll += new System.EventHandler(this.ForwardMinBar_Scroll);
            this.ForwardMinBar.MouseHover += new System.EventHandler(this.ForwardMinBar_MouseHover);
            // 
            // BackwardMaxLabel
            // 
            this.BackwardMaxLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BackwardMaxLabel.Location = new System.Drawing.Point(10, 160);
            this.BackwardMaxLabel.Name = "BackwardMaxLabel";
            this.BackwardMaxLabel.Size = new System.Drawing.Size(130, 20);
            this.BackwardMaxLabel.TabIndex = 12;
            this.BackwardMaxLabel.Text = "Backward Max";
            this.BackwardMaxLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // BackwardMinLabel
            // 
            this.BackwardMinLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BackwardMinLabel.Location = new System.Drawing.Point(145, 160);
            this.BackwardMinLabel.Name = "BackwardMinLabel";
            this.BackwardMinLabel.Size = new System.Drawing.Size(130, 20);
            this.BackwardMinLabel.TabIndex = 13;
            this.BackwardMinLabel.Text = "Backward Min";
            this.BackwardMinLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // BackwardMaxBar
            // 
            this.BackwardMaxBar.AutoSize = false;
            this.BackwardMaxBar.Location = new System.Drawing.Point(10, 180);
            this.BackwardMaxBar.Maximum = 85;
            this.BackwardMaxBar.Minimum = 35;
            this.BackwardMaxBar.Name = "BackwardMaxBar";
            this.BackwardMaxBar.Size = new System.Drawing.Size(120, 20);
            this.BackwardMaxBar.TabIndex = 14;
            this.BackwardMaxBar.Value = 40;
            this.BackwardMaxBar.Scroll += new System.EventHandler(this.BackwardMaxBar_Scroll);
            this.BackwardMaxBar.MouseHover += new System.EventHandler(this.BackwardMaxBar_MouseHover);
            // 
            // BackwardMinBar
            // 
            this.BackwardMinBar.AutoSize = false;
            this.BackwardMinBar.Location = new System.Drawing.Point(145, 180);
            this.BackwardMinBar.Maximum = 30;
            this.BackwardMinBar.Minimum = 5;
            this.BackwardMinBar.Name = "BackwardMinBar";
            this.BackwardMinBar.Size = new System.Drawing.Size(120, 20);
            this.BackwardMinBar.TabIndex = 15;
            this.BackwardMinBar.Value = 5;
            this.BackwardMinBar.Scroll += new System.EventHandler(this.BackwardMinBar_Scroll);
            this.BackwardMinBar.MouseHover += new System.EventHandler(this.BackwardMinBar_MouseHover);
            // 
            // AccInvertCheckBox
            // 
            this.AccInvertCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AccInvertCheckBox.Location = new System.Drawing.Point(30, 220);
            this.AccInvertCheckBox.Name = "AccInvertCheckBox";
            this.AccInvertCheckBox.Size = new System.Drawing.Size(220, 25);
            this.AccInvertCheckBox.TabIndex = 16;
            this.AccInvertCheckBox.Text = "Invert Forward and Backward";
            this.AccInvertCheckBox.UseVisualStyleBackColor = true;
            this.AccInvertCheckBox.Click += new System.EventHandler(this.AccInvertCheckBox_Click);
            // 
            // MonitorUI
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(584, 361);
            this.Controls.Add(this.WheelControl);
            this.Controls.Add(this.WheelPanel);
            this.Controls.Add(this.BthServiceControl);
            this.Controls.Add(this.BthDetail);
            this.Controls.Add(this.BthStatus);
            this.Controls.Add(this.BthStatusLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MonitorUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Service Monitor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MonitorUI_FormClosing);
            this.Load += new System.EventHandler(this.MonitorUI_Load);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MonitorUI_MouseClick);
            this.WheelPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SteerLeftMinBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SteerLeftMaxBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SteerRightMaxBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SteerRightMinBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ForwardMaxBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ForwardMinBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.BackwardMaxBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.BackwardMinBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label BthStatusLabel;
        private System.Windows.Forms.Label BthStatus;
        private System.Windows.Forms.TextBox BthDetail;
        private System.Windows.Forms.Button BthServiceControl;
        private System.Windows.Forms.Panel WheelPanel;
        private System.Windows.Forms.Button WheelControl;
        private System.Windows.Forms.Label SteerLeftMaxLabel;
        private System.Windows.Forms.TrackBar SteerLeftMaxBar;
        private System.Windows.Forms.ToolTip WheelTip;
        private System.Windows.Forms.Label SteerLeftMinLabel;
        private System.Windows.Forms.TrackBar SteerLeftMinBar;
        private System.Windows.Forms.Label SteerRightMinLabel;
        private System.Windows.Forms.Label SteerRightMaxLabel;
        private System.Windows.Forms.TrackBar SteerRightMaxBar;
        private System.Windows.Forms.TrackBar SteerRightMinBar;
        private System.Windows.Forms.Label ForwardMaxLabel;
        private System.Windows.Forms.Label ForwardMinLabel;
        private System.Windows.Forms.TrackBar ForwardMaxBar;
        private System.Windows.Forms.TrackBar ForwardMinBar;
        private System.Windows.Forms.Label BackwardMaxLabel;
        private System.Windows.Forms.Label BackwardMinLabel;
        private System.Windows.Forms.TrackBar BackwardMaxBar;
        private System.Windows.Forms.TrackBar BackwardMinBar;
        private System.Windows.Forms.CheckBox AccInvertCheckBox;
    }
}
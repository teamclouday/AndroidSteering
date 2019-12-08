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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MonitorUI));
            this.BthStatusLabel = new System.Windows.Forms.Label();
            this.WheelStatusLabel = new System.Windows.Forms.Label();
            this.BthStatus = new System.Windows.Forms.Label();
            this.WheelStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // BthStatusLabel
            // 
            this.BthStatusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BthStatusLabel.Location = new System.Drawing.Point(35, 35);
            this.BthStatusLabel.Name = "BthStatusLabel";
            this.BthStatusLabel.Size = new System.Drawing.Size(120, 20);
            this.BthStatusLabel.TabIndex = 0;
            this.BthStatusLabel.Text = "Bluetooth Status";
            // 
            // WheelStatusLabel
            // 
            this.WheelStatusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WheelStatusLabel.Location = new System.Drawing.Point(35, 60);
            this.WheelStatusLabel.Name = "WheelStatusLabel";
            this.WheelStatusLabel.Size = new System.Drawing.Size(120, 20);
            this.WheelStatusLabel.TabIndex = 1;
            this.WheelStatusLabel.Text = "vJoy Status";
            // 
            // BthStatus
            // 
            this.BthStatus.BackColor = System.Drawing.Color.DarkGray;
            this.BthStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BthStatus.Location = new System.Drawing.Point(180, 35);
            this.BthStatus.Name = "BthStatus";
            this.BthStatus.Size = new System.Drawing.Size(80, 20);
            this.BthStatus.TabIndex = 2;
            this.BthStatus.Text = "STATUS";
            this.BthStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // WheelStatus
            // 
            this.WheelStatus.BackColor = System.Drawing.Color.DarkGray;
            this.WheelStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WheelStatus.Location = new System.Drawing.Point(180, 60);
            this.WheelStatus.Name = "WheelStatus";
            this.WheelStatus.Size = new System.Drawing.Size(80, 20);
            this.WheelStatus.TabIndex = 3;
            this.WheelStatus.Text = "STATUS";
            this.WheelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MonitorUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.WheelStatus);
            this.Controls.Add(this.BthStatus);
            this.Controls.Add(this.WheelStatusLabel);
            this.Controls.Add(this.BthStatusLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MonitorUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Service Monitor";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label BthStatusLabel;
        private System.Windows.Forms.Label WheelStatusLabel;
        private System.Windows.Forms.Label BthStatus;
        private System.Windows.Forms.Label WheelStatus;
    }
}
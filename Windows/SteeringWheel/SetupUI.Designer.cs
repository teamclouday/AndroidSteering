using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SteeringWheel
{
    partial class SetupUI
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

        private void MyPaintEventHandler(object sender, PaintEventArgs e)
        {
            Pen pen1 = new Pen(Color.FromArgb(0, 229, 48), 4);
            pen1.LineJoin = LineJoin.Bevel;
            GraphicsPath path1 = new GraphicsPath();
            path1.StartFigure();
            path1.AddLine(new Point(20, 72), new Point(150, 72));
            path1.AddLine(new Point(150, 72), new Point(150, 90));
            e.Graphics.DrawPath(pen1, path1);
            pen1.Dispose();
            path1.Dispose();

            Pen pen2 = new Pen(Color.FromArgb(234, 59, 59), 4);
            pen2.LineJoin = LineJoin.Bevel;
            GraphicsPath path2 = new GraphicsPath();
            path2.StartFigure();
            path2.AddLine(new Point(20, 122), new Point(120, 122));
            e.Graphics.DrawPath(pen2, path2);
            pen2.Dispose();
            path2.Dispose();

            Pen pen3 = new Pen(Color.FromArgb(22, 183, 239), 4);
            pen3.LineJoin = LineJoin.Bevel;
            GraphicsPath path3 = new GraphicsPath();
            path3.StartFigure();
            path3.AddLine(new Point(415, 210), new Point(540, 210));
            path3.AddLine(new Point(540, 210), new Point(540, 72));
            path3.AddLine(new Point(540, 72), new Point(580, 72));
            e.Graphics.DrawPath(pen3, path3);
            pen3.Dispose();
            path3.Dispose();

            Pen pen4 = new Pen(Color.FromArgb(255, 239, 34), 4);
            pen4.LineJoin = LineJoin.Bevel;
            GraphicsPath path4 = new GraphicsPath();
            path4.StartFigure();
            path4.AddLine(new Point(380, 250), new Point(380, 265));
            path4.AddLine(new Point(380, 265), new Point(560, 265));
            path4.AddLine(new Point(560, 265), new Point(560, 122));
            path4.AddLine(new Point(560, 122), new Point(580, 122));
            e.Graphics.DrawPath(pen4, path4);
            pen4.Dispose();
            path4.Dispose();

            Pen pen5 = new Pen(Color.Black, 4);
            pen5.LineJoin = LineJoin.Bevel;
            Graphics graphics = this.CreateGraphics();
            GraphicsPath path5 = new GraphicsPath();
            path5.StartFigure();
            path5.AddLine(new Point(255, 52), new Point(270, 52));
            path5.AddLine(new Point(270, 52), new Point(270, 140));
            path5.StartFigure();
            path5.AddLine(new Point(155, 92), new Point(270, 92));
            path5.StartFigure();
            path5.AddLine(new Point(520, 52), new Point(505, 52));
            path5.AddLine(new Point(505, 52), new Point(505, 140));
            path5.StartFigure();
            path5.AddLine(new Point(620, 92), new Point(505, 92));
            graphics.DrawPath(pen5, path5);
            path5.Dispose();
            pen5.Dispose();
            graphics.Dispose();
        }

        private void MyMouseEventHandler(object sender, MouseEventArgs e)
        {
            int cursorX = e.X;
            int cursorY = e.Y;

            SolidBrush brush = new SolidBrush(Color.FromArgb(115, 0, 255));
            Graphics graphics = this.XboxPicture.CreateGraphics();
            if (IsInDistance(PosHOMEButton, cursorX, cursorY))
            {
                graphics.FillEllipse(brush, new Rectangle(PosHOMEButton[0] - PosHOMEButton[2], PosHOMEButton[1] - PosHOMEButton[2], PosHOMEButton[2] * 2, PosHOMEButton[2] * 2));
                MouseInButton = HoveredButton.HOME;
            }
            else if(IsInDistance(PosRSButton, cursorX, cursorY))
            {
                graphics.FillEllipse(brush, new Rectangle(PosRSButton[0] - PosRSButton[2], PosRSButton[1] - PosRSButton[2], PosRSButton[2] * 2, PosRSButton[2] * 2));
                MouseInButton = HoveredButton.RS;
            }
            else if (IsInDistance(PosLSButton, cursorX, cursorY))
            {
                graphics.FillEllipse(brush, new Rectangle(PosLSButton[0] - PosLSButton[2], PosLSButton[1] - PosLSButton[2], PosLSButton[2] * 2, PosLSButton[2] * 2));
                MouseInButton = HoveredButton.LS;
            }
            else if (IsInDistance(PosXButton, cursorX, cursorY))
            {
                graphics.FillEllipse(brush, new Rectangle(PosXButton[0] - PosXButton[2], PosXButton[1] - PosXButton[2], PosXButton[2] * 2, PosXButton[2] * 2));
                MouseInButton = HoveredButton.X;
            }
            else if (IsInDistance(PosBButton, cursorX, cursorY))
            {
                graphics.FillEllipse(brush, new Rectangle(PosBButton[0] - PosBButton[2], PosBButton[1] - PosBButton[2], PosBButton[2] * 2, PosBButton[2] * 2));
                MouseInButton = HoveredButton.B;
            }
            else if (IsInDistance(PosYButton, cursorX, cursorY))
            {
                graphics.FillEllipse(brush, new Rectangle(PosYButton[0] - PosYButton[2], PosYButton[1] - PosYButton[2], PosYButton[2] * 2, PosYButton[2] * 2));
                MouseInButton = HoveredButton.Y;
            }
            else if (IsInDistance(PosAButton, cursorX, cursorY))
            {
                graphics.FillEllipse(brush, new Rectangle(PosAButton[0] - PosAButton[2], PosAButton[1] - PosAButton[2], PosAButton[2] * 2, PosAButton[2] * 2));
                MouseInButton = HoveredButton.A;
            }
            else if (IsInDistance(PosUPButton, cursorX, cursorY))
            {
                graphics.FillRectangle(brush, new Rectangle(PosUPButton[0] - PosUPButton[2], PosUPButton[1] - PosUPButton[2], PosUPButton[2] * 2, PosUPButton[2] * 2));
                MouseInButton = HoveredButton.UP;
            }
            else if (IsInDistance(PosDOWNButton, cursorX, cursorY))
            {
                graphics.FillRectangle(brush, new Rectangle(PosDOWNButton[0] - PosDOWNButton[2], PosDOWNButton[1] - PosDOWNButton[2], PosDOWNButton[2] * 2, PosDOWNButton[2] * 2));
                MouseInButton = HoveredButton.DOWN;
            }
            else if (IsInDistance(PosLEFTButton, cursorX, cursorY))
            {
                graphics.FillRectangle(brush, new Rectangle(PosLEFTButton[0] - PosLEFTButton[2], PosLEFTButton[1] - PosLEFTButton[2], PosLEFTButton[2] * 2, PosLEFTButton[2] * 2));
                MouseInButton = HoveredButton.LEFT;
            }
            else if (IsInDistance(PosRIGHTButton, cursorX, cursorY))
            {
                graphics.FillRectangle(brush, new Rectangle(PosRIGHTButton[0] - PosRIGHTButton[2], PosRIGHTButton[1] - PosRIGHTButton[2], PosRIGHTButton[2] * 2, PosRIGHTButton[2] * 2));
                MouseInButton = HoveredButton.RIGHT;
            }
            else if (IsInDistance(PosBACKButton, cursorX, cursorY))
            {
                graphics.FillEllipse(brush, new Rectangle(PosBACKButton[0] - PosBACKButton[2], PosBACKButton[1] - PosBACKButton[2], PosBACKButton[2] * 2, PosBACKButton[2] * 2));
                MouseInButton = HoveredButton.BACK;
            }
            else if (IsInDistance(PosSTARTButton, cursorX, cursorY))
            {
                graphics.FillEllipse(brush, new Rectangle(PosSTARTButton[0] - PosSTARTButton[2], PosSTARTButton[1] - PosSTARTButton[2], PosSTARTButton[2] * 2, PosSTARTButton[2] * 2));
                MouseInButton = HoveredButton.START;
            }
            else
            {
                MouseInButton = HoveredButton.NONE;
                this.XboxPicture.Invalidate();
            }
            brush.Dispose();
            graphics.Dispose();
        }

        private bool IsInDistance(int[] data, int targetX, int targetY)
        {
            if (data.Length < 3) return false;
            int distance = (int)(System.Math.Pow(data[0] - targetX, 2) + System.Math.Pow(data[1] - targetY, 2));
            int acceptable = data[2] * data[2];
            return distance <= acceptable;
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupUI));
            this.XboxPicture = new System.Windows.Forms.PictureBox();
            this.OKButton = new System.Windows.Forms.Button();
            this.AxisYRotButton = new System.Windows.Forms.Button();
            this.AxisXRotButton = new System.Windows.Forms.Button();
            this.AxisXMoveButton = new System.Windows.Forms.Button();
            this.AxisYMoveButton = new System.Windows.Forms.Button();
            this.LBButton = new System.Windows.Forms.Button();
            this.RBButton = new System.Windows.Forms.Button();
            this.LTButton = new System.Windows.Forms.Button();
            this.RTButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.XboxPicture)).BeginInit();
            this.SuspendLayout();
            // 
            // XboxPicture
            // 
            this.XboxPicture.BackColor = System.Drawing.SystemColors.Control;
            this.XboxPicture.Image = global::SteeringWheel.Properties.Resources.controller;
            this.XboxPicture.InitialImage = global::SteeringWheel.Properties.Resources.controller;
            this.XboxPicture.Location = new System.Drawing.Point(90, 140);
            this.XboxPicture.Name = "XboxPicture";
            this.XboxPicture.Size = new System.Drawing.Size(600, 400);
            this.XboxPicture.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.XboxPicture.TabIndex = 0;
            this.XboxPicture.TabStop = false;
            this.XboxPicture.Click += new System.EventHandler(this.XboxPicture_Click);
            this.XboxPicture.Paint += new System.Windows.Forms.PaintEventHandler(this.MyPaintEventHandler);
            this.XboxPicture.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MyMouseEventHandler);
            // 
            // OKButton
            // 
            this.OKButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OKButton.Location = new System.Drawing.Point(240, 500);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(300, 30);
            this.OKButton.TabIndex = 1;
            this.OKButton.Text = "To End Setup, Click This Button";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // AxisYRotButton
            // 
            this.AxisYRotButton.Location = new System.Drawing.Point(660, 250);
            this.AxisYRotButton.Name = "AxisYRotButton";
            this.AxisYRotButton.Size = new System.Drawing.Size(100, 25);
            this.AxisYRotButton.TabIndex = 2;
            this.AxisYRotButton.Text = "Vertical Move";
            this.AxisYRotButton.UseVisualStyleBackColor = true;
            this.AxisYRotButton.Click += new System.EventHandler(this.AxisYRotButton_Click);
            // 
            // AxisXRotButton
            // 
            this.AxisXRotButton.Location = new System.Drawing.Point(660, 200);
            this.AxisXRotButton.Name = "AxisXRotButton";
            this.AxisXRotButton.Size = new System.Drawing.Size(100, 25);
            this.AxisXRotButton.TabIndex = 3;
            this.AxisXRotButton.Text = "Horizontal Move";
            this.AxisXRotButton.UseVisualStyleBackColor = true;
            this.AxisXRotButton.Click += new System.EventHandler(this.AxisXRotButton_Click);
            // 
            // AxisXMoveButton
            // 
            this.AxisXMoveButton.Location = new System.Drawing.Point(20, 250);
            this.AxisXMoveButton.Name = "AxisXMoveButton";
            this.AxisXMoveButton.Size = new System.Drawing.Size(100, 25);
            this.AxisXMoveButton.TabIndex = 4;
            this.AxisXMoveButton.Text = "Horizontal Move";
            this.AxisXMoveButton.UseVisualStyleBackColor = true;
            this.AxisXMoveButton.Click += new System.EventHandler(this.AxisXMoveButton_Click);
            // 
            // AxisYMoveButton
            // 
            this.AxisYMoveButton.Location = new System.Drawing.Point(20, 200);
            this.AxisYMoveButton.Name = "AxisYMoveButton";
            this.AxisYMoveButton.Size = new System.Drawing.Size(100, 25);
            this.AxisYMoveButton.TabIndex = 5;
            this.AxisYMoveButton.Text = "Vertical Move";
            this.AxisYMoveButton.UseVisualStyleBackColor = true;
            this.AxisYMoveButton.Click += new System.EventHandler(this.AxisYMoveButton_Click);
            // 
            // LBButton
            // 
            this.LBButton.Location = new System.Drawing.Point(80, 80);
            this.LBButton.Name = "LBButton";
            this.LBButton.Size = new System.Drawing.Size(75, 25);
            this.LBButton.TabIndex = 6;
            this.LBButton.Text = "LB";
            this.LBButton.UseVisualStyleBackColor = true;
            this.LBButton.Click += new System.EventHandler(this.LBButton_Click);
            // 
            // RBButton
            // 
            this.RBButton.Location = new System.Drawing.Point(620, 80);
            this.RBButton.Name = "RBButton";
            this.RBButton.Size = new System.Drawing.Size(75, 25);
            this.RBButton.TabIndex = 7;
            this.RBButton.Text = "RB";
            this.RBButton.UseVisualStyleBackColor = true;
            this.RBButton.Click += new System.EventHandler(this.RBButton_Click);
            // 
            // LTButton
            // 
            this.LTButton.Location = new System.Drawing.Point(180, 40);
            this.LTButton.Name = "LTButton";
            this.LTButton.Size = new System.Drawing.Size(75, 25);
            this.LTButton.TabIndex = 8;
            this.LTButton.Text = "LT";
            this.LTButton.UseVisualStyleBackColor = true;
            this.LTButton.Click += new System.EventHandler(this.LTButton_Click);
            // 
            // RTButton
            // 
            this.RTButton.Location = new System.Drawing.Point(520, 40);
            this.RTButton.Name = "RTButton";
            this.RTButton.Size = new System.Drawing.Size(75, 25);
            this.RTButton.TabIndex = 9;
            this.RTButton.Text = "RT";
            this.RTButton.UseVisualStyleBackColor = true;
            this.RTButton.Click += new System.EventHandler(this.RTButton_Click);
            // 
            // SetupUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.RTButton);
            this.Controls.Add(this.LTButton);
            this.Controls.Add(this.RBButton);
            this.Controls.Add(this.LBButton);
            this.Controls.Add(this.AxisYMoveButton);
            this.Controls.Add(this.AxisXMoveButton);
            this.Controls.Add(this.AxisXRotButton);
            this.Controls.Add(this.AxisYRotButton);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.XboxPicture);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "SetupUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Game Controller Setup For Steam";
            ((System.ComponentModel.ISupportInitialize)(this.XboxPicture)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private PictureBox XboxPicture;
        private Button OKButton;
        private Button AxisYRotButton;
        private Button AxisXRotButton;
        private Button AxisXMoveButton;
        private Button AxisYMoveButton;
        private Button LBButton;
        private Button RBButton;
        private Button LTButton;
        private Button RTButton;

        private int[] PosHOMEButton = new int[] {300, 55, 30};
        private int[] PosRSButton = new int[] {380, 212, 35};
        private int[] PosLSButton = new int[] {152, 122, 35};
        private int[] PosXButton = new int[] {418, 122, 22};
        private int[] PosBButton = new int[] {498, 122, 22};
        private int[] PosYButton = new int[] {458, 82, 22};
        private int[] PosAButton = new int[] {458, 162, 22};
        private int[] PosUPButton = new int[] {227, 190, 15};
        private int[] PosDOWNButton = new int[] {227, 240, 15};
        private int[] PosLEFTButton = new int[] {202, 216, 15};
        private int[] PosRIGHTButton = new int[] {252, 216, 15};
        private int[] PosBACKButton = new int[] {259, 122, 15};
        private int[] PosSTARTButton = new int[] {347, 122, 15};
        private HoveredButton MouseInButton = HoveredButton.NONE;
    }

    public enum HoveredButton
    {
        NONE,
        HOME,
        A,
        X,
        Y,
        B,
        UP,
        DOWN,
        LEFT,
        RIGHT,
        LS,
        RS,
        START,
        BACK
    }
}
using System.Windows.Forms;

namespace SteeringWheel
{
    public partial class MonitorUI : Form
    {
        public MonitorUI()
        {
            InitializeComponent();
        }

        private void BthServiceControl_Click(object sender, System.EventArgs e)
        {
            if(Program.bltDevice.okForConnection)
            {
                Program.bltDevice.Pause();
                ((Button)sender).Text = "Resume Bluetooth";
            }
            else
            {
                Program.bltDevice.Resume();
                ((Button)sender).Text = "Pause Bluetooth";
            }
        }

        private void WheelControl_Click(object sender, System.EventArgs e)
        {
            if(Program.wheelDevice.okForRunning)
            {
                Program.wheelDevice.Pause();
                ((Button)sender).Text = "Resume Wheel";
            }
            else
            {
                Program.wheelDevice.Resume();
                ((Button)sender).Text = "Pause Wheel";
            }
        }

        private void MonitorUI_MouseClick(object sender, MouseEventArgs e)
        {
            ActiveControl = BthStatusLabel;
        }

        private void SetBthStatusLabel(MyBluetoothStatus status)
        {
            switch(status)
            {
                case MyBluetoothStatus.CONNECTED:
                    if(BthStatus.Text != "Connected")
                    {
                        BthStatus.Text = "Connected";
                        BthStatus.BackColor = System.Drawing.Color.FromArgb(46, 110, 19);
                        BthStatus.ForeColor = System.Drawing.Color.FromArgb(185, 255, 56);
                        UpdateBthDetail();
                    }
                    break;
                case MyBluetoothStatus.LISTENING:
                    if(BthStatus.Text != "Listening")
                    {
                        BthStatus.Text = "Listening";
                        BthStatus.BackColor = System.Drawing.Color.FromArgb(7, 50, 168);
                        BthStatus.ForeColor = System.Drawing.Color.FromArgb(43, 255, 248);
                        UpdateBthDetail();
                    }
                    break;
                case MyBluetoothStatus.NONE:
                    if(BthStatus.Text != "Dead")
                    {
                        BthStatus.Text = "Dead";
                        BthStatus.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
                        BthStatus.ForeColor = System.Drawing.Color.FromArgb(230, 230, 230);
                        UpdateBthDetail();
                    }
                    break;
            }
        }

        private void UpdateBthDetail()
        {
            string[] newData = Program.bltDevice.GetDeviceInfo();
            BthDetail.ResetText();
            if(newData != null)
            {
                foreach(string line in newData)
                {
                    BthDetail.AppendText(line);
                    BthDetail.AppendText(System.Environment.NewLine);
                }
            }
        }

        private void ServiceUpdate_Tick(object sender, System.EventArgs e)
        {
            BeginInvoke(new System.Action(() => SetBthStatusLabel(Program.bltDevice.status)));
        }

        private void MonitorUI_Load(object sender, System.EventArgs e)
        {
            serviceUpdate = new System.Timers.Timer();
            serviceUpdate.Elapsed += new System.Timers.ElapsedEventHandler(ServiceUpdate_Tick);
            serviceUpdate.AutoReset = true;
            serviceUpdate.Interval = 100;
            serviceUpdate.Start();

            SteerLeftMaxBar.Value = MyWheel.steerLeftMax;
            SteerLeftMinBar.Value = MyWheel.steerLeftMin;
            SteerRightMaxBar.Value = MyWheel.steerRightMax;
            SteerRightMinBar.Value = MyWheel.steerRightMin;

            ForwardMaxBar.Value = MyWheel.accForwardMax;
            ForwardMinBar.Value = MyWheel.accForwardMin;
            BackwardMaxBar.Value = MyWheel.accBackwardMax;
            BackwardMinBar.Value = MyWheel.accBackwardMin;
        }

        private void MonitorUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            serviceUpdate.Close();
        }

        private System.Timers.Timer serviceUpdate;

        private void SteerLeftMaxBar_Scroll(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(SteerLeftMaxBar, SteerLeftMaxBar.Value.ToString());
            MyWheel.steerLeftMax = SteerLeftMaxBar.Value;
        }

        private void SteerLeftMinBar_Scroll(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(SteerLeftMinBar, SteerLeftMinBar.Value.ToString());
            MyWheel.steerLeftMin = SteerLeftMinBar.Value;
        }

        private void SteerRightMaxBar_Scroll(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(SteerRightMaxBar, SteerRightMaxBar.Value.ToString());
            MyWheel.steerRightMax = SteerRightMaxBar.Value;
        }

        private void SteerRightMinBar_Scroll(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(SteerRightMinBar, SteerRightMinBar.Value.ToString());
            MyWheel.steerRightMin = SteerRightMinBar.Value;
        }

        private void ForwardMaxBar_Scroll(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(ForwardMaxBar, ForwardMaxBar.Value.ToString());
            MyWheel.accForwardMax = ForwardMaxBar.Value;
        }

        private void ForwardMinBar_Scroll(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(ForwardMinBar, ForwardMinBar.Value.ToString());
            MyWheel.accForwardMin = ForwardMinBar.Value;
        }

        private void BackwardMaxBar_Scroll(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(BackwardMaxBar, BackwardMaxBar.Value.ToString());
            MyWheel.accBackwardMax = BackwardMaxBar.Value;
        }

        private void BackwardMinBar_Scroll(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(BackwardMinBar, BackwardMinBar.Value.ToString());
            MyWheel.accBackwardMin = BackwardMinBar.Value;
        }

        private void SteerLeftMaxBar_MouseHover(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(SteerLeftMaxBar, SteerLeftMaxBar.Value.ToString());
        }

        private void SteerLeftMinBar_MouseHover(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(SteerLeftMinBar, SteerLeftMinBar.Value.ToString());
        }

        private void SteerRightMaxBar_MouseHover(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(SteerRightMaxBar, SteerRightMaxBar.Value.ToString());
        }

        private void SteerRightMinBar_MouseHover(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(SteerRightMinBar, SteerRightMinBar.Value.ToString());
        }

        private void ForwardMaxBar_MouseHover(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(ForwardMaxBar, ForwardMaxBar.Value.ToString());
        }

        private void ForwardMinBar_MouseHover(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(ForwardMinBar, ForwardMinBar.Value.ToString());
        }

        private void BackwardMaxBar_MouseHover(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(BackwardMaxBar, BackwardMaxBar.Value.ToString());
        }

        private void BackwardMinBar_MouseHover(object sender, System.EventArgs e)
        {
            WheelTip.SetToolTip(BackwardMinBar, BackwardMinBar.Value.ToString());
        }

        private void AccInvertCheckBox_Click(object sender, System.EventArgs e)
        {
            MyWheel.accInverted = AccInvertCheckBox.Checked;
        }
    }
}

using System.Windows.Forms;

namespace SteeringWheel
{
    public partial class SetupUI : Form
    {
        public SetupUI()
        {
            InitializeComponent();
        }

        public System.Drawing.Icon GetIcon()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupUI));
            return (System.Drawing.Icon)resources.GetObject("$this.Icon");
        }

        private void OKButton_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void AxisYRotButton_Click(object sender, System.EventArgs e)
        {
            Program.wheelDevice.TriggerControl(ControlAxis.YRot);
        }

        private void AxisXRotButton_Click(object sender, System.EventArgs e)
        {
            Program.wheelDevice.TriggerControl(ControlAxis.XRot);
        }

        private void AxisYMoveButton_Click(object sender, System.EventArgs e)
        {
            Program.wheelDevice.TriggerControl(ControlAxis.Y);
        }

        private void AxisXMoveButton_Click(object sender, System.EventArgs e)
        {
            Program.wheelDevice.TriggerControl(ControlAxis.X);
        }

        private void LBButton_Click(object sender, System.EventArgs e)
        {
            Program.wheelDevice.TriggerControl(ControlButton.LB);
        }

        private void LTButton_Click(object sender, System.EventArgs e)
        {
            Program.wheelDevice.TriggerControl(ControlAxis.Z);
        }

        private void RTButton_Click(object sender, System.EventArgs e)
        {
            Program.wheelDevice.TriggerControl(ControlAxis.ZRot);
        }

        private void RBButton_Click(object sender, System.EventArgs e)
        {
            Program.wheelDevice.TriggerControl(ControlButton.RB);
        }

        private void XboxPicture_Click(object sender, System.EventArgs e)
        {
            switch(MouseInButton)
            {
                case HoveredButton.A:
                    Program.wheelDevice.TriggerControl(ControlButton.A);
                    break;
                case HoveredButton.B:
                    Program.wheelDevice.TriggerControl(ControlButton.B);
                    break;
                case HoveredButton.BACK:
                    Program.wheelDevice.TriggerControl(ControlButton.BACK);
                    break;
                case HoveredButton.DOWN:
                    Program.wheelDevice.TriggerControl(ControlAxis.POVDown);
                    break;
                case HoveredButton.HOME:
                    Program.wheelDevice.TriggerControl(ControlButton.HOME);
                    break;
                case HoveredButton.LEFT:
                    Program.wheelDevice.TriggerControl(ControlAxis.POVLeft);
                    break;
                case HoveredButton.LS:
                    Program.wheelDevice.TriggerControl(ControlButton.LS);
                    break;
                case HoveredButton.RIGHT:
                    Program.wheelDevice.TriggerControl(ControlAxis.POVRight);
                    break;
                case HoveredButton.RS:
                    Program.wheelDevice.TriggerControl(ControlButton.RS);
                    break;
                case HoveredButton.START:
                    Program.wheelDevice.TriggerControl(ControlButton.START);
                    break;
                case HoveredButton.UP:
                    Program.wheelDevice.TriggerControl(ControlAxis.POVUp);
                    break;
                case HoveredButton.X:
                    Program.wheelDevice.TriggerControl(ControlButton.X);
                    break;
                case HoveredButton.Y:
                    Program.wheelDevice.TriggerControl(ControlButton.Y);
                    break;
                default:
                    break;
            }
        }
    }
}

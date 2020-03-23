using System;
using System.ComponentModel;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;

namespace ManiaRTRender
{
    public partial class RenderForm : Form
    {
        private RenderManager renderManager;
        private System.Timers.Timer fpsTimer;

        public RenderForm(Game game)
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None; // no borders
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.SetStyle(ControlStyles.ResizeRedraw, true); // this is to avoid visual artifacts

            renderManager = new RenderManager(glControl, game, bg);

            SetupCallback(glControl);
            SetupCallback(bg);

            if (Setting.BackgroundPicture.Trim() != string.Empty)
            {
                bg.Image = new Bitmap(Setting.BackgroundPicture);
            }
            else
            {
                bg.Image = Properties.Resource.bg;
            }

            fpsTimer = new System.Timers.Timer(1000);
            fpsTimer.Enabled = true;
            fpsTimer.Elapsed += new ElapsedEventHandler(CalculateFPS);
            fpsTimer.AutoReset = true;

            controlLabel.Hide();
        }

        private void SetupCallback(Control control)
        {
            control.MouseMove += GLMouseMove;
            control.MouseDown += GLMouseDown;
            control.MouseEnter += GLMouseEnter;
            control.MouseLeave += GLMouseLeave;
        }
        private void CalculateFPS(object sender, ElapsedEventArgs e)
        {
            controlLabel.Text = $"FPS: {renderManager.GetRenderCountAndClear()}";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            renderManager.Load(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            renderManager.Close(e);
            base.OnClosing(e);
        }

        #region resize window
        private const int
            HTCAPTION = 2,
            HTLEFT = 10,
            HTRIGHT = 11,
            HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            HTBOTTOM = 15,
            HTBOTTOMLEFT = 16,
            HTBOTTOMRIGHT = 17;

        const int _ = 8;

        Rectangle Top { get { return new Rectangle(0, 0, this.ClientSize.Width, _); } }
        Rectangle Left { get { return new Rectangle(0, 0, _, this.ClientSize.Height); } }
        Rectangle Bottom { get { return new Rectangle(0, this.ClientSize.Height - _, this.ClientSize.Width, _); } }
        Rectangle Right { get { return new Rectangle(this.ClientSize.Width - _, 0, _, this.ClientSize.Height); } }

        Rectangle TopLeft { get { return new Rectangle(0, 0, _, _); } }
        Rectangle TopRight { get { return new Rectangle(this.ClientSize.Width - _, 0, _, _); } }
        Rectangle BottomLeft { get { return new Rectangle(0, this.ClientSize.Height - _, _, _); } }
        Rectangle BottomRight { get { return new Rectangle(this.ClientSize.Width - _, this.ClientSize.Height - _, _, _); } }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            if (message.Msg == 0x84) // WM_NCHITTEST
            {
                var cursor = this.PointToClient(Cursor.Position);

                if (TopLeft.Contains(cursor)) message.Result = (IntPtr)HTTOPLEFT;
                else if (TopRight.Contains(cursor)) message.Result = (IntPtr)HTTOPRIGHT;
                else if (BottomLeft.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMLEFT;
                else if (BottomRight.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMRIGHT;

                else if (Top.Contains(cursor)) message.Result = (IntPtr)HTTOP;
                else if (Left.Contains(cursor)) message.Result = (IntPtr)HTLEFT;
                else if (Right.Contains(cursor)) message.Result = (IntPtr)HTRIGHT;
                else if (Bottom.Contains(cursor)) message.Result = (IntPtr)HTBOTTOM;
                else message.Result = (IntPtr)HTCAPTION;
            }
        }

        #endregion

        #region shift window

        Point downPoint;
        private void GLMouseDown(object sender, MouseEventArgs e)
        {
            downPoint = new Point(e.X, e.Y);
        }

        private void GLMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Location = new Point(this.Location.X + e.X - downPoint.X,
                    this.Location.Y + e.Y - downPoint.Y);
            }
        }

        private void GLMouseEnter(object sender, EventArgs e)
        {
            controlLabel.Show();
            controlLabel.BringToFront();
            BackColor = SystemColors.Highlight;
        }

        private void GLMouseLeave(object sender, EventArgs e)
        {
            controlLabel.Hide();
            BackColor = Color.Black;
        }

        #endregion
    }
}

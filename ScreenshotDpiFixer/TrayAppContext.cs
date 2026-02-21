using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenshotDpiFixer
{
    public class TrayAppContext : ApplicationContext
    {
        private bool waitingForManualClick = false;
        private NotifyIcon trayIcon;
        private Timer clipboardTimer;

        private bool autoMode = false; // Manual by default
        private bool suppressNextCheck = false;

        private int lastClipboardWidth = 0;
        private int lastClipboardHeight = 0;

        private Bitmap pendingResized = null;

        public TrayAppContext()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = Properties.Resources.Icon,
                Visible = true,
                Text = "Screenshot DPI Fixer"
            };

            var autoItem = new MenuItem("Auto scale to 100%")
            {
                Checked = autoMode
            };

            autoItem.Click += (s, e) =>
            {
                autoMode = !autoMode;
                autoItem.Checked = autoMode;

                trayIcon.BalloonTipTitle = "Mode changed";
                trayIcon.BalloonTipText = autoMode
                    ? "Auto mode ON: screenshots will be scaled automatically."
                    : "Manual mode ON: click balloon to copy scaled version.";
                trayIcon.ShowBalloonTip(1500);
            };

            trayIcon.ContextMenu = new ContextMenu(new MenuItem[]
            {
                autoItem,
                new MenuItem("About...", ShowAbout),
                new MenuItem("-"),
                new MenuItem("Exit", Exit)
            });

            trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;

            clipboardTimer = new Timer();
            clipboardTimer.Interval = 300;
            clipboardTimer.Tick += ClipboardTimer_Tick;
            clipboardTimer.Start();
        }

        private void ClipboardTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (suppressNextCheck)
                {
                    suppressNextCheck = false;
                    return;
                }

                if (!Clipboard.ContainsImage())
                    return;

                using (var image = Clipboard.GetImage())
                {
                    if (image == null)
                        return;

                    if (image.Width == lastClipboardWidth &&
                        image.Height == lastClipboardHeight)
                        return;

                    lastClipboardWidth = image.Width;
                    lastClipboardHeight = image.Height;

                    var screen = Screen.FromPoint(Cursor.Position);
                    float scale = GetMonitorScale(screen);

                    if (scale <= 1.01f)
                        return;

                    int newWidth = (int)Math.Round(image.Width / scale);
                    int newHeight = (int)Math.Round(image.Height / scale);

                    if (image.Width == newWidth &&
                        image.Height == newHeight)
                        return;

                    Bitmap resized = new Bitmap(newWidth, newHeight);
                    resized.SetResolution(96, 96);

                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(image, 0, 0, newWidth, newHeight);
                    }

                    if (autoMode)
                    {
                        suppressNextCheck = true;

                        Clipboard.SetImage(resized);

                        lastClipboardWidth = resized.Width;
                        lastClipboardHeight = resized.Height;

                        trayIcon.BalloonTipTitle = "Screenshot scaled";
                        trayIcon.BalloonTipText =
                            $"Detected {scale * 100:F0}% scaling. 100% version copied.";
                        trayIcon.ShowBalloonTip(1500);

                        resized.Dispose();
                    }
                    else
                    {
                        pendingResized?.Dispose();
                        pendingResized = resized;

                        waitingForManualClick = true;

                        trayIcon.BalloonTipTitle = "Screenshot detected";
                        trayIcon.BalloonTipText =
                            $"Detected {scale * 100:F0}% scaling. Click to copy 100% version.";
                        trayIcon.ShowBalloonTip(3000);
                    }
                }
            }
            catch
            {
            }
        }

        private void TrayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            // Only react if this was the scaling balloon
            if (!waitingForManualClick)
                return;

            waitingForManualClick = false;

            if (pendingResized == null)
                return;

            suppressNextCheck = true;

            Clipboard.SetImage(pendingResized);

            lastClipboardWidth = pendingResized.Width;
            lastClipboardHeight = pendingResized.Height;

            trayIcon.BalloonTipTitle = "Copied";
            trayIcon.BalloonTipText = "100% version copied to clipboard.";
            trayIcon.ShowBalloonTip(1200);
        }

        private float GetMonitorScale(Screen screen)
        {
            uint dpiX, dpiY;

            IntPtr hMonitor = NativeMethods.MonitorFromPoint(
                new Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1),
                NativeMethods.MONITOR_DEFAULTTONEAREST);

            NativeMethods.GetDpiForMonitor(
                hMonitor,
                NativeMethods.MDT_EFFECTIVE_DPI,
                out dpiX,
                out dpiY);

            return dpiX / 96f;
        }

        private void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;

            clipboardTimer.Stop();
            clipboardTimer.Dispose();

            pendingResized?.Dispose();
            pendingResized = null;

            Application.Exit();
        }
        private void ShowAbout(object sender, EventArgs e)
        {
            Form about = new Form();
            about.Text = "About Screenshot DPI Fixer";
            about.FormBorderStyle = FormBorderStyle.FixedDialog;
            about.StartPosition = FormStartPosition.CenterScreen;
            about.MaximizeBox = false;
            about.MinimizeBox = false;
            about.ClientSize = new Size(800, 300);
            about.Icon = Properties.Resources.Icon;

            Label text = new Label();
            text.Text =
                "Screenshot DPI Fixer\n\n" +
                "Automatically scales high-DPI screenshots\n" +
                "to 100% logical size.\n\n" +
                "Created by Gal Shemesh (PacketLauncher)";
            text.AutoSize = true;
            text.Location = new Point(110, 30);

            PictureBox pic = new PictureBox();
            pic.Image = Properties.Resources.Icon.ToBitmap();
            pic.SizeMode = PictureBoxSizeMode.StretchImage;
            pic.Size = new Size(64, 64);
            pic.Location = new Point(20, 35);

            Button ok = new Button();
            ok.Text = "OK";
            ok.DialogResult = DialogResult.OK;
            ok.Location = new Point(250, 130);
            ok.Width = 75;

            about.Controls.Add(text);
            about.Controls.Add(pic);
            about.Controls.Add(ok);
            about.AcceptButton = ok;

            about.ShowDialog();
        }
    }
}
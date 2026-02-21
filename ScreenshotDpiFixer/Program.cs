using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenshotDpiFixer
{
    static class Program
    {
        [DllImport("Shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        [STAThread]
        static void Main()
        {
            try { SetProcessDpiAwareness(2); } catch { } // Per-monitor DPI aware

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayAppContext());
        }
    }
}
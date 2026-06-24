using System;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize database once (creates tables + seeds admin if first run)
            DatabaseService.Instance.Initialize();

            // ── Login loop ────────────────────────────────────────────
            // Keep re-showing the login form until the user closes the window
            // entirely (instead of signing out). Sign Out sets a flag that
            // breaks back to this loop and shows login again.
            while (true)
            {
                // Show login
                using var login = new LoginForm();
                if (login.ShowDialog() != DialogResult.OK)
                    break;   // user closed the login window — exit app

                // Run main form; it signals sign-out via MainForm.UserSignedOut
                var main = new MainForm();
                Application.Run(main);

                // Log out session after main form closes
                SessionManager.Logout();

                // If the user chose "Exit" rather than "Sign Out", stop the loop
                if (!MainForm.RestartAfterSignOut)
                    break;

                // Otherwise loop back and show login again
                MainForm.RestartAfterSignOut = false;
            }
        }
    }
}
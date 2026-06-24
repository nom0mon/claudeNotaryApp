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

            // 1. Initialize database (creates tables + seeds admin if first run)
            DatabaseService.Instance.Initialize();

            // 2. Show login
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK)
                return;   // user closed login window

            // 3. Open main form
            Application.Run(new MainForm());

            // 4. Log out on close
            SessionManager.Logout();
        }
    }
}

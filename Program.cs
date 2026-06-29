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

            // 1. Initialise Firestore (seeds default admin if collection is empty)
            try
            {
                FirestoreService.Instance.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to connect to Firestore:\n\n{ex.Message}\n\n" +
                    "Make sure firebase-credentials.json is next to the executable " +
                    "and the project ID in FirestoreService.cs is correct.",
                    "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2. Show login
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK)
                return;

            // 3. Open main form
            Application.Run(new MainForm());

            // 4. Log out on close
            SessionManager.Logout();
        }
    }
}

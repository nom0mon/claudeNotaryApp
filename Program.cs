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

            try
            {
                FirestoreService.Instance.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Startup Error");
                return;
            }

            Application.Run(new LoginForm());
        }
    }
}
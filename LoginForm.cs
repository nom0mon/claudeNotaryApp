using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
namespace LegalOfficeApp
{
    public class LoginForm : Form
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);

        private TextBox txtUsername, txtPassword;
        private Label   lblError;
        private Button  btnLogin;

        public LoginForm()
        {
            Text            = "Legal Office – Calamba City";
            Size            = new Size(420, 520);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);
            
            ApplyIcon();
            BuildUI();
        }

        private void ApplyIcon()
        {
            // 1. Try next to the .exe (deployment)
            string exeDir  = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = {
                Path.Combine(exeDir, "logo.ico"),
                Path.Combine(exeDir, "app.ico"),
                // 2. Try project root (development / VS Code)
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "logo.ico"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "app.ico"),
            };
 
            foreach (var p in paths)
            {
                try
                {
                    string full = Path.GetFullPath(p);
                    if (File.Exists(full))
                    {
                        var icon = new Icon(full);
                        this.Icon                    = icon;
                        // Also set the taskbar icon via ShowInTaskbar
                        this.ShowInTaskbar           = true;
                        return;
                    }
                }
                catch { }
            }
 
            // 3. Fall back to embedded resource named "logo.ico" or "app.ico"
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                    {
                        using var stream = asm.GetManifestResourceStream(name)!;
                        this.Icon          = new Icon(stream);
                        this.ShowInTaskbar = true;
                        return;
                    }
                }
            }
            catch { }
        }

        private void BuildUI()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 160, BackColor = Navy };
            var lblTitle = new Label
            {
                Text      = "LEGAL OFFICE\nCALAMBA CITY",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            };
            header.Controls.Add(lblTitle);

            var pnlForm = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40, 30, 40, 20) };

            var lblU = Lbl("Username");
            lblU.Location       = new Point(40, 30);
            txtUsername         = Input();
            txtUsername.Location = new Point(40, 52);
            txtUsername.Width    = 320;

            var lblP = Lbl("Password");
            lblP.Location          = new Point(40, 100);
            txtPassword            = Input();
            txtPassword.Location   = new Point(40, 122);
            txtPassword.Width      = 320;
            txtPassword.PasswordChar = '●';
            txtPassword.KeyDown   += (s, e) => { if (e.KeyCode == Keys.Enter) Login(); };

            lblError = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(163, 45, 45),
                Font      = new Font("Segoe UI", 8.5f),
                Location  = new Point(40, 165),
                Size      = new Size(320, 20)
            };

            btnLogin = new Button
            {
                Text      = "Sign In",
                Location  = new Point(40, 192),
                Size      = new Size(320, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Navy,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += (s, e) => Login();

            var lblHint = new Label
            {
                Text      = "Default: admin / admin123",
                ForeColor = Color.FromArgb(160, 160, 160),
                Font      = new Font("Segoe UI", 8f),
                Location  = new Point(40, 245),
                AutoSize  = true
            };

            pnlForm.Controls.AddRange(
                new Control[] { lblU, txtUsername, lblP, txtPassword, lblError, btnLogin, lblHint });

            this.Controls.Add(pnlForm);
            this.Controls.Add(header);
        }

        private async void Login()
        {
            lblError.Text  = "";
            btnLogin.Enabled = false;
            btnLogin.Text  = "Signing in…";

            string u = txtUsername.Text.Trim();
            string p = txtPassword.Text;

            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            {
                lblError.Text    = "Please enter both username and password.";
                btnLogin.Enabled = true;
                btnLogin.Text    = "Sign In";
                return;
            }

            try
            {
                var user = await FirestoreService.Instance.ValidateLoginAsync(u, p);
                if (user == null)
                {
                    lblError.Text = "Invalid username or password.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                SessionManager.Login(user);
                var main = new MainForm();
                main.FormClosed += (ms, me) =>
                {
                    // When MainForm closes (sign-out hid it then user exited), exit app
                    if (SessionManager.Current == null)
                        Application.Exit();
                };
                main.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Connection error: {ex.Message}";
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text    = "Sign In";
            }
        }

        private Label Lbl(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(90, 90, 90),
            AutoSize  = true
        };

        private TextBox Input() => new TextBox
        {
            Height      = 30,
            BorderStyle = BorderStyle.FixedSingle,
            Font        = new Font("Segoe UI", 10f)
        };
    }
}

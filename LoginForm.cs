using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class LoginForm : Form
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);

        private TextBox    txtUsername, txtPassword;
        private Label      lblError;
        private Button     btnLogin;

        public LoginForm()
        {
            Text            = "Legal Office – Calamba City";
            Size            = new Size(420, 520);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);

            // FIX (root cause #1): the previous code did
            //   this.Icon = new Icon("C:\Users\Admin\source\repos\NotaryApp\resources\logo.ico");
            // which is a hardcoded path that only exists on the original developer's machine.
            // Icon's constructor throws if the file is missing, so the app crashed at
            // startup on every other machine. Load relative to the executable and guard
            // with File.Exists so a missing icon is cosmetic, never fatal.
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
            if (File.Exists(iconPath))
                this.Icon = new Icon(iconPath);

            BuildUI();
        }

        private void BuildUI()
        {
            // Header panel
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 160,
                BackColor = Navy
            };
            var lblTitle = new Label
            {
                Text      = "LEGAL OFFICE\nCALAMBA CITY",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            };
            header.Controls.Add(lblTitle);

            // Form fields
            var pnlForm = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(40, 30, 40, 20)
            };

            var lblU = Lbl("Username");
            lblU.Location = new Point(40, 30);
            txtUsername = Input();
            txtUsername.Location = new Point(40, 52);
            txtUsername.Width    = 320;

            var lblP = Lbl("Password");
            lblP.Location = new Point(40, 100);
            txtPassword = Input();
            txtPassword.Location    = new Point(40, 122);
            txtPassword.Width       = 320;
            txtPassword.PasswordChar = '●';
            txtPassword.KeyDown     += (s, e) => { if (e.KeyCode == Keys.Enter) Login(); };

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

            // FIX (root cause #9 / security): the previous build permanently displayed
            // "Default: admin / admin123" on the production login screen, which hands
            // working admin credentials to anyone who opens the app. Removed.
            // (If a first-run setup hint is wanted later, show it only when the seeded
            // default admin password has never been changed — not unconditionally.)

            pnlForm.Controls.AddRange(new Control[]
                { lblU, txtUsername, lblP, txtPassword, lblError, btnLogin });

            this.Controls.Add(pnlForm);
            this.Controls.Add(header);
        }

        private void Login()
        {
            lblError.Text = "";
            string u = txtUsername.Text.Trim();
            string p = txtPassword.Text;

            if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            {
                lblError.Text = "Please enter both username and password.";
                return;
            }

            var user = DatabaseService.Instance.ValidateLogin(u, p);
            if (user == null)
            {
                lblError.Text     = "Invalid username or password.";
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            SessionManager.Login(user);
            this.DialogResult = DialogResult.OK;
            this.Close();
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
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class SignupForm : Form
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);

        private TextBox  txtFullName, txtUsername, txtPassword, txtConfirm;
        private ComboBox cboRole;
        private Label    lblError;

        public SignupForm()
        {
            Text            = "Create New Account";
            Size            = new Size(420, 560);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);

            BuildUI();
        }

        private void BuildUI()
        {
            // ── Header ──────────────────────────────────────────
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Navy
            };
            header.Controls.Add(new Label
            {
                Text      = "CREATE NEW ACCOUNT",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            });

            // ── Form body ────────────────────────────────────────
            var pnl = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(36, 20, 36, 20)
            };

            int y = 20;

            // Full Name
            pnl.Controls.Add(Lbl("Full Name", y));
            txtFullName = Input(); txtFullName.Location = new Point(36, y + 22); txtFullName.Width = 330;
            pnl.Controls.Add(txtFullName);
            y += 66;

            // Username
            pnl.Controls.Add(Lbl("Username", y));
            txtUsername = Input(); txtUsername.Location = new Point(36, y + 22); txtUsername.Width = 330;
            pnl.Controls.Add(txtUsername);
            y += 66;

            // Role
            pnl.Controls.Add(Lbl("Role", y));
            cboRole = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location      = new Point(36, y + 22),
                Width         = 330,
                Font          = new Font("Segoe UI", 10f),
                Height        = 30
            };
            cboRole.Items.AddRange(new object[] { "Staff", "Admin" });
            cboRole.SelectedIndex = 0;
            pnl.Controls.Add(cboRole);
            y += 66;

            // Password
            pnl.Controls.Add(Lbl("Password", y));
            txtPassword = Input(); txtPassword.Location = new Point(36, y + 22); txtPassword.Width = 330;
            txtPassword.PasswordChar = '●';
            pnl.Controls.Add(txtPassword);
            y += 66;

            // Confirm Password
            pnl.Controls.Add(Lbl("Confirm Password", y));
            txtConfirm = Input(); txtConfirm.Location = new Point(36, y + 22); txtConfirm.Width = 330;
            txtConfirm.PasswordChar = '●';
            txtConfirm.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) TryCreate(); };
            pnl.Controls.Add(txtConfirm);
            y += 66;

            // Error label
            lblError = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(163, 45, 45),
                Font      = new Font("Segoe UI", 8.5f),
                Location  = new Point(36, y),
                Size      = new Size(330, 18)
            };
            pnl.Controls.Add(lblError);
            y += 24;

            // Buttons
            var btnCreate = new Button
            {
                Text      = "Create Account",
                Location  = new Point(36, y),
                Size      = new Size(330, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Navy,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += (s, e) => TryCreate();
            pnl.Controls.Add(btnCreate);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Location  = new Point(36, y + 46),
                Size      = new Size(330, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(80, 80, 80),
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            pnl.Controls.Add(btnCancel);

            this.Controls.Add(pnl);
            this.Controls.Add(header);
        }

        private void TryCreate()
        {
            lblError.Text = "";

            string fullName = txtFullName.Text.Trim();
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string confirm  = txtConfirm.Text;
            string role     = cboRole.SelectedItem?.ToString() ?? "Staff";

            // Validation
            if (string.IsNullOrEmpty(fullName))
            { lblError.Text = "Full name is required."; return; }

            if (string.IsNullOrEmpty(username))
            { lblError.Text = "Username is required."; return; }

            if (username.Length < 3)
            { lblError.Text = "Username must be at least 3 characters."; return; }

            if (string.IsNullOrEmpty(password))
            { lblError.Text = "Password is required."; return; }

            if (password.Length < 6)
            { lblError.Text = "Password must be at least 6 characters."; return; }

            if (password != confirm)
            { lblError.Text = "Passwords do not match."; return; }

            try
            {
                var newUser = new AppUser
                {
                    Username = username,
                    FullName = fullName,
                    Role     = role,
                    IsActive = true
                };

                DatabaseService.Instance.CreateUser(newUser, password);

                // Log the account creation
                DatabaseService.Instance.InsertLog(
                    SessionManager.Current?.FullName ?? "Admin",
                    "Account Created",
                    $"Created {role} account for '{fullName}' (username: {username})");

                MessageBox.Show(
                    $"Account created successfully!\n\nName     : {fullName}\nUsername : {username}\nRole     : {role}",
                    "Account Created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("UNIQUE"))
            {
                lblError.Text = "Username already exists. Choose a different one.";
            }
            catch (Exception ex)
            {
                lblError.Text = $"Error: {ex.Message}";
            }
        }

        private Label Lbl(string text, int y) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(90, 90, 90),
            Location  = new Point(36, y),
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
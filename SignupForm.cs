using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class SignupForm : Form
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);

        private TextBox txtUsername, txtPassword, txtConfirm, txtFullName;
        private ComboBox cboRole;
        private Label lblError;

        public SignupForm()
        {
            Text            = "Create Account – Legal Office";
            Size            = new Size(420, 560);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);
            BuildUI();
        }

        private void BuildUI()
        {
            // Header
            var header = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = Navy };
            var lblTitle = new Label
            {
                Text      = "CREATE ACCOUNT\nLEGAL OFFICE",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill
            };
            header.Controls.Add(lblTitle);

            var pnlForm = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40, 20, 40, 20) };

            // Full Name
            var lblFull = Lbl("Full Name");
            lblFull.Location     = new Point(40, 16);
            txtFullName          = Input();
            txtFullName.Location = new Point(40, 38);
            txtFullName.Width    = 320;

            // Username
            var lblU = Lbl("Username");
            lblU.Location        = new Point(40, 80);
            txtUsername          = Input();
            txtUsername.Location = new Point(40, 102);
            txtUsername.Width    = 320;

            // Role
            var lblRole = Lbl("Role");
            lblRole.Location = new Point(40, 144);
            cboRole = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location      = new Point(40, 166),
                Width         = 320,
                Font          = new Font("Segoe UI", 10f)
            };
            cboRole.Items.AddRange(new object[] { "Staff", "Admin" });
            cboRole.SelectedIndex = 0;

            // Password
            var lblP = Lbl("Password");
            lblP.Location        = new Point(40, 208);
            txtPassword          = Input();
            txtPassword.Location = new Point(40, 230);
            txtPassword.Width    = 320;
            txtPassword.PasswordChar = '●';

            // Confirm Password
            var lblC = Lbl("Confirm Password");
            lblC.Location        = new Point(40, 272);
            txtConfirm           = Input();
            txtConfirm.Location  = new Point(40, 294);
            txtConfirm.Width     = 320;
            txtConfirm.PasswordChar = '●';

            // Error label
            lblError = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(163, 45, 45),
                Font      = new Font("Segoe UI", 8.5f),
                Location  = new Point(40, 330),
                Size      = new Size(320, 20)
            };

            // Buttons
            var btnCreate = new Button
            {
                Text      = "Create Account",
                Location  = new Point(40, 356),
                Size      = new Size(320, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Navy,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += BtnCreate_Click;

            var btnBack = new Button
            {
                Text      = "Back to Login",
                Location  = new Point(40, 404),
                Size      = new Size(320, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(80, 80, 80),
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnBack.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btnBack.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlForm.Controls.AddRange(new Control[]
            {
                lblFull, txtFullName,
                lblU, txtUsername,
                lblRole, cboRole,
                lblP, txtPassword,
                lblC, txtConfirm,
                lblError, btnCreate, btnBack
            });

            this.Controls.Add(pnlForm);
            this.Controls.Add(header);
        }

        private async void BtnCreate_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            string fullName = txtFullName.Text.Trim();
            string username = txtUsername.Text.Trim();
            string role     = cboRole.SelectedItem?.ToString() ?? "Staff";
            string password = txtPassword.Text;
            string confirm  = txtConfirm.Text;

            // Validation
            if (string.IsNullOrEmpty(fullName))
            {
                lblError.Text = "Full name is required.";
                return;
            }
            if (string.IsNullOrEmpty(username))
            {
                lblError.Text = "Username is required.";
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                lblError.Text = "Password is required.";
                return;
            }
            if (password != confirm)
            {
                lblError.Text = "Passwords do not match.";
                txtConfirm.Clear();
                txtConfirm.Focus();
                return;
            }
            if (password.Length < 6)
            {
                lblError.Text = "Password must be at least 6 characters.";
                return;
            }

            var btn = (Button)sender!;
            btn.Enabled = false;
            btn.Text    = "Creating…";

            try
            {
                var newUser = new AppUser
                {
                    Username = username,
                    FullName = fullName,
                    Role     = role,
                    IsActive = true
                };

                await FirestoreService.Instance.CreateUserAsync(newUser, password);

                await FirestoreService.Instance.InsertLogAsync(
                    SessionManager.Current?.FullName ?? "Admin",
                    "AccountCreate",
                    $"Created account: {username} ({role})",
                    "account");

                MessageBox.Show(
                    $"Account '{username}' created successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btn.Enabled = true;
                btn.Text    = "Create Account";
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
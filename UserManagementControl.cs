using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    /// <summary>
    /// Admin-only screen: view, edit, deactivate and create user accounts.
    /// Add this to MainForm as ucUsers and wire a nav button (admin only).
    /// </summary>
    public class UserManagementControl : UserControl
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);
        private DataGridView dgv;

        public UserManagementControl()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            BuildUI();
        }

        public void RefreshData() => LoadUsers();

        private void BuildUI()
        {
            // ── Toolbar ──────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.White,
                Padding   = new Padding(12, 8, 12, 8)
            };

            var btnNew = new Button
            {
                Text      = "+ New User",
                Location  = new Point(0, 4),
                Size      = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Navy,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Click += BtnNew_Click;

            var btnRefresh = new Button
            {
                Text      = "Refresh",
                Location  = new Point(108, 4),
                Size      = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(50, 50, 50),
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btnRefresh.Click += (s, e) => LoadUsers();

            toolbar.Controls.AddRange(new Control[] { btnNew, btnRefresh });

            // ── Grid ─────────────────────────────────────────────
            var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 8, 0, 0) };

            dgv = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                ReadOnly              = true,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                Font                  = new Font("Segoe UI", 9f),
                GridColor             = Color.FromArgb(230, 230, 230),
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                AllowUserToResizeRows = false,
                RowTemplate           = { Height = 36 }
            };
            dgv.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(220, 230, 255);
            dgv.DefaultCellStyle.SelectionForeColor     = Color.Black;
            dgv.DefaultCellStyle.Padding                = new Padding(4, 0, 4, 0);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 248);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles               = false;
            dgv.ColumnHeadersHeight                     = 36;
            dgv.ColumnHeadersHeightSizeMode             = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id",       Visible = false });
            dgv.Columns.Add("Username", "Username");
            dgv.Columns.Add("FullName", "Full Name");
            dgv.Columns.Add("Role",     "Role");
            dgv.Columns.Add("Active",   "Active");

            // Edit button
            var btnEditCol = new DataGridViewButtonColumn
            {
                Name = "Edit", HeaderText = "Edit",
                Text = "✎ Edit", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat, Width = 90
            };
            dgv.Columns.Add(btnEditCol);
            dgv.Columns["Edit"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            // Deactivate / Reactivate button
            var btnToggleCol = new DataGridViewButtonColumn
            {
                Name = "Toggle", HeaderText = "Deactivate",
                Text = "Deactivate", UseColumnTextForButtonValue = false,
                FlatStyle = FlatStyle.Flat, Width = 100
            };
            dgv.Columns.Add(btnToggleCol);
            dgv.Columns["Toggle"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.CellClick      += Dgv_CellClick;

            card.Controls.Add(dgv);
            this.Controls.Add(card);
            this.Controls.Add(toolbar);

            this.HandleCreated += (s, e) => LoadUsers();
        }

        private void LoadUsers()
        {
            dgv.Rows.Clear();
            var users = DatabaseService.Instance.GetAllUsers();
            foreach (var u in users)
            {
                int idx = dgv.Rows.Add(
                    u.Id, u.Username, u.FullName, u.Role,
                    u.IsActive ? "Yes" : "No");

                // Set toggle button text dynamically
                dgv.Rows[idx].Cells["Toggle"].Value = u.IsActive ? "Deactivate" : "Reactivate";

                // Dim deactivated rows
                if (!u.IsActive)
                    dgv.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 160);
            }
            if (dgv.Rows.Count == 0)
                dgv.Rows.Add(0, "—", "No users found", "—", "—");
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["Edit"].Index)
            {
                e.CellStyle.BackColor = Color.FromArgb(230, 241, 251);
                e.CellStyle.ForeColor = Color.FromArgb(24, 95, 165);
            }
            if (e.ColumnIndex == dgv.Columns["Toggle"].Index)
            {
                e.CellStyle.BackColor = Color.FromArgb(252, 235, 235);
                e.CellStyle.ForeColor = Color.FromArgb(163, 45, 45);
            }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            int    id       = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Id"].Value);
            string username = dgv.Rows[e.RowIndex].Cells["Username"].Value?.ToString() ?? "";
            string active   = dgv.Rows[e.RowIndex].Cells["Active"].Value?.ToString() ?? "Yes";
            if (id == 0) return;

            // ── EDIT ──────────────────────────────────────────────
            if (e.ColumnIndex == dgv.Columns["Edit"].Index)
            {
                var users = DatabaseService.Instance.GetAllUsers();
                var user  = users.Find(x => x.Id == id);
                if (user == null) return;

                var dlg = new EditUserDialog(user);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    DatabaseService.Instance.UpdateUser(id, dlg.FullName, dlg.Role, dlg.IsActive);
                    if (!string.IsNullOrEmpty(dlg.NewPassword))
                        DatabaseService.Instance.ChangePassword(id, dlg.NewPassword);

                    DatabaseService.Instance.InsertLog(
                        SessionManager.Current?.FullName ?? "Admin",
                        "AccountEdit",
                        $"Edited account for {username}",
                        "account");  // <-- category: account (hidden from staff)
                    LoadUsers();
                }
                return;
            }

            // ── DEACTIVATE / REACTIVATE ───────────────────────────
            if (e.ColumnIndex == dgv.Columns["Toggle"].Index)
            {
                bool isCurrentlyActive = active == "Yes";
                string verb = isCurrentlyActive ? "deactivate" : "reactivate";

                // Prevent admin from deactivating their own account
                if (id == SessionManager.Current?.Id && isCurrentlyActive)
                {
                    MessageBox.Show("You cannot deactivate your own account.",
                        "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show($"Are you sure you want to {verb} '{username}'?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                if (isCurrentlyActive)
                    DatabaseService.Instance.DeactivateUser(id);
                else
                    DatabaseService.Instance.UpdateUser(id,
                        dgv.Rows[e.RowIndex].Cells["FullName"].Value?.ToString() ?? "",
                        dgv.Rows[e.RowIndex].Cells["Role"].Value?.ToString() ?? "Staff",
                        true);

                DatabaseService.Instance.InsertLog(
                    SessionManager.Current?.FullName ?? "Admin",
                    isCurrentlyActive ? "AccountDeactivate" : "AccountReactivate",
                    $"{(isCurrentlyActive ? "Deactivated" : "Reactivated")} account: {username}",
                    "account");  // hidden from staff
                LoadUsers();
            }
        }

        private void BtnNew_Click(object? sender, EventArgs e)
        {
            var dlg = new NewUserDialog();
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var newUser = new AppUser
            {
                Username = dlg.Username,
                FullName = dlg.FullName,
                Role     = dlg.Role,
                IsActive = true
            };

            try
            {
                DatabaseService.Instance.CreateUser(newUser, dlg.Password);
                DatabaseService.Instance.InsertLog(
                    SessionManager.Current?.FullName ?? "Admin",
                    "AccountCreate",
                    $"Created account for {dlg.Username} ({dlg.Role})",
                    "account");  // hidden from staff activity view
                LoadUsers();
                MessageBox.Show($"Account '{dlg.Username}' created successfully.",
                    "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create user: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    //  EDIT USER DIALOG
    // ════════════════════════════════════════════════════════════
    public class EditUserDialog : Form
    {
        public string FullName    { get; private set; } = "";
        public string Role        { get; private set; } = "Staff";
        public bool   IsActive    { get; private set; } = true;
        public string NewPassword { get; private set; } = "";

        private TextBox   txtFullName, txtPassword;
        private ComboBox  cboRole;
        private CheckBox  chkActive;

        public EditUserDialog(AppUser u)
        {
            Text            = $"Edit User — {u.Username}";
            Size            = new Size(380, 310);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);
            BackColor       = Color.White;

            var lblFull = L("Full Name", 16);
            txtFullName = T(u.FullName, 38);

            var lblRole = L("Role", 90);
            cboRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, 112), Width = 340, Font = new Font("Segoe UI", 10f) };
            cboRole.Items.AddRange(new object[] { "Admin", "Staff" });
            cboRole.SelectedItem = u.Role;

            var lblPwd = L("New Password (leave blank to keep current)", 144);
            txtPassword = T("", 166);
            txtPassword.PasswordChar = '●';

            chkActive = new CheckBox { Text = "Account is active", Location = new Point(16, 206), AutoSize = true, Checked = u.IsActive };

            var btnSave   = Btn("Save",   Color.FromArgb(10, 26, 107), new Point(16, 236));
            var btnCancel = Btn("Cancel", Color.FromArgb(90, 90, 90),   new Point(136, 236));

            btnSave.Click += (_, __) =>
            {
                FullName    = txtFullName.Text.Trim();
                Role        = cboRole.SelectedItem?.ToString() ?? "Staff";
                IsActive    = chkActive.Checked;
                NewPassword = txtPassword.Text;
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { lblFull, txtFullName, lblRole, cboRole, lblPwd, txtPassword, chkActive, btnSave, btnCancel });
        }

        private Label   L(string t, int y) => new Label   { Text = t, Location = new Point(16, y), AutoSize = true, ForeColor = Color.FromArgb(80, 80, 80) };
        private TextBox T(string v, int y) => new TextBox { Text = v, Location = new Point(16, y), Width = 340, Font = new Font("Segoe UI", 10f) };
        private Button  Btn(string t, Color bg, Point loc) { var b = new Button { Text = t, Location = loc, Size = new Size(110, 34), FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }
    }

    // ════════════════════════════════════════════════════════════
    //  NEW USER DIALOG
    // ════════════════════════════════════════════════════════════
    public class NewUserDialog : Form
    {
        public string Username { get; private set; } = "";
        public string FullName { get; private set; } = "";
        public string Role     { get; private set; } = "Staff";
        public string Password { get; private set; } = "";

        private TextBox  txtUsername, txtFullName, txtPassword;
        private ComboBox cboRole;

        public NewUserDialog()
        {
            Text            = "Create New User";
            Size            = new Size(380, 310);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);
            BackColor       = Color.White;

            var lblUser = L("Username", 16);
            txtUsername = T("", 38);

            var lblFull = L("Full Name", 84);
            txtFullName = T("", 106);

            var lblRole = L("Role", 152);
            cboRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, 174), Width = 340, Font = new Font("Segoe UI", 10f) };
            cboRole.Items.AddRange(new object[] { "Admin", "Staff" });
            cboRole.SelectedIndex = 1;

            var lblPwd = L("Password", 206);
            txtPassword = T("", 228);
            txtPassword.PasswordChar = '●';

            var btnCreate = Btn("Create", Color.FromArgb(10, 26, 107), new Point(16, 234));
            var btnCancel = Btn("Cancel", Color.FromArgb(90, 90, 90),   new Point(136, 234));

            btnCreate.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Username and password are required.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Username    = txtUsername.Text.Trim();
                FullName    = txtFullName.Text.Trim();
                Role        = cboRole.SelectedItem?.ToString() ?? "Staff";
                Password    = txtPassword.Text;
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { lblUser, txtUsername, lblFull, txtFullName, lblRole, cboRole, lblPwd, txtPassword, btnCreate, btnCancel });
        }

        private Label   L(string t, int y) => new Label   { Text = t, Location = new Point(16, y), AutoSize = true, ForeColor = Color.FromArgb(80, 80, 80) };
        private TextBox T(string v, int y) => new TextBox { Text = v, Location = new Point(16, y), Width = 340, Font = new Font("Segoe UI", 10f) };
        private Button  Btn(string t, Color bg, Point loc) { var b = new Button { Text = t, Location = loc, Size = new Size(110, 34), FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }
    }
}

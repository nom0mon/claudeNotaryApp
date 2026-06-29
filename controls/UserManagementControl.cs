using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class UserManagementControl : UserControl
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);
        private DataGridView dgv;
        private List<AppUser> _users = new();

        public UserManagementControl()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            BuildUI();
        }

        public void RefreshData() => _ = LoadUsersAsync();

        private void BuildUI()
        {
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
            btnRefresh.Click += (s, e) => _ = LoadUsersAsync();
            toolbar.Controls.AddRange(new Control[] { btnNew, btnRefresh });

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

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "FsId", Visible = false });
            dgv.Columns.Add("Username", "Username");
            dgv.Columns.Add("FullName", "Full Name");
            dgv.Columns.Add("Role",     "Role");
            dgv.Columns.Add("Active",   "Active");

            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Edit", HeaderText = "Edit", Text = "✎ Edit",
                UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, Width = 90
            });
            dgv.Columns["Edit"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Toggle", HeaderText = "Status", Text = "Deactivate",
                UseColumnTextForButtonValue = false, FlatStyle = FlatStyle.Flat, Width = 100
            });
            dgv.Columns["Toggle"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            // ── NEW: Delete column ─────────────────────────────────
            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Delete", HeaderText = "Delete", Text = "🗑 Delete",
                UseColumnTextForButtonValue = true, FlatStyle = FlatStyle.Flat, Width = 90
            });
            dgv.Columns["Delete"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.CellClick      += Dgv_CellClick;

            card.Controls.Add(dgv);
            this.Controls.Add(card);
            this.Controls.Add(toolbar);

            this.HandleCreated += (s, e) => _ = LoadUsersAsync();
        }

        private async System.Threading.Tasks.Task LoadUsersAsync()
        {
            _users = await FirestoreService.Instance.GetAllUsersAsync();
            dgv.Rows.Clear();
            foreach (var u in _users)
            {
                int idx = dgv.Rows.Add(u.Id, u.Username, u.FullName, u.Role, u.IsActive ? "Yes" : "No");
                dgv.Rows[idx].Cells["Toggle"].Value = u.IsActive ? "Deactivate" : "Reactivate";
                if (!u.IsActive)
                    dgv.Rows[idx].DefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 160);
            }
            if (dgv.Rows.Count == 0)
                dgv.Rows.Add("", "—", "No users found", "—", "—");
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
            // ── NEW: Delete button styling ─────────────────────────
            if (e.ColumnIndex == dgv.Columns["Delete"].Index)
            {
                e.CellStyle.BackColor = Color.FromArgb(80, 20, 20);
                e.CellStyle.ForeColor = Color.White;
            }
        }

        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string fsId     = dgv.Rows[e.RowIndex].Cells["FsId"].Value?.ToString() ?? "";
            string username = dgv.Rows[e.RowIndex].Cells["Username"].Value?.ToString() ?? "";
            string active   = dgv.Rows[e.RowIndex].Cells["Active"].Value?.ToString() ?? "Yes";
            if (string.IsNullOrEmpty(fsId)) return;

            var user = _users.Find(x => x.Id == fsId);

            // ── EDIT ──────────────────────────────────────────────
            if (e.ColumnIndex == dgv.Columns["Edit"].Index && user != null)
            {
                var dlg = new EditUserDialog(user);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _ = FirestoreService.Instance.UpdateUserAsync(fsId, dlg.FullName, dlg.Role, dlg.IsActive);
                    if (!string.IsNullOrEmpty(dlg.NewPassword))
                        _ = FirestoreService.Instance.ChangePasswordAsync(fsId, dlg.NewPassword);
                    _ = FirestoreService.Instance.InsertLogAsync(
                            SessionManager.Current?.FullName ?? "Admin",
                            "AccountEdit", $"Edited account: {username}", "account");
                    _ = LoadUsersAsync();
                }
                return;
            }

            // ── DEACTIVATE / REACTIVATE ───────────────────────────
            if (e.ColumnIndex == dgv.Columns["Toggle"].Index)
            {
                bool isActive = active == "Yes";
                string verb   = isActive ? "deactivate" : "reactivate";

                if (fsId == SessionManager.Current?.Id && isActive)
                {
                    MessageBox.Show("You cannot deactivate your own account.",
                        "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show($"Are you sure you want to {verb} '{username}'?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                if (isActive)
                    _ = FirestoreService.Instance.DeactivateUserAsync(fsId);
                else
                    _ = FirestoreService.Instance.UpdateUserAsync(
                            fsId,
                            dgv.Rows[e.RowIndex].Cells["FullName"].Value?.ToString() ?? "",
                            dgv.Rows[e.RowIndex].Cells["Role"].Value?.ToString() ?? "Staff",
                            true);

                _ = FirestoreService.Instance.InsertLogAsync(
                        SessionManager.Current?.FullName ?? "Admin",
                        isActive ? "AccountDeactivate" : "AccountReactivate",
                        $"{(isActive ? "Deactivated" : "Reactivated")}: {username}",
                        "account");
                _ = LoadUsersAsync();
                return;
            }

            // ── DELETE ────────────────────────────────────────────
            if (e.ColumnIndex == dgv.Columns["Delete"].Index)
            {
                // Block self-deletion
                if (fsId == SessionManager.Current?.Id)
                {
                    MessageBox.Show("You cannot delete your own account.",
                        "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Two-step confirmation dialog
                var dlg = new DeleteUserConfirmDialog(username);
                if (dlg.ShowDialog() != DialogResult.OK) return;

                _ = DeleteUserAsync(fsId, username);
            }
        }

        private async System.Threading.Tasks.Task DeleteUserAsync(string fsId, string username)
        {
            try
            {
                await FirestoreService.Instance.DeleteUserAsync(fsId);
                await FirestoreService.Instance.InsertLogAsync(
                    SessionManager.Current?.FullName ?? "Admin",
                    "AccountDelete",
                    $"Permanently deleted account: {username}",
                    "account");
                await LoadUsersAsync();
                MessageBox.Show($"Account '{username}' has been permanently deleted.",
                    "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete user:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnNew_Click(object? sender, EventArgs e)
        {
            var dlg = new NewUserDialog();
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                await FirestoreService.Instance.CreateUserAsync(
                    new AppUser { Username = dlg.Username, FullName = dlg.FullName, Role = dlg.Role },
                    dlg.Password);

                await FirestoreService.Instance.InsertLogAsync(
                    SessionManager.Current?.FullName ?? "Admin",
                    "AccountCreate",
                    $"Created account: {dlg.Username} ({dlg.Role})",
                    "account");

                await LoadUsersAsync();
                MessageBox.Show($"Account '{dlg.Username}' created.",
                    "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create user:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    //  DELETE CONFIRMATION DIALOG  (two-step: type username)
    // ════════════════════════════════════════════════════════════
    public class DeleteUserConfirmDialog : Form
    {
        private readonly string _expectedUsername;
        private TextBox txtConfirm;
        private Button  btnDelete;

        public DeleteUserConfirmDialog(string username)
        {
            _expectedUsername = username;

            Text            = "Delete User — Confirm";
            Size            = new Size(400, 240);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);

            // Warning icon + header
            var lblWarning = new Label
            {
                Text      = "⚠  This action is permanent and cannot be undone.",
                Location  = new Point(16, 16),
                Size      = new Size(360, 22),
                ForeColor = Color.FromArgb(163, 45, 45),
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            var lblInstruction = new Label
            {
                Text      = $"Type the username  \"{username}\"  below to confirm deletion:",
                Location  = new Point(16, 50),
                Size      = new Size(360, 40),
                ForeColor = Color.FromArgb(50, 50, 50),
                Font      = new Font("Segoe UI", 9.5f)
            };

            txtConfirm = new TextBox
            {
                Location    = new Point(16, 96),
                Width       = 356,
                Font        = new Font("Segoe UI", 10.5f),
                PlaceholderText = "Type username here…"
            };
            txtConfirm.TextChanged += (s, e) =>
            {
                bool matches = txtConfirm.Text.Trim() == _expectedUsername;
                btnDelete.Enabled   = matches;
                btnDelete.BackColor = matches
                    ? Color.FromArgb(163, 45, 45)
                    : Color.FromArgb(200, 160, 160);
            };

            btnDelete = new Button
            {
                Text      = "🗑  Permanently Delete",
                Location  = new Point(16, 148),
                Size      = new Size(180, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 160, 160),   // disabled tint until username typed
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Enabled   = false
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Location  = new Point(204, 148),
                Size      = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(90, 90, 90),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
                { lblWarning, lblInstruction, txtConfirm, btnDelete, btnCancel });
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

        private TextBox  txtFullName, txtPassword;
        private ComboBox cboRole;
        private CheckBox chkActive;

        public EditUserDialog(AppUser u)
        {
            Text            = $"Edit User — {u.Username}";
            Size            = new Size(380, 310);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);
            BackColor       = Color.White;

            Controls.AddRange(new Control[]
            {
                L("Full Name", 16),            txtFullName = T(u.FullName, 38),
                L("Role", 90),
                cboRole     = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, 112), Width = 340, Font = new Font("Segoe UI", 10f) },
                L("New Password (leave blank to keep current)", 144),
                txtPassword = T("", 166),
                chkActive   = new CheckBox { Text = "Account is active", Location = new Point(16, 206), AutoSize = true, Checked = u.IsActive },
                Btn("Save",   Color.FromArgb(10, 26, 107), new Point(16, 236), OnSave),
                Btn("Cancel", Color.FromArgb(90, 90, 90),  new Point(136, 236), (_, __) => { DialogResult = DialogResult.Cancel; Close(); })
            });

            cboRole.Items.AddRange(new object[] { "Admin", "Staff" });
            cboRole.SelectedItem  = u.Role;
            txtPassword.PasswordChar = '●';
        }

        private void OnSave(object? s, EventArgs e)
        {
            FullName    = txtFullName.Text.Trim();
            Role        = cboRole.SelectedItem?.ToString() ?? "Staff";
            IsActive    = chkActive.Checked;
            NewPassword = txtPassword.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private Label   L(string t, int y) => new() { Text = t, Location = new Point(16, y), AutoSize = true, ForeColor = Color.FromArgb(80, 80, 80) };
        private TextBox T(string v, int y) => new() { Text = v, Location = new Point(16, y), Width = 340, Font = new Font("Segoe UI", 10f) };
        private Button  Btn(string t, Color bg, Point loc, EventHandler h)
        {
            var b = new Button { Text = t, Location = loc, Size = new Size(110, 34), FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += h;
            return b;
        }
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
            Size            = new Size(380, 400);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);
            BackColor       = Color.White;

            Controls.AddRange(new Control[]
            {
                L("Username", 16),  txtUsername = T("", 38),
                L("Full Name", 84), txtFullName = T("", 106),
                L("Role", 152),
                cboRole     = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(16, 174), Width = 340, Font = new Font("Segoe UI", 10f) },
                L("Password", 206), txtPassword = T("", 228),
                Btn("Create", Color.FromArgb(10, 26, 107), new Point(16, 275), OnCreate),
                Btn("Cancel", Color.FromArgb(90, 90, 90),  new Point(136, 275), (_, __) => { DialogResult = DialogResult.Cancel; Close(); })
            });

            cboRole.Items.AddRange(new object[] { "Admin", "Staff" });
            cboRole.SelectedIndex    = 1;
            txtPassword.PasswordChar = '●';
        }

        private void OnCreate(object? s, EventArgs e)
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
        }

        private Label   L(string t, int y) => new() { Text = t, Location = new Point(16, y), AutoSize = true, ForeColor = Color.FromArgb(80, 80, 80) };
        private TextBox T(string v, int y) => new() { Text = v, Location = new Point(16, y), Width = 340, Font = new Font("Segoe UI", 10f) };
        private Button  Btn(string t, Color bg, Point loc, EventHandler h)
        {
            var b = new Button { Text = t, Location = loc, Size = new Size(110, 34), FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.Click += h;
            return b;
        }
    }
}
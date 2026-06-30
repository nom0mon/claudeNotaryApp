using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;

namespace LegalOfficeApp
{
    public class TrackingControl : UserControl
    {
        private readonly Color Navy  = Color.FromArgb(10, 26, 107);
        private readonly Color Green = Color.FromArgb(22, 101, 52);

        private DataGridView     dgv;
        private TextBox          txtSearch;
        private ComboBox         cboStatus;
        private Label            lblSelected;
        private Button           btnBatchEmail;
        private Button           btnBatchDelete;

        private List<Submission> _currentSubmissions = new();
        private CancellationTokenSource? _filterCts; 

        public TrackingControl()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            BuildUI();
        }

        public void RefreshData() => _ = ApplyFilterAsync();

        private void BuildUI()
        {
            // ── Filter / action bar ─────────────────────────────
            var filterPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.White,
                Padding   = new Padding(12, 8, 12, 8)
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "Search by document",
                Width           = 210,
                Height          = 28,
                Font            = new Font("Segoe UI", 9.5f),
                BorderStyle     = BorderStyle.FixedSingle,
                Location        = new Point(0, 6)
            };
            txtSearch.TextChanged += (s, e) => _ = DebouncedApplyFilterAsync();

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 120,
                Font          = new Font("Segoe UI", 9.5f),
                Location      = new Point(218, 6)
            };
            cboStatus.Items.AddRange(new object[] { "All Status", "Pending", "Approved", "Rejected" });
            cboStatus.SelectedIndex         = 0;
            cboStatus.SelectedIndexChanged += (s, e) => _ = ApplyFilterAsync();

            var btnRefresh = BarBtn("Refresh", Navy, Color.White, new Point(346, 4));
            btnRefresh.Click += (s, e) => _ = ApplyFilterAsync();

            var btnExport = BarBtn("Export", Color.White, Color.FromArgb(50, 50, 50), new Point(426, 4));
            btnExport.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btnExport.Click += Export_Click;

            btnBatchEmail = new Button
            {
                Text      = "📧  Send Selected",
                Location  = new Point(506, 4),
                Size      = new Size(130, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Green,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Enabled   = false
            };
            btnBatchEmail.FlatAppearance.BorderSize = 0;
            btnBatchEmail.Click += BtnBatchEmail_Click;

            btnBatchDelete = new Button
            {
                Text      = "🗑  Delete Selected",
                Location  = new Point(644, 4),
                Size      = new Size(136, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(163, 45, 45),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Enabled   = false,
                Visible   = SessionManager.IsAdmin
            };
            btnBatchDelete.FlatAppearance.BorderSize = 0;
            btnBatchDelete.Click += BtnBatchDelete_Click;

            lblSelected = new Label
            {
                Text      = "Select rows to act on (max 10)",
                Location  = new Point(788, 10),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(120, 120, 120)
            };

            filterPanel.Controls.AddRange(new Control[]
            {
                txtSearch, cboStatus, btnRefresh, btnExport,
                btnBatchEmail, btnBatchDelete, lblSelected
            });

            // ── Grid ─────────────────────────────────────────────
            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(0, 8, 0, 0)
            };

            dgv = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                ReadOnly              = true,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = true,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                Font                  = new Font("Segoe UI", 9f),
                GridColor             = Color.FromArgb(230, 230, 230),
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                AllowUserToResizeRows = false,
                RowTemplate           = { Height = 36 }
            };
            dgv.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(210, 230, 255);
            dgv.DefaultCellStyle.SelectionForeColor     = Color.Black;
            dgv.DefaultCellStyle.Padding                = new Padding(4, 0, 4, 0);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 248);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles               = false;
            dgv.ColumnHeadersHeight                     = 36;
            dgv.ColumnHeadersHeightSizeMode             = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "FsId", Visible = false });
            dgv.Columns.Add("BookNo",    "Book #");
            dgv.Columns.Add("DocName",   "Document Name");
            dgv.Columns.Add("Submitted", "Date Submitted");
            dgv.Columns.Add("Status",    "Status");

            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Action", HeaderText = "Review",
                Text = "View / Review", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat, Width = 110
            });
            dgv.Columns["Action"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Edit", HeaderText = "Edit",
                Text = "✎ Edit", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat, Width = 72
            });
            dgv.Columns["Edit"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Delete", HeaderText = "Delete",
                Text = "🗑 Del", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat, Width = 72
            });
            dgv.Columns["Delete"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Email", HeaderText = "Email",
                Text = "📧", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat, Width = 50
            });
            dgv.Columns["Email"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            dgv.Columns["Email"].ToolTipText  = "Send this document by email";

            dgv.CellFormatting   += Dgv_CellFormatting;
            dgv.CellClick        += Dgv_CellClick;
            dgv.SelectionChanged += Dgv_SelectionChanged;

            card.Controls.Add(dgv);
            this.Controls.Add(card);
            this.Controls.Add(filterPanel);

            this.HandleCreated += (s, e) => _ = ApplyFilterAsync();
        }

        private async Task DebouncedApplyFilterAsync()
        {
            _filterCts?.Cancel();
            var cts = new CancellationTokenSource();
            _filterCts = cts;

            try
            {
                await Task.Delay(300, cts.Token);   // wait for typing to pause
                if (cts.IsCancellationRequested) return;
                await ApplyFilterAsync(cts.Token);
            }
            catch (TaskCanceledException)
            {
                // expected when a newer keystroke supersedes this call
            }
        }
        private async Task ApplyFilterAsync(CancellationToken token = default)
        {
            string search = txtSearch?.Text.Trim() ?? "";
            string status  = cboStatus?.SelectedItem?.ToString() ?? "All Status";

            string? statusFilter = status == "All Status" ? null : status;
            string? nameSearch   = string.IsNullOrEmpty(search) ? null : search;

            List<Submission> results;
            try
            {
                results = await FirestoreService.Instance
                    .GetSubmissionsAsync(statusFilter, nameSearch);
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested) return;
                MessageBox.Show($"Failed to load submissions:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // If a newer call has started since this one began, drop these stale results
            if (token.IsCancellationRequested) return;

            _currentSubmissions = results;

            bool isAdmin = SessionManager.IsAdmin;
            dgv.Columns["Action"].Visible = isAdmin;
            dgv.Columns["Edit"]  .Visible = isAdmin;
            dgv.Columns["Delete"].Visible = isAdmin;
            dgv.Columns["Email"] .Visible = true;

            dgv.Rows.Clear();

            foreach (var s in _currentSubmissions)
            {
                dgv.Rows.Add(
                    s.Id,
                    s.BookNumber,
                    s.DocumentName,
                    s.DateSubmitted.ToLocalTime().ToString("MMM dd, yyyy"),
                    s.Status);
            }

            if (dgv.Rows.Count == 0)
                dgv.Rows.Add("", "—", "No records found", "—", "—");

            Dgv_SelectionChanged(null, EventArgs.Empty);
        }

        // ── Selection changed ─────────────────────────────────────
        private void Dgv_SelectionChanged(object? sender, EventArgs e)
        {
            var ids   = GetSelectedIds();
            int count = ids.Count;

            bool tooMany = count > 10;

            btnBatchEmail.Enabled  = count > 0 && !tooMany;
            btnBatchDelete.Enabled = count > 0 && !tooMany && SessionManager.IsAdmin;

            if (count == 0)
            {
                lblSelected.Text      = "Select rows to act on (max 10)";
                lblSelected.ForeColor = Color.FromArgb(120, 120, 120);
                btnBatchEmail .BackColor = Green;
                btnBatchDelete.BackColor = Color.FromArgb(163, 45, 45);
            }
            else if (tooMany)
            {
                lblSelected.Text      = $"⚠  {count} selected — max is 10";
                lblSelected.ForeColor = Color.FromArgb(163, 45, 45);
                btnBatchEmail .BackColor = Color.FromArgb(163, 45, 45);
                btnBatchDelete.BackColor = Color.FromArgb(163, 45, 45);
            }
            else
            {
                lblSelected.Text      = $"{count} file{(count == 1 ? "" : "s")} selected";
                lblSelected.ForeColor = Green;
                btnBatchEmail .BackColor = Green;
                btnBatchDelete.BackColor = Color.FromArgb(163, 45, 45);
            }
        }

        // ── Batch email ───────────────────────────────────────────
        private void BtnBatchEmail_Click(object? sender, EventArgs e)
        {
            var ids      = GetSelectedIds();
            var selected = _currentSubmissions.Where(s => ids.Contains(s.Id)).ToList();
            if (selected.Count == 0 || selected.Count > 10) return;
            new SendEmailDialog(selected).ShowDialog();
        }

        // ── Batch delete ──────────────────────────────────────────
        private async void BtnBatchDelete_Click(object? sender, EventArgs e)
        {
            var ids      = GetSelectedIds();
            var selected = _currentSubmissions.Where(s => ids.Contains(s.Id)).ToList();
            if (selected.Count == 0) return;

            string nameList = string.Join("\n",
                selected.Take(5).Select(s => $"  • {s.DocumentName}") );
            if (selected.Count > 5)
                nameList += $"\n  … and {selected.Count - 5} more";

            if (MessageBox.Show(
                    $"Permanently delete {selected.Count} submission(s)?\n\n{nameList}\n\n" +
                    "Firestore records will be removed. MEGA files must be deleted separately.",
                    "Confirm Batch Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes) return;

            btnBatchDelete.Enabled = false;
            btnBatchDelete.Text    = "Deleting…";

            try
            {
                await FirestoreService.Instance.BatchDeleteSubmissionsAsync(ids.ToList());

                string docList = string.Join(", ", selected.Select(s => $"'{s.DocumentName}'"));
                await FirestoreService.Instance.InsertLogAsync(
                    SessionManager.Current?.FullName ?? "Admin",
                    "Deletion",
                    $"Batch deleted {selected.Count} submission(s): {docList}",
                    "file");

                MessageBox.Show(
                    $"{selected.Count} submission(s) deleted successfully.",
                    "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _ = ApplyFilterAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Batch delete failed:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBatchDelete.Enabled = SessionManager.IsAdmin;
                btnBatchDelete.Text    = "🗑  Delete Selected";
            }
        }

        // ── Load data ─────────────────────────────────────────────
       private async System.Threading.Tasks.Task ApplyFilterAsync()
        {
            string search = txtSearch?.Text.Trim() ?? "";
            string status = cboStatus?.SelectedItem?.ToString() ?? "All Status";

            string? statusFilter = status == "All Status" ? null : status;
            string? nameSearch   = string.IsNullOrEmpty(search) ? null : search;

            try
            {
                _currentSubmissions = await FirestoreService.Instance
                    .GetSubmissionsAsync(statusFilter, nameSearch);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load submissions:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool isAdmin = SessionManager.IsAdmin;
            dgv.Columns["Action"].Visible = isAdmin;
            dgv.Columns["Edit"]  .Visible = isAdmin;
            dgv.Columns["Delete"].Visible = isAdmin;
            dgv.Columns["Email"] .Visible = true;

            dgv.Rows.Clear();

            foreach (var s in _currentSubmissions)
            {
                dgv.Rows.Add(
                    s.Id,
                    s.BookNumber,
                    s.DocumentName,
                    s.DateSubmitted.ToLocalTime().ToString("MMM dd, yyyy"),
                    s.Status);
            }

            if (dgv.Rows.Count == 0)
                dgv.Rows.Add("", "—", "No records found", "—", "—");

            Dgv_SelectionChanged(null, EventArgs.Empty);
        }
        // ── Cell formatting ───────────────────────────────────────
        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;

            if (e.ColumnIndex == dgv.Columns["Status"].Index)
            {
                switch (e.Value.ToString())
                {
                    case "Pending":
                        e.CellStyle.ForeColor = Color.FromArgb(133, 79, 11);
                        e.CellStyle.BackColor = Color.FromArgb(250, 238, 218);
                        break;
                    case "Approved":
                        e.CellStyle.ForeColor = Color.FromArgb(58, 109, 17);
                        e.CellStyle.BackColor = Color.FromArgb(234, 243, 222);
                        break;
                    case "Rejected":
                        e.CellStyle.ForeColor = Color.FromArgb(163, 45, 45);
                        e.CellStyle.BackColor = Color.FromArgb(252, 235, 235);
                        break;
                }
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }

            if (e.ColumnIndex == dgv.Columns["Edit"].Index)
            {
                e.CellStyle.BackColor = Color.FromArgb(230, 241, 251);
                e.CellStyle.ForeColor = Color.FromArgb(24, 95, 165);
            }
            if (e.ColumnIndex == dgv.Columns["Delete"].Index)
            {
                e.CellStyle.BackColor = Color.FromArgb(252, 235, 235);
                e.CellStyle.ForeColor = Color.FromArgb(163, 45, 45);
            }
            if (e.ColumnIndex == dgv.Columns["Email"].Index)
            {
                e.CellStyle.BackColor = Color.FromArgb(230, 248, 237);
                e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
            }
        }

        // ── Cell click ────────────────────────────────────────────
        private async void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string fsId    = dgv.Rows[e.RowIndex].Cells["FsId"].Value?.ToString()    ?? "";
            string docName = dgv.Rows[e.RowIndex].Cells["DocName"].Value?.ToString() ?? "";
            string status  = dgv.Rows[e.RowIndex].Cells["Status"].Value?.ToString()  ?? "";
            if (string.IsNullOrEmpty(fsId)) return;

            var sub = _currentSubmissions.Find(x => x.Id == fsId);

            if (e.ColumnIndex == dgv.Columns["Action"].Index)
            {
                if (status == "Pending" && SessionManager.IsAdmin)
                {
                    var dlg = new ApprovalDialog(docName);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        string newStatus = dlg.Approved ? "Approved" : "Rejected";
                        string reviewer  = SessionManager.Current?.FullName ?? "Admin";
                        await FirestoreService.Instance.ReviewSubmissionAsync(
                            fsId, newStatus, dlg.Remarks, reviewer);
                        await FirestoreService.Instance.InsertLogAsync(
                            reviewer,
                            newStatus == "Approved" ? "Approval" : "Rejection",
                            $"{newStatus}: '{docName}'", "file");
                        _ = ApplyFilterAsync();
                    }
                }
                else if (sub != null)
                    ShowDetails(sub);
                return;
            }

            if (e.ColumnIndex == dgv.Columns["Edit"].Index && SessionManager.IsAdmin && sub != null)
            {
                var editDlg = new EditSubmissionDialog(sub);
                if (editDlg.ShowDialog() == DialogResult.OK)
                {
                    await FirestoreService.Instance.UpdateSubmissionAsync(editDlg.Updated);
                    await FirestoreService.Instance.InsertLogAsync(
                        SessionManager.Current?.FullName ?? "Admin",
                        "Edit", $"Edited '{docName}'", "file");
                    _ = ApplyFilterAsync();
                }
                return;
            }

            if (e.ColumnIndex == dgv.Columns["Delete"].Index && SessionManager.IsAdmin)
            {
                if (MessageBox.Show(
                        $"Permanently delete '{docName}'?\n\nThis removes the Firestore record.\n" +
                        "Delete the MEGA file separately.",
                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    != DialogResult.Yes) return;

                await FirestoreService.Instance.DeleteSubmissionAsync(fsId);
                await FirestoreService.Instance.InsertLogAsync(
                    SessionManager.Current?.FullName ?? "Admin",
                    "Deletion", $"Deleted '{docName}'", "file");
                _ = ApplyFilterAsync();
                return;
            }

            if (e.ColumnIndex == dgv.Columns["Email"].Index && sub != null)
            {
                var selectedIds = GetSelectedIds();
                if (selectedIds.Count > 1 && selectedIds.Contains(fsId))
                    BtnBatchEmail_Click(null, EventArgs.Empty);
                else
                    new SendEmailDialog(sub).ShowDialog();
            }
        }

        // ── Helpers ───────────────────────────────────────────────
        private HashSet<string> GetSelectedIds() =>
            dgv.SelectedRows
               .Cast<DataGridViewRow>()
               .Select(r => r.Cells["FsId"].Value?.ToString() ?? "")
               .Where(id => !string.IsNullOrEmpty(id))
               .ToHashSet();

        private static void ShowDetails(Submission sub)
        {
            string info =
                $"Document : {sub.DocumentName}\n"  +
                (string.IsNullOrEmpty(sub.BookNumber) ? "" : $"Book #   : {sub.BookNumber}\n") +
                $"Date     : {sub.DateOfCommission}\n" +
                $"File     : {sub.FileName}\n"         +
                $"Status   : {sub.Status}\n"           +
                $"Remarks  : {sub.Remarks}\n\n"        +
                (string.IsNullOrEmpty(sub.MegaLink)
                    ? "No MEGA link available."
                    : $"MEGA Link:\n{sub.MegaLink}");

            MessageBox.Show(info, $"Details — {sub.DocumentName}",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Export_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
                { Filter = "CSV|*.csv", FileName = "submissions.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            using var sw = new StreamWriter(dlg.FileName);
            sw.WriteLine("Document Name,Book #,Notary,Date Submitted,Status,MEGA Link");
            foreach (var s in _currentSubmissions)
                sw.WriteLine($"\"{s.DocumentName}\",\"{s.BookNumber}\",\"{s.NotaryName}\"," +
                             $"\"{s.DateSubmitted.ToLocalTime():MMM dd, yyyy}\"," +
                             $"\"{s.Status}\",\"{s.MegaLink}\"");
            MessageBox.Show("Exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Button BarBtn(string text, Color bg, Color fg, Point loc)
        {
            var b = new Button
            {
                Text      = text,
                Location  = loc,
                Size      = new Size(72, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = fg,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = bg == Color.White ? 1 : 0;
            return b;
        }
    }
    // ════════════════════════════════════════════════════════════
    //  APPROVAL DIALOG
    // ════════════════════════════════════════════════════════════
    public class ApprovalDialog : Form
    {
        public bool   Approved { get; private set; }
        public string Remarks  { get; private set; } = "";
        private TextBox txtRemarks;

        public ApprovalDialog(string docName)
        {
            Text            = $"Review — {docName}";
            Size            = new Size(380, 230);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);

            var lbl    = new Label { Text = $"Remarks for '{docName}' (optional):", Location = new Point(16, 16), Size = new Size(340, 20) };
            txtRemarks = new TextBox { Multiline = true, Location = new Point(16, 40), Size = new Size(340, 70), Font = new Font("Segoe UI", 9.5f) };

            var btnApprove = Btn("✔  Approve", Color.FromArgb(58, 96, 18));
            var btnReject  = Btn("✖  Reject",  Color.FromArgb(163, 45, 45));
            var btnCancel  = Btn("Cancel",      Color.FromArgb(90, 90, 90));

            btnApprove.Location = new Point(16,  124);
            btnReject .Location = new Point(136, 124);
            btnCancel .Location = new Point(256, 124);

            btnApprove.Click += (s, e) => { Approved = true;  Remarks = txtRemarks.Text; DialogResult = DialogResult.OK;     Close(); };
            btnReject .Click += (s, e) => { Approved = false; Remarks = txtRemarks.Text; DialogResult = DialogResult.OK;     Close(); };
            btnCancel .Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { lbl, txtRemarks, btnApprove, btnReject, btnCancel });
        }

        private Button Btn(string text, Color bg)
        {
            var b = new Button { Text = text, Size = new Size(110, 34), FlatStyle = FlatStyle.Flat,
                BackColor = bg, ForeColor = Color.White, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }

    // ════════════════════════════════════════════════════════════
    //  EDIT SUBMISSION DIALOG
    // ════════════════════════════════════════════════════════════
    public class EditSubmissionDialog : Form
    {
        public Submission Updated { get; private set; }
        private TextBox        txtBookNo, txtDocName;
        private DateTimePicker dtpCommission;

        public EditSubmissionDialog(Submission s)
        {
            Updated         = s;
            Text            = $"Edit — {s.DocumentName}";
            Size            = new Size(440, 280);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);
            BackColor       = Color.White;

            int[] ys = { 16, 70, 124 };

            Label L(string t, int y, bool req = false) => new Label
            {
                Text = t, Location = new Point(16, y), AutoSize = true,
                ForeColor = req ? Color.FromArgb(10, 26, 107) : Color.FromArgb(80, 80, 80)
            };
            TextBox T(string v, int y, int w = 394) => new TextBox
                { Text = v, Location = new Point(16, y + 20), Width = w, Font = new Font("Segoe UI", 10f) };

            var lblDocName = L("Document Name *", ys[0], req: true); txtDocName = T(s.DocumentName, ys[0]);
            var lblBook    = L("Book Number",     ys[1]);             txtBookNo  = T(s.BookNumber,   ys[1]);
            var lblDate    = L("Date of Commission *", ys[2], req: true);
            dtpCommission  = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short, Location = new Point(16, ys[2] + 20),
                Width  = 394, Font = new Font("Segoe UI", 10f)
            };
            if (DateTime.TryParse(s.DateOfCommission, out var doc)) dtpCommission.Value = doc;

            var btnSave   = AB("Save",   Color.FromArgb(10, 26, 107), new Point(16,  196));
            var btnCancel = AB("Cancel", Color.FromArgb(90, 90, 90),  new Point(136, 196));

            btnSave.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(txtDocName.Text))
                {
                    MessageBox.Show("Document Name is required.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Updated.DocumentName     = txtDocName.Text.Trim();
                Updated.BookNumber       = txtBookNo .Text.Trim();
                Updated.DateOfCommission = dtpCommission.Value.ToShortDateString();
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
                { lblDocName, txtDocName, lblBook, txtBookNo, lblDate, dtpCommission, btnSave, btnCancel });
        }

        private Button AB(string t, Color bg, Point loc)
        {
            var b = new Button { Text = t, Location = loc, Size = new Size(110, 34),
                FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}

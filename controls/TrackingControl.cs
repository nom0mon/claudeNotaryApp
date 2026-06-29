using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class TrackingControl : UserControl
    {
         private readonly Color Navy = Color.FromArgb(10, 26, 107);
        private DataGridView dgv;
        private TextBox      txtSearch;
        private ComboBox     cboStatus;

        // Cache for the current page so cell-click can look up the full object
        private List<Submission> _currentSubmissions = new();

        public TrackingControl()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            BuildUI();
        }

        public void RefreshData() => _ = ApplyFilterAsync();

        private void BuildUI()
        {
            // ── Filter bar ──────────────────────────────────────
            var filterPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.White,
                Padding   = new Padding(12, 8, 12, 8)
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "Search notary name…",
                Width           = 220,
                Height          = 28,
                Font            = new Font("Segoe UI", 9.5f),
                BorderStyle     = BorderStyle.FixedSingle,
                Location        = new Point(0, 6)
            };
            txtSearch.TextChanged += (s, e) => _ = ApplyFilterAsync();

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 130,
                Font          = new Font("Segoe UI", 9.5f),
                Location      = new Point(228, 6)
            };
            cboStatus.Items.AddRange(new object[] { "All Status", "Pending", "Approved", "Rejected" });
            cboStatus.SelectedIndex         = 0;
            cboStatus.SelectedIndexChanged += (s, e) => _ = ApplyFilterAsync();

            var btnRefresh = NavBtn("Refresh", Navy, Color.White);
            btnRefresh.Location = new Point(368, 4);
            btnRefresh.Click   += (s, e) => _ = ApplyFilterAsync();

            var btnExport = NavBtn("Export", Color.White, Color.FromArgb(50, 50, 50));
            btnExport.Location = new Point(448, 4);
            btnExport.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btnExport.Click += Export_Click;

            filterPanel.Controls.AddRange(new Control[] { txtSearch, cboStatus, btnRefresh, btnExport });

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
                MultiSelect            = true,
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

            // Hidden Firestore ID column
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "FsId", Visible = false });
            dgv.Columns.Add("BookNo",    "Book #");
            dgv.Columns.Add("DocName",   "Document Name");
            dgv.Columns.Add("Submitted", "Date Submitted");
            dgv.Columns.Add("Status",    "Status");

            // Action button
            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Action", HeaderText = "Review",
                Text = "View / Review", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat, Width = 110
            });
            dgv.Columns["Action"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            // Edit button (admin only)
            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Edit", HeaderText = "Edit",
                Text = "✎ Edit", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat, Width = 72
            });
            dgv.Columns["Edit"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            // Delete button (admin only)
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
                Text = "📧 Send", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat, Width = 84
            });
            dgv.Columns["Email"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.CellClick      += Dgv_CellClick;

            card.Controls.Add(dgv);
            this.Controls.Add(card);
            this.Controls.Add(filterPanel);

            this.HandleCreated += (s, e) => _ = ApplyFilterAsync();
        }

        // ── Fetch from Firestore ──────────────────────────────────
        private async System.Threading.Tasks.Task ApplyFilterAsync()
        {
            string search = txtSearch?.Text.Trim() ?? "";
            string status = cboStatus?.SelectedItem?.ToString() ?? "All Status";

            string? statusFilter = status == "All Status" ? null : status;
            string? nameSearch   = string.IsNullOrEmpty(search) ? null : search;

            _currentSubmissions = await FirestoreService.Instance
                .GetSubmissionsAsync(statusFilter, nameSearch);

            bool isAdmin = SessionManager.IsAdmin;
            dgv.Rows.Clear();

            // Hide columns for staff (do this once, not per row)
            if (!isAdmin)
            {
                dgv.Columns["Action"].Visible = false;
                dgv.Columns["Edit"]  .Visible = false;
                dgv.Columns["Delete"].Visible = false;
                dgv.Columns["Email"] .Visible = true;
            }
            else
            {
                dgv.Columns["Action"].Visible = true;
                dgv.Columns["Edit"]  .Visible = true;
                dgv.Columns["Delete"].Visible = true;
                dgv.Columns["Email"] .Visible = true;
            }

            foreach (var s in _currentSubmissions)
            {
                int idx = dgv.Rows.Add(
                    s.Id,
                    s.BookNumber,
                    s.DocumentName,
                    s.DateSubmitted.ToLocalTime().ToString("MMM dd, yyyy"),
                    s.Status);
            }

            if (dgv.Rows.Count == 0)
                dgv.Rows.Add("", "—", "No records found", "—", "—");
        }

        // ── Cell formatting ────────────────────────────────────────
        private async void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["Status"].Index && e.Value != null)
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
            // Collect all selected submissions (multi-select support)
                var selected = dgv.SelectedRows
                    .Cast<DataGridViewRow>()
                    .Select(r => r.Cells["FsId"].Value?.ToString() ?? "")
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(id => _currentSubmissions.Find(s => s.Id == id))
                    .Where(s => s != null)
                    .Cast<Submission>()
                    .ToList();

                if (selected.Count == 0) return;

                if (selected.Count > 10)
                {
                    MessageBox.Show(
                        "Maximum batch size is 10 documents. Please select up to 10 rows.",
                        "Batch Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var dlg = new SendEmailDialog(selected);
                dlg.ShowDialog();
                return;
            }
        }

        // ── Cell click ────────────────────────────────────────────
        private async void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string fsId   = dgv.Rows[e.RowIndex].Cells["FsId"].Value?.ToString() ?? "";
            string docName = dgv.Rows[e.RowIndex].Cells["DocName"].Value?.ToString() ?? "";
            string status  = dgv.Rows[e.RowIndex].Cells["Status"].Value?.ToString()  ?? "";
            if (string.IsNullOrEmpty(fsId)) return;

            var sub = _currentSubmissions.Find(x => x.Id == fsId);

// ── VIEW / REVIEW ──────────────────────────────────────────────
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
                                $"{newStatus}: '{docName}'",
                                "file");
                        _ = ApplyFilterAsync();
                    }
                }
                else if (sub != null)
                {
                    ShowDetails(sub);
                }
                return;
            }

            // ── EDIT ───────────────────────────────────────────────
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

            // ── DELETE ─────────────────────────────────────────────
            if (e.ColumnIndex == dgv.Columns["Delete"].Index && SessionManager.IsAdmin)
            {
                if (MessageBox.Show(
                        $"Permanently delete '{docName}'?\n\nThis removes the Firestore record.\n" +
                        "Delete the MEGA file separately from the MEGA app.",
                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    != DialogResult.Yes) return;

                await FirestoreService.Instance.DeleteSubmissionAsync(fsId);
                await FirestoreService.Instance.InsertLogAsync(
                        SessionManager.Current?.FullName ?? "Admin",
                        "Deletion", $"Deleted '{docName}'", "file");
                _ = ApplyFilterAsync();
            }

            // ── EMAIL ──────────────────────────────────────────────
            if (e.ColumnIndex == dgv.Columns["Email"].Index && sub != null)
            {
                var dlg = new SendEmailDialog(sub);
                dlg.ShowDialog();
                return;
            }
        }
        private static void ShowDetails(Submission sub)
        {
            string info =
                $"Document : {sub.DocumentName}\n"   +
                (string.IsNullOrEmpty(sub.BookNumber)  ? "" : $"Book #   : {sub.BookNumber}\n")  +
                (string.IsNullOrEmpty(sub.NotaryName)  ? "" : $"Notary   : {sub.NotaryName}\n")  +
                (string.IsNullOrEmpty(sub.PtrNumber)   ? "" : $"PTR      : {sub.PtrNumber}\n")   +
                (string.IsNullOrEmpty(sub.IbpNumber)   ? "" : $"IBP      : {sub.IbpNumber}\n")   +
                $"Date     : {sub.DateOfCommission}\n" +
                $"File     : {sub.FileName}\n"         +
                $"Status   : {sub.Status}\n"           +
                $"Remarks  : {sub.Remarks}\n\n"        +
                (string.IsNullOrEmpty(sub.MegaLink)
                    ? "No MEGA link available."
                    : $"MEGA Link:\n{sub.MegaLink}");

            MessageBox.Show(info, $"Submission Details — {sub.DocumentName}",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Export CSV ────────────────────────────────────────────
        private void Export_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "submissions.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            using var sw = new StreamWriter(dlg.FileName);
            sw.WriteLine("Document Name,Book #,Notary Name,PTR No.,Date Submitted,Status,MEGA Link");
            foreach (var s in _currentSubmissions)
            {
                sw.WriteLine($"\"{s.DocumentName}\",\"{s.BookNumber}\"," +
                             $"\"{s.DateSubmitted.ToLocalTime():MMM dd, yyyy}\"," +
                             $"\"{s.Status}\",\"{s.MegaLink}\"");
            }
            MessageBox.Show("Exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Button NavBtn(string text, Color bg, Color fg)
        {
            var b = new Button
            {
                Text      = text,
                Size      = new Size(72, 28),
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
            Size            = new Size(440, 280);   // shorter form
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);
            BackColor       = Color.White;

            int[] ys = { 16, 70, 124 };   // only 3 rows now

            Label L(string t, int y, bool req = false) => new Label
            {
                Text      = t,
                Location  = new Point(16, y),
                AutoSize  = true,
                ForeColor = req ? Color.FromArgb(10, 26, 107) : Color.FromArgb(80, 80, 80)
            };
            TextBox T(string v, int y, int x = 16, int w = 394) => new TextBox
                { Text = v, Location = new Point(x, y + 20), Width = w, Font = new Font("Segoe UI", 10f) };

            var lblDocName = L("Document Name *", ys[0], req: true); 
            txtDocName = T(s.DocumentName, ys[0]);
            
            var lblBook = L("Book Number", ys[1]);             
            txtBookNo = T(s.BookNumber, ys[1]);
            
            var lblDate = L("Date of Commission *", ys[2], req: true);
            dtpCommission = new DateTimePicker
            {
                Format   = DateTimePickerFormat.Short,
                Location = new Point(16, ys[2] + 20),
                Width    = 410,
                Font     = new Font("Segoe UI", 10f)
            };
            if (DateTime.TryParse(s.DateOfCommission, out var doc)) 
                dtpCommission.Value = doc;

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
                Updated.BookNumber       = txtBookNo.Text.Trim();
                Updated.DateOfCommission = dtpCommission.Value.ToShortDateString();
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (_, __) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                lblDocName, txtDocName, 
                lblBook, txtBookNo, 
                lblDate, dtpCommission,
                btnSave, btnCancel
            });
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

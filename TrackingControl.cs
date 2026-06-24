using System;
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

        public TrackingControl()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            BuildUI();
        }

        public void RefreshData() => ApplyFilter();

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
                PlaceholderText = "Search notary name...",
                Width           = 220,
                Height          = 28,
                Font            = new Font("Segoe UI", 9.5f),
                BorderStyle     = BorderStyle.FixedSingle,
                Location        = new Point(0, 6)
            };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 130,
                Font          = new Font("Segoe UI", 9.5f),
                Location      = new Point(228, 6)
            };
            cboStatus.Items.AddRange(new object[] { "All Status", "Pending", "Approved", "Rejected" });
            cboStatus.SelectedIndex         = 0;
            cboStatus.SelectedIndexChanged += (s, e) => ApplyFilter();

            var btnRefresh = NavBtn("Refresh", Navy, Color.White);
            btnRefresh.Location = new Point(368, 4);
            btnRefresh.Click   += (s, e) => ApplyFilter();

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

            // Hidden column to store submission ID
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
            dgv.Columns.Add("BookNo",    "Book #");
            dgv.Columns.Add("Notary",    "Notary Name");
            dgv.Columns.Add("PTR",       "PTR No.");
            dgv.Columns.Add("Submitted", "Date Submitted");
            dgv.Columns.Add("Status",    "Status");

            var btnCol = new DataGridViewButtonColumn
            {
                Name       = "Action",
                HeaderText = "Action",
                Text       = "View / Review",
                UseColumnTextForButtonValue = true,
                FlatStyle  = FlatStyle.Flat,
                Width      = 110
            };
            dgv.Columns.Add(btnCol);
            dgv.Columns["Action"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.CellClick      += Dgv_CellClick;

            card.Controls.Add(dgv);
            this.Controls.Add(card);
            this.Controls.Add(filterPanel);

            this.HandleCreated += (s, e) => ApplyFilter();
        }

        // ── Pull from DB with optional filters ───────────────────
        private void ApplyFilter()
        {
            string search = txtSearch?.Text.Trim() ?? "";
            string status = cboStatus?.SelectedItem?.ToString() ?? "All Status";

            string? statusFilter = status == "All Status" ? null : status;
            string? nameSearch   = string.IsNullOrEmpty(search) ? null : search;

            var submissions = DatabaseService.Instance.GetSubmissions(statusFilter, nameSearch);

            dgv.Rows.Clear();
            foreach (var s in submissions)
            {
                dgv.Rows.Add(
                    s.Id,
                    s.BookNumber,
                    s.NotaryName,
                    s.PtrNumber,
                    s.DateSubmitted.ToString("MMM dd, yyyy"),
                    s.Status);
            }

            if (dgv.Rows.Count == 0)
                dgv.Rows.Add(0, "—", "No records found", "—", "—", "—");
        }

        // ── Cell formatting ───────────────────────────────────────
        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex != dgv.Columns["Status"].Index || e.Value == null) return;
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

        // ── Row action button ──────────────────────────────────────
        private void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != dgv.Columns["Action"].Index) return;

            int    id     = Convert.ToInt32(dgv.Rows[e.RowIndex].Cells["Id"].Value);
            string bookNo = dgv.Rows[e.RowIndex].Cells["BookNo"].Value?.ToString() ?? "";
            string status = dgv.Rows[e.RowIndex].Cells["Status"].Value?.ToString()  ?? "";

            if (id == 0) return;   // empty-state row

            if (status == "Pending" && SessionManager.IsAdmin)
            {
                var dlg = new ApprovalDialog(bookNo);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string newStatus = dlg.Approved ? "Approved" : "Rejected";
                    string reviewer  = SessionManager.Current?.FullName ?? "Admin";

                    DatabaseService.Instance.ReviewSubmission(id, newStatus, dlg.Remarks, reviewer);
                    DatabaseService.Instance.InsertLog(
                        reviewer, newStatus == "Approved" ? "Approval" : "Rejection",
                        $"{newStatus} Book {bookNo}");

                    ApplyFilter();   // refresh grid from DB
                }
            }
            else
            {
                // Show details dialog
                var submissions = DatabaseService.Instance.GetSubmissions();
                var sub         = submissions.Find(x => x.Id == id);
                if (sub == null) return;

                string info =
                    $"Book #  : {sub.BookNumber}\n"        +
                    $"Notary  : {sub.NotaryName}\n"        +
                    $"PTR     : {sub.PtrNumber}\n"         +
                    $"IBP     : {sub.IbpNumber}\n"         +
                    $"Year    : {sub.YearCovered}\n"       +
                    $"File    : {sub.FileName}\n"          +
                    $"Status  : {sub.Status}\n"            +
                    $"Remarks : {sub.Remarks}\n\n"         +
                    (string.IsNullOrEmpty(sub.MegaLink)
                        ? "No MEGA link available."
                        : $"MEGA Link:\n{sub.MegaLink}");

                MessageBox.Show(info, $"Submission Details — {sub.BookNumber}",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ── Export ─────────────────────────────────────────────────
        private void Export_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "submissions.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            using var sw  = new StreamWriter(dlg.FileName);
            sw.WriteLine("ID,Book #,Notary Name,PTR No.,Date Submitted,Status,MEGA Link");
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                sw.WriteLine($"{row.Cells["Id"].Value}," +
                             $"{row.Cells["BookNo"].Value}," +
                             $"{row.Cells["Notary"].Value}," +
                             $"{row.Cells["PTR"].Value}," +
                             $"{row.Cells["Submitted"].Value}," +
                             $"{row.Cells["Status"].Value}");
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

    // ── Approval dialog ─────────────────────────────────────────
    public class ApprovalDialog : Form
    {
        public bool   Approved { get; private set; }
        public string Remarks  { get; private set; } = "";
        private TextBox txtRemarks;

        public ApprovalDialog(string bookNo)
        {
            Text            = $"Review — {bookNo}";
            Size            = new Size(380, 230);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);

            var lbl = new Label { Text = $"Remarks for {bookNo} (optional):", Location = new Point(16, 16), Size = new Size(340, 20) };
            txtRemarks  = new TextBox { Multiline = true, Location = new Point(16, 40), Size = new Size(340, 70), Font = new Font("Segoe UI", 9.5f) };

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
            var b = new Button { Text = text, Size = new Size(110, 34), FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}

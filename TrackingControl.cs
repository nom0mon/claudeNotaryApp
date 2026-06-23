using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class TrackingControl : UserControl
    {
        private readonly Color Navy  = Color.FromArgb(10, 26, 107);
        private DataGridView dgv;
        private TextBox txtSearch;
        private ComboBox cboStatus;

        public TrackingControl()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            BuildUI();
        }

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

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 130,
                Font          = new Font("Segoe UI", 9.5f),
                Location      = new Point(228, 6)
            };
            cboStatus.Items.AddRange(new object[] { "All Status", "Pending", "Approved", "Rejected" });
            cboStatus.SelectedIndex = 0;

            var btnSearch = new Button
            {
                Text      = "Search",
                Location  = new Point(368, 4),
                Size      = new Size(70, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Navy,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (s, e) => FilterGrid();

            var btnExport = new Button
            {
                Text      = "Export",
                Location  = new Point(448, 4),
                Size      = new Size(70, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(50, 50, 50),
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btnExport.Click += Export_Click;

            filterPanel.Controls.AddRange(new Control[] { txtSearch, cboStatus, btnSearch, btnExport });

            // ── DataGridView ─────────────────────────────────────
            var card = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(12, 8, 12, 8)
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
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                Font                  = new Font("Segoe UI", 9f),
                GridColor             = Color.FromArgb(230, 230, 230),
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                AllowUserToResizeRows = false
            };
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 230, 255);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(245, 245, 245);
            dgv.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;

            // Columns
            dgv.Columns.Add("BookNo",    "Book #");
            dgv.Columns.Add("Notary",    "Notary Name");
            dgv.Columns.Add("PTR",       "PTR No.");
            dgv.Columns.Add("Submitted", "Date Submitted");
            dgv.Columns.Add("Status",    "Status");

            // Button column
            var btnCol = new DataGridViewButtonColumn
            {
                Name       = "Action",
                HeaderText = "Action",
                Text       = "View",
                UseColumnTextForButtonValue = true,
                FlatStyle  = FlatStyle.Flat
            };
            dgv.Columns.Add(btnCol);

            LoadData();

            // Color status + button text
            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.CellClick      += Dgv_CellClick;

            card.Controls.Add(dgv);

            this.Controls.Add(card);
            this.Controls.Add(filterPanel);
        }

        private void LoadData()
        {
            dgv.Rows.Clear();
            dgv.Rows.Add("#45", "Atty. Maria Santos", "PTR-00123", "Jun 23, 2026", "Pending");
            dgv.Rows.Add("#44", "Atty. Jose Reyes",   "PTR-00098", "Jun 20, 2026", "Approved");
            dgv.Rows.Add("#43", "Atty. Ana Cruz",     "PTR-00077", "Jun 18, 2026", "Approved");
            dgv.Rows.Add("#42", "Atty. Pedro Lim",    "PTR-00055", "Jun 15, 2026", "Rejected");
            dgv.Rows.Add("#41", "Atty. Rosa Tan",     "PTR-00040", "Jun 10, 2026", "Rejected");
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
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
        }

        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string bookNo = dgv.Rows[e.RowIndex].Cells["BookNo"].Value?.ToString() ?? "";
            string status = dgv.Rows[e.RowIndex].Cells["Status"].Value?.ToString()  ?? "";

            if (e.ColumnIndex == dgv.Columns["Action"].Index)
            {
                if (status == "Pending")
                {
                    var dlg = new ApprovalDialog(bookNo);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        dgv.Rows[e.RowIndex].Cells["Status"].Value = dlg.Approved ? "Approved" : "Rejected";
                    }
                }
                else
                {
                    MessageBox.Show($"Book {bookNo}\nStatus: {status}", "Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void FilterGrid()
        {
            string search = txtSearch.Text.Trim().ToLower();
            string status = cboStatus.SelectedItem?.ToString() ?? "All Status";

            foreach (DataGridViewRow row in dgv.Rows)
            {
                bool nameMatch   = string.IsNullOrEmpty(search) || row.Cells["Notary"].Value?.ToString().ToLower().Contains(search) == true;
                bool statusMatch = status == "All Status"        || row.Cells["Status"].Value?.ToString() == status;
                row.Visible = nameMatch && statusMatch;
            }
        }

        private void Export_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "submissions.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            using var sw = new System.IO.StreamWriter(dlg.FileName);
            // Header
            sw.WriteLine("Book #,Notary Name,PTR No.,Date Submitted,Status");
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                sw.WriteLine($"{row.Cells["BookNo"].Value},{row.Cells["Notary"].Value},{row.Cells["PTR"].Value},{row.Cells["Submitted"].Value},{row.Cells["Status"].Value}");
            }
            MessageBox.Show("Exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // ── Simple Approve/Reject dialog ────────────────────────────
    public class ApprovalDialog : Form
    {
        public bool Approved { get; private set; }

        public ApprovalDialog(string bookNo)
        {
            this.Text            = $"Review — {bookNo}";
            this.Size            = new Size(360, 220);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;

            var lbl = new Label
            {
                Text      = $"Add remarks for {bookNo}:",
                Location  = new Point(16, 16),
                Size      = new Size(320, 20),
                Font      = new Font("Segoe UI", 9.5f)
            };

            var txt = new TextBox
            {
                Multiline = true,
                Location  = new Point(16, 42),
                Size      = new Size(320, 70),
                Font      = new Font("Segoe UI", 9.5f)
            };

            var Navy = Color.FromArgb(10, 26, 107);

            var btnApprove = new Button
            {
                Text      = "Approve",
                Location  = new Point(16, 126),
                Size      = new Size(90, 32),
                BackColor = Color.FromArgb(58, 96, 18),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            btnApprove.FlatAppearance.BorderSize = 0;
            btnApprove.Click += (s, e) => { Approved = true;  this.DialogResult = DialogResult.OK; this.Close(); };

            var btnReject = new Button
            {
                Text      = "Reject",
                Location  = new Point(116, 126),
                Size      = new Size(90, 32),
                BackColor = Color.FromArgb(163, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            btnReject.FlatAppearance.BorderSize = 0;
            btnReject.Click += (s, e) => { Approved = false; this.DialogResult = DialogResult.OK; this.Close(); };

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Location  = new Point(216, 126),
                Size      = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { lbl, txt, btnApprove, btnReject, btnCancel });
        }
    }
}

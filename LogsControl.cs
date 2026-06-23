using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class LogsControl : UserControl
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);
        private DataGridView dgv;
        private ComboBox cboAction;
        private DateTimePicker dtpFrom, dtpTo;

        public LogsControl()
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

            cboAction = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 120,
                Font          = new Font("Segoe UI", 9.5f),
                Location      = new Point(0, 6)
            };
            cboAction.Items.AddRange(new object[] { "All Actions", "Submission", "Approval", "Rejection", "Login" });
            cboAction.SelectedIndex = 0;

            var lblFrom = new Label { Text = "From:", Location = new Point(130, 10), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            dtpFrom = new DateTimePicker
            {
                Format   = DateTimePickerFormat.Short,
                Width    = 100,
                Location = new Point(168, 6),
                Value    = DateTime.Now.AddDays(-7),
                Font     = new Font("Segoe UI", 9.5f)
            };

            var lblTo = new Label { Text = "To:", Location = new Point(276, 10), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            dtpTo = new DateTimePicker
            {
                Format   = DateTimePickerFormat.Short,
                Width    = 100,
                Location = new Point(298, 6),
                Value    = DateTime.Now,
                Font     = new Font("Segoe UI", 9.5f)
            };

            var btnFilter = new Button
            {
                Text      = "Filter",
                Location  = new Point(408, 4),
                Size      = new Size(70, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Navy,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnFilter.FlatAppearance.BorderSize = 0;
            btnFilter.Click += (s, e) => FilterLogs();

            var btnExport = new Button
            {
                Text      = "Export CSV",
                Location  = new Point(488, 4),
                Size      = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(50, 50, 50),
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btnExport.Click += Export_Click;

            filterPanel.Controls.AddRange(new Control[] { cboAction, lblFrom, dtpFrom, lblTo, dtpTo, btnFilter, btnExport });

            // ── Log grid ─────────────────────────────────────────
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
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;

            dgv.Columns.Add("Timestamp", "Timestamp");
            dgv.Columns.Add("User",      "User");
            dgv.Columns.Add("Action",    "Action");
            dgv.Columns.Add("Details",   "Details");

            dgv.Columns["Timestamp"].FillWeight = 20;
            dgv.Columns["User"]     .FillWeight = 18;
            dgv.Columns["Action"]   .FillWeight = 15;
            dgv.Columns["Details"]  .FillWeight = 47;

            dgv.CellFormatting += Dgv_CellFormatting;

            LoadLogs();

            card.Controls.Add(dgv);
            this.Controls.Add(card);
            this.Controls.Add(filterPanel);
        }

        private void LoadLogs()
        {
            dgv.Rows.Clear();
            dgv.Rows.Add("Jun 23, 2026  9:14 AM",  "Atty. Maria Santos", "Submission", "Submitted Book #45");
            dgv.Rows.Add("Jun 23, 2026  8:30 AM",  "Admin Juan",         "Approval",   "Approved Book #44 — Atty. Jose Reyes");
            dgv.Rows.Add("Jun 23, 2026  8:00 AM",  "Admin Juan",         "Login",      "Logged into the system");
            dgv.Rows.Add("Jun 22, 2026  4:12 PM",  "Admin Juan",         "Rejection",  "Rejected Book #41 — incomplete documents");
            dgv.Rows.Add("Jun 22, 2026  2:45 PM",  "Atty. Pedro Lim",   "Submission", "Submitted Book #42");
            dgv.Rows.Add("Jun 20, 2026  10:05 AM", "Atty. Jose Reyes",  "Submission", "Submitted Book #44");
            dgv.Rows.Add("Jun 18, 2026  3:20 PM",  "Admin Juan",         "Approval",   "Approved Book #43 — Atty. Ana Cruz");
            dgv.Rows.Add("Jun 18, 2026  9:00 AM",  "Atty. Ana Cruz",    "Submission", "Submitted Book #43");
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["Action"].Index && e.Value != null)
            {
                switch (e.Value.ToString())
                {
                    case "Submission":
                        e.CellStyle.ForeColor = Color.FromArgb(133, 79, 11);
                        e.CellStyle.BackColor = Color.FromArgb(250, 238, 218);
                        break;
                    case "Approval":
                        e.CellStyle.ForeColor = Color.FromArgb(58, 109, 17);
                        e.CellStyle.BackColor = Color.FromArgb(234, 243, 222);
                        break;
                    case "Rejection":
                        e.CellStyle.ForeColor = Color.FromArgb(163, 45, 45);
                        e.CellStyle.BackColor = Color.FromArgb(252, 235, 235);
                        break;
                    case "Login":
                        e.CellStyle.ForeColor = Color.FromArgb(24, 95, 165);
                        e.CellStyle.BackColor = Color.FromArgb(230, 241, 251);
                        break;
                }
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
        }

        private void FilterLogs()
        {
            string action = cboAction.SelectedItem?.ToString() ?? "All Actions";
            foreach (DataGridViewRow row in dgv.Rows)
            {
                bool actionMatch = action == "All Actions" || row.Cells["Action"].Value?.ToString() == action;
                row.Visible = actionMatch;
            }
        }

        private void Export_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "activity_logs.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            using var sw = new System.IO.StreamWriter(dlg.FileName);
            sw.WriteLine("Timestamp,User,Action,Details");
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                sw.WriteLine($"{row.Cells["Timestamp"].Value},{row.Cells["User"].Value},{row.Cells["Action"].Value},{row.Cells["Details"].Value}");
            }
            MessageBox.Show("Logs exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class LogsControl : UserControl
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);

        private DataGridView  dgv;
        private ComboBox      cboAction;
        private DateTimePicker dtpFrom, dtpTo;

        // ISO timestamps for accurate date filtering
        private readonly (string Ts, string User, string Action, string Details)[] _allLogs =
        {
            ("2026-06-23 09:14", "Atty. Maria Santos", "Submission", "Submitted Book #45"),
            ("2026-06-23 08:30", "Admin Juan",         "Approval",   "Approved Book #44 — Atty. Jose Reyes"),
            ("2026-06-23 08:00", "Admin Juan",         "Login",      "Logged into the system"),
            ("2026-06-22 16:12", "Admin Juan",         "Rejection",  "Rejected Book #41 — incomplete documents"),
            ("2026-06-22 14:45", "Atty. Pedro Lim",   "Submission", "Submitted Book #42"),
            ("2026-06-20 10:05", "Atty. Jose Reyes",  "Submission", "Submitted Book #44"),
            ("2026-06-18 15:20", "Admin Juan",         "Approval",   "Approved Book #43 — Atty. Ana Cruz"),
            ("2026-06-18 09:00", "Atty. Ana Cruz",    "Submission", "Submitted Book #43"),
            ("2026-06-15 11:30", "Atty. Pedro Lim",   "Submission", "Submitted Book #41"),
            ("2026-06-10 08:45", "Admin Juan",         "Login",      "Logged into the system"),
        };

        public LogsControl()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            BuildUI();
        }

        private void BuildUI()
        {
            // ── Filter bar ──────────────────────────────────────────
            var filterCard = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 54,
                BackColor = Color.White,
                Padding   = new Padding(14, 10, 14, 10)
            };

            // Action combo
            var lblAction = new Label { Text = "Action:", Location = new Point(0, 8), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            cboAction = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 110,
                Font          = new Font("Segoe UI", 9.5f),
                Location      = new Point(50, 4)
            };
            cboAction.Items.AddRange(new object[] { "All Actions", "Submission", "Approval", "Rejection", "Login" });
            cboAction.SelectedIndex         = 0;
            cboAction.SelectedIndexChanged += (s, e) => ApplyFilter();

            // Date range
            var lblFrom = new Label { Text = "From:", Location = new Point(172, 8), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            dtpFrom = new DateTimePicker
            {
                Format   = DateTimePickerFormat.Short,
                Width    = 96,
                Location = new Point(210, 4),
                Value    = new DateTime(2026, 6, 1),
                Font     = new Font("Segoe UI", 9f)
            };
            dtpFrom.ValueChanged += (s, e) => ApplyFilter();

            var lblTo = new Label { Text = "To:", Location = new Point(314, 8), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            dtpTo = new DateTimePicker
            {
                Format   = DateTimePickerFormat.Short,
                Width    = 96,
                Location = new Point(336, 4),
                Value    = DateTime.Now,
                Font     = new Font("Segoe UI", 9f)
            };
            dtpTo.ValueChanged += (s, e) => ApplyFilter();

            var btnExport = new Button
            {
                Text      = "Export CSV",
                Location  = new Point(442, 2),
                Size      = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(50, 50, 50),
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            btnExport.Click += Export_Click;

            filterCard.Controls.AddRange(new Control[]
                { lblAction, cboAction, lblFrom, dtpFrom, lblTo, dtpTo, btnExport });

            // ── Grid ─────────────────────────────────────────────────
            var gridCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 8, 0, 0) };

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
            dgv.DefaultCellStyle.SelectionBackColor            = Color.FromArgb(220, 230, 255);
            dgv.DefaultCellStyle.SelectionForeColor            = Color.Black;
            dgv.DefaultCellStyle.Padding                       = new Padding(4, 0, 4, 0);
            dgv.ColumnHeadersDefaultCellStyle.BackColor        = Color.FromArgb(245, 245, 248);
            dgv.ColumnHeadersDefaultCellStyle.Font             = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding          = new Padding(4, 0, 4, 0);
            dgv.EnableHeadersVisualStyles                      = false;
            dgv.ColumnHeadersHeight                            = 36;
            dgv.ColumnHeadersHeightSizeMode                    = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.Columns.Add("Timestamp", "Timestamp");
            dgv.Columns.Add("User",      "User");
            dgv.Columns.Add("Action",    "Action");
            dgv.Columns.Add("Details",   "Details");

            dgv.Columns["Timestamp"].FillWeight = 18;
            dgv.Columns["User"]     .FillWeight = 20;
            dgv.Columns["Action"]   .FillWeight = 13;
            dgv.Columns["Details"]  .FillWeight = 49;

            dgv.CellFormatting += Dgv_CellFormatting;

            gridCard.Controls.Add(dgv);
            this.Controls.Add(gridCard);
            this.Controls.Add(filterCard);

            // Load data after controls are set up
            ApplyFilter();
        }

        // ── Filter — rebuild rows from master data ─────────────────
        private void ApplyFilter()
        {
            string action = cboAction.SelectedItem?.ToString() ?? "All Actions";
            DateTime from = dtpFrom.Value.Date;
            DateTime to   = dtpTo.Value.Date.AddDays(1).AddTicks(-1); // inclusive end-of-day

            dgv.Rows.Clear();

            foreach (var log in _allLogs)
            {
                bool actionOk = action == "All Actions" || log.Action == action;
                bool dateOk   = DateTime.TryParse(log.Ts, out var dt) && dt.Date >= from && dt.Date <= dtpTo.Value.Date;

                if (actionOk && dateOk)
                    dgv.Rows.Add(FmtTs(log.Ts), log.User, log.Action, log.Details);
            }
        }

        // ── Cell formatting ────────────────────────────────────────
        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex != dgv.Columns["Action"].Index || e.Value == null) return;
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

        // ── Export ─────────────────────────────────────────────────
        private void Export_Click(object sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "activity_logs.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            using var sw = new System.IO.StreamWriter(dlg.FileName);
            sw.WriteLine("Timestamp,User,Action,Details");
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;
                sw.WriteLine($"{row.Cells["Timestamp"].Value},{row.Cells["User"].Value}," +
                             $"{row.Cells["Action"].Value},{row.Cells["Details"].Value}");
            }
            MessageBox.Show("Logs exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string FmtTs(string raw) =>
            DateTime.TryParse(raw, out var dt) ? dt.ToString("MMM dd, yyyy  h:mm tt") : raw;
    }
}

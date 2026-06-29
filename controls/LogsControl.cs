using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class LogsControl : UserControl
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);
        private DataGridView   dgv;
        private ComboBox       cboAction;
        private DateTimePicker dtpFrom, dtpTo;

        public LogsControl()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            BuildUI();
        }

        public void RefreshData() => _ = ApplyFilterAsync();

        private void BuildUI()
        {
            var filterCard = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 54,
                BackColor = Color.White,
                Padding   = new Padding(14, 10, 14, 10)
            };

            var lblAction = new Label { Text = "Action:", Location = new Point(0, 8), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            cboAction = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 130,
                Font          = new Font("Segoe UI", 9.5f),
                Location      = new Point(52, 4)
            };

            if (SessionManager.IsAdmin)
            {
                cboAction.Items.AddRange(new object[]
                {
                    "All Actions", "Submission", "Approval", "Rejection",
                    "Edit", "Deletion", "Login", "Logout", "Warning",
                    "AccountCreate", "AccountEdit", "AccountDeactivate", "AccountReactivate"
                });
            }
            else
            {
                cboAction.Items.AddRange(new object[]
                {
                    "All Actions", "Submission", "Approval", "Rejection",
                    "Edit", "Deletion", "Warning"
                });
            }
            cboAction.SelectedIndex         = 0;
            cboAction.SelectedIndexChanged += (s, e) => _ = ApplyFilterAsync();

            var lblFrom = new Label { Text = "From:", Location = new Point(192, 8), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            dtpFrom = new DateTimePicker
            {
                Format   = DateTimePickerFormat.Short,
                Width    = 96,
                Location = new Point(230, 4),
                Value    = new DateTime(DateTime.Now.Year, 1, 1),
                Font     = new Font("Segoe UI", 9f)
            };
            dtpFrom.ValueChanged += (s, e) => _ = ApplyFilterAsync();

            var lblTo = new Label { Text = "To:", Location = new Point(334, 8), AutoSize = true, Font = new Font("Segoe UI", 9f) };
            dtpTo = new DateTimePicker
            {
                Format   = DateTimePickerFormat.Short,
                Width    = 96,
                Location = new Point(356, 4),
                Value    = DateTime.Now,
                Font     = new Font("Segoe UI", 9f)
            };
            dtpTo.ValueChanged += (s, e) => _ = ApplyFilterAsync();

            var btnExport = new Button
            {
                Text      = "Export CSV",
                Location  = new Point(462, 2),
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
            dgv.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(220, 230, 255);
            dgv.DefaultCellStyle.SelectionForeColor     = Color.Black;
            dgv.DefaultCellStyle.Padding                = new Padding(4, 0, 4, 0);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 248);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles               = false;
            dgv.ColumnHeadersHeight                     = 36;
            dgv.ColumnHeadersHeightSizeMode             = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgv.Columns.Add("Timestamp", "Timestamp");
            dgv.Columns.Add("User",      "User");
            dgv.Columns.Add("Action",    "Action");
            dgv.Columns.Add("Details",   "Details");

            dgv.Columns["Timestamp"].FillWeight = 20;
            dgv.Columns["User"]     .FillWeight = 18;
            dgv.Columns["Action"]   .FillWeight = 14;
            dgv.Columns["Details"]  .FillWeight = 48;

            dgv.CellFormatting += Dgv_CellFormatting;

            gridCard.Controls.Add(dgv);
            this.Controls.Add(gridCard);
            this.Controls.Add(filterCard);

            this.HandleCreated += (s, e) => _ = ApplyFilterAsync();
        }

        // Cache for export
        private System.Collections.Generic.List<ActivityLog> _logs = new();

        private async System.Threading.Tasks.Task ApplyFilterAsync()
        {
            string action = cboAction?.SelectedItem?.ToString() ?? "All Actions";

            string? actionFilter   = action == "All Actions" ? null : action;
            DateTime? from         = dtpFrom?.Value.Date;
            DateTime? to           = dtpTo?.Value.Date;
            string? categoryFilter = SessionManager.IsAdmin ? null : "file";
        
            _logs = await FirestoreService.Instance.GetLogsAsync(
                actionFilter, from, to, categoryFilter);

            if (!SessionManager.IsAdmin)
                list = list.Where(l => l.Action != "Login" && l.Action != "Logout").ToList();

            dgv.Rows.Clear();
            foreach (var log in _logs)
            {
                dgv.Rows.Add(
                    log.Timestamp.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt"),
                    log.User,
                    log.Action,
                    log.Details);
            }

            if (dgv.Rows.Count == 0)
                dgv.Rows.Add("—", "—", "—", "No logs found for the selected filter.");
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
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
                case "Edit":
                    e.CellStyle.ForeColor = Color.FromArgb(24, 95, 165);
                    e.CellStyle.BackColor = Color.FromArgb(230, 241, 251);
                    break;
                case "Deletion":
                    e.CellStyle.ForeColor = Color.FromArgb(120, 0, 0);
                    e.CellStyle.BackColor = Color.FromArgb(255, 220, 220);
                    break;
                case "Login": case "Logout":
                    e.CellStyle.ForeColor = Color.FromArgb(24, 95, 165);
                    e.CellStyle.BackColor = Color.FromArgb(230, 241, 251);
                    break;
                case "Warning":
                    e.CellStyle.ForeColor = Color.FromArgb(130, 60, 0);
                    e.CellStyle.BackColor = Color.FromArgb(255, 245, 220);
                    break;
                case "AccountCreate": case "AccountEdit":
                case "AccountDeactivate": case "AccountReactivate":
                    e.CellStyle.ForeColor = Color.FromArgb(80, 0, 120);
                    e.CellStyle.BackColor = Color.FromArgb(240, 228, 255);
                    break;
            }
            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        private void Export_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "activity_logs.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            using var sw = new System.IO.StreamWriter(dlg.FileName);
            sw.WriteLine("Timestamp,User,Action,Details");
            foreach (var log in _logs)
            {
                sw.WriteLine($"\"{log.Timestamp.ToLocalTime():MMM dd, yyyy  h:mm tt}\"," +
                             $"\"{log.User}\",\"{log.Action}\",\"{log.Details}\"");
            }
            MessageBox.Show("Logs exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

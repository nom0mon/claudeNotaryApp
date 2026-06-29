using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class SchedulingControl : UserControl
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);
        private DataGridView dgv;
        private ComboBox     cboStatus;
        private List<ScheduledEmail> _schedules = new();

        public SchedulingControl()
        {
            this.BackColor = Color.FromArgb(240, 240, 240);
            BuildUI();
        }

        public void RefreshData() => _ = LoadSchedulesAsync();

        private void BuildUI()
        {
            // ── Filter bar ───────────────────────────────────────
            var filterPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.White,
                Padding   = new Padding(12, 8, 12, 8)
            };

            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width         = 130,
                Font          = new Font("Segoe UI", 9.5f),
                Location      = new Point(0, 6)
            };
            cboStatus.Items.AddRange(new object[] { "All", "Pending", "Sent", "Cancelled" });
            cboStatus.SelectedIndex         = 0;
            cboStatus.SelectedIndexChanged += (s, e) => _ = LoadSchedulesAsync();

            var btnRefresh = NavBtn("Refresh", Navy, Color.White);
            btnRefresh.Location = new Point(140, 4);
            btnRefresh.Click   += (s, e) => _ = LoadSchedulesAsync();

            filterPanel.Controls.AddRange(new Control[] { cboStatus, btnRefresh });

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
            dgv.Columns.Add("Documents",  "Documents");
            dgv.Columns.Add("Recipient",  "Recipient");
            dgv.Columns.Add("ScheduledAt","Scheduled For");
            dgv.Columns.Add("CreatedBy",  "Created By");
            dgv.Columns.Add("Status",     "Status");

            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "Cancel", HeaderText = "Action",
                Text = "✖ Cancel", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat, Width = 90
            });
            dgv.Columns["Cancel"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.CellClick      += Dgv_CellClick;

            card.Controls.Add(dgv);
            this.Controls.Add(card);
            this.Controls.Add(filterPanel);

            this.HandleCreated += (s, e) => _ = LoadSchedulesAsync();
        }

        private async System.Threading.Tasks.Task LoadSchedulesAsync()
        {
            string status = cboStatus?.SelectedItem?.ToString() ?? "All";
            string? filter = status == "All" ? null : status;

            _schedules = await FirestoreService.Instance.GetSchedulesAsync(filter);

            dgv.Rows.Clear();
            foreach (var s in _schedules)
            {
                string docs = s.DocumentNames.Count == 1
                    ? s.DocumentNames[0]
                    : $"{s.DocumentNames[0]} (+{s.DocumentNames.Count - 1} more)";

                int idx = dgv.Rows.Add(
                    s.Id,
                    docs,
                    s.RecipientEmail,
                    s.ScheduledAt.ToLocalTime().ToString("MMM dd, yyyy  hh:mm tt"),
                    s.CreatedBy,
                    s.Status);

                // Hide cancel button for non-pending
                if (s.Status != "Pending")
                    dgv.Rows[idx].Cells["Cancel"].Value = "";
            }

            if (dgv.Rows.Count == 0)
                dgv.Rows.Add("", "—", "—", "No schedules found", "—", "—");
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == dgv.Columns["Status"].Index && e.Value != null)
            {
                switch (e.Value.ToString())
                {
                    case "Pending":
                        e.CellStyle.ForeColor = Color.FromArgb(133, 79, 11);
                        e.CellStyle.BackColor = Color.FromArgb(250, 238, 218);
                        break;
                    case "Sent":
                        e.CellStyle.ForeColor = Color.FromArgb(58, 109, 17);
                        e.CellStyle.BackColor = Color.FromArgb(234, 243, 222);
                        break;
                    case "Cancelled":
                        e.CellStyle.ForeColor = Color.FromArgb(163, 45, 45);
                        e.CellStyle.BackColor = Color.FromArgb(252, 235, 235);
                        break;
                }
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
            if (e.ColumnIndex == dgv.Columns["Cancel"].Index)
            {
                e.CellStyle.BackColor = Color.FromArgb(252, 235, 235);
                e.CellStyle.ForeColor = Color.FromArgb(163, 45, 45);
            }
        }

        private async void Dgv_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string fsId   = dgv.Rows[e.RowIndex].Cells["FsId"].Value?.ToString() ?? "";
            string status = dgv.Rows[e.RowIndex].Cells["Status"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(fsId) || status != "Pending") return;

            if (e.ColumnIndex == dgv.Columns["Cancel"].Index)
            {
                if (MessageBox.Show(
                        "Cancel this scheduled email? This cannot be undone.",
                        "Confirm Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    != DialogResult.Yes) return;

                await FirestoreService.Instance.CancelScheduleAsync(fsId);
                await FirestoreService.Instance.InsertLogAsync(
                    SessionManager.Current?.FullName ?? "",
                    "EmailScheduleCancelled",
                    $"Cancelled scheduled email (ID: {fsId})",
                    "file");
                _ = LoadSchedulesAsync();
            }
        }

        private Button NavBtn(string text, Color bg, Color fg)
        {
            var b = new Button
            {
                Text      = text,
                Size      = new Size(80, 30),
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
}
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class DashboardControl : UserControl
    {
        private readonly Color Navy  = Color.FromArgb(10, 26, 107);
        private readonly Color Amber = Color.FromArgb(201, 125, 0);
        private readonly Color Green = Color.FromArgb(58, 96, 18);
        private readonly Color Gray  = Color.FromArgb(85, 85, 85);

        private Label lblTotal, lblPending, lblApproved, lblRejected;
        private Panel chartCanvas;
        private int[] chartData = new int[12];
        private FlowLayoutPanel notifStack;
        private DataGridView    dgvRecent;

        public DashboardControl()
        {
            this.BackColor  = Color.FromArgb(240, 240, 240);
            this.AutoScroll = true;
            BuildUI();
        }

        public void RefreshData() => _ = RefreshDataAsync();

        private async System.Threading.Tasks.Task RefreshDataAsync()
        {
            await LoadCountsAsync();
            await LoadChartAsync();
            await LoadNotificationsAsync();
            await LoadRecentSubmissionsAsync();
        }

        // ════════════════════════════════════════════════════════
        //  BUILD UI
        // ════════════════════════════════════════════════════════
        private void BuildUI()
        {
            

            // ── Stat cards ──────────────────────────────────────
            var cardRow = new TableLayoutPanel
            {
                ColumnCount = 4, RowCount = 1,
                Dock        = DockStyle.Top, Height = 120,
                Padding     = new Padding(0, 0, 0, 12),
                BackColor   = Color.Transparent
            };
            for (int i = 0; i < 4; i++)
                cardRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            (var c0, lblTotal)    = StatCard("Total Documents", "📄", Navy);
            (var c1, lblPending)  = StatCard("Pending",         "📋", Amber);
            (var c2, lblApproved) = StatCard("Approved",        "✔",  Green);
            (var c3, lblRejected) = StatCard("Rejected",        "✖",  Gray);

            cardRow.Controls.Add(c0, 0, 0);
            cardRow.Controls.Add(c1, 1, 0);
            cardRow.Controls.Add(c2, 2, 0);
            cardRow.Controls.Add(c3, 3, 0);

            // ── Middle row ───────────────────────────────────────
            var midRow = new TableLayoutPanel
            {
                ColumnCount = 2, RowCount = 1,
                Dock        = DockStyle.Top, Height = 220,
                Padding     = new Padding(0, 0, 0, 12),
                BackColor   = Color.Transparent
            };
            midRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            midRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            midRow.Controls.Add(BuildChartCard(), 0, 0);
            midRow.Controls.Add(BuildNotifCard(), 1, 0);

            // ── Recent submissions ────────────────────────────────
            var tableCard = CardPanel("Recent Submissions", DockStyle.Top, 210);

            dgvRecent = new DataGridView
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
                RowTemplate           = { Height = 34 }
            };
            dgvRecent.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(220, 230, 255);
            dgvRecent.DefaultCellStyle.SelectionForeColor     = Color.Black;
            dgvRecent.DefaultCellStyle.Padding                = new Padding(4, 0, 4, 0);
            dgvRecent.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 248);
            dgvRecent.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvRecent.EnableHeadersVisualStyles               = false;
            dgvRecent.ColumnHeadersHeight                     = 34;
            dgvRecent.ColumnHeadersHeightSizeMode             = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            dgvRecent.Columns.Add("DocName",   "Document Name");
            dgvRecent.Columns.Add("Notary",    "Notary Name");
            dgvRecent.Columns.Add("Submitted", "Date Submitted");
            dgvRecent.Columns.Add("Status",    "Status");

            dgvRecent.CellFormatting += DgvRecent_CellFormatting;
            tableCard.Controls.Add(dgvRecent);

            this.Controls.Add(tableCard);
            this.Controls.Add(midRow);
            this.Controls.Add(cardRow);

            this.HandleCreated += (s, e) => _ = RefreshDataAsync();
        }

        // ════════════════════════════════════════════════════════
        //  DATA LOADERS
        // ════════════════════════════════════════════════════════
        private async System.Threading.Tasks.Task LoadCountsAsync()
        {
            var (total, pending, approved, rejected) =
                await FirestoreService.Instance.GetDashboardCountsAsync();

            lblTotal   .Text = total   .ToString();
            lblPending .Text = pending .ToString();
            lblApproved.Text = approved.ToString();
            lblRejected.Text = rejected.ToString();
        }

        private async System.Threading.Tasks.Task LoadChartAsync()
        {
            chartData = await FirestoreService.Instance
                .GetMonthlySubmissionsAsync(DateTime.Now.Year);
            chartCanvas?.Invalidate();
        }

        private async System.Threading.Tasks.Task LoadNotificationsAsync()
        {
            notifStack.Controls.Clear();

            var logs   = await FirestoreService.Instance.GetLogsAsync();

            if (!SessionManager.IsAdmin)
            {
                logs = logs
                    .Where(l => l.Category == "file")
                    .Where(l => l.Action != "Login" && l.Action != "Logout")
                    .ToList();
            }

            var recent = logs.Take(5).ToList();

            if (recent.Count == 0)
            {
                notifStack.Controls.Add(new Label
                {
                    Text      = "No recent activity.",
                    ForeColor = Color.Gray,
                    Font      = new Font("Segoe UI", 8.5f),
                    AutoSize  = true,
                    Margin    = new Padding(4, 8, 0, 0)
                });
                return;
            }

            foreach (var log in recent)
            {
                Color dot = log.Action switch
                {
                    "Approval"   => Color.FromArgb(58,  96,  18),
                    "Rejection"  => Color.FromArgb(163, 45,  45),
                    "Submission" => Color.FromArgb(201, 125, 0),
                    _            => Color.FromArgb(24,  95,  165)
                };
                var row    = new Panel { Width = 340, Height = 52 };
                var lblDot = new Label { Text = "●", ForeColor = dot,       Location = new Point(4, 6),  Size = new Size(16, 16), Font = new Font("Segoe UI", 10f) };
                var lblMsg = new Label { Text = log.Details,                 Location = new Point(24, 4),  Size = new Size(310, 28), Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(40, 40, 40) };
                var lblTm  = new Label { Text = log.Timestamp.ToLocalTime().ToString("MMM dd, yyyy  h:mm tt"), Location = new Point(24, 32), Size = new Size(310, 16), Font = new Font("Segoe UI", 7.5f), ForeColor = Color.Gray };
                row.Controls.AddRange(new Control[] { lblDot, lblMsg, lblTm });
                notifStack.Controls.Add(row);
            }
        }

        private async System.Threading.Tasks.Task LoadRecentSubmissionsAsync()
        {
            dgvRecent.Rows.Clear();
            var submissions = await FirestoreService.Instance.GetSubmissionsAsync();

            foreach (var s in submissions.Take(10))
            {
                dgvRecent.Rows.Add(
                    s.DocumentName,
                    s.NotaryName,
                    s.DateSubmitted.ToLocalTime().ToString("MMM dd, yyyy"),
                    s.Status);
            }

            if (dgvRecent.Rows.Count == 0)
                dgvRecent.Rows.Add("—", "No submissions yet", "—", "—");
        }

        // ════════════════════════════════════════════════════════
        //  UI BUILDERS
        // ════════════════════════════════════════════════════════
        private (Panel card, Label valueLabel) StatCard(string label, string icon, Color bg)
        {
            var card = new Panel { BackColor = bg, Margin = new Padding(0, 0, 8, 0), Dock = DockStyle.Fill };

            var lblValue = new Label
            {
                Text      = "—",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 26f, FontStyle.Bold),
                TextAlign = ContentAlignment.TopRight,
                Dock      = DockStyle.None,
                AutoSize  = false,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding   = new Padding(0, 8, 12, 0)
            };

            // Position lblValue dynamically on resize
            card.Resize += (s, e) =>
            {
                lblValue.Location = new Point(0, 8);
                lblValue.Size     = new Size(card.Width, 50);
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var fLabel = new Font("Segoe UI", 9f);
                using var brLabel = new SolidBrush(Color.White);
                // Draw label text at the bottom of the card
                g.DrawString(label, fLabel, brLabel, 12, card.Height - 28);
            };

            card.Controls.Add(lblValue);
            return (card, lblValue);
        }

        private Panel BuildChartCard()
        {
            var card = CardPanel("Monthly Submissions", DockStyle.Fill, 0);
            chartCanvas = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            chartCanvas.Paint  += PaintChart;
            chartCanvas.Resize += (s, e) => chartCanvas.Invalidate();
            card.Controls.Add(chartCanvas);
            return card;
        }

        private void PaintChart(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int padL = 36, padR = 12, padT = 16, padB = 30;
            int w      = chartCanvas.Width  - padL - padR;
            int h      = chartCanvas.Height - padT - padB;
            int maxVal = Math.Max(chartData.Max(), 1);

            using var gridPen = new Pen(Color.FromArgb(230, 230, 230), 1f);
            for (int i = 0; i <= 4; i++)
            {
                int yPos = padT + h - (h * i / 4);
                g.DrawLine(gridPen, padL, yPos, padL + w, yPos);
                using var fGrid = new Font("Segoe UI", 7.5f);
                using var brG   = new SolidBrush(Color.FromArgb(150, 150, 150));
                g.DrawString((maxVal * i / 4).ToString(), fGrid, brG, 2, yPos - 8);
            }

            string[] months = { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };
            int barW    = Math.Max(6, (w / 12) - 8);
            int spacing = (w - barW * 12) / 13;

            for (int i = 0; i < 12; i++)
            {
                int barH = chartData[i] == 0 ? 2 : (int)((double)chartData[i] / maxVal * h);
                int x    = padL + spacing + i * (barW + spacing);
                int y    = padT + h - barH;

                using var brush = new LinearGradientBrush(
                    new Rectangle(x, y, barW, Math.Max(barH, 1)),
                    Color.FromArgb(40, 60, 160), Color.FromArgb(10, 26, 107),
                    LinearGradientMode.Vertical);
                g.FillRectangle(brush, x, y, barW, barH);

                if (chartData[i] > 0)
                {
                    using var fVal = new Font("Segoe UI", 7f, FontStyle.Bold);
                    using var brV  = new SolidBrush(Color.FromArgb(10, 26, 107));
                    string vs  = chartData[i].ToString();
                    var    vsz = g.MeasureString(vs, fVal);
                    g.DrawString(vs, fVal, brV, x + (barW - vsz.Width) / 2, y - vsz.Height - 1);
                }

                using var fM  = new Font("Segoe UI", 7.5f);
                using var brM = new SolidBrush(Color.FromArgb(100, 100, 100));
                var msz = g.MeasureString(months[i], fM);
                g.DrawString(months[i], fM, brM, x + (barW - msz.Width) / 2, padT + h + 5);
            }
        }

        private Panel BuildNotifCard()
        {
            var card = CardPanel("Recent Activity", DockStyle.Fill, 0);
            notifStack = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                Padding       = new Padding(4)
            };
            card.Controls.Add(notifStack);
            return card;
        }

        private Panel CardPanel(string title, DockStyle dock, int height)
        {
            var card = new Panel
            {
                BackColor = Color.White, Dock = dock,
                Margin    = new Padding(0, 0, 0, 12),
                Padding   = new Padding(12, 8, 12, 8)
            };
            if (height > 0) card.Height = height;
            card.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Top,
                Height    = 28
            });
            return card;
        }

        private void DgvRecent_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex != dgvRecent.Columns["Status"].Index || e.Value == null) return;
            switch (e.Value.ToString())
            {
                case "Pending":
                    e.CellStyle.ForeColor = Color.FromArgb(133, 79,  11);
                    e.CellStyle.BackColor = Color.FromArgb(250, 238, 218);
                    break;
                case "Approved":
                    e.CellStyle.ForeColor = Color.FromArgb(58,  109, 17);
                    e.CellStyle.BackColor = Color.FromArgb(234, 243, 222);
                    break;
                case "Rejected":
                    e.CellStyle.ForeColor = Color.FromArgb(163, 45,  45);
                    e.CellStyle.BackColor = Color.FromArgb(252, 235, 235);
                    break;
            }
            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }
    }
}

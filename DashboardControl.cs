using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class DashboardControl : UserControl
    {
        private readonly Color Navy  = Color.FromArgb(10, 26, 107);
        private readonly Color Amber = Color.FromArgb(201, 125, 0);
        private readonly Color Green = Color.FromArgb(58, 96, 18);
        private readonly Color Gray  = Color.FromArgb(85, 85, 85);

        public DashboardControl()
        {
            this.BackColor  = Color.FromArgb(240, 240, 240);
            this.AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            // ── Stat cards row ──────────────────────────────────
            var cardRow = new TableLayoutPanel
            {
                ColumnCount = 4,
                RowCount    = 1,
                Dock        = DockStyle.Top,
                Height      = 120,
                Padding     = new Padding(0, 0, 0, 12),
                BackColor   = Color.Transparent
            };
            for (int i = 0; i < 4; i++)
                cardRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            cardRow.Controls.Add(StatCard("Total Documents", "67", "📄", Navy),  0, 0);
            cardRow.Controls.Add(StatCard("Pending",         "8",  "📋", Amber), 1, 0);
            cardRow.Controls.Add(StatCard("Approved",        "3",  "✔",  Green), 2, 0);
            cardRow.Controls.Add(StatCard("Rejected",        "7",  "✖",  Gray),  3, 0);

            // ── Middle row: chart + notifications ───────────────
            var midRow = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount    = 1,
                Dock        = DockStyle.Top,
                Height      = 210,
                Padding     = new Padding(0, 0, 0, 12),
                BackColor   = Color.Transparent
            };
            midRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            midRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));

            midRow.Controls.Add(BuildChartCard(), 0, 0);
            midRow.Controls.Add(BuildNotifCard(), 1, 0);

            // ── Recent submissions table ─────────────────────────
            var tableCard = CardPanel("Recent Submissions", DockStyle.Top, 200);

            var dgv = new DataGridView
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
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal
            };
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 230, 255);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;

            dgv.Columns.Add("BookNo",    "Book #");
            dgv.Columns.Add("Notary",    "Notary Name");
            dgv.Columns.Add("Submitted", "Date Submitted");
            dgv.Columns.Add("Status",    "Status");

            dgv.Rows.Add("#45", "Atty. Maria Santos", "Jun 23, 2026", "Pending");
            dgv.Rows.Add("#44", "Atty. Jose Reyes",   "Jun 20, 2026", "Approved");
            dgv.Rows.Add("#43", "Atty. Ana Cruz",     "Jun 18, 2026", "Approved");
            dgv.Rows.Add("#42", "Atty. Pedro Lim",    "Jun 15, 2026", "Rejected");

            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex == 3 && e.Value != null)
                {
                    switch (e.Value.ToString())
                    {
                        case "Pending":  e.CellStyle.ForeColor = Color.FromArgb(133, 79, 11);  break;
                        case "Approved": e.CellStyle.ForeColor = Color.FromArgb(58, 109, 17);  break;
                        case "Rejected": e.CellStyle.ForeColor = Color.FromArgb(163, 45, 45);  break;
                    }
                    e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
            };

            tableCard.Controls.Add(dgv);

            // Stack (reverse order for DockStyle.Top)
            this.Controls.Add(tableCard);
            this.Controls.Add(midRow);
            this.Controls.Add(cardRow);
        }

        // ── Stat card ────────────────────────────────────────────
        private Panel StatCard(string label, string value, string icon, Color bg)
        {
            var card = new Panel
            {
                BackColor = bg,
                Margin    = new Padding(0, 0, 8, 0),
                Dock      = DockStyle.Fill
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var fIcon  = new Font("Segoe UI", 20f);
                using var fLabel = new Font("Segoe UI", 9f);
                using var fVal   = new Font("Segoe UI", 26f, FontStyle.Bold);
                using var brDim  = new SolidBrush(Color.FromArgb(180, 255, 255, 255));
                using var brW    = new SolidBrush(Color.White);
                g.DrawString(icon,  fIcon,  brDim, 12, 14);
                g.DrawString(label, fLabel, brDim, 12, 52);
                var valSize = g.MeasureString(value, fVal);
                g.DrawString(value, fVal, brW, card.Width - valSize.Width - 12, 10);
            };
            return card;
        }

        // ── Custom bar chart (no external library) ───────────────
        private Panel BuildChartCard()
        {
            var card = CardPanel("Monthly Submissions", DockStyle.Fill, 0);

            string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            int[]    vals   = { 8, 12, 7, 15, 11, 14 };
            int      maxVal = 20; // Y-axis ceiling

            var canvas = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };

            canvas.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int padL = 36, padR = 12, padT = 12, padB = 30;
                int w = canvas.Width  - padL - padR;
                int h = canvas.Height - padT - padB;

                // Grid lines
                using var gridPen = new Pen(Color.FromArgb(230, 230, 230), 1f);
                for (int y = 0; y <= 4; y++)
                {
                    int yPos = padT + h - (h * y / 4);
                    g.DrawLine(gridPen, padL, yPos, padL + w, yPos);
                    using var fGrid = new Font("Segoe UI", 7.5f);
                    using var brG   = new SolidBrush(Color.FromArgb(160, 160, 160));
                    g.DrawString((maxVal * y / 4).ToString(), fGrid, brG, 2, yPos - 8);
                }

                // Bars
                int barCount  = vals.Length;
                int barW      = Math.Max(8, (w / barCount) - 10);
                int spacing   = (w - barW * barCount) / (barCount + 1);

                for (int i = 0; i < barCount; i++)
                {
                    int barH   = (int)((double)vals[i] / maxVal * h);
                    int x      = padL + spacing + i * (barW + spacing);
                    int y      = padT + h - barH;

                    // Bar fill
                    using var barBrush = new LinearGradientBrush(
                        new Rectangle(x, y, barW, barH),
                        Color.FromArgb(40, 60, 160),
                        Color.FromArgb(10, 26, 107),
                        LinearGradientMode.Vertical);
                    g.FillRectangle(barBrush, x, y, barW, barH);

                    // Value label above bar
                    using var fVal = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                    using var brV  = new SolidBrush(Color.FromArgb(10, 26, 107));
                    string vStr   = vals[i].ToString();
                    var    vSz    = g.MeasureString(vStr, fVal);
                    g.DrawString(vStr, fVal, brV, x + (barW - vSz.Width) / 2, y - vSz.Height - 1);

                    // Month label below bar
                    using var fMonth = new Font("Segoe UI", 8f);
                    using var brM    = new SolidBrush(Color.FromArgb(100, 100, 100));
                    var mSz = g.MeasureString(months[i], fMonth);
                    g.DrawString(months[i], fMonth, brM,
                        x + (barW - mSz.Width) / 2,
                        padT + h + 5);
                }
            };

            card.Controls.Add(canvas);
            return card;
        }

        // ── Notifications card ───────────────────────────────────
        private Panel BuildNotifCard()
        {
            var card = CardPanel("Notifications", DockStyle.Fill, 0);

            string[][] items =
            {
                new[] { "Book #45 submitted by Atty. Santos",       "Today, 9:14 AM",    "amber" },
                new[] { "Book #43 approved by Admin",               "Today, 8:30 AM",    "green" },
                new[] { "Book #41 rejected — incomplete documents", "Yesterday, 4:12 PM","red"   },
            };

            var stack = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                Padding       = new Padding(4)
            };

            foreach (var item in items)
            {
                Color dot = item[2] == "amber" ? Color.FromArgb(201, 125, 0)
                          : item[2] == "green" ? Color.FromArgb(58, 96, 18)
                          :                      Color.FromArgb(163, 45, 45);

                var row    = new Panel { Width = 300, Height = 52, Padding = new Padding(4) };
                var lblDot = new Label { Text = "●", ForeColor = dot,  Location = new Point(4, 6),  Size = new Size(16, 16), Font = new Font("Segoe UI", 10f) };
                var lblMsg = new Label { Text = item[0], ForeColor = Color.FromArgb(40, 40, 40), Location = new Point(24, 4),  Size = new Size(270, 30), Font = new Font("Segoe UI", 8.5f) };
                var lblTm  = new Label { Text = item[1], ForeColor = Color.Gray, Location = new Point(24, 32), Size = new Size(270, 16), Font = new Font("Segoe UI", 7.5f) };
                row.Controls.AddRange(new Control[] { lblDot, lblMsg, lblTm });
                stack.Controls.Add(row);
            }

            card.Controls.Add(stack);
            return card;
        }

        // ── Card wrapper ─────────────────────────────────────────
        private Panel CardPanel(string title, DockStyle dock, int height)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Dock      = dock,
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
    }
}

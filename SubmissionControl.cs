using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class SubmissionControl : UserControl
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);

        private TextBox txtName, txtPTR, txtIBP, txtBookNo, txtYear;
        private DateTimePicker dtpCommission;
        private Label lblAttachedFile;
        private string attachedFilePath = "";

        public SubmissionControl()
        {
            this.BackColor  = Color.FromArgb(240, 240, 240);
            this.AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            // ── Notary Info card ────────────────────────────────
            var infoCard = Card("Notary Information");

            var grid = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount    = 3,
                Dock        = DockStyle.Top,
                AutoSize    = true,
                Padding     = new Padding(0, 4, 0, 0)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            txtName       = AddField(grid, "Notary Full Name",      0, 0);
            txtPTR        = AddField(grid, "PTR Number",             0, 1);
            txtIBP        = AddField(grid, "IBP Number",             1, 0);
            dtpCommission = null; // built separately below
            txtBookNo     = AddField(grid, "Book Number",            2, 0);
            txtYear       = AddField(grid, "Year Covered",           2, 1);

            // Commission date picker
            var dtpWrapper = FieldWrapper("Date of Commission");
            dtpCommission = new DateTimePicker
            {
                Dock   = DockStyle.Fill,
                Format = DateTimePickerFormat.Long,
                Value  = DateTime.Now,
                Font   = new Font("Segoe UI", 9.5f)
            };
            dtpWrapper.Controls.Add(dtpCommission);
            grid.Controls.Add(dtpWrapper, 1, 1);

            infoCard.Controls.Add(grid);

            // ── Attachment card ─────────────────────────────────
            var attachCard = Card("Attach Notarial Book (PDF)");

            var uploadZone = new Panel
            {
                Dock        = DockStyle.Top,
                Height      = 100,
                BackColor   = Color.FromArgb(248, 248, 248),
                Cursor      = Cursors.Hand,
                Margin      = new Padding(0, 4, 0, 8)
            };
            uploadZone.Paint += (s, e) =>
            {
                var g   = e.Graphics;
                var pen = new Pen(Color.FromArgb(180, 180, 180), 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                g.DrawRectangle(pen, 2, 2, uploadZone.Width - 4, uploadZone.Height - 4);

                using var fIcon = new Font("Segoe UI", 22f);
                using var fText = new Font("Segoe UI", 9f);
                using var br    = new SolidBrush(Color.FromArgb(160, 160, 160));
                var icon = "📎";
                var sz   = g.MeasureString(icon, fIcon);
                g.DrawString(icon, fIcon, br, (uploadZone.Width - sz.Width) / 2, 10);
                var msg = "Click to browse or drag & drop PDF here";
                var msz = g.MeasureString(msg, fText);
                g.DrawString(msg, fText, br, (uploadZone.Width - msz.Width) / 2, 62);
            };
            uploadZone.Click += BrowseFile;

            lblAttachedFile = new Label
            {
                Text      = "No file selected.",
                Dock      = DockStyle.Top,
                Height    = 24,
                ForeColor = Color.Gray,
                Font      = new Font("Segoe UI", 8.5f)
            };

            attachCard.Controls.Add(lblAttachedFile);
            attachCard.Controls.Add(uploadZone);

            // ── Buttons ─────────────────────────────────────────
            var btnRow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 44,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 6, 0, 0)
            };

            var btnSubmit = Btn("  Submit", true);
            var btnDraft  = Btn("  Save Draft", false);
            btnSubmit.Click += Submit_Click;
            btnDraft .Click += SaveDraft_Click;

            btnRow.Controls.Add(btnSubmit);
            btnRow.Controls.Add(btnDraft);

            // Stack (reverse for Top dock)
            this.Controls.Add(btnRow);
            this.Controls.Add(attachCard);
            this.Controls.Add(infoCard);
        }

        // ── Field helpers ────────────────────────────────────────
        private TextBox AddField(TableLayoutPanel grid, string label, int row, int col)
        {
            var wrapper = FieldWrapper(label);
            var tb = new TextBox
            {
                Dock        = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 9.5f)
            };
            wrapper.Controls.Add(tb);
            grid.Controls.Add(wrapper, col, row);
            return tb;
        }

        private Panel FieldWrapper(string label)
        {
            var p = new Panel { Dock = DockStyle.Fill, Height = 58, Padding = new Padding(0, 0, 8, 8) };
            p.Controls.Add(new Label
            {
                Text      = label,
                Dock      = DockStyle.Top,
                Height    = 20,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(90, 90, 90)
            });
            return p;
        }

        private Panel Card(string title)
        {
            var card = new Panel
            {
                Dock      = DockStyle.Top,
                BackColor = Color.White,
                Padding   = new Padding(16, 12, 16, 12),
                Margin    = new Padding(0, 0, 0, 12),
                AutoSize  = true
            };
            card.Controls.Add(new Label
            {
                Text      = title,
                Dock      = DockStyle.Top,
                Height    = 28,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30)
            });
            return card;
        }

        private Button Btn(string text, bool primary)
        {
            var b = new Button
            {
                Text      = text,
                AutoSize  = true,
                Padding   = new Padding(10, 0, 10, 0),
                Height    = 34,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            if (primary)
            {
                b.BackColor = Navy;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0;
            }
            else
            {
                b.BackColor = Color.White;
                b.ForeColor = Color.FromArgb(50, 50, 50);
                b.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
                b.FlatAppearance.BorderSize  = 1;
            }
            return b;
        }

        // ── Events ───────────────────────────────────────────────
        private void BrowseFile(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "PDF Files|*.pdf",
                Title  = "Select Notarial Book PDF"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                attachedFilePath    = dlg.FileName;
                lblAttachedFile.Text      = "📎 " + Path.GetFileName(dlg.FileName);
                lblAttachedFile.ForeColor = Color.FromArgb(10, 26, 107);
            }
        }

        private void Submit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtPTR.Text)  ||
                string.IsNullOrWhiteSpace(txtBookNo.Text))
            {
                MessageBox.Show("Please fill in all required fields.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show($"Book {txtBookNo.Text} submitted successfully!\nStatus: Pending review.",
                "Submitted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearForm();
        }

        private void SaveDraft_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Draft saved.", "Draft", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearForm()
        {
            txtName.Clear(); txtPTR.Clear(); txtIBP.Clear();
            txtBookNo.Clear(); txtYear.Clear();
            attachedFilePath    = "";
            lblAttachedFile.Text      = "No file selected.";
            lblAttachedFile.ForeColor = Color.Gray;
        }
    }
}

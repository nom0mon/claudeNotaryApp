using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text.Json;
namespace LegalOfficeApp
{
    public class SubmissionControl : UserControl
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);

        private TextBox        txtName, txtPTR, txtIBP, txtBookNo, txtYear;
        private DateTimePicker dtpCommission;
        private Label          lblAttachedFile;
        private string         attachedFilePath = "";

        public SubmissionControl()
        {
            this.BackColor  = Color.FromArgb(240, 240, 240);
            this.AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            // ── Notary info card ─────────────────────────────────────
            var infoCard = Card("Notary Information", 240);

            // 2-column, 3-row grid for fields
            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 3,
                BackColor   = Color.White,
                Padding     = new Padding(0, 4, 0, 4)
            };
            for (int i = 0; i < 2; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            for (int i = 0; i < 3; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            txtName       = Field(grid, "Notary Full Name",   0, 0);
            txtPTR        = Field(grid, "PTR Number",         0, 1);
            txtIBP        = Field(grid, "IBP Number",         1, 0);
            DateField(grid,            "Date of Commission",  1, 1);
            txtBookNo     = Field(grid, "Book Number",        2, 0);
            txtYear       = Field(grid, "Year Covered",       2, 1);

            infoCard.Controls.Add(grid);

            // ── Attachment card ───────────────────────────────────────
            var attachCard = Card("Attach Notarial Book (PDF)", 170);

            var uploadZone = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(248, 249, 252),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 4, 0, 4)
            };
            uploadZone.Paint += DrawUploadZone;
            uploadZone.Click += BrowseFile;

            lblAttachedFile = new Label
            {
                Text      = "No file selected.",
                Dock      = DockStyle.Top,
                Height    = 22,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray
            };

            // Add in reverse (Top dock = last added appears topmost visually)
            attachCard.Controls.Add(lblAttachedFile);
            attachCard.Controls.Add(uploadZone);

            // ── Button row ────────────────────────────────────────────
            var btnRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 10, 0, 0)
            };

            var btnSubmit = Btn("Submit",     true);
            var btnDraft  = Btn("Save Draft", false);

            btnSubmit.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            btnDraft .Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            btnSubmit.Click   += Submit_Click;
            btnDraft .Click   += SaveDraft_Click;

            // Position anchored to right edge
            btnRow.Controls.Add(btnSubmit);
            btnRow.Controls.Add(btnDraft);
            btnRow.SizeChanged += (s, e) =>
            {
                btnSubmit.Location = new Point(btnRow.Width - 110, 10);
                btnDraft .Location = new Point(btnRow.Width - 225, 10);
            };

            // Stack cards (reverse order for DockStyle.Top)
            this.Controls.Add(btnRow);
            this.Controls.Add(attachCard);
            this.Controls.Add(infoCard);
        }

        // ── Card wrapper ─────────────────────────────────────────────
        private Panel Card(string title, int height)
        {
            var card = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = height,
                BackColor = Color.White,
                Padding   = new Padding(18, 14, 18, 10)
            };
            card.Controls.Add(new Label
            {
                Text      = title,
                Dock      = DockStyle.Top,
                Height    = 26,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30)
            });
            return card;
        }

        // ── Text field ───────────────────────────────────────────────
        private TextBox Field(TableLayoutPanel grid, string label, int row, int col)
        {
            var wrap = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(4, 4, 10, 0)
            };

            var lbl = new Label
            {
                Text      = label,
                Dock      = DockStyle.Top,
                Height    = 18,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            // Use a Panel as a styled border container so the TextBox
            // fills naturally and is always clickable/focusable
            var border = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 28,
                BackColor = Color.FromArgb(200, 200, 220),
                Padding   = new Padding(1)
            };

            var tb = new TextBox
            {
                Dock        = DockStyle.Fill,
                BorderStyle = BorderStyle.None,         // border from parent panel
                Font        = new Font("Segoe UI", 10f),
                BackColor   = Color.White,
                ForeColor   = Color.FromArgb(20, 20, 20),
                TabStop     = true
            };

            // Make clicking anywhere in the wrapper focus the textbox
            lbl .Click += (s, e) => tb.Focus();
            wrap.Click += (s, e) => tb.Focus();

            border.Controls.Add(tb);
            wrap.Controls.Add(border);
            wrap.Controls.Add(lbl);
            grid.Controls.Add(wrap, col, row);
            return tb;
        }

        // ── Date field ───────────────────────────────────────────────
        private void DateField(TableLayoutPanel grid, string label, int row, int col)
        {
            var wrap = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(4, 4, 10, 0)
            };

            var lbl = new Label
            {
                Text      = label,
                Dock      = DockStyle.Top,
                Height    = 18,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 100, 100)
            };

            dtpCommission = new DateTimePicker
            {
                Dock   = DockStyle.Top,
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Now,
                Font   = new Font("Segoe UI", 10f),
                Height = 28
            };

            wrap.Controls.Add(dtpCommission);
            wrap.Controls.Add(lbl);
            grid.Controls.Add(wrap, col, row);
        }

        // ── Button helper ─────────────────────────────────────────────
        private Button Btn(string text, bool primary)
        {
            var b = new Button
            {
                Text      = text,
                Width     = 105,
                Height    = 32,
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

        // ── Upload zone paint ─────────────────────────────────────────
        private void DrawUploadZone(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            var g = e.Graphics;
            var pen = new Pen(Color.FromArgb(170, 170, 210), 1.5f)
                { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            g.DrawRectangle(pen, 2, 2, p.Width - 5, p.Height - 5);
            pen.Dispose();

            using var fIcon = new Font("Segoe UI", 18f);
            using var fTxt  = new Font("Segoe UI", 9f);
            using var br    = new SolidBrush(Color.FromArgb(130, 130, 160));

            string icon = "📄";
            var isz = g.MeasureString(icon, fIcon);
            g.DrawString(icon, fIcon, br, (p.Width - isz.Width) / 2f, 8f);

            string msg = "Click to browse or drag & drop PDF here";
            var msz = g.MeasureString(msg, fTxt);
            g.DrawString(msg, fTxt, br, (p.Width - msz.Width) / 2f, isz.Height + 10f);
        }

        // ── Events ───────────────────────────────────────────────────
        private void BrowseFile(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter      = "PDF Files|*.pdf|All Files|*.*",
                Title       = "Select Notarial Book PDF",
                Multiselect = false
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                attachedFilePath        = dlg.FileName;
                var info                = new FileInfo(dlg.FileName);
                lblAttachedFile.Text      = $"📎  {info.Name}  ({FormatSize(info.Length)})";
                lblAttachedFile.ForeColor = Navy;
            }
        }

        private async void Submit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtPTR.Text)  ||
                string.IsNullOrWhiteSpace(txtBookNo.Text))
            {
                MessageBox.Show("Please fill in Notary Name, PTR Number, and Book Number.",
                    "Required Fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(attachedFilePath) || !File.Exists(attachedFilePath))
            {
                MessageBox.Show("Please attach a valid notarial book PDF before submitting.",
                    "No File Attached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
try
    {
        // Disable submit button to prevent double-clicks
        var btnSubmit = (Button)sender;
        btnSubmit.Enabled = false;

        lblAttachedFile.Text      = "⏫  Connecting to MEGA...";
        lblAttachedFile.ForeColor = Color.FromArgb(10, 26, 107);

        // Load credentials from appsettings.json
        var json     = File.ReadAllText("appsettings.json");
        var config   = System.Text.Json.JsonDocument.Parse(json)
                           .RootElement.GetProperty("Mega");
        string email    = config.GetProperty("Email").GetString()!;
        string password = config.GetProperty("Password").GetString()!;

        var mega = new MegaService();

        // Track upload progress on the label
        mega.OnUploadProgress = pct =>
            this.Invoke(() =>
                lblAttachedFile.Text = $"⏫  Uploading... {pct:F0}%");

        await mega.LoginAsync(email, password);

        string downloadLink = await mega.UploadPdfAsync(attachedFilePath, txtBookNo.Text);

        await mega.LogoutAsync();

        lblAttachedFile.Text      = "✔  Upload complete!";
        lblAttachedFile.ForeColor = Color.FromArgb(58, 96, 18);

        MessageBox.Show(
            $"Book {txtBookNo.Text} submitted and uploaded to MEGA!\n\n" +
            $"Notary : {txtName.Text}\n"  +
            $"PTR    : {txtPTR.Text}\n"   +
            $"File   : {Path.GetFileName(attachedFilePath)}\n\n" +
            $"MEGA link:\n{downloadLink}\n\n" +
            $"Status : Pending review.",
            "Submitted", MessageBoxButtons.OK, MessageBoxIcon.Information);

        ClearForm();
        btnSubmit.Enabled = true;
    }
    catch (FileNotFoundException)
    {
        MessageBox.Show("appsettings.json not found. Please create it with your MEGA credentials.",
            "Config Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
        ResetFileLabel();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Upload failed:\n{ex.Message}",
            "MEGA Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        lblAttachedFile.Text      = "⚠  Upload failed. File still attached locally.";
        lblAttachedFile.ForeColor = Color.FromArgb(163, 45, 45);
        ((Button)sender).Enabled  = true;
    }
}

        private void ResetFileLabel()
        {
            lblAttachedFile.Text      = "No file selected.";
            lblAttachedFile.ForeColor = Color.Gray;
            ((Button)FindForm()?.ActiveControl!).Enabled = true;
        }

        private void SaveDraft_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Draft saved. You can return to complete this submission.",
                "Draft Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearForm()
        {
            txtName.Clear(); txtPTR.Clear(); txtIBP.Clear();
            txtBookNo.Clear(); txtYear.Clear();
            dtpCommission.Value   = DateTime.Now;
            attachedFilePath      = "";
            lblAttachedFile.Text      = "No file selected.";
            lblAttachedFile.ForeColor = Color.Gray;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)        return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024} KB";
            return $"{bytes / (1024 * 1024)} MB";
        }
    }
}

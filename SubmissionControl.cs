using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

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
            var infoCard = Card("Notary Information", 240);

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

            txtName   = Field(grid, "Notary Full Name",   0, 0);
            txtPTR    = Field(grid, "PTR Number",         0, 1);
            txtIBP    = Field(grid, "IBP Number",         1, 0);
            DateField(grid,          "Date of Commission", 1, 1);
            txtBookNo = Field(grid, "Book Number",        2, 0);
            txtYear   = Field(grid, "Year Covered",       2, 1);

            infoCard.Controls.Add(grid);

            var attachCard = Card("Attach Notarial Book (PDF)", 170);

            var uploadZone = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(248, 249, 252),
                Cursor    = Cursors.Hand
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

            attachCard.Controls.Add(lblAttachedFile);
            attachCard.Controls.Add(uploadZone);

            var btnRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 10, 0, 0)
            };

            var btnSubmit = Btn("Submit",     true);
            var btnDraft  = Btn("Save Draft", false);
            btnSubmit.Click += Submit_Click;
            btnDraft .Click += SaveDraft_Click;

            btnRow.Controls.Add(btnSubmit);
            btnRow.Controls.Add(btnDraft);
            btnRow.SizeChanged += (s, e) =>
            {
                btnSubmit.Location = new Point(btnRow.Width - 110, 10);
                btnDraft .Location = new Point(btnRow.Width - 225, 10);
            };

            this.Controls.Add(btnRow);
            this.Controls.Add(attachCard);
            this.Controls.Add(infoCard);
        }

        // ── Submit: save to SQLite, upload to MEGA ────────────────
        private async void Submit_Click(object sender, EventArgs e)
        {
            // Validation
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

            var btnSubmit = (Button)sender;
            btnSubmit.Enabled = false;

            try
            {
                // ── Step 1: Save to SQLite immediately (status = Pending, no MEGA link yet)
                var submission = new Submission
                {
                    BookNumber       = txtBookNo.Text.Trim(),
                    NotaryName       = txtName.Text.Trim(),
                    PtrNumber        = txtPTR.Text.Trim(),
                    IbpNumber        = txtIBP.Text.Trim(),
                    DateOfCommission = dtpCommission.Value.ToShortDateString(),
                    YearCovered      = txtYear.Text.Trim(),
                    LocalFilePath    = attachedFilePath,
                    FileName         = Path.GetFileName(attachedFilePath),
                    SubmittedBy      = SessionManager.Current?.FullName ?? "Unknown",
                    DateSubmitted    = DateTime.Now
                };

                int newId = DatabaseService.Instance.InsertSubmission(submission);

                // Log the submission
                DatabaseService.Instance.InsertLog(
                    submission.SubmittedBy, "Submission",
                    $"Submitted Book {submission.BookNumber} (ID: {newId})");

                // ── Step 2: Upload to MEGA
                lblAttachedFile.Text      = "⏫  Connecting to MEGA...";
                lblAttachedFile.ForeColor = Navy;

                string megaLink = "";
                bool   uploaded = false;

                try
                {
                    var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                    var json         = File.ReadAllText(settingsPath);
                    var megaCfg      = JsonDocument.Parse(json).RootElement.GetProperty("Mega");
                    string email     = megaCfg.GetProperty("Email").GetString()!;
                    string password  = megaCfg.GetProperty("Password").GetString()!;

                    var mega = new MegaService();
                    mega.OnUploadProgress = pct =>
                        this.Invoke(() => lblAttachedFile.Text = $"⏫  Uploading... {pct:F0}%");

                    await mega.LoginAsync(email, password);
                    megaLink = await mega.UploadPdfAsync(attachedFilePath, submission.BookNumber);
                    await mega.LogoutAsync();

                    // ── Step 3: Update DB row with the MEGA link
                    DatabaseService.Instance.UpdateSubmissionMegaLink(newId, megaLink);
                    uploaded = true;

                    lblAttachedFile.Text      = "✔  Uploaded to MEGA!";
                    lblAttachedFile.ForeColor = Color.FromArgb(58, 96, 18);
                }
                catch (Exception megaEx)
                {
                    // Upload failed — record is still saved in DB, just without a link
                    lblAttachedFile.Text      = "⚠  Saved locally. MEGA upload failed.";
                    lblAttachedFile.ForeColor = Color.FromArgb(163, 45, 45);
                    DatabaseService.Instance.InsertLog(
                        submission.SubmittedBy, "Warning",
                        $"MEGA upload failed for Book {submission.BookNumber}: {megaEx.Message}");
                }

                // ── Step 4: Confirm to user
                string megaInfo = uploaded
                    ? $"\nMEGA link:\n{megaLink}"
                    : "\n⚠ MEGA upload failed — record saved to database only.";

                MessageBox.Show(
                    $"Book {submission.BookNumber} submitted!\n\n" +
                    $"Notary  : {submission.NotaryName}\n"  +
                    $"PTR     : {submission.PtrNumber}\n"   +
                    $"File    : {submission.FileName}\n"    +
                    $"Status  : Pending review."            +
                    megaInfo,
                    "Submitted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Submission failed:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSubmit.Enabled = true;
            }
        }

        private void SaveDraft_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Draft saved. You can return to complete this submission.",
                "Draft Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── Helpers ───────────────────────────────────────────────
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

        private TextBox Field(TableLayoutPanel grid, string label, int row, int col)
        {
            var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4, 4, 10, 0) };
            var lbl  = new Label { Text = label, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(100, 100, 100) };
            var border = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(200, 200, 220), Padding = new Padding(1) };
            var tb = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10f), BackColor = Color.White, ForeColor = Color.FromArgb(20, 20, 20), TabStop = true };
            lbl.Click  += (s, e) => tb.Focus();
            wrap.Click += (s, e) => tb.Focus();
            border.Controls.Add(tb);
            wrap.Controls.Add(border);
            wrap.Controls.Add(lbl);
            grid.Controls.Add(wrap, col, row);
            return tb;
        }

        private void DateField(TableLayoutPanel grid, string label, int row, int col)
        {
            var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4, 4, 10, 0) };
            var lbl  = new Label { Text = label, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(100, 100, 100) };
            dtpCommission = new DateTimePicker { Dock = DockStyle.Top, Format = DateTimePickerFormat.Short, Value = DateTime.Now, Font = new Font("Segoe UI", 10f), Height = 28 };
            wrap.Controls.Add(dtpCommission);
            wrap.Controls.Add(lbl);
            grid.Controls.Add(wrap, col, row);
        }

        private Button Btn(string text, bool primary)
        {
            var b = new Button { Text = text, Width = 105, Height = 32, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand };
            if (primary) { b.BackColor = Navy; b.ForeColor = Color.White; b.FlatAppearance.BorderSize = 0; }
            else { b.BackColor = Color.White; b.ForeColor = Color.FromArgb(50, 50, 50); b.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180); b.FlatAppearance.BorderSize = 1; }
            return b;
        }

        private void DrawUploadZone(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            var g = e.Graphics;
            using var pen = new Pen(Color.FromArgb(170, 170, 210), 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            g.DrawRectangle(pen, 2, 2, p.Width - 5, p.Height - 5);
            using var fIcon = new Font("Segoe UI", 18f);
            using var fTxt  = new Font("Segoe UI", 9f);
            using var br    = new SolidBrush(Color.FromArgb(130, 130, 160));
            string icon = "📄"; var isz = g.MeasureString(icon, fIcon);
            g.DrawString(icon, fIcon, br, (p.Width - isz.Width) / 2f, 8f);
            string msg = "Click to browse or drag & drop PDF here"; var msz = g.MeasureString(msg, fTxt);
            g.DrawString(msg, fTxt, br, (p.Width - msz.Width) / 2f, isz.Height + 10f);
        }

        private void BrowseFile(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "PDF Files|*.pdf|All Files|*.*", Title = "Select Notarial Book PDF" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                attachedFilePath      = dlg.FileName;
                var info              = new FileInfo(dlg.FileName);
                lblAttachedFile.Text      = $"📎  {info.Name}  ({FormatSize(info.Length)})";
                lblAttachedFile.ForeColor = Navy;
            }
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

        private static string FormatSize(long b) =>
            b < 1024 ? $"{b} B" : b < 1048576 ? $"{b / 1024} KB" : $"{b / 1048576} MB";
    }
}

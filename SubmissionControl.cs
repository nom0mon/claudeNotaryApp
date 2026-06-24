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

        private DateTimePicker dtpCommission;
        private TextBox        txtYear;
        private Label          lblAttachedFile;
        private string         attachedFilePath = "";
        private Button         btnSubmit;
        private bool           _uploading = false;
        private System.Threading.CancellationTokenSource? _cts;

        public SubmissionControl()
        {
            this.BackColor  = Color.FromArgb(240, 240, 240);
            this.AutoScroll = true;
            BuildUI();
        }

        private void BuildUI()
        {
            // ── Card: Document Info ──────────────────────────────
            var infoCard = Card("Document Information", 180);

            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = Color.White,
                Padding     = new Padding(0, 4, 0, 4)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            DateField(grid, "Date of Commission", 0, 0);
            txtYear = Field(grid, "Year Covered (e.g. 2024)", 0, 1);

            infoCard.Controls.Add(grid);

            // ── Card: Attach PDF ─────────────────────────────────
            var attachCard = Card("Attach Notarial Book (PDF)", 200);

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

            // Cancel upload button — hidden unless uploading
            var btnCancel = new Button
            {
                Text      = "✖  Cancel Upload",
                Dock      = DockStyle.Top,
                Height    = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(163, 45, 45),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9f),
                Cursor    = Cursors.Hand,
                Visible   = false
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) =>
            {
                _cts?.Cancel();
                btnCancel.Visible         = false;
                lblAttachedFile.Text      = "⚠  Upload cancelled.";
                lblAttachedFile.ForeColor = Color.FromArgb(133, 79, 11);
                btnSubmit.Enabled         = true;
                _uploading                = false;
            };

            attachCard.Controls.Add(lblAttachedFile);
            attachCard.Controls.Add(btnCancel);
            attachCard.Controls.Add(uploadZone);

            // Store reference so Submit can toggle it
            attachCard.Tag = btnCancel;

            // ── Action buttons ───────────────────────────────────
            var btnRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 10, 0, 0)
            };

            btnSubmit        = Btn("Submit", true);
            var btnClearForm = Btn("Clear Form", false);

            btnSubmit  .Click += Submit_Click;
            btnClearForm.Click += (s, e) => ClearForm();

            btnRow.Controls.Add(btnSubmit);
            btnRow.Controls.Add(btnClearForm);
            btnRow.SizeChanged += (s, e) =>
            {
                btnSubmit   .Location = new Point(btnRow.Width - 110, 10);
                btnClearForm.Location = new Point(btnRow.Width - 225, 10);
            };

            this.Controls.Add(btnRow);
            this.Controls.Add(attachCard);
            this.Controls.Add(infoCard);
        }

        // ── Submit ───────────────────────────────────────────────
        private async void Submit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtYear.Text))
            {
                MessageBox.Show("Please enter the Year Covered.",
                    "Required Field", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(attachedFilePath) || !File.Exists(attachedFilePath))
            {
                MessageBox.Show("Please attach a valid notarial book PDF before submitting.",
                    "No File Attached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSubmit.Enabled = false;
            _uploading        = true;
            _cts              = new System.Threading.CancellationTokenSource();

            // Reveal cancel button
            var btnCancel = (Button?)(attachedFilePath == null
                ? null : this.Controls[1]?.Tag);
            // Find it properly
            Button? cancelBtn = null;
            foreach (Control c in this.Controls)
                if (c is Panel p && p.Tag is Button b) { cancelBtn = b; break; }
            if (cancelBtn != null) cancelBtn.Visible = true;

            try
            {
                string fileName = Path.GetFileName(attachedFilePath);

                var submission = new Submission
                {
                    FileName         = fileName,
                    DateOfCommission = dtpCommission.Value.ToShortDateString(),
                    YearCovered      = txtYear.Text.Trim(),
                    LocalFilePath    = attachedFilePath,
                    SubmittedBy      = SessionManager.Current?.FullName ?? "Unknown",
                    DateSubmitted    = DateTime.Now,
                    // Derive a book number from year + filename for tracking
                    BookNumber       = $"{txtYear.Text.Trim()}-{Path.GetFileNameWithoutExtension(fileName)}",
                    NotaryName       = SessionManager.Current?.FullName ?? "Unknown",
                    PtrNumber        = "",
                    IbpNumber        = ""
                };

                int newId = DatabaseService.Instance.InsertSubmission(submission);

                DatabaseService.Instance.InsertLog(
                    submission.SubmittedBy, "Submission",
                    $"Submitted '{fileName}' for year {submission.YearCovered} (ID: {newId})");

                // MEGA upload
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
                        this.Invoke(() =>
                        {
                            if (!_cts.Token.IsCancellationRequested)
                                lblAttachedFile.Text = $"⏫  Uploading... {pct:F0}%";
                        });

                    if (_cts.Token.IsCancellationRequested) return;

                    await mega.LoginAsync(email, password);

                    if (_cts.Token.IsCancellationRequested)
                    {
                        await mega.LogoutAsync();
                        return;
                    }

                    megaLink = await mega.UploadPdfAsync(attachedFilePath, submission.YearCovered);
                    await mega.LogoutAsync();

                    if (!_cts.Token.IsCancellationRequested)
                    {
                        DatabaseService.Instance.UpdateSubmissionMegaLink(newId, megaLink);
                        uploaded = true;
                        lblAttachedFile.Text      = "✔  Uploaded to MEGA!";
                        lblAttachedFile.ForeColor = Color.FromArgb(58, 96, 18);
                    }
                }
                catch (OperationCanceledException)
                {
                    lblAttachedFile.Text      = "⚠  Upload cancelled.";
                    lblAttachedFile.ForeColor = Color.FromArgb(133, 79, 11);
                    return;
                }
                catch (Exception megaEx)
                {
                    lblAttachedFile.Text      = "⚠  Saved locally. MEGA upload failed.";
                    lblAttachedFile.ForeColor = Color.FromArgb(163, 45, 45);
                    DatabaseService.Instance.InsertLog(
                        submission.SubmittedBy, "Warning",
                        $"MEGA upload failed for '{fileName}': {megaEx.Message}");
                }

                if (_cts.Token.IsCancellationRequested) return;

                string megaInfo = uploaded
                    ? $"\nMEGA link:\n{megaLink}"
                    : "\n⚠ MEGA upload failed — record saved locally only.";

                MessageBox.Show(
                    $"Document submitted successfully!\n\n" +
                    $"File     : {fileName}\n"             +
                    $"Year     : {submission.YearCovered}\n" +
                    $"Commission: {submission.DateOfCommission}\n" +
                    $"Status   : Pending review."          +
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
                _uploading        = false;
                btnSubmit.Enabled = true;
                if (cancelBtn != null) cancelBtn.Visible = false;
                _cts?.Dispose();
                _cts = null;
            }
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
            var wrap   = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4, 4, 10, 0) };
            var lbl    = new Label { Text = label, Dock = DockStyle.Top, Height = 18, Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(100, 100, 100) };
            var border = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.FromArgb(200, 200, 220), Padding = new Padding(1) };
            var tb     = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10f), BackColor = Color.White, ForeColor = Color.FromArgb(20, 20, 20) };
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
            dtpCommission = new DateTimePicker { Dock = DockStyle.Top, Format = DateTimePickerFormat.Short, Value = DateTime.Now, Font = new Font("Segoe UI", 10f), Height = 34 };
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
            using var pen = new System.Drawing.Drawing2D.GraphicsPath();
            using var dashPen = new Pen(Color.FromArgb(170, 170, 210), 1.5f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            g.DrawRectangle(dashPen, 2, 2, p.Width - 5, p.Height - 5);
            using var fIcon = new Font("Segoe UI", 18f);
            using var fTxt  = new Font("Segoe UI", 9f);
            using var br    = new SolidBrush(Color.FromArgb(130, 130, 160));
            string icon = "📄"; var isz = g.MeasureString(icon, fIcon);
            g.DrawString(icon, fIcon, br, (p.Width - isz.Width) / 2f, 8f);
            string msg = "Click to browse PDF"; var msz = g.MeasureString(msg, fTxt);
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
            txtYear.Clear();
            dtpCommission.Value   = DateTime.Now;
            attachedFilePath      = "";
            lblAttachedFile.Text      = "No file selected.";
            lblAttachedFile.ForeColor = Color.Gray;
        }

        private static string FormatSize(long b) =>
            b < 1024 ? $"{b} B" : b < 1048576 ? $"{b / 1024} KB" : $"{b / 1048576} MB";
    }
}
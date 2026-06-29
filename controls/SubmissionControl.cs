using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class SubmissionControl : UserControl
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);

        private TextBox        txtBookNo, txtDocName;
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
            // ── Info card ─────────────────────────────────────────
            var infoCard = Card("Notary Information", 180);

            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 2,
                BackColor   = Color.White,
                Padding     = new Padding(0, 4, 0, 4)
            };
            for (int i = 0; i < 2; i++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            // Row 0: Date of Commission | Book Number
            DateField(grid, "Date of Commission *", 0, 0);
            txtBookNo = Field(grid, "Book Number",
                              placeholder: "e.g. BK-2024-001",
                              row: 0, col: 1, required: false);

            // Row 1: Document Name — full width
            txtDocName = FieldSpan(grid, "Document Name *",
                                   placeholder: "e.g. Deed of Absolute Sale",
                                   row: 1, required: true);

            infoCard.Controls.Add(grid);

            // ── File attachment card ──────────────────────────────
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

            // ── Button row ────────────────────────────────────────
            var btnRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 10, 0, 0)
            };

            var btnSubmit = Btn("Submit",     primary: true);
            var btnClear  = Btn("Clear Form", primary: false);
            btnSubmit.Click += Submit_Click;
            btnClear .Click += (s, e) => ClearForm();

            btnRow.Controls.Add(btnSubmit);
            btnRow.Controls.Add(btnClear);
            btnRow.SizeChanged += (s, e) =>
            {
                btnSubmit.Location = new Point(btnRow.Width - 110, 10);
                btnClear .Location = new Point(btnRow.Width - 225, 10);
            };

            this.Controls.Add(btnRow);
            this.Controls.Add(attachCard);
            this.Controls.Add(infoCard);
        }

        // ── Submit ────────────────────────────────────────────────
        private async void Submit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDocName.Text))
            {
                MessageBox.Show("Document Name is required.",
                    "Required Field", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDocName.Focus();
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
                var submission = new Submission
                {
                    BookNumber       = txtBookNo.Text.Trim(),
                    DocumentName     = txtDocName.Text.Trim(),
                    DateOfCommission = dtpCommission.Value.ToShortDateString(),
                    LocalFilePath    = attachedFilePath,
                    FileName         = Path.GetFileName(attachedFilePath),
                    SubmittedBy      = SessionManager.Current?.FullName ?? "Unknown",
                    DateSubmitted    = DateTime.UtcNow,
                    Status           = "Pending"
                };

                // ── Step 1: Save to Firestore ─────────────────────
                string firestoreId = await FirestoreService.Instance
                    .InsertSubmissionAsync(submission);
                submission.Id = firestoreId;

                await FirestoreService.Instance.InsertLogAsync(
                    submission.SubmittedBy, "Submission",
                    $"Submitted '{submission.DocumentName}'" +
                    (string.IsNullOrEmpty(submission.BookNumber)
                        ? "" : $" (Book {submission.BookNumber})"),
                    "file");

                // ── Step 2: Upload to MEGA ────────────────────────
                lblAttachedFile.Text      = "⏫  Connecting to MEGA…";
                lblAttachedFile.ForeColor = Navy;

                string megaLink = "";
                bool   uploaded = false;

                try
                {
                    var (email, pass) = await FirestoreService.Instance
                        .GetMegaCredentialsAsync();

                    var mega = new MegaService();
                    mega.OnUploadProgress = pct =>
                        this.Invoke(() =>
                            lblAttachedFile.Text = $"⏫  Uploading… {pct:F0}%");

                    await mega.LoginAsync(email, pass);
                    string bookRef = string.IsNullOrEmpty(submission.BookNumber)
                        ? submission.DocumentName
                        : submission.BookNumber;
                    megaLink = await mega.UploadPdfAsync(attachedFilePath, bookRef);
                    await mega.LogoutAsync();

                    await FirestoreService.Instance
                        .UpdateSubmissionMegaLinkAsync(firestoreId, megaLink);
                    uploaded = true;

                    lblAttachedFile.Text      = "✔  Uploaded to MEGA!";
                    lblAttachedFile.ForeColor = Color.FromArgb(58, 96, 18);
                }
                catch (Exception megaEx)
                {
                    lblAttachedFile.Text      = "⚠  Saved to Firestore. MEGA upload failed.";
                    lblAttachedFile.ForeColor = Color.FromArgb(163, 45, 45);
                    await FirestoreService.Instance.InsertLogAsync(
                        submission.SubmittedBy, "Warning",
                        $"MEGA upload failed for '{submission.DocumentName}': {megaEx.Message}",
                        "file");
                }

                // ── Step 3: Confirm ───────────────────────────────
                string megaInfo = uploaded
                    ? $"\nMEGA link:\n{megaLink}"
                    : "\n⚠ MEGA upload failed — record saved to Firestore only.";

                MessageBox.Show(
                    $"'{submission.DocumentName}' submitted!\n\n"   +
                    $"Date    : {submission.DateOfCommission}\n"    +
                    (string.IsNullOrEmpty(submission.BookNumber) ? ""
                        : $"Book #  : {submission.BookNumber}\n")  +
                    $"File    : {submission.FileName}\n"            +
                    $"Status  : Pending review."                    +
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

        // ════════════════════════════════════════════════════════
        //  UI HELPERS
        // ════════════════════════════════════════════════════════

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

        // Half-width field with placeholder
        private TextBox Field(TableLayoutPanel grid, string label, string placeholder,
                              int row, int col, bool required = false)
        {
            var wrap   = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4, 4, 10, 0) };
            var lbl    = new Label
            {
                Text      = label,
                Dock      = DockStyle.Top,
                Height    = 18,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = required ? Color.FromArgb(10, 26, 107) : Color.FromArgb(100, 100, 100)
            };
            var border = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 28,
                BackColor = Color.FromArgb(200, 200, 220),
                Padding   = new Padding(1)
            };
            var tb = new TextBox
            {
                Dock            = DockStyle.Fill,
                BorderStyle     = BorderStyle.None,
                Font            = new Font("Segoe UI", 10f),
                BackColor       = Color.White,
                PlaceholderText = placeholder
            };
            lbl.Click  += (s, e) => tb.Focus();
            wrap.Click += (s, e) => tb.Focus();
            border.Controls.Add(tb);
            wrap.Controls.Add(border);
            wrap.Controls.Add(lbl);
            grid.Controls.Add(wrap, col, row);
            return tb;
        }

        // Full-width field spanning both columns with placeholder
        private TextBox FieldSpan(TableLayoutPanel grid, string label, string placeholder,
                                  int row, bool required = false)
        {
            var wrap   = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4, 4, 10, 0) };
            var lbl    = new Label
            {
                Text      = label,
                Dock      = DockStyle.Top,
                Height    = 18,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = required ? Color.FromArgb(10, 26, 107) : Color.FromArgb(100, 100, 100)
            };
            var border = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 28,
                BackColor = Color.FromArgb(200, 200, 220),
                Padding   = new Padding(1)
            };
            var tb = new TextBox
            {
                Dock            = DockStyle.Fill,
                BorderStyle     = BorderStyle.None,
                Font            = new Font("Segoe UI", 10f),
                BackColor       = Color.White,
                PlaceholderText = placeholder
            };
            lbl.Click  += (s, e) => tb.Focus();
            wrap.Click += (s, e) => tb.Focus();
            border.Controls.Add(tb);
            wrap.Controls.Add(border);
            wrap.Controls.Add(lbl);
            grid.Controls.Add(wrap, 0, row);
            grid.SetColumnSpan(wrap, 2);
            return tb;
        }

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
                ForeColor = Color.FromArgb(10, 26, 107)
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

        private void DrawUploadZone(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            var g = e.Graphics;
            using var pen = new System.Drawing.Pen(Color.FromArgb(170, 170, 210), 1.5f)
                { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            g.DrawRectangle(pen, 2, 2, p.Width - 5, p.Height - 5);
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

        private void BrowseFile(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Filter = "PDF Files|*.pdf|All Files|*.*",
                Title  = "Select Notarial Book PDF"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                attachedFilePath          = dlg.FileName;
                var info                  = new FileInfo(dlg.FileName);
                lblAttachedFile.Text      = $"📎  {info.Name}  ({FormatSize(info.Length)})";
                lblAttachedFile.ForeColor = Navy;
            }
        }

        private void ClearForm()
        {
            txtBookNo .Clear();
            txtDocName.Clear();
            dtpCommission.Value       = DateTime.Now;
            attachedFilePath          = "";
            lblAttachedFile.Text      = "No file selected.";
            lblAttachedFile.ForeColor = Color.Gray;
        }

        private static string FormatSize(long b) =>
            b < 1024 ? $"{b} B" : b < 1048576 ? $"{b / 1024} KB" : $"{b / 1048576} MB";
    }
}
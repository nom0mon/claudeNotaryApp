using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class SubmissionControl : UserControl
    {
        private readonly Color Navy  = Color.FromArgb(10, 26, 107);
        private readonly Color Green = Color.FromArgb(58, 96, 18);
        private readonly Color Red   = Color.FromArgb(163, 45, 45);

        private TextBox        txtBookNo, txtDocName;
        private DateTimePicker dtpCommission;
        private Label          lblAttachedFile;
        private Button         btnSubmit, btnClear, btnAddMore, btnRemove;
        private ListBox        lstFiles;
        private UploadZonePanel         uploadZone;

        // Each entry: (localFilePath, displayName)
        private readonly List<(string Path, string Name)> _attachedFiles = new();
        private class UploadZonePanel : Panel
{
        public UploadZonePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
        }
    }
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

            DateField(grid, "Date of Commission *", 0, 0);
            txtBookNo = Field(grid, "Book Number",
                              placeholder: "e.g. BK-2024-001",
                              row: 0, col: 1, required: false);
            txtDocName = FieldSpan(grid, "Document Name *",
                                   placeholder: "e.g. Deed of Absolute Sale",
                                   row: 1, required: true);

            infoCard.Controls.Add(grid);

            // ── Attach card ───────────────────────────────────────
            var attachCard = Card("Attach Notarial Books (PDF)", 230);

            // Drop zone — click to add files
            uploadZone = new UploadZonePanel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = Color.FromArgb(248, 249, 252),
                Cursor    = Cursors.Hand
            };
            uploadZone.Paint += DrawUploadZone;
            uploadZone.Click += (s, e) => BrowseFiles();

            // File list box
            lstFiles = new ListBox
            {
                Dock          = DockStyle.Top,
                Height        = 90,
                Font          = new Font("Segoe UI", 8.5f),
                BorderStyle   = BorderStyle.FixedSingle,
                BackColor     = Color.FromArgb(248, 249, 252),
                SelectionMode = SelectionMode.MultiExtended
            };

            // File action buttons
            var fileBar = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.White };

            lblAttachedFile = new Label
            {
                Text      = "No files selected.",
                AutoSize  = false,
                Dock      = DockStyle.Fill,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(2, 0, 0, 0)
            };

            btnAddMore = SmallBtn("+ Add Files", Navy);
            btnAddMore.Dock   = DockStyle.Right;
            btnAddMore.Width  = 88;
            btnAddMore.Click += (s, e) => BrowseFiles();

            btnRemove = SmallBtn("✖ Remove", Red);
            btnRemove.Dock   = DockStyle.Right;
            btnRemove.Width  = 80;
            btnRemove.Click += RemoveSelected_Click;

            fileBar.Controls.Add(lblAttachedFile);
            fileBar.Controls.Add(btnRemove);
            fileBar.Controls.Add(btnAddMore);

            attachCard.Controls.Add(fileBar);
            attachCard.Controls.Add(lstFiles);
            attachCard.Controls.Add(uploadZone);

            // ── Button row ────────────────────────────────────────
            var btnRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 10, 0, 0)
            };

            btnSubmit = Btn("Submit",     primary: true);
            btnClear  = Btn("Clear Form", primary: false);
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

        // ── Browse and add files ──────────────────────────────────
        private void BrowseFiles()
        {
            using var dlg = new OpenFileDialog
            {
                Filter      = "PDF Files|*.pdf|All Files|*.*",
                Title       = "Select Notarial Book PDF(s)",
                Multiselect = true
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            foreach (var path in dlg.FileNames)
            {
                // Skip duplicates
                if (_attachedFiles.Any(f => f.Path == path)) continue;
                _attachedFiles.Add((path, Path.GetFileName(path)));
            }

            RefreshFileList();
        }

        private void RemoveSelected_Click(object? sender, EventArgs e)
        {
            // Remove in reverse index order so indices don't shift
            var indices = lstFiles.SelectedIndices
                .Cast<int>()
                .OrderByDescending(i => i)
                .ToList();

            foreach (var i in indices)
                _attachedFiles.RemoveAt(i);

            RefreshFileList();
        }

        private void RefreshFileList()
        {
            lstFiles.Items.Clear();
            foreach (var (path, name) in _attachedFiles)
            {
                long size = File.Exists(path) ? new FileInfo(path).Length : 0;
                lstFiles.Items.Add($"  📄  {name}   ({FormatSize(size)})");
            }

            int count = _attachedFiles.Count;
            if (count == 0)
            {
                lblAttachedFile.Text      = "No files selected.";
                lblAttachedFile.ForeColor = Color.Gray;
            }
            else
            {
                long total = _attachedFiles
                    .Where(f => File.Exists(f.Path))
                    .Sum(f => new FileInfo(f.Path).Length);
                lblAttachedFile.Text      = $"{count} file{(count == 1 ? "" : "s")} selected  •  {FormatSize(total)} total";
                lblAttachedFile.ForeColor = Navy;
            }
        }

        // ── Submit (batch) ────────────────────────────────────────
        private async void Submit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDocName.Text))
            {
                MessageBox.Show("Document Name is required.",
                    "Required Field", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDocName.Focus();
                return;
            }

            if (_attachedFiles.Count == 0)
            {
                MessageBox.Show("Please attach at least one PDF before submitting.",
                    "No Files Attached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verify all files still exist on disk
            var missing = _attachedFiles.Where(f => !File.Exists(f.Path)).ToList();
            if (missing.Any())
            {
                MessageBox.Show(
                    "The following files could not be found:\n\n" +
                    string.Join("\n", missing.Select(f => f.Name)),
                    "Files Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSubmit.Enabled = false;
            btnSubmit.Text    = "Submitting…";

            int    total     = _attachedFiles.Count;
            int    succeeded = 0;
            int    failed    = 0;
            string submitter = SessionManager.Current?.FullName ?? "Unknown";
            string docBase   = txtDocName.Text.Trim();
            string bookBase  = txtBookNo .Text.Trim();

            // Get MEGA credentials once for the whole batch
            string megaEmail = "", megaPass = "";
            bool   megaReady = false;
            try
            {
                (megaEmail, megaPass) = await FirestoreService.Instance.GetMegaCredentialsAsync();
                megaReady = true;
            }
            catch (Exception megaEx)
            {
                lblAttachedFile.Text      = $"⚠  MEGA credentials unavailable: {megaEx.Message}";
                lblAttachedFile.ForeColor = Red;
            }

            MegaService? mega = null;
            if (megaReady)
            {
                try
                {
                    mega = new MegaService();
                    await mega.LoginAsync(megaEmail, megaPass);
                }
                catch
                {
                    mega      = null;
                    megaReady = false;
                    lblAttachedFile.Text      = "⚠  MEGA login failed. Files will be saved to Firestore only.";
                    lblAttachedFile.ForeColor = Red;
                }
            }

            try
            {
                for (int i = 0; i < _attachedFiles.Count; i++)
                {
                    var (filePath, fileName) = _attachedFiles[i];

                    // For batches, append file index to document name if multiple files
                    string docName  = total == 1 ? docBase : $"{docBase} ({i + 1} of {total})";
                    string bookRef  = string.IsNullOrEmpty(bookBase)
                        ? docName
                        : (total == 1 ? bookBase : $"{bookBase}-{i + 1}");

                    lblAttachedFile.Text      = $"⏫  [{i + 1}/{total}] Saving '{fileName}'…";
                    lblAttachedFile.ForeColor = Navy;

                    try
                    {
                        // Save to Firestore
                        var submission = new Submission
                        {
                            BookNumber       = bookRef,
                            DocumentName     = docName,
                            DateOfCommission = dtpCommission.Value.ToShortDateString(),
                            LocalFilePath    = filePath,
                            FileName         = fileName,
                            SubmittedBy      = submitter,
                            DateSubmitted    = DateTime.UtcNow,
                            Status           = "Pending"
                        };

                        string fsId = await FirestoreService.Instance
                            .InsertSubmissionAsync(submission);

                        await FirestoreService.Instance.InsertLogAsync(
                            submitter, "Submission",
                            $"Submitted '{docName}'" +
                            (string.IsNullOrEmpty(bookBase) ? "" : $" (Book {bookRef})"),
                            "file");

                        // Upload to MEGA
                        if (mega != null)
                        {
                            lblAttachedFile.Text = $"⏫  [{i + 1}/{total}] Uploading '{fileName}' to MEGA…";
                            mega.OnUploadProgress = pct =>
                                this.Invoke(() =>
                                    lblAttachedFile.Text =
                                        $"⏫  [{i + 1}/{total}] Uploading '{fileName}'… {pct:F0}%");

                            string megaLink = await mega.UploadPdfAsync(filePath, bookRef);
                            await FirestoreService.Instance
                                .UpdateSubmissionMegaLinkAsync(fsId, megaLink);
                        }

                        succeeded++;
                    }
                    catch (Exception fileEx)
                    {
                        failed++;
                        await FirestoreService.Instance.InsertLogAsync(
                            submitter, "Warning",
                            $"Failed to submit '{fileName}': {fileEx.Message}",
                            "file");
                    }
                }
            }
            finally
            {
                // Always logout from MEGA even if some files failed
                if (mega != null)
                {
                    try { await mega.LogoutAsync(); } catch { }
                }
            }

            // ── Result summary ────────────────────────────────────
            if (failed == 0)
            {
                lblAttachedFile.Text      = $"✔  {succeeded} file{(succeeded == 1 ? "" : "s")} submitted successfully!";
                lblAttachedFile.ForeColor = Green;

                MessageBox.Show(
                    $"{succeeded} document{(succeeded == 1 ? "" : "s")} submitted successfully!\n\n" +
                    $"Document : {docBase}\n"                                                         +
                    (string.IsNullOrEmpty(bookBase) ? "" : $"Book #   : {bookBase}\n")               +
                    $"Date     : {dtpCommission.Value.ToShortDateString()}\n"                         +
                    $"Status   : Pending review."                                                     +
                    (megaReady ? "" : "\n\n⚠ MEGA upload skipped — records saved to Firestore only."),
                    "Submitted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ClearForm();
            }
            else
            {
                lblAttachedFile.Text      = $"⚠  {succeeded} succeeded, {failed} failed. Check Activity Logs.";
                lblAttachedFile.ForeColor = Red;

                MessageBox.Show(
                    $"{succeeded} of {total} file(s) submitted.\n" +
                    $"{failed} file(s) failed — see Activity Logs for details.",
                    "Partial Success", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            btnSubmit.Enabled = true;
            btnSubmit.Text    = "Submit";
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
                ForeColor = required ? Navy : Color.FromArgb(100, 100, 100)
            };
            var border = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(200, 200, 220), Padding = new Padding(1) };
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
                ForeColor = required ? Navy : Color.FromArgb(100, 100, 100)
            };
            var border = new Panel { Dock = DockStyle.Top, Height = 28, BackColor = Color.FromArgb(200, 200, 220), Padding = new Padding(1) };
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
            var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(4, 4, 10, 0) };
            var lbl  = new Label
            {
                Text      = label,
                Dock      = DockStyle.Top,
                Height    = 18,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Navy
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

        private Button SmallBtn(string text, Color bg)
        {
            var b = new Button
            {
                Text      = text,
                Height    = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 8f),
                Cursor    = Cursors.Hand,
                Margin    = new Padding(2, 1, 2, 1)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void DrawUploadZone(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            var g = e.Graphics;
            using var pen  = new System.Drawing.Pen(Color.FromArgb(170, 170, 210), 1.5f)
                { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            g.DrawRectangle(pen, 2, 2, p.Width - 5, p.Height - 5);
            using var fIcon = new Font("Segoe UI", 14f);
            using var fTxt  = new Font("Segoe UI", 9f);
            using var br    = new SolidBrush(Color.FromArgb(130, 130, 160));
            string icon = "📄";
            var isz = g.MeasureString(icon, fIcon);
            g.DrawString(icon, fIcon, br, 10f, (p.Height - isz.Height) / 2f);
            string msg = "Click to browse — multiple PDFs supported";
            var msz = g.MeasureString(msg, fTxt);
            g.DrawString(msg, fTxt, br, isz.Width + 14f, (p.Height - msz.Height) / 2f);
        }

        private void ClearForm()
        {
            txtBookNo .Clear();
            txtDocName.Clear();
            dtpCommission.Value = DateTime.Now;
            _attachedFiles.Clear();
            RefreshFileList();
        }

        private static string FormatSize(long b) =>
            b < 1024 ? $"{b} B" : b < 1048576 ? $"{b / 1024} KB" : $"{b / 1048576} MB";
    }
}
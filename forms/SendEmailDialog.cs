using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class SendEmailDialog : Form
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);
        private readonly List<Submission> _submissions;   // supports batch

        // Send Now tab
        private ComboBox txtRecipientNow;
        private TextBox  txtNotesNow;
        private Label    lblStatusNow;
        private Button   btnSendNow;

        // Schedule tab
        private ComboBox     txtRecipientSched;
        private DateTimePicker dtpScheduleDate;
        private DateTimePicker dtpScheduleTime;
        private TextBox      txtNotesSched;
        private Label        lblStatusSched;
        private Button       btnSchedule;

        // Accept either single or multiple submissions
        public SendEmailDialog(Submission submission)
            : this(new List<Submission> { submission }) { }

        public SendEmailDialog(List<Submission> submissions)
        {
            _submissions = submissions;

            int count   = submissions.Count;
            string title = count == 1
                ? $"Send Document — {submissions[0].DocumentName}"
                : $"Send Batch — {count} Documents";

            Text            = title;
            Size            = new Size(500, 420);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);

            BuildUI();
            _ = LoadRecipientsAsync();
        }

        private void BuildUI()
        {
            // ── Header ───────────────────────────────────────────
            var header = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Navy };
            int count  = _submissions.Count;
            string headerText = count == 1
                ? $"📧  {_submissions[0].DocumentName}"
                : $"📧  Batch Send — {count} documents";

            header.Controls.Add(new Label
            {
                Text      = headerText,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0)
            });

            // ── Tabs ─────────────────────────────────────────────
            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f)
            };

            tabs.TabPages.Add(BuildSendNowTab());
            tabs.TabPages.Add(BuildScheduleTab());

            Controls.Add(tabs);
            Controls.Add(header);
        }

        // ── SEND NOW TAB ─────────────────────────────────────────
        private TabPage BuildSendNowTab()
        {
            var page = new TabPage("  Send Now  ") { BackColor = Color.White };

            var lblRecipient = L("Recipient Email *", 16);
            txtRecipientNow  = Combo(84);

            var lblNotes = L("Notes (optional)", 120);
            txtNotesNow  = new TextBox
            {
                Location    = new Point(16, 140),
                Size        = new Size(446, 68),
                Multiline   = true,
                Font        = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblStatusNow = new Label
            {
                Location  = new Point(16, 218),
                Size      = new Size(446, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray
            };

            btnSendNow = Btn("📧  Send Now", Navy, new Point(16, 244));
            btnSendNow.Click += BtnSendNow_Click;

            var btnCancel = Btn("Cancel", Color.FromArgb(90, 90, 90), new Point(166, 244));
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // File list if batch
            if (_submissions.Count > 1)
            {
                var lblFiles = L($"Attachments ({_submissions.Count} files):", 16);
                var lstFiles = new ListBox
                {
                    Location  = new Point(16, 36),
                    Size      = new Size(446, 44),
                    Font      = new Font("Segoe UI", 8.5f),
                    BackColor = Color.FromArgb(248, 249, 252),
                    BorderStyle = BorderStyle.FixedSingle
                };
                foreach (var s in _submissions)
                    lstFiles.Items.Add($"  📄 {s.DocumentName}  ({s.FileName})");

                // Shift controls down
                lblRecipient.Location = new Point(16, 90);
                txtRecipientNow.Location = new Point(16, 110);
                lblNotes.Location = new Point(16, 148);
                txtNotesNow.Location = new Point(16, 168);
                lblStatusNow.Location = new Point(16, 246);
                btnSendNow.Location = new Point(16, 272);
                btnCancel.Location  = new Point(166, 272);

                page.Controls.AddRange(new Control[]
                    { lblFiles, lstFiles, lblRecipient, txtRecipientNow,
                      lblNotes, txtNotesNow, lblStatusNow, btnSendNow, btnCancel });
            }
            else
            {
                page.Controls.AddRange(new Control[]
                    { lblRecipient, txtRecipientNow,
                      lblNotes, txtNotesNow, lblStatusNow, btnSendNow, btnCancel });
            }

            return page;
        }

        // ── SCHEDULE TAB ─────────────────────────────────────────
        private TabPage BuildScheduleTab()
        {
            var page = new TabPage("  Schedule  ") { BackColor = Color.White };

            var lblRecipient   = L("Recipient Email *", 16);
            txtRecipientSched  = Combo(36);

            var lblDate = L("Schedule Date *", 80);
            dtpScheduleDate = new DateTimePicker
            {
                Location = new Point(16, 100),
                Width    = 200,
                Format   = DateTimePickerFormat.Short,
                Value    = DateTime.Now.AddDays(1),
                Font     = new Font("Segoe UI", 10f),
                MinDate  = DateTime.Now
            };

            var lblTime = L("Schedule Time *", 80);
            lblTime.Location = new Point(228, 80);
            dtpScheduleTime = new DateTimePicker
            {
                Location   = new Point(228, 100),
                Width      = 234,
                Format     = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value      = DateTime.Now,
                Font       = new Font("Segoe UI", 10f)
            };

            var lblNotes  = L("Notes (optional)", 140);
            txtNotesSched = new TextBox
            {
                Location    = new Point(16, 160),
                Size        = new Size(446, 58),
                Multiline   = true,
                Font        = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblStatusSched = new Label
            {
                Location  = new Point(16, 228),
                Size      = new Size(446, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray
            };

            btnSchedule = Btn("📅  Schedule", Color.FromArgb(58, 96, 18), new Point(16, 254));
            btnSchedule.Click += BtnSchedule_Click;

            var btnCancel = Btn("Cancel", Color.FromArgb(90, 90, 90), new Point(166, 254));
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            page.Controls.AddRange(new Control[]
            {
                lblRecipient, txtRecipientSched,
                lblDate, dtpScheduleDate,
                lblTime, dtpScheduleTime,
                lblNotes, txtNotesSched,
                lblStatusSched, btnSchedule, btnCancel
            });

            return page;
        }

        // ── SEND NOW HANDLER ─────────────────────────────────────
        private async void BtnSendNow_Click(object? sender, EventArgs e)
        {
            string email = txtRecipientNow.Text.Trim();
            if (!ValidateEmail(email, lblStatusNow)) return;

            var missingFiles = _submissions
                .Where(s => !File.Exists(s.LocalFilePath))
                .Select(s => s.DocumentName)
                .ToList();

            if (missingFiles.Any())
            {
                MessageBox.Show(
                    $"The following files no longer exist locally:\n\n" +
                    string.Join("\n", missingFiles) +
                    "\n\nFiles must be present on disk to send.",
                    "Files Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSendNow.Enabled  = false;
            btnSendNow.Text     = "Sending…";
            lblStatusNow.ForeColor = Color.Gray;
            lblStatusNow.Text   = "Connecting to mail server…";

            try
            {
                await FirestoreService.Instance.Gmail.SendBatchDocumentAsync(
                    email, email,
                    _submissions.Select(s => s.LocalFilePath).ToList(),
                    _submissions.Select(s => s.FileName).ToList(),
                    _submissions.Select(s => s.DocumentName).ToList(),
                    txtNotesNow.Text.Trim());

                await FirestoreService.Instance.SaveEmailRecipientAsync(email);

                string docList = string.Join(", ", _submissions.Select(s => $"'{s.DocumentName}'"));
                await FirestoreService.Instance.InsertLogAsync(
                    SessionManager.Current?.FullName ?? "",
                    "EmailSent",
                    $"Batch of {_submissions.Count} document(s) emailed to {email}: {docList}",
                    "file");

                MessageBox.Show(
                    $"{_submissions.Count} document(s) sent successfully to {email}.",
                    "Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblStatusNow.ForeColor = Color.FromArgb(163, 45, 45);
                lblStatusNow.Text      = $"Failed: {ex.Message}";
                btnSendNow.Enabled     = true;
                btnSendNow.Text        = "📧  Send Now";
            }
        }

        // ── SCHEDULE HANDLER ─────────────────────────────────────
        private async void BtnSchedule_Click(object? sender, EventArgs e)
        {
            string email = txtRecipientSched.Text.Trim();
            if (!ValidateEmail(email, lblStatusSched)) return;

            // Combine date + time
            DateTime scheduledAt = dtpScheduleDate.Value.Date
                                 + dtpScheduleTime.Value.TimeOfDay;

            if (scheduledAt <= DateTime.Now)
            {
                lblStatusSched.ForeColor = Color.FromArgb(163, 45, 45);
                lblStatusSched.Text      = "Scheduled time must be in the future.";
                return;
            }

            btnSchedule.Enabled     = false;
            btnSchedule.Text        = "Saving…";
            lblStatusSched.ForeColor = Color.Gray;
            lblStatusSched.Text     = "Saving schedule to Firestore…";

            try
            {
                var schedule = new ScheduledEmail
                {
                    SubmissionIds  = _submissions.Select(s => s.Id).ToList(),
                    DocumentNames  = _submissions.Select(s => s.DocumentName).ToList(),
                    FilePaths      = _submissions.Select(s => s.LocalFilePath).ToList(),
                    FileNames      = _submissions.Select(s => s.FileName).ToList(),
                    RecipientEmail = email,
                    ScheduledAt    = scheduledAt.ToUniversalTime(),
                    CreatedBy      = SessionManager.Current?.FullName ?? "",
                    Notes          = txtNotesSched.Text.Trim()
                };

                string schedId = await FirestoreService.Instance.CreateScheduleAsync(schedule);

                await FirestoreService.Instance.SaveEmailRecipientAsync(email);

                string docList = string.Join(", ", _submissions.Select(s => $"'{s.DocumentName}'"));
                await FirestoreService.Instance.InsertLogAsync(
                    SessionManager.Current?.FullName ?? "",
                    "EmailScheduled",
                    $"Scheduled {_submissions.Count} document(s) for {scheduledAt:MMM dd, yyyy h:mm tt} to {email}: {docList}",
                    "file");

                MessageBox.Show(
                    $"Batch of {_submissions.Count} document(s) scheduled for:\n" +
                    $"{scheduledAt:MMMM dd, yyyy – hh:mm tt}\n\nTo: {email}",
                    "Scheduled", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblStatusSched.ForeColor = Color.FromArgb(163, 45, 45);
                lblStatusSched.Text      = $"Failed: {ex.Message}";
                btnSchedule.Enabled     = true;
                btnSchedule.Text        = "📅  Schedule";
            }
        }

        // ── SHARED HELPERS ───────────────────────────────────────
        private async Task LoadRecipientsAsync()
        {
            try
            {
                var list    = await FirestoreService.Instance.GetEmailRecipientsAsync();
                var def     = await FirestoreService.Instance.GetDefaultRecipientAsync();
                if (!string.IsNullOrEmpty(def) && !list.Contains(def))
                    list.Insert(0, def);

                foreach (var combo in new[] { txtRecipientNow, txtRecipientSched })
                {
                    combo.Items.Clear();
                    foreach (var addr in list) combo.Items.Add(addr);
                    if (combo.Items.Count > 0) combo.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private static bool ValidateEmail(string email, Label statusLabel)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                statusLabel.ForeColor = Color.FromArgb(163, 45, 45);
                statusLabel.Text      = "Please enter a valid email address.";
                return false;
            }
            return true;
        }

        private Label L(string text, int y) => new Label
        {
            Text      = text,
            Location  = new Point(16, y),
            AutoSize  = true,
            ForeColor = Color.FromArgb(80, 80, 80)
        };

        private ComboBox Combo(int y) => new ComboBox
        {
            Location         = new Point(16, y),
            Width            = 446,
            Font             = new Font("Segoe UI", 10f),
            DropDownStyle    = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };

        private Button Btn(string text, Color bg, Point loc)
        {
            var b = new Button
            {
                Text      = text,
                Location  = loc,
                Size      = new Size(140, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
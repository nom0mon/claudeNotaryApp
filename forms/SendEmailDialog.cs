using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public class SendEmailDialog : Form
    {
        private readonly Color Navy = Color.FromArgb(10, 26, 107);
        private readonly Submission _submission;

        private ComboBox txtRecipient;
        private TextBox  txtNotes;
        private Button   btnSend;
        private Label    lblStatus;

        public SendEmailDialog(Submission submission)
        {
            _submission = submission;

            Text            = $"Send Document — {submission.DocumentName}";
            Size            = new Size(460, 340);
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
            // ── Header strip ─────────────────────────────────
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = Navy
            };
            header.Controls.Add(new Label
            {
                Text      = $"📧  Send: {_submission.DocumentName}",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0)
            });

            // ── Recipient ─────────────────────────────────────
            var lblRecipient = new Label
            {
                Text      = "Recipient Email *",
                Location  = new Point(16, 64),
                AutoSize  = true,
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            txtRecipient = new ComboBox
            {
                Location         = new Point(16, 84),
                Width            = 410,
                Font             = new Font("Segoe UI", 10f),
                DropDownStyle    = ComboBoxStyle.DropDown,   // editable + dropdown
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };

            // ── Notes ────────────────────────────────────────
            var lblNotes = new Label
            {
                Text      = "Notes (optional)",
                Location  = new Point(16, 120),
                AutoSize  = true,
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            txtNotes = new TextBox
            {
                Location    = new Point(16, 140),
                Size        = new Size(410, 68),
                Multiline   = true,
                Font        = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            // ── Status label ─────────────────────────────────
            lblStatus = new Label
            {
                Location  = new Point(16, 218),
                Size      = new Size(410, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray,
                Text      = ""
            };

            // ── Buttons ───────────────────────────────────────
            btnSend = new Button
            {
                Text      = "📧  Send",
                Location  = new Point(16, 244),
                Size      = new Size(130, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Navy,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += BtnSend_Click;

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Location  = new Point(156, 244),
                Size      = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(90, 90, 90),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9.5f),
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[]
            {
                header, lblRecipient, txtRecipient,
                lblNotes, txtNotes, lblStatus, btnSend, btnCancel
            });
        }

        private async Task LoadRecipientsAsync()
        {
            try
            {
                var list = await FirestoreService.Instance.GetEmailRecipientsAsync();

                // Also load the default recipient from config/email
                var snap = await FirestoreService.Instance.GetDefaultRecipientAsync();
                if (!string.IsNullOrEmpty(snap) && !list.Contains(snap))
                    list.Insert(0, snap);

                txtRecipient.Items.Clear();
                foreach (var addr in list)
                    txtRecipient.Items.Add(addr);

                if (txtRecipient.Items.Count > 0)
                    txtRecipient.SelectedIndex = 0;
            }
            catch { /* non-fatal — user can type manually */ }
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            string email = txtRecipient.Text.Trim();

            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                lblStatus.ForeColor = Color.FromArgb(163, 45, 45);
                lblStatus.Text      = "Please enter a valid email address.";
                return;
            }

            if (string.IsNullOrEmpty(_submission.LocalFilePath) ||
                !File.Exists(_submission.LocalFilePath))
            {
                MessageBox.Show(
                    "The local file for this submission no longer exists.\n\n" +
                    "File path: " + _submission.LocalFilePath,
                    "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSend.Enabled = false;
            btnSend.Text    = "Sending…";
            lblStatus.ForeColor = Color.Gray;
            lblStatus.Text      = "Connecting to mail server…";

            try
            {
                // Send the email with attachment
                await FirestoreService.Instance.Gmail.SendDocumentAsync(
                    email,
                    email,   // use email as display name if no name stored
                    _submission.LocalFilePath,
                    _submission.FileName,
                    _submission,
                    txtNotes.Text.Trim());

                // Save recipient for future use
                await FirestoreService.Instance.SaveEmailRecipientAsync(email);

                // Log the action
                await FirestoreService.Instance.InsertLogAsync(
                    SessionManager.Current?.FullName ?? "",
                    "EmailSent",
                    $"Document '{_submission.DocumentName}' emailed to {email}",
                    "file");

                MessageBox.Show(
                    $"Document sent successfully to {email}.",
                    "Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.FromArgb(163, 45, 45);
                lblStatus.Text      = $"Failed: {ex.Message}";
                btnSend.Enabled     = true;
                btnSend.Text        = "📧  Send";
            }
        }
    }
}
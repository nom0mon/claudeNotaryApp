using Google.Cloud.Firestore;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace LegalOfficeApp
{
    public class GmailService
    {
        private readonly FirestoreDb _db;

        private string? _senderEmail;
        private string? _appPassword;
        private string? _displayName;
        private string? _recipientEmail; 
        private string? _recipientName;

        public GmailService(FirestoreDb db)
        {
            _db = db;
        }

        // ── Load sender config ───────────────────────────────
        private async Task EnsureConfigLoadedAsync()
        {
            if (_senderEmail != null) return;

            var snap = await _db.Collection("config").Document("email").GetSnapshotAsync();

            if (!snap.Exists)
                throw new InvalidOperationException(
                    "Firestore document 'config/email' not found. " +
                    "Add sender_email, app_password, sender_display_name fields.");

            // Read all values first before assigning anything
            string senderEmail = snap.ContainsField("sender_email")
                ? snap.GetValue<string>("sender_email")
                : throw new InvalidOperationException("'sender_email' field missing from config/email.");

            string appPassword = snap.ContainsField("app_password")
                ? snap.GetValue<string>("app_password")
                : throw new InvalidOperationException("'app_password' field missing from config/email.");

            string displayName = snap.ContainsField("sender_display_name")
                ? snap.GetValue<string>("sender_display_name")
                : throw new InvalidOperationException("'sender_display_name' field missing from config/email.");

            // Optional fields — default gracefully
            string recipientEmail = snap.ContainsField("recipient_email")
                ? snap.GetValue<string>("recipient_email") : "";

            string recipientName = snap.ContainsField("recipient_name")
                ? snap.GetValue<string>("recipient_name") : recipientEmail;

            // Only assign after all reads succeed
            _senderEmail    = senderEmail;
            _appPassword    = appPassword;
            _displayName    = displayName;
            _recipientEmail = recipientEmail;
            _recipientName  = recipientName;
        }

        // ── Core send with attachment ────────────────────────
        public async Task SendDocumentAsync(
            string toEmail,
            string toName,
            string filePath,
            string fileName,
            Submission submission,
            string? notes = null)
        {
            await EnsureConfigLoadedAsync();

            if (!File.Exists(filePath))
                throw new FileNotFoundException(
                    $"File not found at path:\n{filePath}\n\n" +
                    "The file may have been moved or deleted after submission.");

            string notesRow = string.IsNullOrWhiteSpace(notes)
                ? ""
                : $"<tr><td style='padding:4px 0;color:#555;'>Notes</td>" +
                  $"<td style='padding:4px 16px;'>{notes}</td></tr>";

            string bookRow = string.IsNullOrWhiteSpace(submission.BookNumber)
                ? ""
                : $"<tr><td style='padding:4px 0;color:#555;'>Book Number</td>" +
                  $"<td style='padding:4px 16px;font-weight:600;'>{submission.BookNumber}</td></tr>";

            string html = $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family:Arial,sans-serif;background:#f5f6fa;padding:32px;">
                  <div style="max-width:520px;margin:auto;background:#fff;border-radius:8px;
                              box-shadow:0 2px 8px rgba(0,0,0,.08);overflow:hidden;">
                    <div style="background:#0a1a6b;padding:24px 32px;">
                      <h2 style="color:#fff;margin:0;font-size:18px;">Document Transmittal</h2>
                      <p style="color:#b3c0e8;margin:4px 0 0;font-size:13px;">
                        Legal Office – Calamba City
                      </p>
                    </div>
                    <div style="padding:28px 32px;">
                      <p style="color:#222;">Dear <strong>{toName}</strong>,</p>
                      <p style="color:#444;">
                        Please find the attached document from the Legal Office – Calamba City.
                      </p>
                      <table style="border-collapse:collapse;width:100%;margin:16px 0;">
                        <tr>
                          <td style="padding:4px 0;color:#555;">Document Name</td>
                          <td style="padding:4px 16px;font-weight:600;">{submission.DocumentName}</td>
                        </tr>
                        <tr>
                          <td style="padding:4px 0;color:#555;">Date of Commission</td>
                          <td style="padding:4px 16px;">{submission.DateOfCommission}</td>
                        </tr>
                        {bookRow}
                        <tr>
                          <td style="padding:4px 0;color:#555;">Sent By</td>
                          <td style="padding:4px 16px;">{SessionManager.Current?.FullName}</td>
                        </tr>
                        {notesRow}
                      </table>
                      <p style="color:#888;font-size:12px;margin-top:24px;">
                        This document was transmitted via the Legal Office document management system.
                      </p>
                    </div>
                    <div style="background:#f0f2fb;padding:16px 32px;text-align:center;">
                      <p style="color:#999;font-size:11px;margin:0;">
                        Legal Office – Calamba City · City Hall, Calamba, Laguna
                      </p>
                    </div>
                  </div>
                </body>
                </html>
                """;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_displayName, _senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = $"Document Transmittal – {submission.DocumentName}";

            var builder = new BodyBuilder
            {
                HtmlBody = html,
                TextBody = System.Text.RegularExpressions.Regex
                               .Replace(html, "<[^>]+>", " ").Trim()
            };
            builder.Attachments.Add(filePath);
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_senderEmail, _appPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    
      public async Task SendBatchDocumentAsync(
        string       toEmail,
        string       toName,
        List<string> filePaths,
        List<string> fileNames,
        List<string> documentNames,
        string?      notes  = null,
        string?      sentBy = null)   // <-- ADD THIS
    {
        await EnsureConfigLoadedAsync();

        var missing = filePaths.Where(p => !File.Exists(p)).ToList();
        if (missing.Any())
            throw new FileNotFoundException(
                $"The following files were not found:\n{string.Join("\n", missing)}");

        string notesRow = string.IsNullOrWhiteSpace(notes)
            ? ""
            : $"<tr><td style='padding:4px 0;color:#555;'>Notes</td>" +
              $"<td style='padding:4px 16px;'>{notes}</td></tr>";

        string docRows = string.Join("", documentNames.Select((name, i) =>
            $"<tr><td style='padding:4px 8px;border-bottom:1px solid #eee;'>{i + 1}</td>" +
            $"<td style='padding:4px 8px;border-bottom:1px solid #eee;font-weight:600;'>{name}</td>" +
            $"<td style='padding:4px 8px;border-bottom:1px solid #eee;color:#666;'>{fileNames[i]}</td></tr>"));

        // ── USE sentBy PARAMETER, fall back to session, then "System" ──
        string senderName = sentBy
            ?? SessionManager.Current?.FullName
            ?? "System (Scheduled)";

        string html = $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:Arial,sans-serif;background:#f5f6fa;padding:32px;">
              <div style="max-width:580px;margin:auto;background:#fff;border-radius:8px;
                          box-shadow:0 2px 8px rgba(0,0,0,.08);overflow:hidden;">
                <div style="background:#0a1a6b;padding:24px 32px;">
                  <h2 style="color:#fff;margin:0;font-size:18px;">Batch Document Transmittal</h2>
                  <p style="color:#b3c0e8;margin:4px 0 0;font-size:13px;">
                    Legal Office – Calamba City
                  </p>
                </div>
                <div style="padding:28px 32px;">
                  <p style="color:#222;">Dear <strong>{toName}</strong>,</p>
                  <p style="color:#444;">
                    Please find the attached documents ({filePaths.Count} file{(filePaths.Count > 1 ? "s" : "")})
                    from the Legal Office – Calamba City.
                  </p>
                  <table style="border-collapse:collapse;width:100%;margin:16px 0;
                                border:1px solid #eee;border-radius:4px;">
                    <thead>
                      <tr style="background:#f5f5f8;">
                        <th style="padding:8px;text-align:left;color:#555;font-size:12px;">#</th>
                        <th style="padding:8px;text-align:left;color:#555;font-size:12px;">Document</th>
                        <th style="padding:8px;text-align:left;color:#555;font-size:12px;">File</th>
                      </tr>
                    </thead>
                    <tbody>{docRows}</tbody>
                  </table>
                  <table style="border-collapse:collapse;width:100%;margin:16px 0;">
                    <tr>
                      <td style="padding:4px 0;color:#555;">Sent By</td>
                      <td style="padding:4px 16px;">{senderName}</td>
                    </tr>
                    <tr>
                      <td style="padding:4px 0;color:#555;">Date Sent</td>
                      <td style="padding:4px 16px;">{DateTime.Now:MMMM dd, yyyy – hh:mm tt}</td>
                    </tr>
                    {notesRow}
                  </table>
                  <p style="color:#888;font-size:12px;margin-top:24px;">
                    This document batch was transmitted via the Legal Office document management system.
                  </p>
                </div>
                <div style="background:#f0f2fb;padding:16px 32px;text-align:center;">
                  <p style="color:#999;font-size:11px;margin:0;">
                    Legal Office – Calamba City · City Hall, Calamba, Laguna
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_displayName, _senderEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Batch Document Transmittal – {filePaths.Count} File{(filePaths.Count > 1 ? "s" : "")} – {DateTime.Now:MMM dd, yyyy}";

        var builder = new BodyBuilder
        {
            HtmlBody = html,
            TextBody = System.Text.RegularExpressions.Regex
                          .Replace(html, "<[^>]+>", " ").Trim()
        };

        foreach (var path in filePaths)
            builder.Attachments.Add(path);

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_senderEmail, _appPassword);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

      public async Task SendBatchDocumentFromDriveAsync(
      string       toEmail,
      string       toName,
      List<string> driveFileIds,
      List<string> fileNames,
      List<string> documentNames,
      string?      notes  = null,
      string?      sentBy = null)
    {
        await EnsureConfigLoadedAsync();

        string tempDir = Path.Combine(Path.GetTempPath(), "LegalOfficeScheduled_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var tempFilePaths = new List<string>();

        try
        {
            // Download each Drive file to a temp local path
            for (int i = 0; i < driveFileIds.Count; i++)
            {
                string tempPath = Path.Combine(tempDir, fileNames[i]);
                await FirestoreService.Instance.Drive.DownloadAsync(driveFileIds[i], tempPath);
                tempFilePaths.Add(tempPath);
            }

            // Reuse the existing batch-send logic, now pointed at temp files
            await SendBatchDocumentAsync(
                toEmail, toName, tempFilePaths, fileNames, documentNames, notes, sentBy);
        }
        finally
        {
            // Clean up temp files regardless of success/failure
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
    }
}
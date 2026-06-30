using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LegalOfficeApp
{
    public sealed class SchedulerService : IDisposable
    {
        private static SchedulerService? _instance;
        public  static SchedulerService  Instance =>
            _instance ??= new SchedulerService();

        private System.Threading.Timer? _timer;
        private bool _isProcessing = false;  // re-entrancy guard
        private SynchronizationContext? _syncCtx;

        private SchedulerService() { }

        public void Start()
        {
            // Capture AFTER UI is running — called from MainForm constructor
            _syncCtx = SynchronizationContext.Current;

            _timer = new System.Threading.Timer(
                callback: _ => FireAndForget(ProcessOverdueAsync()),
                state:    null,
                dueTime:  TimeSpan.FromSeconds(30),   // first check after 30s
                period:   TimeSpan.FromSeconds(60));
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose() => Stop();

        // Safe fire-and-forget that surfaces exceptions to the log
        private static void FireAndForget(Task t)
        {
            t.ContinueWith(
                faulted => _ = LogCrashAsync(faulted.Exception?.Flatten().Message ?? "unknown"),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private static async Task LogCrashAsync(string msg)
        {
            try
            {
                await FirestoreService.Instance.InsertLogAsync(
                    "System", "SchedulerCrash",
                    $"Unhandled scheduler exception: {msg}", "file");
            }
            catch { }
        }

        // ── Main poll ────────────────────────────────────────────
        private async Task ProcessOverdueAsync()
        {
            // Prevent overlapping runs if send takes > 60s
            if (Interlocked.Exchange(ref _isProcessingInt, 1) == 1) return;

            try
            {
                var overdue = await FirestoreService.Instance.GetOverdueSchedulesAsync();

                foreach (var schedule in overdue)
                    await SendScheduleAsync(schedule);
            }
            catch (Exception ex)
            {
                await LogCrashAsync($"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Interlocked.Exchange(ref _isProcessingInt, 0);
            }
        }

        private int _isProcessingInt = 0; // backing field for Interlocked

        // ── Send one schedule ────────────────────────────────────
        private async Task SendScheduleAsync(ScheduledEmail schedule)
        {
            // Lock it immediately so parallel polls don't double-send
            await FirestoreService.Instance.UpdateScheduleStatusAsync(
                schedule.Id, "Sending");

            try
            {
                await FirestoreService.Instance.InsertLogAsync(
                    "System", "SchedulerAttempt",
                    $"Sending schedule {schedule.Id} → {schedule.RecipientEmail} | " +
                    $"Files: {string.Join(", ", schedule.FilePaths)} | " +
                    $"ScheduledAt(UTC): {schedule.ScheduledAt:o} | " +
                    $"Now(UTC): {DateTime.UtcNow:o}",
                    "file");

                await FirestoreService.Instance.Gmail.SendBatchDocumentAsync(
                    toEmail:       schedule.RecipientEmail,
                    toName:        schedule.RecipientEmail,
                    filePaths:     schedule.FilePaths,
                    fileNames:     schedule.FileNames,
                    documentNames: schedule.DocumentNames,
                    notes:         schedule.Notes ?? "",
                    sentBy:        schedule.CreatedBy);

                await FirestoreService.Instance.UpdateScheduleStatusAsync(
                    schedule.Id, "Sent");

                string docList = string.Join(", ",
                    schedule.DocumentNames.Select(d => $"'{d}'"));

                await FirestoreService.Instance.InsertLogAsync(
                    schedule.CreatedBy, "EmailSent",
                    $"Scheduled send OK — {schedule.DocumentNames.Count} doc(s) " +
                    $"to {schedule.RecipientEmail}: {docList}",
                    "file");

                // Show toast on UI thread
                _syncCtx?.Post(_ =>
                {
                    MessageBox.Show(
                        $"Scheduled email delivered to {schedule.RecipientEmail}\n\n" +
                        string.Join("\n", schedule.DocumentNames.Select(d => $"• {d}")),
                        "Scheduled Email Sent",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }, null);
            }
            catch (Exception ex)
            {
                string fullError =
                    $"{ex.GetType().Name}: {ex.Message}" +
                    (ex.InnerException != null
                        ? $" | Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
                        : "") +
                    $"\nStack: {ex.StackTrace}";

                await FirestoreService.Instance.InsertLogAsync(
                    "System", "ScheduledEmailFailed",
                    $"Schedule {schedule.Id} FAILED: {fullError}", "file");

                // Revert to Pending so it retries next poll
                await FirestoreService.Instance.UpdateScheduleStatusAsync(
                    schedule.Id, "Pending");
            }
        }
    }
}
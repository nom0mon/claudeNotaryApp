using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Google.Api.Gax.Grpc;

namespace LegalOfficeApp
{
    // ═══════════════════════════════════════════════════════════
    //  DATA MODELS
    // ═══════════════════════════════════════════════════════════

    public class Submission
    {
        public string   Id               { get; set; } = "";   // Firestore document ID
        public string   BookNumber       { get; set; } = "";
        public string   NotaryName       { get; set; } = "";
        public string   PtrNumber        { get; set; } = "";
        public string   IbpNumber        { get; set; } = "";
        public string   DateOfCommission { get; set; } = "";
        public string   DocumentName     { get; set; } = "";
        public string   MegaLink         { get; set; } = "";
        public string   FileName         { get; set; } = "";
        public string   LocalFilePath    { get; set; } = "";
        public string   Status           { get; set; } = "Pending";
        public string   Remarks          { get; set; } = "";
        public string   SubmittedBy      { get; set; } = "";
        public DateTime DateSubmitted    { get; set; } = DateTime.UtcNow;
        public DateTime? DateReviewed   { get; set; }
        public string   ReviewedBy       { get; set; } = "";
    }

    public class ActivityLog
    {
        public string   Id        { get; set; } = "";
        public string   User      { get; set; } = "";
        public string   Action    { get; set; } = "";
        public string   Details   { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string   Category  { get; set; } = "file";   // "file" | "account"
    }

    public class AppUser
    {
        public string Id           { get; set; } = "";
        public string Username     { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role         { get; set; } = "Staff";   // "Admin" | "Staff"
        public string FullName     { get; set; } = "";
        public bool   IsActive     { get; set; } = true;
    }

    // ═══════════════════════════════════════════════════════════
    //  FIRESTORE SERVICE  (replaces DatabaseService entirely)
    // ═══════════════════════════════════════════════════════════

    public class FirestoreService
    {
        // ── Collection names ──────────────────────────────────
        private const string ColSubmissions = "submissions";
        private const string ColUsers       = "users";
        private const string ColLogs        = "activityLogs";

        private readonly FirestoreDb _db;

        // ── Singleton ─────────────────────────────────────────
        private static FirestoreService? _instance;
        public  static FirestoreService  Instance =>
            _instance ??= new FirestoreService();

        private FirestoreService()
        {
          var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream("firebase-credentials.json")
                ?? throw new FileNotFoundException(
                    "Embedded resource 'firebase-credentials.json' not found. " +
                    "Ensure it is marked as EmbeddedResource in the .csproj.");

            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            string projectId = doc.RootElement.GetProperty("project_id").GetString()
                ?? throw new InvalidOperationException("project_id missing from credentials JSON.");

            _db = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                JsonCredentials = json
            }.Build();

            // _db = FirestoreDb.Create(projectId, db);
        }

        // ════════════════════════════════════════════════════════
        //  INITIALISE  (call once on app start — seeds admin)
        // ════════════════════════════════════════════════════════

        public async Task InitializeAsync()
        {
            // Seed a default admin account if no users exist yet
            var users = await _db.Collection(ColUsers)
                                 .Limit(1)
                                 .GetSnapshotAsync();
            if (users.Count == 0)
            {
                await _db.Collection(ColUsers).AddAsync(new Dictionary<string, object>
                {
                    ["username"]     = "admin",
                    ["passwordHash"] = HashPassword("admin123"),
                    ["role"]         = "Admin",
                    ["fullName"]     = "Legal Admin",
                    ["isActive"]     = true
                });
            }
        }

        // ════════════════════════════════════════════════════════
        //  USERS
        // ════════════════════════════════════════════════════════

        public async Task<AppUser?> ValidateLoginAsync(string username, string password)
        {
            string hash = HashPassword(password);

            QuerySnapshot snap = await _db.Collection(ColUsers)
                .WhereEqualTo("username", username)
                .WhereEqualTo("isActive", true)
                .Limit(1)
                .GetSnapshotAsync();

            if (snap.Count == 0) return null;

            var doc = snap.Documents[0];
            string stored = doc.GetValue<string>("passwordHash");
            if (stored != hash) return null;

            return DocToUser(doc);
        }

        public async Task CreateUserAsync(AppUser user, string plainPassword)
        {
            await _db.Collection(ColUsers).AddAsync(new Dictionary<string, object>
            {
                ["username"]     = user.Username,
                ["passwordHash"] = HashPassword(plainPassword),
                ["role"]         = user.Role,
                ["fullName"]     = user.FullName,
                ["isActive"]     = true
            });
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
        {
            var snap = await _db.Collection(ColUsers)
                                .OrderBy("username")
                                .GetSnapshotAsync();
            return snap.Documents.Select(DocToUser).ToList();
        }

        public async Task UpdateUserAsync(string id, string fullName, string role, bool isActive)
        {
            await _db.Collection(ColUsers).Document(id).UpdateAsync(
                new Dictionary<string, object>
                {
                    ["fullName"] = fullName,
                    ["role"]     = role,
                    ["isActive"] = isActive
                });
        }

        public async Task DeactivateUserAsync(string id)
        {
            await _db.Collection(ColUsers).Document(id)
                     .UpdateAsync("isActive", false);
        }

        public async Task DeleteUserAsync(string id)
        {
            await _db.Collection(ColUsers).Document(id).DeleteAsync();
        }

        public async Task ChangePasswordAsync(string id, string newPlainPassword)
        {
            await _db.Collection(ColUsers).Document(id)
                     .UpdateAsync("passwordHash", HashPassword(newPlainPassword));
        }

        // ════════════════════════════════════════════════════════
        //  SUBMISSIONS
        // ════════════════════════════════════════════════════════

        public async Task<string> InsertSubmissionAsync(Submission s)
        {
            var doc = await _db.Collection(ColSubmissions).AddAsync(SubmissionToDict(s));
            return doc.Id;
        }

        public async Task UpdateSubmissionMegaLinkAsync(string id, string megaLink)
        {
            await _db.Collection(ColSubmissions).Document(id)
                     .UpdateAsync("megaLink", megaLink);
        }

        public async Task UpdateSubmissionAsync(Submission s)
        {
            await _db.Collection(ColSubmissions).Document(s.Id).UpdateAsync(
                new Dictionary<string, object>
                {
                    ["bookNumber"]       = s.BookNumber,
                    ["notaryName"]       = s.NotaryName,
                    ["ptrNumber"]        = s.PtrNumber,
                    ["ibpNumber"]        = s.IbpNumber,
                    ["dateOfCommission"] = s.DateOfCommission,
                    ["documentName"]     = s.DocumentName
                });
        }

        public async Task<(string Email, string Password)> GetMegaCredentialsAsync()
        {
            var doc = await _db.Collection("config")
                            .Document("mega")
                            .GetSnapshotAsync();

            if (!doc.Exists)
                throw new Exception(
                    "MEGA credentials not found in Firestore.\n" +
                    "Go to Firebase Console → Firestore → config/mega " +
                    "and add 'email' and 'password' fields.");

            string email    = doc.GetValue<string>("email");
            string password = doc.GetValue<string>("password");

            return (email, password);
        }
        public async Task ReviewSubmissionAsync(string id, string status,
                                                string remarks, string reviewedBy)
        {
            await _db.Collection(ColSubmissions).Document(id).UpdateAsync(
                new Dictionary<string, object>
                {
                    ["status"]       = status,
                    ["remarks"]      = remarks,
                    ["reviewedBy"]   = reviewedBy,
                    ["dateReviewed"] = DateTime.UtcNow.ToString("o")
                });
        }

        public async Task DeleteSubmissionAsync(string id)
        {
            await _db.Collection(ColSubmissions).Document(id).DeleteAsync();
        }

        /// <summary>
        /// Returns submissions filtered by status and/or notary name.
        /// Ordered by dateSubmitted descending.
        /// </summary>
        public async Task<List<Submission>> GetSubmissionsAsync(
            string? statusFilter = null,
            string? nameSearch   = null)
        {
            Query q = _db.Collection(ColSubmissions);

            if (!string.IsNullOrEmpty(statusFilter))
                q = q.WhereEqualTo("status", statusFilter);

            // Firestore doesn't support LIKE; fetch all and filter client-side for name search
            QuerySnapshot snap = await q.OrderByDescending("dateSubmitted").GetSnapshotAsync();

            var list = snap.Documents
                           .Select(DocToSubmission)
                           .ToList();

            if (!string.IsNullOrEmpty(nameSearch))
            {
                string lower = nameSearch.ToLower();
                list = list.Where(s => s.NotaryName.ToLower().Contains(lower)).ToList();
            }

            return list;
        }

        public async Task<(int Total, int Pending, int Approved, int Rejected)>
            GetDashboardCountsAsync()
        {
            var snap = await _db.Collection(ColSubmissions).GetSnapshotAsync();
            int total    = snap.Count;
            int pending  = snap.Documents.Count(d => d.GetValue<string>("status") == "Pending");
            int approved = snap.Documents.Count(d => d.GetValue<string>("status") == "Approved");
            int rejected = snap.Documents.Count(d => d.GetValue<string>("status") == "Rejected");
            return (total, pending, approved, rejected);
        }

        public async Task<int[]> GetMonthlySubmissionsAsync(int year)
        {
            var snap = await _db.Collection(ColSubmissions).GetSnapshotAsync();
            var counts = new int[12];
            foreach (var doc in snap.Documents)
            {
                string ds = doc.ContainsField("dateSubmitted")
                    ? doc.GetValue<string>("dateSubmitted") : "";
                if (DateTime.TryParse(ds, out var dt) && dt.Year == year)
                    counts[dt.Month - 1]++;
            }
            return counts;
        }

        // ════════════════════════════════════════════════════════
        //  ACTIVITY LOGS
        // ════════════════════════════════════════════════════════

        public async Task InsertLogAsync(string user, string action,
                                         string details, string category = "file")
        {
            await _db.Collection(ColLogs).AddAsync(new Dictionary<string, object>
            {
                ["user"]      = user,
                ["action"]    = action,
                ["details"]   = details,
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["category"]  = category
            });
        }

        public async Task<List<ActivityLog>> GetLogsAsync(
            string?   actionFilter   = null,
            DateTime? from           = null,
            DateTime? to             = null,
            string?   categoryFilter = null)
        {
            Query q = _db.Collection(ColLogs);

            if (!string.IsNullOrEmpty(actionFilter))
                q = q.WhereEqualTo("action", actionFilter);

            if (!string.IsNullOrEmpty(categoryFilter))
                q = q.WhereEqualTo("category", categoryFilter);

            QuerySnapshot snap = await q.OrderByDescending("timestamp").GetSnapshotAsync();

            var list = snap.Documents.Select(DocToLog).ToList();

            // Date range filter client-side (avoids composite index requirement)
            if (from.HasValue)
                list = list.Where(l => l.Timestamp.Date >= from.Value.Date).ToList();
            if (to.HasValue)
                list = list.Where(l => l.Timestamp.Date <= to.Value.Date).ToList();

            return list;
        }

        // ════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════

        public static string HashPassword(string plain)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] bytes  = System.Text.Encoding.UTF8.GetBytes(plain);
            byte[] hash   = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        private static Dictionary<string, object> SubmissionToDict(Submission s) =>
            new()
            {
                ["bookNumber"]       = s.BookNumber,
                ["notaryName"]       = s.NotaryName,
                ["ptrNumber"]        = s.PtrNumber,
                ["ibpNumber"]        = s.IbpNumber,
                ["dateOfCommission"] = s.DateOfCommission,
                ["documentName"]     = s.DocumentName,
                ["localFilePath"]    = s.LocalFilePath,
                ["megaLink"]         = s.MegaLink,
                ["fileName"]         = s.FileName,
                ["status"]           = s.Status,
                ["remarks"]          = s.Remarks,
                ["submittedBy"]      = s.SubmittedBy,
                ["dateSubmitted"]    = s.DateSubmitted.ToString("o"),
                ["dateReviewed"]     = s.DateReviewed?.ToString("o") ?? "",
                ["reviewedBy"]       = s.ReviewedBy
            };

        private static Submission DocToSubmission(DocumentSnapshot d) => new()
        {
            Id               = d.Id,
            BookNumber       = d.ContainsField("bookNumber")       ? d.GetValue<string>("bookNumber")       : "",
            NotaryName       = d.ContainsField("notaryName")       ? d.GetValue<string>("notaryName")       : "",
            PtrNumber        = d.ContainsField("ptrNumber")        ? d.GetValue<string>("ptrNumber")        : "",
            IbpNumber        = d.ContainsField("ibpNumber")        ? d.GetValue<string>("ibpNumber")        : "",
            DateOfCommission = d.ContainsField("dateOfCommission") ? d.GetValue<string>("dateOfCommission") : "",
            DocumentName     = d.ContainsField("documentName")     ? d.GetValue<string>("documentName")     : "",
            LocalFilePath    = d.ContainsField("localFilePath")    ? d.GetValue<string>("localFilePath")    : "",
            MegaLink         = d.ContainsField("megaLink")         ? d.GetValue<string>("megaLink")         : "",
            FileName         = d.ContainsField("fileName")         ? d.GetValue<string>("fileName")         : "",
            Status           = d.ContainsField("status")           ? d.GetValue<string>("status")           : "Pending",
            Remarks          = d.ContainsField("remarks")          ? d.GetValue<string>("remarks")          : "",
            SubmittedBy      = d.ContainsField("submittedBy")      ? d.GetValue<string>("submittedBy")      : "",
            ReviewedBy       = d.ContainsField("reviewedBy")       ? d.GetValue<string>("reviewedBy")       : "",
            DateSubmitted    = d.ContainsField("dateSubmitted") &&
                               DateTime.TryParse(d.GetValue<string>("dateSubmitted"), out var ds)
                               ? ds : DateTime.UtcNow,
            DateReviewed     = d.ContainsField("dateReviewed") &&
                               !string.IsNullOrEmpty(d.GetValue<string>("dateReviewed")) &&
                               DateTime.TryParse(d.GetValue<string>("dateReviewed"), out var dr)
                               ? dr : null
        };

        private static ActivityLog DocToLog(DocumentSnapshot d) => new()
        {
            Id        = d.Id,
            User      = d.ContainsField("user")      ? d.GetValue<string>("user")      : "",
            Action    = d.ContainsField("action")    ? d.GetValue<string>("action")    : "",
            Details   = d.ContainsField("details")   ? d.GetValue<string>("details")   : "",
            Category  = d.ContainsField("category")  ? d.GetValue<string>("category")  : "file",
            Timestamp = d.ContainsField("timestamp") &&
                        DateTime.TryParse(d.GetValue<string>("timestamp"), out var ts)
                        ? ts : DateTime.UtcNow
        };

        private static AppUser DocToUser(DocumentSnapshot d) => new()
        {
            Id           = d.Id,
            Username     = d.ContainsField("username") ? d.GetValue<string>("username") : "",
            PasswordHash = d.ContainsField("passwordHash") ? d.GetValue<string>("passwordHash") : "",
            Role         = d.ContainsField("role")     ? d.GetValue<string>("role")     : "Staff",
            FullName     = d.ContainsField("fullName") ? d.GetValue<string>("fullName") : "",
            IsActive     = d.ContainsField("isActive") && d.GetValue<bool>("isActive")
        };
    }
}

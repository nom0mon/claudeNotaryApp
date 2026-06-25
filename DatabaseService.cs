using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace LegalOfficeApp
{
    // ═══════════════════════════════════════════════════════════
    //  DATA MODELS
    // ═══════════════════════════════════════════════════════════

    public class Submission
    {
        public int      Id               { get; set; }
        public string   BookNumber       { get; set; } = "";
        public string   NotaryName       { get; set; } = "";
        public string   PtrNumber        { get; set; } = "";
        public string   IbpNumber        { get; set; } = "";
        public string   DateOfCommission { get; set; } = "";
        public string   YearCovered      { get; set; } = "";
        public string   LocalFilePath    { get; set; } = "";
        public string   MegaLink         { get; set; } = "";
        public string   FileName         { get; set; } = "";
        public string   Status           { get; set; } = "Pending";
        public string   Remarks          { get; set; } = "";
        public string   SubmittedBy      { get; set; } = "";
        public DateTime DateSubmitted    { get; set; } = DateTime.Now;
        public DateTime? DateReviewed   { get; set; }
        public string   ReviewedBy       { get; set; } = "";
        // Firestore document ID for cross-device sync
        public string   FirestoreId      { get; set; } = "";
    }

    public class ActivityLog
    {
        public int      Id        { get; set; }
        public string   User      { get; set; } = "";
        public string   Action    { get; set; } = "";
        public string   Details   { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        // "file" | "account" — used to filter activity for non-admin users
        public string   Category  { get; set; } = "file";
    }

    public class AppUser
    {
        public int    Id           { get; set; }
        public string Username     { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role         { get; set; } = "Staff";
        public string FullName     { get; set; } = "";
        public bool   IsActive     { get; set; } = true;
    }

    // ═══════════════════════════════════════════════════════════
    //  DATABASE SERVICE
    // ═══════════════════════════════════════════════════════════

    public class DatabaseService
    {
        private static readonly string DbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "legaloffice.db");

        private static string ConnStr => $"Data Source={DbPath};";

        private static DatabaseService? _instance;
        public  static DatabaseService  Instance =>
            _instance ??= new DatabaseService();

        private DatabaseService() { }

        // ── Initialise ────────────────────────────────────────
        public void Initialize()
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();

            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA foreign_keys = ON;

                CREATE TABLE IF NOT EXISTS Users (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username     TEXT    NOT NULL UNIQUE,
                    PasswordHash TEXT    NOT NULL,
                    Role         TEXT    NOT NULL DEFAULT 'Staff',
                    FullName     TEXT    NOT NULL DEFAULT '',
                    IsActive     INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS Submissions (
                    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    BookNumber       TEXT    NOT NULL,
                    NotaryName       TEXT    NOT NULL,
                    PtrNumber        TEXT    NOT NULL,
                    IbpNumber        TEXT    NOT NULL DEFAULT '',
                    DateOfCommission TEXT    NOT NULL DEFAULT '',
                    YearCovered      TEXT    NOT NULL DEFAULT '',
                    LocalFilePath    TEXT    NOT NULL DEFAULT '',
                    MegaLink         TEXT    NOT NULL DEFAULT '',
                    FileName         TEXT    NOT NULL DEFAULT '',
                    Status           TEXT    NOT NULL DEFAULT 'Pending',
                    Remarks          TEXT    NOT NULL DEFAULT '',
                    SubmittedBy      TEXT    NOT NULL DEFAULT '',
                    DateSubmitted    TEXT    NOT NULL,
                    DateReviewed     TEXT,
                    ReviewedBy       TEXT    NOT NULL DEFAULT '',
                    FirestoreId      TEXT    NOT NULL DEFAULT ''
                );

                CREATE TABLE IF NOT EXISTS ActivityLogs (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    User      TEXT    NOT NULL,
                    Action    TEXT    NOT NULL,
                    Details   TEXT    NOT NULL DEFAULT '',
                    Timestamp TEXT    NOT NULL,
                    Category  TEXT    NOT NULL DEFAULT 'file'
                );
            ";
            cmd.ExecuteNonQuery();

            // Migrate: add columns if upgrading from old schema
            MigrateSchema(conn);
            SeedDefaultAdmin(conn);
        }

        // Safely add new columns to existing databases (idempotent)
        private void MigrateSchema(SqliteConnection conn)
        {
            var migrations = new[]
            {
                "ALTER TABLE Submissions   ADD COLUMN FirestoreId TEXT NOT NULL DEFAULT '';",
                "ALTER TABLE ActivityLogs  ADD COLUMN Category    TEXT NOT NULL DEFAULT 'file';"
            };
            foreach (var sql in migrations)
            {
                try
                {
                    using var m = conn.CreateCommand();
                    m.CommandText = sql;
                    m.ExecuteNonQuery();
                }
                catch { /* column already exists — ignore */ }
            }
        }

        private void SeedDefaultAdmin(SqliteConnection conn)
        {
            using var check = conn.CreateCommand();
            check.CommandText = "SELECT COUNT(*) FROM Users;";
            long count = (long)(check.ExecuteScalar() ?? 0L);
            if (count > 0) return;

            using var ins = conn.CreateCommand();
            ins.CommandText = @"
                INSERT INTO Users (Username, PasswordHash, Role, FullName)
                VALUES ('admin', @hash, 'Admin', 'Admin Juan');";
            ins.Parameters.AddWithValue("@hash", HashPassword("admin123"));
            ins.ExecuteNonQuery();
        }

        // ════════════════════════════════════════════════════════
        //  USERS
        // ════════════════════════════════════════════════════════

        public AppUser? ValidateLogin(string username, string password)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Username, PasswordHash, Role, FullName, IsActive
                FROM Users WHERE Username = @u AND IsActive = 1;";
            cmd.Parameters.AddWithValue("@u", username);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            string storedHash = r.GetString(2);
            if (storedHash != HashPassword(password)) return null;

            return new AppUser
            {
                Id           = r.GetInt32(0),
                Username     = r.GetString(1),
                PasswordHash = storedHash,
                Role         = r.GetString(3),
                FullName     = r.GetString(4),
                IsActive     = r.GetInt32(5) == 1
            };
        }

        public void CreateUser(AppUser user, string plainPassword)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Users (Username, PasswordHash, Role, FullName, IsActive)
                VALUES (@u, @h, @r, @f, 1);";
            cmd.Parameters.AddWithValue("@u", user.Username);
            cmd.Parameters.AddWithValue("@h", HashPassword(plainPassword));
            cmd.Parameters.AddWithValue("@r", user.Role);
            cmd.Parameters.AddWithValue("@f", user.FullName);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Returns all users (for admin management screen).</summary>
        public List<AppUser> GetAllUsers()
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Role, FullName, IsActive FROM Users ORDER BY Id;";
            var list = new List<AppUser>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new AppUser
                {
                    Id       = r.GetInt32(0),
                    Username = r.GetString(1),
                    Role     = r.GetString(2),
                    FullName = r.GetString(3),
                    IsActive = r.GetInt32(4) == 1
                });
            }
            return list;
        }

        /// <summary>Update a user's role and full name (admin only).</summary>
        public void UpdateUser(int id, string fullName, string role, bool isActive)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Users SET FullName = @f, Role = @r, IsActive = @a WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@f",  fullName);
            cmd.Parameters.AddWithValue("@r",  role);
            cmd.Parameters.AddWithValue("@a",  isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Deactivate (soft-delete) a user account.</summary>
        public void DeactivateUser(int id)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET IsActive = 0 WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Change a user's password.</summary>
        public void ChangePassword(int id, string newPlainPassword)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "UPDATE Users SET PasswordHash = @h WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@h",  HashPassword(newPlainPassword));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ════════════════════════════════════════════════════════
        //  SUBMISSIONS
        // ════════════════════════════════════════════════════════

        public int InsertSubmission(Submission s)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Submissions
                    (BookNumber, NotaryName, PtrNumber, IbpNumber,
                     DateOfCommission, YearCovered, LocalFilePath,
                     MegaLink, FileName, Status, SubmittedBy, DateSubmitted, FirestoreId)
                VALUES
                    (@bn, @nn, @ptr, @ibp,
                     @doc, @yr, @lfp,
                     @ml, @fn, 'Pending', @sb, @ds, @fid);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@bn",  s.BookNumber);
            cmd.Parameters.AddWithValue("@nn",  s.NotaryName);
            cmd.Parameters.AddWithValue("@ptr", s.PtrNumber);
            cmd.Parameters.AddWithValue("@ibp", s.IbpNumber);
            cmd.Parameters.AddWithValue("@doc", s.DateOfCommission);
            cmd.Parameters.AddWithValue("@yr",  s.YearCovered);
            cmd.Parameters.AddWithValue("@lfp", s.LocalFilePath);
            cmd.Parameters.AddWithValue("@ml",  s.MegaLink);
            cmd.Parameters.AddWithValue("@fn",  s.FileName);
            cmd.Parameters.AddWithValue("@sb",  s.SubmittedBy);
            cmd.Parameters.AddWithValue("@ds",  s.DateSubmitted.ToString("o"));
            cmd.Parameters.AddWithValue("@fid", s.FirestoreId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdateSubmissionMegaLink(int id, string megaLink)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "UPDATE Submissions SET MegaLink = @ml WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@ml", megaLink);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void UpdateSubmissionFirestoreId(int id, string firestoreId)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "UPDATE Submissions SET FirestoreId = @fid WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@fid", firestoreId);
            cmd.Parameters.AddWithValue("@id",  id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Update editable submission fields (before approval).</summary>
        public void UpdateSubmission(Submission s)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Submissions
                SET BookNumber       = @bn,
                    NotaryName       = @nn,
                    PtrNumber        = @ptr,
                    IbpNumber        = @ibp,
                    DateOfCommission = @doc,
                    YearCovered      = @yr
                WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@bn",  s.BookNumber);
            cmd.Parameters.AddWithValue("@nn",  s.NotaryName);
            cmd.Parameters.AddWithValue("@ptr", s.PtrNumber);
            cmd.Parameters.AddWithValue("@ibp", s.IbpNumber);
            cmd.Parameters.AddWithValue("@doc", s.DateOfCommission);
            cmd.Parameters.AddWithValue("@yr",  s.YearCovered);
            cmd.Parameters.AddWithValue("@id",  s.Id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Permanently delete a submission record (admin only).</summary>
        public void DeleteSubmission(int id)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Submissions WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void ReviewSubmission(int id, string status, string remarks, string reviewedBy)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Submissions
                SET Status = @st, Remarks = @rm,
                    ReviewedBy = @rb, DateReviewed = @dr
                WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@st", status);
            cmd.Parameters.AddWithValue("@rm", remarks);
            cmd.Parameters.AddWithValue("@rb", reviewedBy);
            cmd.Parameters.AddWithValue("@dr", DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public List<Submission> GetSubmissions(string? statusFilter = null, string? nameSearch = null)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();

            string where = "WHERE 1=1";
            if (!string.IsNullOrEmpty(statusFilter))
            {
                where += " AND Status = @st";
                cmd.Parameters.AddWithValue("@st", statusFilter);
            }
            if (!string.IsNullOrEmpty(nameSearch))
            {
                where += " AND NotaryName LIKE @ns";
                cmd.Parameters.AddWithValue("@ns", $"%{nameSearch}%");
            }

            cmd.CommandText = $@"
                SELECT Id, BookNumber, NotaryName, PtrNumber, IbpNumber,
                       DateOfCommission, YearCovered, LocalFilePath,
                       MegaLink, FileName, Status, Remarks,
                       SubmittedBy, DateSubmitted, DateReviewed, ReviewedBy, FirestoreId
                FROM Submissions {where}
                ORDER BY DateSubmitted DESC;";

            var list = new List<Submission>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Submission
                {
                    Id               = r.GetInt32(0),
                    BookNumber       = r.GetString(1),
                    NotaryName       = r.GetString(2),
                    PtrNumber        = r.GetString(3),
                    IbpNumber        = r.GetString(4),
                    DateOfCommission = r.GetString(5),
                    YearCovered      = r.GetString(6),
                    LocalFilePath    = r.GetString(7),
                    MegaLink         = r.GetString(8),
                    FileName         = r.GetString(9),
                    Status           = r.GetString(10),
                    Remarks          = r.GetString(11),
                    SubmittedBy      = r.GetString(12),
                    DateSubmitted    = DateTime.Parse(r.GetString(13)),
                    DateReviewed     = r.IsDBNull(14) ? null : DateTime.Parse(r.GetString(14)),
                    ReviewedBy       = r.GetString(15),
                    FirestoreId      = r.IsDBNull(16) ? "" : r.GetString(16)
                });
            }
            return list;
        }

        public (int Total, int Pending, int Approved, int Rejected) GetDashboardCounts()
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*),
                       SUM(CASE WHEN Status = 'Pending'  THEN 1 ELSE 0 END),
                       SUM(CASE WHEN Status = 'Approved' THEN 1 ELSE 0 END),
                       SUM(CASE WHEN Status = 'Rejected' THEN 1 ELSE 0 END)
                FROM Submissions;";
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return (0, 0, 0, 0);
            return (r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3));
        }

        public int[] GetMonthlySubmissions(int year)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT strftime('%m', DateSubmitted) AS Month, COUNT(*) AS Total
                FROM Submissions
                WHERE strftime('%Y', DateSubmitted) = @yr
                GROUP BY Month;";
            cmd.Parameters.AddWithValue("@yr", year.ToString());

            var counts = new int[12];
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int month = int.Parse(r.GetString(0)) - 1;
                counts[month] = r.GetInt32(1);
            }
            return counts;
        }

        // ════════════════════════════════════════════════════════
        //  ACTIVITY LOGS
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// category: "file"    – submission / approval / rejection events  (visible to all)
        ///           "account" – user creation / deactivation events        (admin only)
        /// </summary>
        public void InsertLog(string user, string action, string details,
                              string category = "file")
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ActivityLogs (User, Action, Details, Timestamp, Category)
                VALUES (@u, @a, @d, @t, @c);";
            cmd.Parameters.AddWithValue("@u", user);
            cmd.Parameters.AddWithValue("@a", action);
            cmd.Parameters.AddWithValue("@d", details);
            cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("o"));
            cmd.Parameters.AddWithValue("@c", category);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Returns logs filtered by action, date range, and optionally category.
        /// Pass categoryFilter = "file" for staff view; null to see everything (admin).
        /// </summary>
        public List<ActivityLog> GetLogs(string? actionFilter  = null,
                                         DateTime? from        = null,
                                         DateTime? to          = null,
                                         string?   categoryFilter = null)
        {
            using var conn = Open();
            using var cmd  = conn.CreateCommand();

            string where = "WHERE 1=1";
            if (!string.IsNullOrEmpty(actionFilter))
            {
                where += " AND Action = @af";
                cmd.Parameters.AddWithValue("@af", actionFilter);
            }
            if (from.HasValue)
            {
                where += " AND Timestamp >= @fr";
                cmd.Parameters.AddWithValue("@fr", from.Value.ToString("o"));
            }
            if (to.HasValue)
            {
                where += " AND Timestamp <= @to";
                cmd.Parameters.AddWithValue("@to", to.Value.AddDays(1).ToString("o"));
            }
            if (!string.IsNullOrEmpty(categoryFilter))
            {
                where += " AND Category = @cat";
                cmd.Parameters.AddWithValue("@cat", categoryFilter);
            }

            cmd.CommandText = $@"
                SELECT Id, User, Action, Details, Timestamp, Category
                FROM ActivityLogs {where}
                ORDER BY Timestamp DESC;";

            var list = new List<ActivityLog>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ActivityLog
                {
                    Id        = r.GetInt32(0),
                    User      = r.GetString(1),
                    Action    = r.GetString(2),
                    Details   = r.GetString(3),
                    Timestamp = DateTime.Parse(r.GetString(4)),
                    Category  = r.IsDBNull(5) ? "file" : r.GetString(5)
                });
            }
            return list;
        }

        // ════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════

        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(ConnStr);
            conn.Open();
            return conn;
        }

        public static string HashPassword(string plain)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] bytes  = System.Text.Encoding.UTF8.GetBytes(plain);
            byte[] hash   = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}

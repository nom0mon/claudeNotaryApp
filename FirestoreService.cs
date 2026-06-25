using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace LegalOfficeApp
{
    public class FirestoreService
    {
        private readonly FirestoreDb _db;
        private const string Collection = "submissions";

        private static FirestoreService? _instance;
        public  static FirestoreService  Instance =>
            _instance ??= new FirestoreService();

        private FirestoreService()
        {
            // Point to your credentials file
            string credPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "firebase-credentials.json");

            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS", credPath);

            // Replace with your actual Firebase Project ID
            _db = FirestoreDb.Create("legaloffice-3b096");
        }

        // ── Save a new submission to Firestore ──────────────────
        public async Task<string> AddSubmissionAsync(Submission s)
        {
            var doc = new Dictionary<string, object>
            {
                ["bookNumber"]       = s.BookNumber,
                ["notaryName"]       = s.NotaryName,
                ["ptrNumber"]        = s.PtrNumber,
                ["ibpNumber"]        = s.IbpNumber,
                ["dateOfCommission"] = s.DateOfCommission,
                ["yearCovered"]      = s.YearCovered,
                ["fileName"]         = s.FileName,
                ["megaLink"]         = s.MegaLink,
                ["status"]           = s.Status,
                ["submittedBy"]      = s.SubmittedBy,
                ["dateSubmitted"]    = s.DateSubmitted.ToString("o"),
                ["remarks"]          = s.Remarks,
                ["reviewedBy"]       = s.ReviewedBy
            };

            DocumentReference docRef =
                await _db.Collection(Collection).AddAsync(doc);

            return docRef.Id;   // store this as FirestoreId in SQLite
        }

        // ── Update status after review ──────────────────────────
        public async Task UpdateStatusAsync(string firestoreId,
                                            string status,
                                            string remarks,
                                            string reviewedBy)
        {
            var updates = new Dictionary<string, object>
            {
                ["status"]       = status,
                ["remarks"]      = remarks,
                ["reviewedBy"]   = reviewedBy,
                ["dateReviewed"] = DateTime.Now.ToString("o")
            };

            await _db.Collection(Collection)
                     .Document(firestoreId)
                     .UpdateAsync(updates);
        }

        // ── Update MEGA link after upload ───────────────────────
        public async Task UpdateMegaLinkAsync(string firestoreId, string megaLink)
        {
            await _db.Collection(Collection)
                     .Document(firestoreId)
                     .UpdateAsync("megaLink", megaLink);
        }

        // ── Delete a submission ─────────────────────────────────
        public async Task DeleteSubmissionAsync(string firestoreId)
        {
            if (string.IsNullOrEmpty(firestoreId)) return;
            await _db.Collection(Collection)
                     .Document(firestoreId)
                     .DeleteAsync();
        }

        // ── Get all submissions (for sync) ──────────────────────
        public async Task<List<Submission>> GetAllSubmissionsAsync()
        {
            QuerySnapshot snap = await _db.Collection(Collection).GetSnapshotAsync();
            var list = new List<Submission>();
            foreach (DocumentSnapshot doc in snap.Documents)
            {
                var d = doc.ToDictionary();
                list.Add(new Submission
                {
                    FirestoreId      = doc.Id,
                    BookNumber       = d.GetValueOrDefault("bookNumber",       "").ToString()!,
                    NotaryName       = d.GetValueOrDefault("notaryName",       "").ToString()!,
                    PtrNumber        = d.GetValueOrDefault("ptrNumber",        "").ToString()!,
                    IbpNumber        = d.GetValueOrDefault("ibpNumber",        "").ToString()!,
                    DateOfCommission = d.GetValueOrDefault("dateOfCommission", "").ToString()!,
                    YearCovered      = d.GetValueOrDefault("yearCovered",      "").ToString()!,
                    FileName         = d.GetValueOrDefault("fileName",         "").ToString()!,
                    MegaLink         = d.GetValueOrDefault("megaLink",         "").ToString()!,
                    Status           = d.GetValueOrDefault("status",           "Pending").ToString()!,
                    SubmittedBy      = d.GetValueOrDefault("submittedBy",      "").ToString()!,
                    Remarks          = d.GetValueOrDefault("remarks",          "").ToString()!,
                    ReviewedBy       = d.GetValueOrDefault("reviewedBy",       "").ToString()!,
                    DateSubmitted    = DateTime.TryParse(
                        d.GetValueOrDefault("dateSubmitted","").ToString(), out var dt) ? dt : DateTime.Now
                });
            }
            return list;
        }
    }
}
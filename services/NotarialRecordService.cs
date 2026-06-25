using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

public class NotarialRecordService
{
    private readonly FirestoreDb _db;

    public NotarialRecordService(FirestoreDb db)
    {
        _db = db;
    }

    // Handles concurrent updates by checking the version field
    public async Task UpdateWithConcurrencyCheckAsync(string recordId, int expectedVersion, Dictionary<string, object> changes)
    {
        var docRef = _db.Collection("notarialRecords").Document(recordId);

        await _db.RunTransactionAsync(async transaction =>
        {
            var snap = await transaction.GetSnapshotAsync(docRef);
            if (!snap.Exists) throw new Exception("Record not found.");

            int currentVersion = snap.GetValue<int>("version");

            if (currentVersion != expectedVersion)
            {
                throw new Exception($"Record was modified by another user (v{currentVersion} vs expected v{expectedVersion}).");
            }

            changes["version"] = currentVersion + 1;
            changes["updatedAt"] = Timestamp.GetCurrentTimestamp();
            transaction.Update(docRef, changes);
        });
    }
}
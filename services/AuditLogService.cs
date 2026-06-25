using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

public class AuditLogService
{
    private readonly CollectionReference _logs;

    public AuditLogService(FirestoreDb db)
    {
        _logs = db.Collection("auditLogs");
    }

    public Task LogAsync(string uid, string action, string category, string targetType, string targetId, string details)
    {
        return _logs.AddAsync(new Dictionary<string, object>
        {
            ["userId"] = uid,
            ["action"] = action,
            ["category"] = category,
            ["targetType"] = targetType,
            ["targetId"] = targetId,
            ["details"] = details,
            ["timestamp"] = Timestamp.GetCurrentTimestamp()
        });
    }
}
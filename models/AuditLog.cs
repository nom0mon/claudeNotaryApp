using Google.Cloud.Firestore;

[FirestoreData]
public class AuditLog
{
    [FirestoreDocumentId] 
    public string LogId { get; set; }

    [FirestoreProperty("userId")] 
    public string UserId { get; set; }

    [FirestoreProperty("action")] 
    public string Action { get; set; }

    [FirestoreProperty("category")] 
    public string Category { get; set; }

    [FirestoreProperty("targetType")] 
    public string TargetType { get; set; }

    [FirestoreProperty("targetId")] 
    public string TargetId { get; set; }

    [FirestoreProperty("details")] 
    public string Details { get; set; }

    [FirestoreProperty("timestamp")] 
    public Timestamp Timestamp { get; set; }
}
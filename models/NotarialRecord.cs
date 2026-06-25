using Google.Cloud.Firestore;
using System;

[FirestoreData]
public class NotarialRecord
{
    [FirestoreDocumentId] 
    public string RecordId { get; set; }

    [FirestoreProperty("bookNumber")] 
    public string BookNumber { get; set; }

    [FirestoreProperty("notaryName")] 
    public string NotaryName { get; set; }

    [FirestoreProperty("clientId")] 
    public string ClientId { get; set; }

    [FirestoreProperty("documentType")] 
    public string DocumentType { get; set; }

    [FirestoreProperty("status")] 
    public string Status { get; set; }

    [FirestoreProperty("submittedBy")] 
    public string SubmittedBy { get; set; }

    [FirestoreProperty("dateSubmitted")] 
    public Timestamp DateSubmitted { get; set; }

    [FirestoreProperty("version")] 
    public int Version { get; set; }

    [FirestoreProperty("documentCount")] 
    public int DocumentCount { get; set; }
}
namespace LegalOfficeApp
{
    public class ScheduledEmail
    {
        public string       Id             { get; set; } = "";
        public List<string> SubmissionIds  { get; set; } = new();
        public List<string> DocumentNames  { get; set; } = new();
        public List<string> FilePaths      { get; set; } = new();
        public List<string> FileNames      { get; set; } = new();
        public string       RecipientEmail { get; set; } = "";
        public DateTime     ScheduledAt    { get; set; } = DateTime.UtcNow;
        public string       CreatedBy      { get; set; } = "";
        public string       Status         { get; set; } = "Pending";
        public string?      Notes          { get; set; }
        public DateTime?    SentAt         { get; set; }
        public List<string> DriveFileIds { get; set; } = new();
    }
}
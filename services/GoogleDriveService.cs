using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace LegalOfficeApp
{
    public class GoogleDriveService
    {
        private readonly DriveService _drive;
        private readonly string _stagingFolderId;

        public GoogleDriveService(string serviceAccountJson, string stagingFolderId)
        {
            var credential = GoogleCredential
                .FromJson(serviceAccountJson)
                .CreateScoped(DriveService.Scope.Drive);

            _drive = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "LegalOfficeApp"
            });

            _stagingFolderId = stagingFolderId;
        }

        public async Task<string> UploadAsync(string localFilePath, string fileName)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = new List<string> { _stagingFolderId }
            };

            using var stream = File.OpenRead(localFilePath);
            var request = _drive.Files.Create(fileMetadata, stream, "application/pdf");
            request.Fields = "id";
            await request.UploadAsync();

            return request.ResponseBody.Id;
        }

        public async Task DeleteAsync(string fileId)
        {
            try { await _drive.Files.Delete(fileId).ExecuteAsync(); }
            catch { /* already gone, non-fatal */ }
        }

        public async Task DownloadAsync(string fileId, string destinationPath)
        {
            var request = _drive.Files.Get(fileId);
            using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
            await request.DownloadAsync(fs);
        }
    }
}
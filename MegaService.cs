using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CG.Web.MegaApiClient;

namespace LegalOfficeApp
{
    public class MegaService
    {
        private readonly MegaApiClient _client;
        private bool _isLoggedIn = false;

        // Folder name in your MEGA account where PDFs will be stored
        private const string FolderName = "LegalOfficeSubmissions";

        public MegaService()
        {
            _client = new MegaApiClient();
        }

        // ── Login ────────────────────────────────────────────────
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                await _client.LoginAsync(email, password);
                _isLoggedIn = true;
                return true;
            }
            catch (Exception ex)
            {
                _isLoggedIn = false;
                throw new Exception($"MEGA login failed: {ex.Message}");
            }
        }

        public async Task LogoutAsync()
        {
            if (_isLoggedIn)
            {
                await _client.LogoutAsync();
                _isLoggedIn = false;
            }
        }

        // ── Upload a PDF ─────────────────────────────────────────
        public async Task<string> UploadPdfAsync(string localFilePath, string bookNumber)
        {
            if (!_isLoggedIn)
                throw new InvalidOperationException("Not logged in to MEGA.");

            // Get or create the submissions folder
            var folder = await GetOrCreateFolderAsync(FolderName);

            // Rename the file to include book number for easy identification
            string fileName = $"Book_{bookNumber}_{Path.GetFileName(localFilePath)}";

            INode uploadedNode;
            using (var stream = File.OpenRead(localFilePath))
            {
                uploadedNode = await _client.UploadAsync(stream, fileName, folder,
                    new Progress<double>(p => OnUploadProgress?.Invoke(p)));
            }

            // Return a shareable download link
            Uri link = await _client.GetDownloadLinkAsync(uploadedNode);
            return link.ToString();
        }

        // ── Download a file ──────────────────────────────────────
        public async Task DownloadFileAsync(string megaLink, string saveToPath)
        {
            var uri  = new Uri(megaLink);
            var node = await _client.GetNodeFromLinkAsync(uri);
            await _client.DownloadFileAsync(node, saveToPath,
                new Progress<double>(p => OnDownloadProgress?.Invoke(p)));
        }

        // ── List all uploaded submissions ────────────────────────
        public async Task<List<(string Name, string Link, DateTime Modified)>> ListSubmissionsAsync()
        {
            var folder = await GetOrCreateFolderAsync(FolderName);
            var nodes  = await _client.GetNodesAsync(folder);

            var result = new List<(string, string, DateTime)>();
            foreach (var node in nodes.Where(n => n.Type == NodeType.File))
            {
                var link = await _client.GetDownloadLinkAsync(node);
                result.Add((
                    node.Name,
                    link.ToString(),
                    node.ModificationDate ?? node.CreationDate ?? DateTime.MinValue
                ));
            }
            return result;
        }

        // ── Delete a file ────────────────────────────────────────
        public async Task DeleteFileAsync(string fileName)
        {
            var folder = await GetOrCreateFolderAsync(FolderName);
            var nodes  = await _client.GetNodesAsync(folder);
            var target = nodes.FirstOrDefault(n => n.Name == fileName && n.Type == NodeType.File);
            if (target != null)
                await _client.DeleteAsync(target, moveToTrash: true);
        }

        // ── Helper: get or create folder ─────────────────────────
        private async Task<INode> GetOrCreateFolderAsync(string folderName)
        {
            var allNodes = await _client.GetNodesAsync();
            var root     = allNodes.Single(n => n.Type == NodeType.Root);

            // Look for existing folder
            var existing = allNodes.FirstOrDefault(n =>
                n.Type == NodeType.Directory &&
                n.Name == folderName &&
                n.ParentId == root.Id);

            if (existing != null) return existing;

            // Create it if not found
            return await _client.CreateFolderAsync(folderName, root);
        }

        // ── Progress events ───────────────────────────────────────
        public Action<double>? OnUploadProgress;
        public Action<double>? OnDownloadProgress;
    }
}
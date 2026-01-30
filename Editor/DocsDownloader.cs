using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityDocsIndex.Editor
{
    public static class DocsDownloader
    {
        public const string OfficialCdnUrl = "https://cloudmedia-docs.unity3d.com/docscloudstorage/en";

        private static string _customCdnUrl = "";

        public static string GetUnityVersion()
        {
            return Application.unityVersion;
        }

        public static string NormalizeVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return "";

            var match = Regex.Match(version, @"^(\d+)\.(\d+)");
            if (match.Success)
            {
                return $"{match.Groups[1].Value}.{match.Groups[2].Value}";
            }
            return version;
        }

        public static string CdnUrl
        {
            get => _customCdnUrl;
            set => _customCdnUrl = value ?? "";
        }

        public static string GetDocsUrl(string version)
        {
            var normalized = NormalizeVersion(version);
            var baseUrl = string.IsNullOrEmpty(_customCdnUrl) ? OfficialCdnUrl : _customCdnUrl;
            return $"{baseUrl}/{normalized}/UnityDocumentation.zip";
        }

        public static string GetCurrentDocsUrl(string version, string cdnUrl)
        {
            var normalized = NormalizeVersion(version);
            var baseUrl = string.IsNullOrEmpty(cdnUrl) ? OfficialCdnUrl : cdnUrl;
            return $"{baseUrl}/{normalized}/UnityDocumentation.zip";
        }

        public static async Task<DownloadResult> DownloadDocsAsync(
            string version,
            string destPath,
            Action<float> onProgress = null,
            CancellationToken cancellationToken = default)
        {
            // Precondition: version must be valid
            if (string.IsNullOrEmpty(version))
            {
                Debug.LogError("DownloadDocsAsync: version is null or empty");
                return DownloadResult.ErrorInvalidVersion;
            }

            var normalized = NormalizeVersion(version);
            if (string.IsNullOrEmpty(normalized))
            {
                Debug.LogError("DownloadDocsAsync: could not normalize version");
                return DownloadResult.ErrorInvalidVersion;
            }

            var url = GetDocsUrl(version);
            var tempZipPath = Path.Combine(destPath, "temp-docs.zip");

            Debug.Log($"Downloading Unity {version} documentation...");
            Debug.Log($"URL: {url}");

            Directory.CreateDirectory(destPath);

            using (var request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        Debug.Log("Download cancelled.");
                        return DownloadResult.ErrorCancelled;
                    }

                    onProgress?.Invoke(request.downloadProgress);
                    await Task.Yield();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.Log("Download cancelled.");
                    return DownloadResult.ErrorCancelled;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to download documentation: {request.error}");
                    return DownloadResult.ErrorNetworkFailure;
                }

                File.WriteAllBytes(tempZipPath, request.downloadHandler.data);
            }

            Debug.Log("Extracting documentation...");

            try
            {
                ZipFile.ExtractToDirectory(tempZipPath, destPath, true);
                File.Delete(tempZipPath);

                // Handle nested structure: Documentation/en/ -> move contents up
                var docEnSubfolder = Path.Combine(destPath, "Documentation", "en");
                if (Directory.Exists(docEnSubfolder))
                {
                    MoveDirectoryContents(docEnSubfolder, destPath);
                    Directory.Delete(Path.Combine(destPath, "Documentation"), true);
                    Debug.Log("Moved contents from Documentation/en/ subfolder");
                }
                else
                {
                    // Fallback: check for just en/ subfolder
                    var enSubfolder = Path.Combine(destPath, "en");
                    if (Directory.Exists(enSubfolder))
                    {
                        MoveDirectoryContents(enSubfolder, destPath);
                        Directory.Delete(enSubfolder, true);
                        Debug.Log("Moved contents from en/ subfolder");
                    }
                }

                Debug.Log($"Documentation extracted to {destPath}");
                return DownloadResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to extract documentation: {e.Message}");
                return DownloadResult.ErrorExtractionFailed;
            }
        }

        private static void MoveDirectoryContents(string sourceDir, string destDir)
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                if (File.Exists(destFile)) File.Delete(destFile);
                File.Move(file, destFile);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                var destSubDir = Path.Combine(destDir, dirName);
                if (Directory.Exists(destSubDir))
                {
                    Directory.Delete(destSubDir, true);
                }
                Directory.Move(dir, destSubDir);
            }
        }

        public static CopyResult CopyDocsFromLocal(string sourcePath, string destPath)
        {
            // Precondition: sourcePath must not be empty
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogError("CopyDocsFromLocal: sourcePath is null or empty");
                return CopyResult.ErrorSourcePathEmpty;
            }

            // Precondition: source directory must exist
            if (!Directory.Exists(sourcePath))
            {
                Debug.LogError($"CopyDocsFromLocal: source directory not found: {sourcePath}");
                return CopyResult.ErrorSourceNotFound;
            }

            Debug.Log($"Copying documentation from {sourcePath}...");

            try
            {
                CopyDirectory(sourcePath, destPath);
                Debug.Log($"Documentation copied to {destPath}");
                return CopyResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogError($"CopyDocsFromLocal: copy failed: {e.Message}");
                return CopyResult.ErrorCopyFailed;
            }
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        public static bool DocsExist(string docsPath)
        {
            if (string.IsNullOrEmpty(docsPath))
                return false;

            return Directory.Exists(Path.Combine(docsPath, "Manual"))
                || Directory.Exists(Path.Combine(docsPath, "ScriptReference"));
        }

        public static string GetSectionPath(string docsPath, string section)
        {
            if (string.IsNullOrEmpty(docsPath) || string.IsNullOrEmpty(section))
                return null;

            var sectionPath = Path.Combine(docsPath, section);
            return Directory.Exists(sectionPath) ? sectionPath : null;
        }
    }
}

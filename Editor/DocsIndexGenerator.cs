using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityDocsIndex.Editor
{
    public static class DocsIndexGenerator
    {
        public const string StartMarker = "<!-- UNITY-DOCS-INDEX-START -->";
        public const string EndMarker = "<!-- UNITY-DOCS-INDEX-END -->";

        /// <summary>
        /// Collect all HTML files recursively from a directory
        /// </summary>
        public static List<string> CollectAllFiles(string dirPath)
        {
            var files = new List<string>();

            // Precondition: directory must exist
            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath))
                return files;

            foreach (var file in Directory.EnumerateFiles(dirPath, "*.html", SearchOption.AllDirectories))
            {
                // Get relative path from dirPath
                var relativePath = file.Substring(dirPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                // Normalize to forward slashes
                relativePath = relativePath.Replace('\\', '/');
                files.Add(relativePath);
            }

            files.Sort();
            return files;
        }

        /// <summary>
        /// Group files by their parent directory
        /// </summary>
        public static Dictionary<string, List<string>> GroupByDirectory(List<string> files)
        {
            var grouped = new Dictionary<string, List<string>>();

            // Precondition: files must not be null
            if (files == null)
                return grouped;

            foreach (var filePath in files)
            {
                var lastSlash = filePath.LastIndexOf('/');
                var dir = lastSlash == -1 ? "." : filePath.Substring(0, lastSlash);
                var fileName = lastSlash == -1 ? filePath : filePath.Substring(lastSlash + 1);

                if (!grouped.ContainsKey(dir))
                {
                    grouped[dir] = new List<string>();
                }
                grouped[dir].Add(fileName);
            }

            return grouped;
        }

        /// <summary>
        /// Generate the compact index string from grouped files
        /// </summary>
        public static (GenerateResult result, string index) GenerateIndex(
            Dictionary<string, List<string>> groupedFiles,
            string root,
            string version = "")
        {
            // Precondition: groupedFiles must not be null
            if (groupedFiles == null)
            {
                return (GenerateResult.ErrorNoDocumentationFound, null);
            }

            // Precondition: root must not be empty
            if (string.IsNullOrEmpty(root))
            {
                return (GenerateResult.ErrorRootPathEmpty, null);
            }

            var parts = new List<string>
            {
                "[Unity Docs Index]",
                $"root: {root}"
            };

            if (!string.IsNullOrEmpty(version))
            {
                parts.Add($"version: {version}");
            }

            parts.Add("IMPORTANT: Prefer retrieval-led reasoning over pre-training-led reasoning for any Unity tasks.");
            parts.Add("If docs missing, install com.tsuchigoe.agent-unity-docs package and run Tools > Unity Docs Index");
            parts.Add($"ScriptReference (API docs) available at: {root}/ScriptReference/ (not indexed due to size)");

            // Sort directories for consistent output
            var sortedDirs = groupedFiles.Keys.OrderBy(k => k).ToList();

            foreach (var dir in sortedDirs)
            {
                var fileList = groupedFiles[dir];
                if (fileList.Count > 0)
                {
                    parts.Add($"{dir}:{{{string.Join(",", fileList)}}}");
                }
            }

            return (GenerateResult.Success, string.Join("|", parts));
        }

        /// <summary>
        /// Generate the index string wrapped with markers
        /// </summary>
        public static (GenerateResult result, string index) GenerateIndexWithMarkers(
            Dictionary<string, List<string>> groupedFiles,
            string root,
            string version = "")
        {
            var (result, index) = GenerateIndex(groupedFiles, root, version);

            if (result != GenerateResult.Success)
            {
                return (result, null);
            }

            return (GenerateResult.Success, $"{StartMarker}{index}{EndMarker}");
        }

        /// <summary>
        /// Generate full index for Unity documentation (Manual + ScriptReference)
        /// </summary>
        public static (GenerateResult result, string index) GenerateFullIndex(
            string manualPath,
            string scriptRefPath,
            string root,
            string version = "")
        {
            // Precondition: root must not be empty
            if (string.IsNullOrEmpty(root))
            {
                return (GenerateResult.ErrorRootPathEmpty, null);
            }

            // Precondition: at least one path must be valid
            var manualExists = !string.IsNullOrEmpty(manualPath) && Directory.Exists(manualPath);
            var scriptRefExists = !string.IsNullOrEmpty(scriptRefPath) && Directory.Exists(scriptRefPath);

            if (!manualExists && !scriptRefExists)
            {
                return (GenerateResult.ErrorNoDocumentationFound, null);
            }

            var allGrouped = new Dictionary<string, List<string>>();

            if (manualExists)
            {
                var manualFiles = CollectAllFiles(manualPath);
                var manualGrouped = GroupByDirectory(manualFiles);

                foreach (var kvp in manualGrouped)
                {
                    var key = kvp.Key == "." ? "Manual" : $"Manual/{kvp.Key}";
                    allGrouped[key] = kvp.Value;
                }
            }

            if (scriptRefExists)
            {
                var scriptRefFiles = CollectAllFiles(scriptRefPath);
                var scriptRefGrouped = GroupByDirectory(scriptRefFiles);

                foreach (var kvp in scriptRefGrouped)
                {
                    var key = kvp.Key == "." ? "ScriptReference" : $"ScriptReference/{kvp.Key}";
                    allGrouped[key] = kvp.Value;
                }
            }

            return GenerateIndexWithMarkers(allGrouped, root, version);
        }
    }
}

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityDocsIndex.Editor
{
    public static class FileInjector
    {
        public static InjectResult InjectIntoFile(
            string filePath,
            string indexContent,
            bool createIfMissing = true)
        {
            // Precondition: filePath must not be empty
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("InjectIntoFile: filePath is null or empty");
                return InjectResult.ErrorFilePathEmpty;
            }

            // Precondition: indexContent must not be null
            if (indexContent == null)
            {
                Debug.LogError("InjectIntoFile: indexContent is null");
                return InjectResult.ErrorWriteFailed;
            }

            var startMarker = DocsIndexGenerator.StartMarker;
            var endMarker = DocsIndexGenerator.EndMarker;

            try
            {
                if (!File.Exists(filePath))
                {
                    if (createIfMissing)
                    {
                        File.WriteAllText(filePath, indexContent + "\n", Encoding.UTF8);
                        Debug.Log($"Created {filePath}");
                        return InjectResult.SuccessCreated;
                    }
                    else
                    {
                        Debug.LogError($"InjectIntoFile: file not found: {filePath}");
                        return InjectResult.ErrorFileNotFound;
                    }
                }

                var content = File.ReadAllText(filePath, Encoding.UTF8);

                var startIdx = content.IndexOf(startMarker);
                var endIdx = content.IndexOf(endMarker);

                if (startIdx != -1 && endIdx != -1 && endIdx > startIdx)
                {
                    var before = content.Substring(0, startIdx);
                    var after = content.Substring(endIdx + endMarker.Length);
                    content = before + indexContent + after;
                    File.WriteAllText(filePath, content, Encoding.UTF8);
                    Debug.Log($"Updated existing index in {filePath}");
                    return InjectResult.SuccessUpdated;
                }
                else
                {
                    content = content.TrimEnd() + "\n\n" + indexContent + "\n";
                    File.WriteAllText(filePath, content, Encoding.UTF8);
                    Debug.Log($"Appended index to {filePath}");
                    return InjectResult.SuccessAppended;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"InjectIntoFile: write failed: {e.Message}");
                return InjectResult.ErrorWriteFailed;
            }
        }

        public static GitignoreResult UpdateGitignore(string gitignorePath, string entry)
        {
            // Precondition: gitignorePath must not be empty
            if (string.IsNullOrEmpty(gitignorePath))
            {
                Debug.LogError("UpdateGitignore: gitignorePath is null or empty");
                return GitignoreResult.ErrorPathEmpty;
            }

            // Precondition: entry must not be empty
            if (string.IsNullOrEmpty(entry))
            {
                Debug.LogError("UpdateGitignore: entry is null or empty");
                return GitignoreResult.ErrorPathEmpty;
            }

            try
            {
                if (!File.Exists(gitignorePath))
                {
                    File.WriteAllText(gitignorePath, $"{entry}\n", Encoding.UTF8);
                    Debug.Log($"Created {gitignorePath} with {entry}");
                    return GitignoreResult.SuccessCreated;
                }

                var content = File.ReadAllText(gitignorePath, Encoding.UTF8);
                var lines = content.Split('\n');

                foreach (var line in lines)
                {
                    if (line.Trim() == entry)
                    {
                        Debug.Log($"{entry} already in {gitignorePath}");
                        return GitignoreResult.AlreadyPresent;
                    }
                }

                File.AppendAllText(gitignorePath, $"\n{entry}\n", Encoding.UTF8);
                Debug.Log($"Added {entry} to {gitignorePath}");
                return GitignoreResult.SuccessUpdated;
            }
            catch (Exception e)
            {
                Debug.LogError($"UpdateGitignore: write failed: {e.Message}");
                return GitignoreResult.ErrorWriteFailed;
            }
        }
    }
}

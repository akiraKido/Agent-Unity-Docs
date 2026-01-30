using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityDocsIndex.Editor
{
    [InitializeOnLoad]
    public static class DocsVersionChecker
    {
        private const string DocsDir = ".unity-docs";
        private const string IndexMarker = "<!-- UNITY-DOCS-INDEX-START -->";
        private const string PrefsPrefix = "UnityDocsIndex_";
        private const string LastDownloadedVersionKey = PrefsPrefix + "LastDownloadedVersion";

        static DocsVersionChecker()
        {
            // Delay check to avoid blocking startup
            EditorApplication.delayCall += CheckVersion;
        }

        private static void CheckVersion()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var docsPath = Path.Combine(projectRoot, DocsDir);
            var docsExist = DocsDownloader.DocsExist(docsPath);

            // Check if CLAUDE.md has index but docs are missing
            if (!docsExist && HasIndexInClaudeMd(projectRoot))
            {
                PromptDocsMissing();
                return;
            }

            // Only check version if docs have been downloaded before
            if (!docsExist)
            {
                return;
            }

            var currentVersion = DocsDownloader.NormalizeVersion(DocsDownloader.GetUnityVersion());
            var lastDownloadedVersion = EditorPrefs.GetString(LastDownloadedVersionKey, "");

            // If version changed, prompt user
            if (!string.IsNullOrEmpty(lastDownloadedVersion) && lastDownloadedVersion != currentVersion)
            {
                var result = EditorUtility.DisplayDialog(
                    Localization.Get("versionChangedTitle"),
                    Localization.Get("versionChangedMessage", lastDownloadedVersion, currentVersion),
                    Localization.Get("update"),
                    Localization.Get("skip"));

                if (result)
                {
                    // Open window and trigger download
                    var window = EditorWindow.GetWindow<UnityDocsIndexWindow>("Unity Docs Index");
                    window.StartAutoUpdate(currentVersion);
                }
            }
        }

        private static bool HasIndexInClaudeMd(string projectRoot)
        {
            var claudeMdPath = Path.Combine(projectRoot, "CLAUDE.md");
            if (!File.Exists(claudeMdPath))
            {
                return false;
            }

            var content = File.ReadAllText(claudeMdPath);
            return content.Contains(IndexMarker);
        }

        private static void PromptDocsMissing()
        {
            var result = EditorUtility.DisplayDialog(
                Localization.Get("docsMissingTitle"),
                Localization.Get("docsMissingMessage"),
                Localization.Get("download"),
                Localization.Get("ignore"));

            if (result)
            {
                var window = EditorWindow.GetWindow<UnityDocsIndexWindow>("Unity Docs Index");
                var currentVersion = DocsDownloader.NormalizeVersion(DocsDownloader.GetUnityVersion());
                window.StartAutoUpdate(currentVersion);
            }
        }

        public static void SaveDownloadedVersion(string version)
        {
            EditorPrefs.SetString(LastDownloadedVersionKey, version);
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace UnityDocsIndex.Editor
{
    [InitializeOnLoad]
    public static class DocsVersionChecker
    {
        private const string PrefsPrefix = "UnityDocsIndex_";
        private const string LastDownloadedVersionKey = PrefsPrefix + "LastDownloadedVersion";

        static DocsVersionChecker()
        {
            // Delay check to avoid blocking startup
            EditorApplication.delayCall += CheckVersion;
        }

        private static void CheckVersion()
        {
            // Step 1: Check if skill is installed
            if (!SkillInstaller.IsSkillInstalled())
            {
                PromptSkillInstall();
                return;
            }

            // Step 2: Check if docs exist in skill directory
            if (!SkillInstaller.DocsExist())
            {
                PromptDocsMissing();
                return;
            }

            // Step 3: Check version change
            var currentVersion = DocsDownloader.NormalizeVersion(DocsDownloader.GetUnityVersion());
            var lastDownloadedVersion = EditorPrefs.GetString(LastDownloadedVersionKey, "");

            if (!string.IsNullOrEmpty(lastDownloadedVersion) && lastDownloadedVersion != currentVersion)
            {
                var result = EditorUtility.DisplayDialog(
                    Localization.Get("versionChangedTitle"),
                    Localization.Get("versionChangedMessage", lastDownloadedVersion, currentVersion),
                    Localization.Get("update"),
                    Localization.Get("skip"));

                if (result)
                {
                    var window = EditorWindow.GetWindow<UnityDocsIndexWindow>("Unity Docs Index");
                    window.StartAutoUpdate(currentVersion);
                }
            }
        }

        private static void PromptSkillInstall()
        {
            var result = EditorUtility.DisplayDialog(
                Localization.Get("skillNotInstalledTitle"),
                Localization.Get("skillNotInstalledMessage"),
                Localization.Get("install"),
                Localization.Get("ignore"));

            if (!result)
                return;

            var installResult = SkillInstaller.InstallSkill();
            if (installResult != SkillInstallResult.Success
                && installResult != SkillInstallResult.AlreadyInstalled)
            {
                Debug.LogError($"Skill installation failed: {installResult}");
                return;
            }

            SkillInstaller.EnsureGitignoreAllowsSkill();
            SkillInstaller.CleanLegacyIndex();
            SkillInstaller.MigrateLegacyDocs();

            // Open window to download docs if needed
            if (!SkillInstaller.DocsExist())
            {
                EditorWindow.GetWindow<UnityDocsIndexWindow>("Unity Docs Index");
            }
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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityDocsIndex.Editor
{
    public class UnityDocsIndexWindow : EditorWindow
    {
        private const string DocsDir = ".unity-docs";
        private const string PrefsPrefix = "UnityDocsIndex_";

        // UI Elements
        private Label _titleLabel;
        private Label _descriptionLabel;
        private Label _sourceHeaderLabel;
        private TextField _versionField;
        private TextField _cdnUrlField;
        private Label _urlPreviewLabel;
        private Label _cdnExampleLabel;
        private Label _outputHeaderLabel;
        private TextField _outputFileField;
        private VisualElement _statusBox;
        private Label _statusLabel;
        private ProgressBar _progressBar;
        private Button _generateButton;
        private Button _cancelButton;
        private Button _resetButton;
        private Button _browseOutputButton;
        private Button _enButton;
        private Button _jaButton;
        private Button _zhButton;
        private VisualElement _mainContent;

        // State
        private string _version = "";
        private string _cdnUrl = "";
        private string _outputFile = "CLAUDE.md";

        private bool _isProcessing = false;
        private float _downloadProgress = 0f;
        private string _statusMessage = "";
        private CancellationTokenSource _cancellationTokenSource;

        // Background task state
        private Task<(GenerateResult result, string index)> _backgroundTask;
        private string _pendingOutputPath;
        private string _pendingGitignorePath;
        private string _pendingVersion;

        [MenuItem("Tools/Unity Docs Index Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnityDocsIndexWindow>(Localization.Get("windowTitle"));
            window.minSize = new Vector2(400, 380);
        }

        private void OnEnable()
        {
            LoadPrefs();
            if (string.IsNullOrEmpty(_version))
            {
                _version = DocsDownloader.NormalizeVersion(DocsDownloader.GetUnityVersion());
            }
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SavePrefs();
            EditorApplication.update -= OnEditorUpdate;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // Load UXML
            var uxmlPath = FindAssetPath("UnityDocsIndexWindow.uxml");
            if (!string.IsNullOrEmpty(uxmlPath))
            {
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
                if (visualTree != null)
                {
                    visualTree.CloneTree(root);
                }
            }

            // Load USS
            var ussPath = FindAssetPath("UnityDocsIndexWindow.uss");
            if (!string.IsNullOrEmpty(ussPath))
            {
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                if (styleSheet != null)
                {
                    root.styleSheets.Add(styleSheet);
                }
            }

            // Get UI element references
            BindUIElements(root);

            // Setup event handlers
            SetupEventHandlers();

            // Initialize UI
            SyncFieldsFromState();    // Set field values first
            UpdateLocalizedTexts();   // Then update labels (includes UpdateUrlPreview)
            UpdateUIState();          // Finally update visibility/state
        }

        private string FindAssetPath(string filename)
        {
            var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(filename));
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(filename))
                {
                    return path;
                }
            }
            return null;
        }

        private void BindUIElements(VisualElement root)
        {
            _mainContent = root.Q<VisualElement>(className: "root");
            _titleLabel = root.Q<Label>("title");
            _descriptionLabel = root.Q<Label>("description");
            _sourceHeaderLabel = root.Q<Label>("source-header");
            _versionField = root.Q<TextField>("version");
            _cdnUrlField = root.Q<TextField>("cdn-url");
            _urlPreviewLabel = root.Q<Label>("url-preview");
            _cdnExampleLabel = root.Q<Label>("cdn-example");
            _outputHeaderLabel = root.Q<Label>("output-header");
            _outputFileField = root.Q<TextField>("output-file");
            _statusBox = root.Q<VisualElement>("status-box");
            _statusLabel = root.Q<Label>("status-message");
            _progressBar = root.Q<ProgressBar>("progress-bar");
            _generateButton = root.Q<Button>("btn-generate");
            _cancelButton = root.Q<Button>("btn-cancel");
            _resetButton = root.Q<Button>("btn-reset");
            _browseOutputButton = root.Q<Button>("btn-browse-output");
            _enButton = root.Q<Button>("btn-en");
            _jaButton = root.Q<Button>("btn-ja");
            _zhButton = root.Q<Button>("btn-zh");
        }

        private void SetupEventHandlers()
        {
            // Language buttons
            _enButton?.RegisterCallback<ClickEvent>(_ => SetLanguage("en"));
            _jaButton?.RegisterCallback<ClickEvent>(_ => SetLanguage("ja"));
            _zhButton?.RegisterCallback<ClickEvent>(_ => SetLanguage("zh-CN"));

            // Text fields - use input event for URL preview updates only
            _versionField?.RegisterCallback<InputEvent>(_ => UpdateUrlPreview());
            _cdnUrlField?.RegisterCallback<InputEvent>(_ => UpdateUrlPreview());

            // Browse button
            _browseOutputButton?.RegisterCallback<ClickEvent>(_ => BrowseOutputFile());

            // Action buttons
            _generateButton?.RegisterCallback<ClickEvent>(_ => GenerateIndex());
            _cancelButton?.RegisterCallback<ClickEvent>(_ => CancelOperation());
            _resetButton?.RegisterCallback<ClickEvent>(_ => ResetState());
        }

        private void SetLanguage(string lang)
        {
            Localization.CurrentLanguage = lang;
            titleContent = new GUIContent(Localization.Get("windowTitle"));
            UpdateLocalizedTexts();
            UpdateLanguageButtonStyles();
        }

        private void UpdateLocalizedTexts()
        {
            _titleLabel?.SetText(Localization.Get("title"));
            _descriptionLabel?.SetText(Localization.Get("description"));
            _sourceHeaderLabel?.SetText(Localization.Get("documentationSource"));
            _versionField?.SetLabel(Localization.Get("unityVersion"));
            _cdnUrlField?.SetLabel(Localization.Get("cdnUrlOptional"));
            _cdnExampleLabel?.SetText(Localization.Get("cdnUrlExample"));
            _outputHeaderLabel?.SetText(Localization.Get("outputSettings"));
            _outputFileField?.SetLabel(Localization.Get("outputFile"));
            _generateButton?.SetText(Localization.Get("generateIndex"));
            _cancelButton?.SetText(Localization.Get("cancel"));
            _resetButton?.SetText(Localization.Get("reset"));
            _browseOutputButton?.SetText(Localization.Get("browse"));

            UpdateUrlPreview();
            UpdateLanguageButtonStyles();
        }

        private void UpdateLanguageButtonStyles()
        {
            var lang = Localization.CurrentLanguage;

            UpdateButtonStyle(_enButton, lang == "en");
            UpdateButtonStyle(_jaButton, lang == "ja");
            UpdateButtonStyle(_zhButton, lang == "zh-CN");
        }

        private void UpdateButtonStyle(Button button, bool isActive)
        {
            if (button == null) return;

            button.RemoveFromClassList("lang-btn-active");
            button.RemoveFromClassList("lang-btn-inactive");
            button.AddToClassList(isActive ? "lang-btn-active" : "lang-btn-inactive");
        }

        private void UpdateUrlPreview()
        {
            if (_urlPreviewLabel == null) return;

            // Read directly from fields
            var version = _versionField?.value ?? _version;
            var cdnUrl = _cdnUrlField?.value ?? _cdnUrl;

            var fullUrl = DocsDownloader.GetCurrentDocsUrl(version, cdnUrl);
            _urlPreviewLabel.text = $"{Localization.Get("downloadUrlPrefix")} {fullUrl}";

            // Show/hide CDN example based on whether custom URL is set
            if (_cdnExampleLabel != null)
            {
                _cdnExampleLabel.style.display = string.IsNullOrEmpty(cdnUrl)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void UpdateUIState()
        {
            UpdateLanguageButtonStyles();

            // Status box
            if (_statusBox != null)
            {
                var hasStatus = !string.IsNullOrEmpty(_statusMessage);
                _statusBox.EnableInClassList("status-box-visible", hasStatus);
                _statusLabel?.SetText(_statusMessage);
            }

            // Progress bar
            if (_progressBar != null)
            {
                var showProgress = _isProcessing && _downloadProgress > 0f && _downloadProgress < 1f;
                _progressBar.EnableInClassList("progress-bar-visible", showProgress);
                if (showProgress)
                {
                    _progressBar.value = _downloadProgress * 100f;
                    _progressBar.title = Localization.Get("downloading", (_downloadProgress * 100).ToString("F0"));
                }
            }

            // Cancel button
            if (_cancelButton != null)
            {
                _cancelButton.EnableInClassList("cancel-btn-visible", _isProcessing);
            }

            // Reset button
            if (_resetButton != null)
            {
                var showReset = !_isProcessing && !string.IsNullOrEmpty(_statusMessage);
                _resetButton.EnableInClassList("reset-btn-visible", showReset);
            }

            // Generate button
            if (_generateButton != null)
            {
                _generateButton.SetEnabled(!_isProcessing);
            }

            // Disable main content during processing
            if (_mainContent != null)
            {
                _mainContent.SetEnabled(!_isProcessing || true); // Keep enabled but disable interactive elements
            }

            // Disable input fields during processing
            SetInputsEnabled(!_isProcessing);
        }

        private void SetInputsEnabled(bool enabled)
        {
            _versionField?.SetEnabled(enabled);
            _cdnUrlField?.SetEnabled(enabled);
            _outputFileField?.SetEnabled(enabled);
            _browseOutputButton?.SetEnabled(enabled);
            _enButton?.SetEnabled(enabled);
            _jaButton?.SetEnabled(enabled);
            _zhButton?.SetEnabled(enabled);
        }

        private void SyncFieldsFromState()
        {
            _versionField?.SetValueWithoutNotify(_version);
            _cdnUrlField?.SetValueWithoutNotify(_cdnUrl);
            _outputFileField?.SetValueWithoutNotify(_outputFile);
        }

        private void BrowseOutputFile()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var path = EditorUtility.SaveFilePanel(Localization.Get("saveIndexFile"), projectRoot, _outputFile, "md");
            if (!string.IsNullOrEmpty(path))
            {
                // Make relative to project root if possible
                if (path.StartsWith(projectRoot))
                {
                    path = path.Substring(projectRoot.Length + 1);
                }
                _outputFile = path;
                _outputFileField?.SetValueWithoutNotify(path);
            }
        }

        private void OnEditorUpdate()
        {
            // Check if background task completed
            if (_backgroundTask != null && _backgroundTask.IsCompleted)
            {
                OnBackgroundTaskCompleted();
            }
        }

        private void OnBackgroundTaskCompleted()
        {
            try
            {
                if (_backgroundTask.IsFaulted)
                {
                    _statusMessage = $"Error: {_backgroundTask.Exception?.InnerException?.Message ?? "Unknown error"}";
                    Debug.LogException(_backgroundTask.Exception);
                }
                else if (_backgroundTask.IsCanceled)
                {
                    _statusMessage = Localization.Get("cancelled");
                }
                else
                {
                    // Get the result
                    var (generateResult, indexContent) = _backgroundTask.Result;

                    // Handle generation result
                    if (generateResult != GenerateResult.Success)
                    {
                        _statusMessage = GetGenerateResultMessage(generateResult);
                        return;
                    }

                    // Write files on main thread (safer for Unity)
                    var injectResult = FileInjector.InjectIntoFile(_pendingOutputPath, indexContent);
                    if (!IsInjectSuccess(injectResult))
                    {
                        _statusMessage = GetInjectResultMessage(injectResult);
                        return;
                    }

                    var gitignoreResult = FileInjector.UpdateGitignore(_pendingGitignorePath, DocsDir);
                    // Gitignore errors are non-fatal, just log them
                    if (IsGitignoreError(gitignoreResult))
                    {
                        Debug.LogWarning(GetGitignoreResultMessage(gitignoreResult));
                    }

                    DocsVersionChecker.SaveDownloadedVersion(_pendingVersion);

                    _statusMessage = Localization.Get("done", _outputFile);
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                _statusMessage = $"Error: {e.Message}";
                Debug.LogException(e);
            }
            finally
            {
                _backgroundTask = null;
                _isProcessing = false;
                _downloadProgress = 0f;
                UpdateUIState();
            }
        }

        #region Result Message Helpers

        private static string GetDownloadResultMessage(DownloadResult result)
        {
            switch (result)
            {
                case DownloadResult.Success:
                    return "";
                case DownloadResult.ErrorInvalidVersion:
                    return Localization.Get("errorInvalidVersion");
                case DownloadResult.ErrorNetworkFailure:
                    return Localization.Get("errorNetworkFailure");
                case DownloadResult.ErrorExtractionFailed:
                    return Localization.Get("errorExtractionFailed");
                case DownloadResult.ErrorCancelled:
                    return Localization.Get("cancelled");
                default:
                    return $"Error: Unknown download error ({result})";
            }
        }

        private static string GetGenerateResultMessage(GenerateResult result)
        {
            switch (result)
            {
                case GenerateResult.Success:
                    return "";
                case GenerateResult.ErrorManualPathNotFound:
                case GenerateResult.ErrorScriptRefPathNotFound:
                case GenerateResult.ErrorNoDocumentationFound:
                    return Localization.Get("errorNoDocumentationFound");
                case GenerateResult.ErrorRootPathEmpty:
                    return Localization.Get("errorRootPathEmpty");
                default:
                    return $"Error: Unknown generation error ({result})";
            }
        }

        private static string GetInjectResultMessage(InjectResult result)
        {
            switch (result)
            {
                case InjectResult.SuccessCreated:
                case InjectResult.SuccessUpdated:
                case InjectResult.SuccessAppended:
                    return "";
                case InjectResult.ErrorFilePathEmpty:
                    return Localization.Get("errorFilePathEmpty");
                case InjectResult.ErrorFileNotFound:
                    return Localization.Get("errorFileNotFound");
                case InjectResult.ErrorWriteFailed:
                    return Localization.Get("errorWriteFailed");
                default:
                    return $"Error: Unknown inject error ({result})";
            }
        }

        private static bool IsInjectSuccess(InjectResult result)
        {
            return result == InjectResult.SuccessCreated
                || result == InjectResult.SuccessUpdated
                || result == InjectResult.SuccessAppended;
        }

        private static string GetGitignoreResultMessage(GitignoreResult result)
        {
            switch (result)
            {
                case GitignoreResult.SuccessCreated:
                case GitignoreResult.SuccessUpdated:
                case GitignoreResult.AlreadyPresent:
                    return "";
                case GitignoreResult.ErrorPathEmpty:
                    return "Gitignore path is empty";
                case GitignoreResult.ErrorWriteFailed:
                    return "Failed to update .gitignore";
                default:
                    return $"Unknown gitignore error ({result})";
            }
        }

        private static bool IsGitignoreError(GitignoreResult result)
        {
            return result == GitignoreResult.ErrorPathEmpty
                || result == GitignoreResult.ErrorWriteFailed;
        }

        #endregion

        private void LoadPrefs()
        {
            // Check prefs version - reset if outdated or corrupted
            const int currentPrefsVersion = 4;
            var savedPrefsVersion = EditorPrefs.GetInt(PrefsPrefix + "PrefsVersion", 0);

            if (savedPrefsVersion < currentPrefsVersion)
            {
                // Clear old/corrupted prefs
                ClearPrefs();
                EditorPrefs.SetInt(PrefsPrefix + "PrefsVersion", currentPrefsVersion);
            }

            _version = EditorPrefs.GetString(PrefsPrefix + "Version", "");
            _cdnUrl = EditorPrefs.GetString(PrefsPrefix + "CdnUrl", "");
            _outputFile = EditorPrefs.GetString(PrefsPrefix + "OutputFile", "CLAUDE.md");
        }

        private static void ClearPrefs()
        {
            EditorPrefs.DeleteKey(PrefsPrefix + "Version");
            EditorPrefs.DeleteKey(PrefsPrefix + "CdnUrl");
            EditorPrefs.DeleteKey(PrefsPrefix + "OutputFile");
            // Also delete old keys for clean migration
            EditorPrefs.DeleteKey(PrefsPrefix + "SourcePath");
            EditorPrefs.DeleteKey(PrefsPrefix + "UseLocalSource");
        }

        private void SavePrefs()
        {
            // Read directly from fields to avoid callback issues
            var version = _versionField?.value ?? _version;
            var cdnUrl = _cdnUrlField?.value ?? _cdnUrl;
            var outputFile = _outputFileField?.value ?? _outputFile;

            EditorPrefs.SetString(PrefsPrefix + "Version", version);
            EditorPrefs.SetString(PrefsPrefix + "CdnUrl", cdnUrl);
            EditorPrefs.SetString(PrefsPrefix + "OutputFile", outputFile);
        }

        public void StartAutoUpdate(string version)
        {
            _version = version;

            // Delete existing docs to force re-download
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var docsPath = Path.Combine(projectRoot, DocsDir);
            if (Directory.Exists(docsPath))
            {
                Directory.Delete(docsPath, true);
            }

            SyncFieldsFromState();
            GenerateIndex();
        }

        private void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
            _statusMessage = Localization.Get("cancelled");
            _isProcessing = false;
            _downloadProgress = 0f;
            _backgroundTask = null;
            UpdateUIState();
        }

        private void ResetState()
        {
            _statusMessage = "";
            _downloadProgress = 0f;
            _isProcessing = false;
            _backgroundTask = null;
            UpdateUIState();
        }

        private async void GenerateIndex()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            try
            {
                // Read values from fields
                var version = _versionField?.value ?? _version;
                var cdnUrl = _cdnUrlField?.value ?? _cdnUrl;
                var outputFile = _outputFileField?.value ?? _outputFile;

                _isProcessing = true;
                _statusMessage = Localization.Get("starting");
                UpdateUIState();

                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                var docsPath = Path.Combine(projectRoot, DocsDir);

                // Step 1: Download documentation if needed
                if (!DocsDownloader.DocsExist(docsPath))
                {
                    _statusMessage = Localization.Get("downloadingDocumentation");
                    UpdateUIState();

                    // Set custom CDN URL if provided
                    DocsDownloader.CdnUrl = cdnUrl;

                    var downloadResult = await DocsDownloader.DownloadDocsAsync(
                        version,
                        docsPath,
                        progress =>
                        {
                            _downloadProgress = progress;
                            UpdateUIState();
                        },
                        token);

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (downloadResult != DownloadResult.Success)
                    {
                        _statusMessage = GetDownloadResultMessage(downloadResult);
                        _isProcessing = false;
                        UpdateUIState();
                        return;
                    }
                }

                // Step 2: Find Manual directory (ScriptReference excluded for context size)
                var manualPath = DocsDownloader.GetSectionPath(docsPath, "Manual");

                if (string.IsNullOrEmpty(manualPath))
                {
                    _statusMessage = Localization.Get("errorNoDocumentationFound");
                    _isProcessing = false;
                    UpdateUIState();
                    return;
                }

                // Step 3: Generate index in background thread
                _statusMessage = Localization.Get("generatingIndex");
                _downloadProgress = 0f; // Hide progress bar
                UpdateUIState();

                // Store paths for completion handler
                _pendingOutputPath = Path.IsPathRooted(outputFile)
                    ? outputFile
                    : Path.Combine(projectRoot, outputFile);
                _pendingGitignorePath = Path.Combine(projectRoot, ".gitignore");
                _pendingVersion = version;

                // Capture values for background thread
                var capturedManualPath = manualPath;
                var capturedDocsDir = DocsDir;
                var capturedVersion = version;

                // Run heavy index generation in background (Manual only, ScriptReference excluded)
                _backgroundTask = Task.Run(() =>
                {
                    return DocsIndexGenerator.GenerateFullIndex(
                        capturedManualPath,
                        null,
                        $"./{capturedDocsDir}",
                        capturedVersion);
                }, token);

                // The rest will be handled by OnEditorUpdate -> OnBackgroundTaskCompleted
            }
            catch (Exception e)
            {
                _statusMessage = $"Error: {e.Message}";
                Debug.LogException(e);
                _isProcessing = false;
                _downloadProgress = 0f;
                UpdateUIState();
            }
        }
    }

    // Extension methods for UI Elements
    internal static class UIElementExtensions
    {
        public static void SetText(this Label label, string text)
        {
            if (label != null) label.text = text;
        }

        public static void SetText(this Button button, string text)
        {
            if (button != null) button.text = text;
        }

        public static void SetLabel(this TextField field, string label)
        {
            if (field != null) field.label = label;
        }
    }
}

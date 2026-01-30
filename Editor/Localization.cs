using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityDocsIndex.Editor
{
    public static class Localization
    {
        private const string PrefsKey = "UnityDocsIndex_Language";
        private static Dictionary<string, Dictionary<string, string>> _strings;
        private static string _currentLanguage = "en";

        public static string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                EditorPrefs.SetString(PrefsKey, value);
            }
        }

        static Localization()
        {
            _currentLanguage = EditorPrefs.GetString(PrefsKey, "en");
            LoadStrings();
        }

        private static void LoadStrings()
        {
            _strings = new Dictionary<string, Dictionary<string, string>>();

            // Find the strings.json file relative to this script
            var guids = AssetDatabase.FindAssets("strings t:TextAsset");
            string jsonPath = null;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("Localization/strings.json"))
                {
                    jsonPath = path;
                    break;
                }
            }

            if (string.IsNullOrEmpty(jsonPath))
            {
                // Fallback: try to find it relative to the package
                var scriptPath = GetScriptPath();
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    var dir = Path.GetDirectoryName(scriptPath);
                    jsonPath = Path.Combine(dir, "Localization", "strings.json");
                }
            }

            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
            {
                Debug.LogWarning("Unity Docs Index: strings.json not found, using fallback strings");
                InitializeFallbackStrings();
                return;
            }

            try
            {
                var json = File.ReadAllText(jsonPath);
                _strings = ParseLocalizationJson(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Unity Docs Index: Failed to load strings.json: {e.Message}");
                InitializeFallbackStrings();
            }
        }

        private static string GetScriptPath()
        {
            var guids = AssetDatabase.FindAssets("Localization t:Script");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("Localization.cs"))
                {
                    return path;
                }
            }
            return null;
        }

        private static Dictionary<string, Dictionary<string, string>> ParseLocalizationJson(string json)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();

            // Simple JSON parser for our specific format
            json = json.Trim();
            if (!json.StartsWith("{") || !json.EndsWith("}"))
                throw new Exception("Invalid JSON format");

            json = json.Substring(1, json.Length - 2).Trim();

            var currentLang = "";
            var inLangBlock = false;
            var depth = 0;
            var langBlockStart = 0;

            for (var i = 0; i < json.Length; i++)
            {
                var c = json[i];

                if (c == '"' && !inLangBlock)
                {
                    var endQuote = json.IndexOf('"', i + 1);
                    currentLang = json.Substring(i + 1, endQuote - i - 1);
                    i = endQuote;
                }
                else if (c == '{')
                {
                    if (!inLangBlock)
                    {
                        inLangBlock = true;
                        langBlockStart = i + 1;
                        depth = 1;
                    }
                    else
                    {
                        depth++;
                    }
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && inLangBlock)
                    {
                        var langBlock = json.Substring(langBlockStart, i - langBlockStart);
                        result[currentLang] = ParseStringBlock(langBlock);
                        inLangBlock = false;
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, string> ParseStringBlock(string block)
        {
            var result = new Dictionary<string, string>();
            var inString = false;
            var isKey = true;
            var currentKey = "";
            var currentValue = new System.Text.StringBuilder();
            var escaped = false;

            for (var i = 0; i < block.Length; i++)
            {
                var c = block[i];

                if (escaped)
                {
                    currentValue.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    if (i + 1 < block.Length && block[i + 1] == 'n')
                    {
                        currentValue.Append('\n');
                        i++;
                        escaped = false;
                    }
                    continue;
                }

                if (c == '"')
                {
                    if (!inString)
                    {
                        inString = true;
                        currentValue.Clear();
                    }
                    else
                    {
                        inString = false;
                        if (isKey)
                        {
                            currentKey = currentValue.ToString();
                            isKey = false;
                        }
                        else
                        {
                            result[currentKey] = currentValue.ToString();
                            isKey = true;
                        }
                    }
                }
                else if (inString)
                {
                    currentValue.Append(c);
                }
            }

            return result;
        }

        private static void InitializeFallbackStrings()
        {
            _strings = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["windowTitle"] = "Unity Docs Index",
                    ["title"] = "Unity Documentation Index Generator",
                    ["description"] = "Generate a documentation index for AI coding agents",
                    ["generateIndex"] = "Generate Index",
                    ["cancel"] = "Cancel",
                    ["reset"] = "Reset",
                    ["browse"] = "Browse"
                },
                ["ja"] = new Dictionary<string, string>
                {
                    ["windowTitle"] = "Unity Docs Index",
                    ["title"] = "Unity ドキュメント インデックス生成",
                    ["description"] = "AIコーディングエージェント向けのドキュメントインデックスを生成します",
                    ["generateIndex"] = "インデックス生成",
                    ["cancel"] = "キャンセル",
                    ["reset"] = "リセット",
                    ["browse"] = "参照"
                },
                ["zh-CN"] = new Dictionary<string, string>
                {
                    ["windowTitle"] = "Unity Docs Index",
                    ["title"] = "Unity 文档索引生成器",
                    ["description"] = "为 AI 编程助手生成文档索引",
                    ["generateIndex"] = "生成索引",
                    ["cancel"] = "取消",
                    ["reset"] = "重置",
                    ["browse"] = "浏览"
                }
            };
        }

        public static string Get(string key)
        {
            if (_strings == null)
                LoadStrings();

            if (_strings.TryGetValue(_currentLanguage, out var langStrings))
            {
                if (langStrings.TryGetValue(key, out var value))
                    return value;
            }

            // Fallback to English
            if (_strings.TryGetValue("en", out var enStrings))
            {
                if (enStrings.TryGetValue(key, out var value))
                    return value;
            }

            return $"[{key}]";
        }

        public static string Get(string key, params object[] args)
        {
            var format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }
    }
}

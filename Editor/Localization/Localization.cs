using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// 言語選択オプション
    /// </summary>
    public enum LanguageOption
    {
        English,
        Japanese,
        System
    }

    /// <summary>
    /// ローカライゼーションデータのエントリ
    /// </summary>
    [Serializable]
    internal class LocalizationEntry
    {
        public string label;
        public string tooltip;
    }

    /// <summary>
    /// JSONファイル全体を表すクラス
    /// </summary>
    [Serializable]
    internal class LocalizationData
    {
        // JsonUtilityでは辞書を直接デシリアライズできないため、
        // 手動でパースする必要がある
    }

    /// <summary>
    /// 多言語対応のためのローカライゼーションクラス
    /// JSONファイルから言語データを読み込み、言語選択に応じた文字列を提供
    /// </summary>
    public static class Localization
    {
        private const string PREFS_KEY = "CopyComponentsByRegex.Language";
        private const string DEFAULT_LANGUAGE = "ja";

        private static LanguageOption currentLanguageOption = LanguageOption.System;
        private static string currentLanguage = DEFAULT_LANGUAGE;
        private static Dictionary<string, Dictionary<string, string>> languageData = new Dictionary<string, Dictionary<string, string>>();
        private static Dictionary<string, Dictionary<string, string>> tooltipData = new Dictionary<string, Dictionary<string, string>>();
        private static bool isInitialized = false;

        /// <summary>
        /// 現在の言語オプションを取得・設定
        /// </summary>
        public static LanguageOption CurrentLanguageOption
        {
            get => currentLanguageOption;
            set
            {
                currentLanguageOption = value;
                EditorPrefs.SetInt(PREFS_KEY, (int)value);
                UpdateCurrentLanguage();
            }
        }

        /// <summary>
        /// 現在の言語コードを取得
        /// </summary>
        public static string CurrentLanguage => currentLanguage;

        /// <summary>
        /// 初期化（自動的に呼び出される）
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (isInitialized) return;

            LoadLanguageOption();
            LoadAllLanguages();
            UpdateCurrentLanguage();
            isInitialized = true;
        }

        /// <summary>
        /// 初期化状態を確認し、必要なら初期化する
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 言語オプションを読み込む
        /// </summary>
        private static void LoadLanguageOption()
        {
            int savedOption = EditorPrefs.GetInt(PREFS_KEY, (int)LanguageOption.System);
            currentLanguageOption = (LanguageOption)savedOption;
        }

        /// <summary>
        /// 現在の言語を更新
        /// </summary>
        private static void UpdateCurrentLanguage()
        {
            switch (currentLanguageOption)
            {
                case LanguageOption.English:
                    currentLanguage = "en";
                    break;
                case LanguageOption.Japanese:
                    currentLanguage = "ja";
                    break;
                case LanguageOption.System:
                default:
                    currentLanguage = DetectSystemLanguage();
                    break;
            }
        }

        /// <summary>
        /// システム言語を検出
        /// </summary>
        private static string DetectSystemLanguage()
        {
            SystemLanguage sysLang = Application.systemLanguage;
            return sysLang == SystemLanguage.Japanese ? "ja" : "en";
        }

        /// <summary>
        /// すべての言語ファイルを読み込む
        /// </summary>
        private static void LoadAllLanguages()
        {
            languageData.Clear();
            tooltipData.Clear();

            string localizationPath = GetLocalizationPath();
            if (string.IsNullOrEmpty(localizationPath)) return;

            string[] languageFiles = { "ja.json", "en.json" };
            foreach (string fileName in languageFiles)
            {
                string langCode = Path.GetFileNameWithoutExtension(fileName);
                string filePath = Path.Combine(localizationPath, fileName);

                if (File.Exists(filePath))
                {
                    LoadLanguageFile(langCode, filePath);
                }
            }
        }

        /// <summary>
        /// ローカライゼーションディレクトリのパスを取得
        /// </summary>
        private static string GetLocalizationPath()
        {
            // PathUtilityを使用してパスを解決
            string packagePath = PathUtility.PackageRootPath;
            if (string.IsNullOrEmpty(packagePath)) return null;

            return Path.Combine(packagePath, "Editor", "Localization");
        }

        /// <summary>
        /// 言語ファイルを読み込む
        /// </summary>
        private static void LoadLanguageFile(string langCode, string filePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                var labels = new Dictionary<string, string>();
                var tooltips = new Dictionary<string, string>();

                // シンプルなJSONパース（JsonUtilityでは辞書を扱えないため手動で）
                ParseLocalizationJson(jsonContent, labels, tooltips);

                languageData[langCode] = labels;
                tooltipData[langCode] = tooltips;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Localization] Failed to load language file {filePath}: {e.Message}");
            }
        }

        /// <summary>
        /// JSONをパースしてラベルとツールチップを抽出
        /// </summary>
        private static void ParseLocalizationJson(string json, Dictionary<string, string> labels, Dictionary<string, string> tooltips)
        {
            // 簡易的なJSONパーサー
            // 形式: { "key": { "label": "...", "tooltip": "..." }, ... }
            
            int pos = 0;
            SkipWhitespace(json, ref pos);
            
            if (pos >= json.Length || json[pos] != '{') return;
            pos++; // skip '{'

            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                if (pos >= json.Length) break;
                if (json[pos] == '}') break;
                if (json[pos] == ',') { pos++; continue; }

                // キーを読み取り
                string key = ReadString(json, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != ':') break;
                pos++; // skip ':'

                SkipWhitespace(json, ref pos);
                if (pos >= json.Length || json[pos] != '{') break;
                pos++; // skip '{'

                string label = "";
                string tooltip = "";

                // 内部オブジェクトを読み取り
                while (pos < json.Length)
                {
                    SkipWhitespace(json, ref pos);
                    if (pos >= json.Length) break;
                    if (json[pos] == '}') { pos++; break; }
                    if (json[pos] == ',') { pos++; continue; }

                    string innerKey = ReadString(json, ref pos);
                    if (string.IsNullOrEmpty(innerKey)) break;

                    SkipWhitespace(json, ref pos);
                    if (pos >= json.Length || json[pos] != ':') break;
                    pos++; // skip ':'

                    SkipWhitespace(json, ref pos);
                    string value = ReadString(json, ref pos);

                    if (innerKey == "label") label = value;
                    else if (innerKey == "tooltip") tooltip = value;
                }

                labels[key] = label;
                tooltips[key] = tooltip;
            }
        }

        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && char.IsWhiteSpace(json[pos]))
            {
                pos++;
            }
        }

        private static string ReadString(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length || json[pos] != '"') return null;
            pos++; // skip opening '"'

            var result = new System.Text.StringBuilder();
            while (pos < json.Length)
            {
                char c = json[pos];
                if (c == '"')
                {
                    pos++; // skip closing '"'
                    return result.ToString();
                }
                if (c == '\\' && pos + 1 < json.Length)
                {
                    pos++;
                    char escaped = json[pos];
                    switch (escaped)
                    {
                        case 'n': result.Append('\n'); break;
                        case 't': result.Append('\t'); break;
                        case 'r': result.Append('\r'); break;
                        case '"': result.Append('"'); break;
                        case '\\': result.Append('\\'); break;
                        default: result.Append(escaped); break;
                    }
                }
                else
                {
                    result.Append(c);
                }
                pos++;
            }
            return result.ToString();
        }

        /// <summary>
        /// ラベルを取得
        /// </summary>
        /// <param name="key">ローカライゼーションキー</param>
        /// <returns>現在の言語に対応するラベル文字列</returns>
        public static string L(string key)
        {
            EnsureInitialized();

            if (languageData.TryGetValue(currentLanguage, out var labels))
            {
                if (labels.TryGetValue(key, out string label))
                {
                    return label;
                }
            }

            // フォールバック: 英語を試す
            if (currentLanguage != "en" && languageData.TryGetValue("en", out var enLabels))
            {
                if (enLabels.TryGetValue(key, out string label))
                {
                    return label;
                }
            }

            // 見つからない場合はキーをそのまま返す
            return key;
        }

        /// <summary>
        /// ツールチップを取得
        /// </summary>
        /// <param name="key">ローカライゼーションキー</param>
        /// <returns>現在の言語に対応するツールチップ文字列</returns>
        public static string Tooltip(string key)
        {
            EnsureInitialized();

            if (tooltipData.TryGetValue(currentLanguage, out var tooltips))
            {
                if (tooltips.TryGetValue(key, out string tooltip))
                {
                    return tooltip;
                }
            }

            // フォールバック: 英語を試す
            if (currentLanguage != "en" && tooltipData.TryGetValue("en", out var enTooltips))
            {
                if (enTooltips.TryGetValue(key, out string tooltip))
                {
                    return tooltip;
                }
            }

            return "";
        }

        /// <summary>
        /// GUIContentを取得（ラベル+ツールチップ）
        /// </summary>
        /// <param name="key">ローカライゼーションキー</param>
        /// <returns>ラベルとツールチップを含むGUIContent</returns>
        public static GUIContent Content(string key)
        {
            return new GUIContent(L(key), Tooltip(key));
        }

        /// <summary>
        /// 利用可能な言語を取得
        /// </summary>
        public static string[] GetAvailableLanguages()
        {
            EnsureInitialized();
            var languages = new List<string>(languageData.Keys);
            return languages.ToArray();
        }

        /// <summary>
        /// 言語データを再読み込み
        /// </summary>
        public static void Reload()
        {
            isInitialized = false;
            Initialize();
        }

        /// <summary>
        /// 指定した言語のキー数を取得（テスト用）
        /// </summary>
        internal static int GetKeyCount(string langCode)
        {
            EnsureInitialized();
            if (languageData.TryGetValue(langCode, out var labels))
            {
                return labels.Count;
            }
            return 0;
        }

        /// <summary>
        /// 指定した言語のキー一覧を取得（テスト用）
        /// </summary>
        internal static IEnumerable<string> GetKeys(string langCode)
        {
            EnsureInitialized();
            if (languageData.TryGetValue(langCode, out var labels))
            {
                return labels.Keys;
            }
            return new string[0];
        }
    }
}

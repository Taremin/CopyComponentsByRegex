using System.Collections.Generic;
using UnityEditor;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// コピー操作の設定を管理するクラス
    /// EditorUserSettingsを使用して設定を永続化
    /// </summary>
    public class CopySettings
    {
        private const string ConfigPrefix = "CopyComponentsByRegex/";

        /// <summary>
        /// コンポーネント検索用の正規表現パターン
        /// </summary>
        public string pattern = "";

        /// <summary>
        /// コピー先に同じコンポーネントがある場合、削除してからコピー
        /// </summary>
        public bool isRemoveBeforeCopy = false;

        /// <summary>
        /// コピー先にオブジェクトがない場合、オブジェクトごとコピー
        /// </summary>
        public bool isObjectCopy = false;

        /// <summary>
        /// マッチしたコンポーネントを持つオブジェクトのみコピー
        /// </summary>
        public bool isObjectCopyMatchOnly = false;

        /// <summary>
        /// Clothコンポーネントの頂点を最近傍検索でコピー
        /// </summary>
        public bool isClothNNS = false;

        /// <summary>
        /// Transformコンポーネントがマッチした場合に値をコピー
        /// </summary>
        public bool copyTransform = false;

        /// <summary>
        /// Paste後に結果レポートを表示
        /// </summary>
        public bool showReportAfterPaste = false;

        /// <summary>
        /// 置換リストの折りたたみ状態
        /// </summary>
        public bool showReplacementRules = false;

        /// <summary>
        /// 注意書きの折りたたみ状態
        /// </summary>
        public bool showNotes = false;

        /// <summary>
        /// デバッグ情報エクスポートの折りたたみ状態
        /// </summary>
        public bool showDebugExport = false;

        /// <summary>
        /// コンポーネント一覧の折りたたみ状態
        /// </summary>
        public bool showComponentList = false;

        /// <summary>
        /// 名前置換ルールのリスト
        /// </summary>
        public List<ReplacementRule> replacementRules = new List<ReplacementRule>();

        /// <summary>
        /// EditorUserSettingsから設定を読み込む
        /// </summary>
        public void Load()
        {
            pattern = EditorUserSettings.GetConfigValue(ConfigPrefix + "pattern") ?? "";
            isRemoveBeforeCopy = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "isRemoveBeforeCopy"), isRemoveBeforeCopy);
            isObjectCopy = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "isObjectCopy"), isObjectCopy);
            isObjectCopyMatchOnly = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "isObjectCopyMatchOnly"), isObjectCopyMatchOnly);
            isClothNNS = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "isClothNNS"), isClothNNS);
            copyTransform = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "copyTransform"), copyTransform);
            showReportAfterPaste = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "showReportAfterPaste"), showReportAfterPaste);
            showReplacementRules = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "showReplacementRules"), showReplacementRules);
            showNotes = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "showNotes"), showNotes);
            showDebugExport = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "showDebugExport"), showDebugExport);
            showComponentList = ParseBool(EditorUserSettings.GetConfigValue(ConfigPrefix + "showComponentList"), showComponentList);
        }

        /// <summary>
        /// 設定をEditorUserSettingsに保存
        /// </summary>
        public void Save()
        {
            EditorUserSettings.SetConfigValue(ConfigPrefix + "pattern", pattern);
            EditorUserSettings.SetConfigValue(ConfigPrefix + "isRemoveBeforeCopy", isRemoveBeforeCopy.ToString());
            EditorUserSettings.SetConfigValue(ConfigPrefix + "isObjectCopy", isObjectCopy.ToString());
            EditorUserSettings.SetConfigValue(ConfigPrefix + "isObjectCopyMatchOnly", isObjectCopyMatchOnly.ToString());
            EditorUserSettings.SetConfigValue(ConfigPrefix + "isClothNNS", isClothNNS.ToString());
            EditorUserSettings.SetConfigValue(ConfigPrefix + "copyTransform", copyTransform.ToString());
            EditorUserSettings.SetConfigValue(ConfigPrefix + "showReportAfterPaste", showReportAfterPaste.ToString());
            EditorUserSettings.SetConfigValue(ConfigPrefix + "showReplacementRules", showReplacementRules.ToString());
            EditorUserSettings.SetConfigValue(ConfigPrefix + "showNotes", showNotes.ToString());
            EditorUserSettings.SetConfigValue(ConfigPrefix + "showDebugExport", showDebugExport.ToString());
            EditorUserSettings.SetConfigValue(ConfigPrefix + "showComponentList", showComponentList.ToString());
        }

        /// <summary>
        /// 個別の設定を保存（GUI操作時用）
        /// </summary>
        public void SaveSetting(string key, string value)
        {
            EditorUserSettings.SetConfigValue(ConfigPrefix + key, value);
        }

        private static bool ParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return bool.Parse(value);
        }
    }
}

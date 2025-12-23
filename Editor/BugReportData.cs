using System;
using System.Collections.Generic;
using System.Text;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// バグレポートのルートデータ構造
    /// JSON形式でのエクスポート/インポートに使用
    /// </summary>
    [Serializable]
    public class BugReportData
    {
        /// <summary>JSONフォーマットのバージョン</summary>
        public string version = "1.0.0";

        /// <summary>エディタ拡張のバージョン（package.jsonから取得）</summary>
        public string editorVersion;

        /// <summary>エクスポート日時（ISO 8601形式）</summary>
        public string timestamp;

        /// <summary>プロパティ値を含むかどうか</summary>
        public bool includeProperties = true;

        /// <summary>コピー元オブジェクトの情報</summary>
        public ObjectData source;

        /// <summary>コピー先オブジェクトの情報</summary>
        public ObjectData destination;

        /// <summary>コピー設定</summary>
        public SettingsData settings;

        /// <summary>変更ログ</summary>
        public List<ModificationLogData> modificationLogs = new List<ModificationLogData>();
    }

    /// <summary>
    /// オブジェクト情報（ソースまたはデスティネーション）
    /// </summary>
    [Serializable]
    public class ObjectData
    {
        /// <summary>オブジェクト名</summary>
        public string name;

        /// <summary>Humanoidかどうか</summary>
        public bool isHumanoid;

        /// <summary>オブジェクト階層</summary>
        public List<HierarchyItemData> hierarchy = new List<HierarchyItemData>();
    }

    /// <summary>
    /// 階層内の各オブジェクトを表すデータ
    /// </summary>
    [Serializable]
    public class HierarchyItemData
    {
        /// <summary>オブジェクト名</summary>
        public string name;

        /// <summary>ルートからのパス</summary>
        public string path;

        /// <summary>コンポーネントリスト</summary>
        public List<ComponentData> components = new List<ComponentData>();

        /// <summary>子オブジェクト</summary>
        public List<HierarchyItemData> children = new List<HierarchyItemData>();
    }

    /// <summary>
    /// コンポーネント情報
    /// </summary>
    [Serializable]
    public class ComponentData
    {
        /// <summary>コンポーネントの型名（短縮形）</summary>
        public string typeName;

        /// <summary>コンポーネントの完全修飾型名</summary>
        public string typeFullName;

        /// <summary>プロパティリスト</summary>
        public List<PropertyData> properties = new List<PropertyData>();
    }

    /// <summary>
    /// プロパティ情報
    /// </summary>
    [Serializable]
    public class PropertyData
    {
        /// <summary>プロパティ名</summary>
        public string name;

        /// <summary>プロパティの型</summary>
        public string type;

        /// <summary>プロパティ値（JSON文字列）</summary>
        public string value;
    }

    /// <summary>
    /// コピー設定のシリアライズ用データ
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        /// <summary>コンポーネント検索パターン</summary>
        public string pattern;

        /// <summary>コピー前に既存コンポーネントを削除</summary>
        public bool isRemoveBeforeCopy;

        /// <summary>オブジェクトごとコピー</summary>
        public bool isObjectCopy;

        /// <summary>マッチしたコンポーネントを持つオブジェクトのみコピー</summary>
        public bool isObjectCopyMatchOnly;

        /// <summary>Clothコンポーネントの最近傍検索</summary>
        public bool isClothNNS;

        /// <summary>Transformの値をコピー</summary>
        public bool copyTransform;

        /// <summary>置換ルールリスト</summary>
        public List<RuleData> replacementRules = new List<RuleData>();
    }

    /// <summary>
    /// 置換ルールのシリアライズ用データ
    /// </summary>
    [Serializable]
    public class RuleData
    {
        /// <summary>ルールタイプ（"Regex" または "HumanoidBone"）</summary>
        public string type;

        /// <summary>正規表現パターン（Regexタイプの場合）</summary>
        public string srcPattern;

        /// <summary>置換後のパターン（Regexタイプの場合）</summary>
        public string dstPattern;

        /// <summary>ボーングループ（HumanoidBoneタイプの場合）</summary>
        public string boneGroup;

        /// <summary>選択モード（"Group" または "Individual"）</summary>
        public string boneSelectionMode;

        /// <summary>個別ボーン名（Individual選択の場合）</summary>
        public string singleBone;

        /// <summary>ルールが有効かどうか</summary>
        public bool enabled;
    }

    /// <summary>
    /// 変更ログのシリアライズ用データ
    /// </summary>
    [Serializable]
    public class ModificationLogData
    {
        /// <summary>対象オブジェクトのパス</summary>
        public string targetPath;

        /// <summary>コンポーネント型名</summary>
        public string componentType;

        /// <summary>操作種類（"Add", "Remove", "Update", "CreateObject"）</summary>
        public string operation;

        /// <summary>操作メッセージ</summary>
        public string message;
    }

    /// <summary>
    /// シンプルなJSONシリアライザ
    /// UnityのJsonUtilityはDictionaryをサポートしないため、カスタム実装
    /// </summary>
    public static class SimpleJsonSerializer
    {
        /// <summary>
        /// BugReportDataをJSON文字列に変換
        /// </summary>
        public static string Serialize(BugReportData data, bool prettyPrint = true)
        {
            var sb = new StringBuilder();
            SerializeObject(sb, data, 0, prettyPrint);
            return sb.ToString();
        }

        private static void SerializeObject(StringBuilder sb, object obj, int indent, bool prettyPrint)
        {
            if (obj == null)
            {
                sb.Append("null");
                return;
            }

            var type = obj.GetType();

            if (type == typeof(string))
            {
                sb.Append("\"");
                sb.Append(EscapeString((string)obj));
                sb.Append("\"");
            }
            else if (type == typeof(bool))
            {
                sb.Append((bool)obj ? "true" : "false");
            }
            else if (type == typeof(int) || type == typeof(long))
            {
                sb.Append(obj.ToString());
            }
            else if (type == typeof(float))
            {
                sb.Append(((float)obj).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (type == typeof(double))
            {
                sb.Append(((double)obj).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                SerializeList(sb, (System.Collections.IList)obj, indent, prettyPrint);
            }
            else if (type.IsClass)
            {
                SerializeClass(sb, obj, indent, prettyPrint);
            }
            else
            {
                sb.Append("\"");
                sb.Append(EscapeString(obj.ToString()));
                sb.Append("\"");
            }
        }

        private static void SerializeClass(StringBuilder sb, object obj, int indent, bool prettyPrint)
        {
            sb.Append("{");
            if (prettyPrint) sb.AppendLine();

            var fields = obj.GetType().GetFields();
            bool first = true;

            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                if (value == null) continue;

                if (!first)
                {
                    sb.Append(",");
                    if (prettyPrint) sb.AppendLine();
                }
                first = false;

                if (prettyPrint) sb.Append(new string(' ', (indent + 1) * 2));
                sb.Append("\"");
                sb.Append(field.Name);
                sb.Append("\": ");

                SerializeObject(sb, value, indent + 1, prettyPrint);
            }

            if (prettyPrint)
            {
                sb.AppendLine();
                sb.Append(new string(' ', indent * 2));
            }
            sb.Append("}");
        }

        private static void SerializeList(StringBuilder sb, System.Collections.IList list, int indent, bool prettyPrint)
        {
            sb.Append("[");
            if (prettyPrint && list.Count > 0) sb.AppendLine();

            for (int i = 0; i < list.Count; i++)
            {
                if (prettyPrint) sb.Append(new string(' ', (indent + 1) * 2));
                SerializeObject(sb, list[i], indent + 1, prettyPrint);

                if (i < list.Count - 1)
                {
                    sb.Append(",");
                }
                if (prettyPrint) sb.AppendLine();
            }

            if (prettyPrint && list.Count > 0) sb.Append(new string(' ', indent * 2));
            sb.Append("]");
        }

        private static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        /// <summary>
        /// JSON文字列からBugReportDataをデシリアライズ
        /// </summary>
        public static BugReportData Deserialize(string json)
        {
            // JsonUtilityを使用してデシリアライズ（BugReportDataはDictionaryを使用しないため可能）
            return UnityEngine.JsonUtility.FromJson<BugReportData>(json);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// バグレポートのエクスポート機能を提供するクラス
    /// TreeItem, CopySettings, ModificationLogsからBugReportDataを生成し、
    /// JSON形式でクリップボードにコピーする
    /// </summary>
    public static class BugReportExporter
    {
        /// <summary>
        /// バグレポートデータをエクスポート
        /// </summary>
        /// <param name="source">コピー元のルートオブジェクト</param>
        /// <param name="copyTree">コピー元のツリー構造</param>
        /// <param name="destination">コピー先のルートオブジェクト</param>
        /// <param name="settings">コピー設定</param>
        /// <param name="modificationLogs">コンポーネント変更ログ</param>
        /// <param name="modificationObjectLogs">オブジェクト変更ログ</param>
        /// <param name="includeProperties">プロパティ値を含むかどうか</param>
        /// <returns>エクスポートされたバグレポートデータ</returns>
        public static BugReportData Export(
            GameObject source,
            TreeItem copyTree,
            GameObject destination,
            CopySettings settings,
            List<ModificationEntry> modificationLogs,
            List<ModificationEntry> modificationObjectLogs,
            bool includeProperties = true)
        {
            var data = new BugReportData
            {
                version = "1.0.0",
                editorVersion = GetEditorVersion(),
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                includeProperties = includeProperties
            };

            // コピー元情報
            if (source != null)
            {
                data.source = new ObjectData
                {
                    name = source.name,
                    isHumanoid = NameMatcher.IsHumanoid(source)
                };

                if (copyTree != null)
                {
                    data.source.hierarchy = BuildHierarchy(copyTree, "", includeProperties);
                }
            }

            // コピー先情報
            if (destination != null)
            {
                data.destination = new ObjectData
                {
                    name = destination.name,
                    isHumanoid = NameMatcher.IsHumanoid(destination)
                };

                // デスティネーションの階層を構築
                var dstTree = BuildTreeItemFromGameObject(destination);
                data.destination.hierarchy = BuildHierarchy(dstTree, "", includeProperties);
            }

            // 設定情報
            data.settings = SerializeSettings(settings);

            // 変更ログ
            if (modificationLogs != null)
            {
                foreach (var log in modificationLogs)
                {
                    data.modificationLogs.Add(SerializeModificationLog(log));
                }
            }

            if (modificationObjectLogs != null)
            {
                foreach (var log in modificationObjectLogs)
                {
                    data.modificationLogs.Add(SerializeModificationLog(log));
                }
            }

            return data;
        }

        /// <summary>
        /// TreeItemからHierarchyItemDataのリストを構築
        /// </summary>
        private static List<HierarchyItemData> BuildHierarchy(TreeItem item, string parentPath, bool includeProperties)
        {
            var result = new List<HierarchyItemData>();

            string currentPath = string.IsNullOrEmpty(parentPath) ? item.name : $"{parentPath}/{item.name}";

            var hierarchyItem = new HierarchyItemData
            {
                name = item.name,
                path = currentPath
            };

            // コンポーネント情報
            if (item.components != null)
            {
                foreach (var comp in item.components)
                {
                    if (comp == null) continue;
                    hierarchyItem.components.Add(SerializeComponent(comp, includeProperties));
                }
            }

            // 子オブジェクト
            if (item.children != null)
            {
                foreach (var child in item.children)
                {
                    var childHierarchy = BuildHierarchy(child, currentPath, includeProperties);
                    hierarchyItem.children.AddRange(childHierarchy);
                }
            }

            result.Add(hierarchyItem);
            return result;
        }

        /// <summary>
        /// GameObjectからTreeItemを再帰的に構築
        /// </summary>
        private static TreeItem BuildTreeItemFromGameObject(GameObject go)
        {
            var item = new TreeItem(go);
            
            foreach (Transform child in go.transform)
            {
                item.children.Add(BuildTreeItemFromGameObject(child.gameObject));
            }
            
            return item;
        }

        /// <summary>
        /// コンポーネントをシリアライズ
        /// </summary>
        private static ComponentData SerializeComponent(Component component, bool includeProperties)
        {
            var componentType = component.GetType();
            var data = new ComponentData
            {
                typeName = componentType.Name,
                typeFullName = componentType.FullName
            };

            if (includeProperties)
            {
                try
                {
                    var serializedObject = new SerializedObject(component);
                    var iterator = serializedObject.GetIterator();

                    // 最初の子プロパティに入る
                    if (iterator.NextVisible(true))
                    {
                        do
                        {
                            // 内部プロパティはスキップ
                            if (iterator.name.StartsWith("m_") && IsInternalProperty(iterator.name))
                            {
                                continue;
                            }

                            var propertyData = GetPropertyData(iterator);
                            if (propertyData != null)
                            {
                                data.properties.Add(propertyData);
                            }
                        }
                        while (iterator.NextVisible(false));
                    }
                }
                catch (Exception)
                {
                    // シリアライズ失敗時は空のプロパティで続行
                }
            }

            return data;
        }

        /// <summary>
        /// 内部プロパティかどうかを判定
        /// </summary>
        private static bool IsInternalProperty(string propertyName)
        {
            // Unityの内部プロパティをフィルタリング
            var internalProps = new HashSet<string>
            {
                "m_ObjectHideFlags",
                "m_CorrespondingSourceObject",
                "m_PrefabInstance",
                "m_PrefabAsset",
                "m_GameObject",
                "m_Enabled",
                "m_EditorHideFlags",
                "m_Script",
                "m_Name",
                "m_EditorClassIdentifier"
            };
            return internalProps.Contains(propertyName);
        }

        /// <summary>
        /// SerializedPropertyからPropertyDataを取得
        /// </summary>
        private static PropertyData GetPropertyData(SerializedProperty property)
        {
            var data = new PropertyData
            {
                name = property.name,
                type = property.propertyType.ToString()
            };

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    data.value = property.intValue.ToString();
                    break;
                case SerializedPropertyType.Boolean:
                    data.value = property.boolValue.ToString().ToLower();
                    break;
                case SerializedPropertyType.Float:
                    data.value = property.floatValue.ToString(CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.String:
                    data.value = property.stringValue ?? "";
                    break;
                case SerializedPropertyType.Color:
                    var color = property.colorValue;
                    data.value = $"{{\"r\":{color.r.ToString(CultureInfo.InvariantCulture)},\"g\":{color.g.ToString(CultureInfo.InvariantCulture)},\"b\":{color.b.ToString(CultureInfo.InvariantCulture)},\"a\":{color.a.ToString(CultureInfo.InvariantCulture)}}}";
                    break;
                case SerializedPropertyType.Vector2:
                    var v2 = property.vector2Value;
                    data.value = $"{{\"x\":{v2.x.ToString(CultureInfo.InvariantCulture)},\"y\":{v2.y.ToString(CultureInfo.InvariantCulture)}}}";
                    break;
                case SerializedPropertyType.Vector3:
                    var v3 = property.vector3Value;
                    data.value = $"{{\"x\":{v3.x.ToString(CultureInfo.InvariantCulture)},\"y\":{v3.y.ToString(CultureInfo.InvariantCulture)},\"z\":{v3.z.ToString(CultureInfo.InvariantCulture)}}}";
                    break;
                case SerializedPropertyType.Vector4:
                    var v4 = property.vector4Value;
                    data.value = $"{{\"x\":{v4.x.ToString(CultureInfo.InvariantCulture)},\"y\":{v4.y.ToString(CultureInfo.InvariantCulture)},\"z\":{v4.z.ToString(CultureInfo.InvariantCulture)},\"w\":{v4.w.ToString(CultureInfo.InvariantCulture)}}}";
                    break;
                case SerializedPropertyType.Quaternion:
                    var q = property.quaternionValue;
                    data.value = $"{{\"x\":{q.x.ToString(CultureInfo.InvariantCulture)},\"y\":{q.y.ToString(CultureInfo.InvariantCulture)},\"z\":{q.z.ToString(CultureInfo.InvariantCulture)},\"w\":{q.w.ToString(CultureInfo.InvariantCulture)}}}";
                    break;
                case SerializedPropertyType.Enum:
                    data.value = property.enumNames.Length > property.enumValueIndex && property.enumValueIndex >= 0
                        ? property.enumNames[property.enumValueIndex]
                        : property.enumValueIndex.ToString();
                    break;
                case SerializedPropertyType.ObjectReference:
                    // オブジェクト参照はパス情報のみ
                    if (property.objectReferenceValue != null)
                    {
                        data.value = $"{{\"type\":\"{property.objectReferenceValue.GetType().Name}\",\"name\":\"{property.objectReferenceValue.name}\"}}";
                    }
                    else
                    {
                        data.value = "null";
                    }
                    break;
                case SerializedPropertyType.ArraySize:
                    data.value = property.intValue.ToString();
                    break;
                case SerializedPropertyType.Bounds:
                    var bounds = property.boundsValue;
                    data.value = $"{{\"center\":{{\"x\":{bounds.center.x.ToString(CultureInfo.InvariantCulture)},\"y\":{bounds.center.y.ToString(CultureInfo.InvariantCulture)},\"z\":{bounds.center.z.ToString(CultureInfo.InvariantCulture)}}},\"size\":{{\"x\":{bounds.size.x.ToString(CultureInfo.InvariantCulture)},\"y\":{bounds.size.y.ToString(CultureInfo.InvariantCulture)},\"z\":{bounds.size.z.ToString(CultureInfo.InvariantCulture)}}}}}";
                    break;
                case SerializedPropertyType.Rect:
                    var rect = property.rectValue;
                    data.value = $"{{\"x\":{rect.x.ToString(CultureInfo.InvariantCulture)},\"y\":{rect.y.ToString(CultureInfo.InvariantCulture)},\"width\":{rect.width.ToString(CultureInfo.InvariantCulture)},\"height\":{rect.height.ToString(CultureInfo.InvariantCulture)}}}";
                    break;
                default:
                    // 未対応の型はnullを返す
                    return null;
            }

            return data;
        }

        /// <summary>
        /// CopySettingsをシリアライズ
        /// </summary>
        private static SettingsData SerializeSettings(CopySettings settings)
        {
            if (settings == null)
            {
                return new SettingsData();
            }

            var data = new SettingsData
            {
                pattern = settings.pattern,
                isRemoveBeforeCopy = settings.isRemoveBeforeCopy,
                isObjectCopy = settings.isObjectCopy,
                isObjectCopyMatchOnly = settings.isObjectCopyMatchOnly,
                isClothNNS = settings.isClothNNS,
                copyTransform = settings.copyTransform
            };

            if (settings.replacementRules != null)
            {
                foreach (var rule in settings.replacementRules)
                {
                    data.replacementRules.Add(SerializeRule(rule));
                }
            }

            return data;
        }

        /// <summary>
        /// ReplacementRuleをシリアライズ
        /// </summary>
        private static RuleData SerializeRule(ReplacementRule rule)
        {
            return new RuleData
            {
                type = rule.type.ToString(),
                srcPattern = rule.srcPattern,
                dstPattern = rule.dstPattern,
                boneGroup = rule.boneGroup.ToString(),
                boneSelectionMode = rule.boneSelectionMode.ToString(),
                singleBone = rule.singleBone.ToString(),
                enabled = rule.enabled
            };
        }

        /// <summary>
        /// ModificationEntryをシリアライズ
        /// </summary>
        private static ModificationLogData SerializeModificationLog(ModificationEntry entry)
        {
            return new ModificationLogData
            {
                targetPath = entry.targetPath,
                componentType = entry.componentType,
                operation = entry.operation.ToString(),
                message = entry.message
            };
        }

        /// <summary>
        /// BugReportDataをJSON文字列に変換
        /// </summary>
        public static string ToJson(BugReportData data, bool prettyPrint = true)
        {
            return SimpleJsonSerializer.Serialize(data, prettyPrint);
        }

        /// <summary>
        /// JSONをクリップボードにコピー
        /// </summary>
        public static void CopyToClipboard(BugReportData data)
        {
            string json = ToJson(data);
            GUIUtility.systemCopyBuffer = json;
            Debug.Log($"[BugReportExporter] バグレポートをクリップボードにコピーしました（{json.Length} 文字）");
        }

        /// <summary>
        /// エクスポートしてクリップボードにコピー（便利メソッド）
        /// </summary>
        public static void ExportAndCopy(
            GameObject source,
            TreeItem copyTree,
            GameObject destination,
            CopySettings settings,
            List<ModificationEntry> modificationLogs,
            List<ModificationEntry> modificationObjectLogs,
            bool includeProperties = true)
        {
            var data = Export(source, copyTree, destination, settings, modificationLogs, modificationObjectLogs, includeProperties);
            CopyToClipboard(data);
        }

        /// <summary>
        /// package.jsonからエディタ拡張のバージョンを取得
        /// </summary>
        private static string GetEditorVersion()
        {
            try
            {
                // PathUtilityを使用してパッケージのアセットパスを取得
                string packageAssetPath = PathUtility.GetPackageAssetPath();
                string[] guids = AssetDatabase.FindAssets("package t:TextAsset", new[] { packageAssetPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith("package.json"))
                    {
                        string json = File.ReadAllText(path);
                        // 簡易的にバージョンを抽出
                        int versionIndex = json.IndexOf("\"version\"");
                        if (versionIndex >= 0)
                        {
                            int colonIndex = json.IndexOf(":", versionIndex);
                            int firstQuote = json.IndexOf("\"", colonIndex + 1);
                            int secondQuote = json.IndexOf("\"", firstQuote + 1);
                            if (firstQuote >= 0 && secondQuote > firstQuote)
                            {
                                return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // エラー時は不明を返す
            }
            return "unknown";
        }
    }
}

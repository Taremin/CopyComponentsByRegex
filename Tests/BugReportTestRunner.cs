using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// 実行環境にないコンポーネントの代替として使用するスタブ
    /// JSONから復元されたコンポーネント情報を保持する
    /// </summary>
    public class StubComponent : MonoBehaviour
    {
        /// <summary>元のコンポーネント型名（短縮形）</summary>
        public string originalTypeName;

        /// <summary>元のコンポーネント完全修飾型名</summary>
        public string originalTypeFullName;

        /// <summary>プロパティ情報（JSONから復元）</summary>
        public List<PropertyData> properties = new List<PropertyData>();
    }

    /// <summary>
    /// JSONファイルを読み込んでテストを実行するランナー
    /// Tests/TestCases/ ディレクトリ内の.jsonファイルを自動検出して実行
    /// 
    /// ハイブリッドアプローチ:
    /// - 型が見つかる → 本物のコンポーネントを追加
    /// - 型が見つからない → StubComponentで代替し、警告を出力
    /// </summary>
    [TestFixture]
    public class BugReportTestRunner
    {
        // 型が見つからなかったコンポーネントを記録
        private List<string> missingComponentTypes = new List<string>();

        /// <summary>
        /// テストケースファイルのパスを取得
        /// </summary>
        public static IEnumerable<string> GetTestCases()
        {
            var testCasesDir = PathUtility.GetTestCasesDirectoryPath();
            
            if (!Directory.Exists(testCasesDir))
            {
                yield break;
            }

            foreach (var jsonFile in Directory.GetFiles(testCasesDir, "*.json"))
            {
                yield return jsonFile;
            }
        }

        /// <summary>
        /// JSONファイルからBugReportDataを読み込む
        /// </summary>
        public static BugReportData LoadFromFile(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            return SimpleJsonSerializer.Deserialize(json);
        }

        /// <summary>
        /// JSONベースのテストを実行
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public void RunBugReportTest(string jsonPath)
        {
            // JSONファイルを読み込む
            var data = LoadFromFile(jsonPath);
            Assert.IsNotNull(data, $"Failed to load JSON from: {jsonPath}");

            // バージョンチェック
            Assert.AreEqual("1.0.0", data.version, "Unsupported JSON version");

            // テストケースを実行
            ExecuteTestCase(data, Path.GetFileNameWithoutExtension(jsonPath));
        }

        /// <summary>
        /// テストケースを実行
        /// </summary>
        private void ExecuteTestCase(BugReportData data, string testName)
        {
            missingComponentTypes.Clear();

            // 1. オブジェクト階層を再構築
            GameObject sourceRoot = null;
            GameObject destRoot = null;

            try
            {
                // === 検証1: オブジェクト階層の再構築 ===
                if (data.source != null && data.source.hierarchy.Count > 0)
                {
                    sourceRoot = BuildHierarchy(data.source.name, data.source.hierarchy);
                }

                if (data.destination != null)
                {
                    destRoot = new GameObject(data.destination.name);
                }

                Assert.IsNotNull(sourceRoot, $"[{testName}] ソースオブジェクトの構築に失敗");
                Assert.IsNotNull(destRoot, $"[{testName}] デスティネーションオブジェクトの構築に失敗");

                // 見つからなかったコンポーネント型を警告
                if (missingComponentTypes.Count > 0)
                {
                    Debug.LogWarning($"[{testName}] 以下のコンポーネント型が見つからず、StubComponentで代替しました:\n" +
                        string.Join("\n", missingComponentTypes.Select(t => $"  - {t}")));
                }

                // === 検証2: 設定の復元 ===
                var settings = BuildSettings(data.settings);
                Assert.IsNotNull(settings, $"[{testName}] 設定の復元に失敗");
                Assert.AreEqual(data.settings?.pattern ?? "", settings.pattern, $"[{testName}] パターンの復元が不正");

                // 置換ルールの数を検証
                int expectedRuleCount = data.settings?.replacementRules?.Count ?? 0;
                Assert.AreEqual(expectedRuleCount, settings.replacementRules.Count, 
                    $"[{testName}] 置換ルール数の復元が不正");

                // === 検証3: modificationLogsの期待値検証 ===
                // JSONに記録されたmodificationLogsを検証（実際のコピー操作とは独立）
                AssertExpectedModificationLogs(data.modificationLogs, testName);

                // === 追加検証: 型が全て利用可能な場合のみコピー操作を実行 ===
                if (missingComponentTypes.Count == 0)
                {
                    // ComponentCopierの状態を初期化
                    ComponentCopier.activeObject = destRoot;

                    // Copy操作を実行
                    ComponentCopier.Copy(sourceRoot, settings);
                    Assert.IsNotNull(ComponentCopier.copyTree, $"[{testName}] CopyTreeの作成に失敗");

                    // Paste操作を実行（showReportAfterPasteをfalseに設定してポップアップを抑制）
                    settings.showReportAfterPaste = false;
                    ComponentCopier.Paste(destRoot, settings);

                    // 実際のmodificationLogsと比較
                    AssertActualModificationLogs(data.modificationLogs, testName);
                }
                else
                {
                    Debug.Log($"[{testName}] 一部のコンポーネント型が利用不可のため、コピー操作テストをスキップしました");
                }
            }
            finally
            {
                // クリーンアップ
                if (sourceRoot != null) UnityEngine.Object.DestroyImmediate(sourceRoot);
                if (destRoot != null) UnityEngine.Object.DestroyImmediate(destRoot);

                // ComponentCopierの状態をクリア
                ComponentCopier.copyTree = null;
                ComponentCopier.root = null;
                ComponentCopier.transforms = null;
                ComponentCopier.components = null;
                ComponentCopier.modificationLogs.Clear();
                ComponentCopier.modificationObjectLogs.Clear();
                ComponentCopier.srcBoneMapping = null;
                ComponentCopier.dstBoneMapping = null;
            }
        }

        /// <summary>
        /// オブジェクト階層を構築
        /// </summary>
        private GameObject BuildHierarchy(string rootName, List<HierarchyItemData> hierarchy)
        {
            GameObject root = new GameObject(rootName);
            root.hideFlags = HideFlags.HideAndDontSave;

            foreach (var item in hierarchy)
            {
                BuildHierarchyRecursive(root.transform, item);
            }

            return root;
        }

        /// <summary>
        /// 階層を再帰的に構築
        /// ハイブリッドアプローチ: 型が見つからない場合はStubComponentで代替
        /// </summary>
        private void BuildHierarchyRecursive(Transform parent, HierarchyItemData item)
        {
            // 親と同名でない場合のみ新しいGameObjectを作成
            Transform current = parent;
            if (parent.name != item.name)
            {
                var go = new GameObject(item.name);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.SetParent(parent);
                current = go.transform;
            }

            // コンポーネントを追加
            foreach (var compData in item.components)
            {
                var type = GetComponentType(compData.typeFullName);

                if (type != null && type != typeof(Transform) && type != typeof(RectTransform))
                {
                    // 型が見つかった → 本物のコンポーネントを追加
                    current.gameObject.AddComponent(type);
                }
                else if (type == null && !string.IsNullOrEmpty(compData.typeFullName))
                {
                    // 型が見つからない → StubComponentで代替
                    var stub = current.gameObject.AddComponent<StubComponent>();
                    stub.originalTypeName = compData.typeName;
                    stub.originalTypeFullName = compData.typeFullName;
                    stub.properties = compData.properties != null 
                        ? new List<PropertyData>(compData.properties) 
                        : new List<PropertyData>();

                    // 記録
                    if (!missingComponentTypes.Contains(compData.typeFullName))
                    {
                        missingComponentTypes.Add(compData.typeFullName);
                    }
                }
            }

            // 子オブジェクトを構築
            foreach (var child in item.children)
            {
                BuildHierarchyRecursive(current, child);
            }
        }

        /// <summary>
        /// コンポーネント型を取得
        /// </summary>
        private Type GetComponentType(string typeFullName)
        {
            if (string.IsNullOrEmpty(typeFullName)) return null;

            // Unity標準アセンブリから型を探す
            var type = Type.GetType(typeFullName);
            if (type != null) return type;

            // 全アセンブリから探す
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeFullName);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        /// CopySettingsを構築
        /// </summary>
        private CopySettings BuildSettings(SettingsData settingsData)
        {
            var settings = new CopySettings();

            if (settingsData == null) return settings;

            settings.pattern = settingsData.pattern ?? "";
            settings.isRemoveBeforeCopy = settingsData.isRemoveBeforeCopy;
            settings.isObjectCopy = settingsData.isObjectCopy;
            settings.isObjectCopyMatchOnly = settingsData.isObjectCopyMatchOnly;
            settings.isClothNNS = settingsData.isClothNNS;
            settings.copyTransform = settingsData.copyTransform;

            if (settingsData.replacementRules != null)
            {
                foreach (var ruleData in settingsData.replacementRules)
                {
                    var rule = new ReplacementRule
                    {
                        enabled = ruleData.enabled
                    };

                    if (Enum.TryParse<RuleType>(ruleData.type, out var ruleType))
                    {
                        rule.type = ruleType;
                    }

                    rule.srcPattern = ruleData.srcPattern ?? "";
                    rule.dstPattern = ruleData.dstPattern ?? "";

                    if (Enum.TryParse<HumanoidBoneGroup>(ruleData.boneGroup, out var boneGroup))
                    {
                        rule.boneGroup = boneGroup;
                    }

                    if (Enum.TryParse<HumanoidBoneSelectionMode>(ruleData.boneSelectionMode, out var selectionMode))
                    {
                        rule.boneSelectionMode = selectionMode;
                    }

                    if (Enum.TryParse<HumanBodyBones>(ruleData.singleBone, out var singleBone))
                    {
                        rule.singleBone = singleBone;
                    }

                    settings.replacementRules.Add(rule);
                }
            }

            return settings;
        }

        /// <summary>
        /// JSONに記録されたmodificationLogsの期待値を検証
        /// （実際のコピー操作とは独立して、JSONデータの妥当性を検証）
        /// </summary>
        private void AssertExpectedModificationLogs(List<ModificationLogData> expectedLogs, string testName)
        {
            if (expectedLogs == null || expectedLogs.Count == 0)
            {
                return;
            }

            // 各ログエントリが必要なフィールドを持っているか確認
            foreach (var log in expectedLogs)
            {
                Assert.IsFalse(string.IsNullOrEmpty(log.componentType), 
                    $"[{testName}] modificationLog.componentType が空です");
                Assert.IsFalse(string.IsNullOrEmpty(log.operation), 
                    $"[{testName}] modificationLog.operation が空です");
            }
        }

        /// <summary>
        /// 実際のmodificationLogsと期待値を比較
        /// </summary>
        private void AssertActualModificationLogs(List<ModificationLogData> expectedLogs, string testName)
        {
            if (expectedLogs == null || expectedLogs.Count == 0)
            {
                return;
            }

            var actualLogs = ComponentCopier.modificationLogs;

            foreach (var expected in expectedLogs)
            {
                bool found = actualLogs.Any(actual =>
                    actual.componentType == expected.componentType &&
                    actual.operation.ToString() == expected.operation
                );

                Assert.IsTrue(found, 
                    $"[{testName}] 期待されるログが見つかりません: {expected.componentType} - {expected.operation}");
            }
        }
    }
}

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
    /// BugReportTestRunnerの実行結果
    /// </summary>
    public class BugReportExecutionResult
    {
        /// <summary>テスト名</summary>
        public string TestName { get; set; }

        /// <summary>ソースオブジェクト</summary>
        public GameObject SourceRoot { get; set; }

        /// <summary>デスティネーションオブジェクト</summary>
        public GameObject DestinationRoot { get; set; }

        /// <summary>設定</summary>
        public CopySettings Settings { get; set; }

        /// <summary>元のBugReportData</summary>
        public BugReportData Data { get; set; }

        /// <summary>コンポーネントの変更ログ</summary>
        public List<ModificationEntry> ModificationLogs { get; set; } = new List<ModificationEntry>();

        /// <summary>オブジェクトの変更ログ</summary>
        public List<ModificationEntry> ModificationObjectLogs { get; set; } = new List<ModificationEntry>();

        /// <summary>見つからなかったコンポーネント型</summary>
        public List<string> MissingComponentTypes { get; set; } = new List<string>();

        /// <summary>実行が成功したかどうか</summary>
        public bool Success { get; set; }

        /// <summary>エラーメッセージ（失敗時）</summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// リソースをクリーンアップ
        /// </summary>
        public void Cleanup()
        {
            if (SourceRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(SourceRoot);
                SourceRoot = null;
            }
            if (DestinationRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(DestinationRoot);
                DestinationRoot = null;
            }

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
    /// JSONファイルを読み込んでPaste/DryRunを実行するユーティリティ
    /// テストケースの検証ロジックはIntegrationTestsに実装する
    /// </summary>
    public static class BugReportTestUtility
    {
        /// <summary>
        /// JSONファイルからBugReportDataを読み込む
        /// </summary>
        public static BugReportData LoadFromFile(string jsonPath)
        {
            string json = File.ReadAllText(jsonPath);
            return SimpleJsonSerializer.Deserialize(json);
        }

        /// <summary>
        /// JSONファイルからテスト環境を構築してPasteを実行
        /// </summary>
        /// <param name="jsonFileName">TestCasesディレクトリ内のJSONファイル名（例: "case3.json"）</param>
        /// <returns>実行結果</returns>
        public static BugReportExecutionResult ExecuteFromFile(string jsonFileName)
        {
            var testCasesDir = PathUtility.GetTestCasesDirectoryPath();
            var jsonPath = Path.Combine(testCasesDir, jsonFileName);
            
            if (!File.Exists(jsonPath))
            {
                return new BugReportExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"JSONファイルが見つかりません: {jsonPath}"
                };
            }

            var data = LoadFromFile(jsonPath);
            return Execute(data, Path.GetFileNameWithoutExtension(jsonFileName));
        }

        /// <summary>
        /// BugReportDataからテスト環境を構築してPasteを実行
        /// </summary>
        public static BugReportExecutionResult Execute(BugReportData data, string testName = "test")
        {
            var result = new BugReportExecutionResult
            {
                TestName = testName,
                Data = data
            };

            try
            {
                // オブジェクト階層を再構築
                if (data.source != null && data.source.hierarchy.Count > 0)
                {
                    result.SourceRoot = BuildHierarchy(data.source.name, data.source.hierarchy, result.MissingComponentTypes);
                }

                if (data.destination != null)
                {
                    if (data.destination.hierarchy != null && data.destination.hierarchy.Count > 0)
                    {
                        result.DestinationRoot = BuildHierarchy(data.destination.name, data.destination.hierarchy, result.MissingComponentTypes);
                    }
                    else
                    {
                        result.DestinationRoot = new GameObject(data.destination.name);
                        result.DestinationRoot.hideFlags = HideFlags.HideAndDontSave;
                    }
                }

                if (result.SourceRoot == null || result.DestinationRoot == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "オブジェクト階層の構築に失敗";
                    return result;
                }

                // 設定の復元
                result.Settings = BuildSettings(data.settings);

                // 見つからなかったコンポーネント型を警告
                if (result.MissingComponentTypes.Count > 0)
                {
                    Debug.LogWarning($"[{testName}] 以下のコンポーネント型が見つからず、StubComponentで代替しました:\n" +
                        string.Join("\n", result.MissingComponentTypes.Select(t => $"  - {t}")));
                }

                // ComponentCopierの状態を初期化
                ComponentCopier.activeObject = result.DestinationRoot;

                // Copy操作を実行
                ComponentCopier.Copy(result.SourceRoot, result.Settings);

                // HumanoidBoneルールがある場合、ボーンマッピングを設定
                bool hasHumanoidRule = result.Settings.replacementRules.Any(r => 
                    r.enabled && r.type == RuleType.HumanoidBone);
                if (hasHumanoidRule)
                {
                    ComponentCopier.srcBoneMapping = BuildBoneMappingFromHierarchy(
                        data.source?.hierarchy, data.source?.isHumanoid ?? false);
                    ComponentCopier.dstBoneMapping = BuildBoneMappingFromHierarchy(
                        data.destination?.hierarchy, data.destination?.isHumanoid ?? false);
                }

                // Paste操作を実行
                result.Settings.showReportAfterPaste = false;
                ComponentCopier.Paste(result.DestinationRoot, result.Settings);

                // 結果を収集
                result.ModificationLogs = new List<ModificationEntry>(ComponentCopier.modificationLogs);
                result.ModificationObjectLogs = new List<ModificationEntry>(ComponentCopier.modificationObjectLogs);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// オブジェクト階層を構築
        /// </summary>
        private static GameObject BuildHierarchy(string rootName, List<HierarchyItemData> hierarchy, List<string> missingComponentTypes)
        {
            GameObject root = new GameObject(rootName);
            root.hideFlags = HideFlags.HideAndDontSave;

            foreach (var item in hierarchy)
            {
                BuildHierarchyRecursive(root.transform, item, missingComponentTypes);
            }

            return root;
        }

        /// <summary>
        /// 階層を再帰的に構築
        /// </summary>
        private static void BuildHierarchyRecursive(Transform parent, HierarchyItemData item, List<string> missingComponentTypes)
        {
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
                    current.gameObject.AddComponent(type);
                }
                else if (type == null && !string.IsNullOrEmpty(compData.typeFullName))
                {
                    var stub = current.gameObject.AddComponent<StubComponent>();
                    stub.originalTypeName = compData.typeName;
                    stub.originalTypeFullName = compData.typeFullName;
                    stub.properties = compData.properties != null 
                        ? new List<PropertyData>(compData.properties) 
                        : new List<PropertyData>();

                    if (!missingComponentTypes.Contains(compData.typeFullName))
                    {
                        missingComponentTypes.Add(compData.typeFullName);
                    }
                }
            }

            // 子オブジェクトを構築
            foreach (var child in item.children)
            {
                BuildHierarchyRecursive(current, child, missingComponentTypes);
            }
        }

        /// <summary>
        /// コンポーネント型を取得
        /// </summary>
        private static Type GetComponentType(string typeFullName)
        {
            if (string.IsNullOrEmpty(typeFullName)) return null;

            var type = Type.GetType(typeFullName);
            if (type != null) return type;

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
        private static CopySettings BuildSettings(SettingsData settingsData)
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
        /// 階層構造からボーンマッピングを構築
        /// </summary>
        private static Dictionary<string, HumanBodyBones> BuildBoneMappingFromHierarchy(
            List<HierarchyItemData> hierarchy, bool isHumanoid)
        {
            var mapping = new Dictionary<string, HumanBodyBones>();
            
            if (!isHumanoid || hierarchy == null)
            {
                return mapping;
            }

            var boneNamePatterns = new Dictionary<string, HumanBodyBones>(StringComparer.OrdinalIgnoreCase)
            {
                { "hips", HumanBodyBones.Hips },
                { "siri", HumanBodyBones.Hips },
                { "pelvis", HumanBodyBones.Hips },
                { "spine", HumanBodyBones.Spine },
                { "spine1", HumanBodyBones.Spine },
                { "chest", HumanBodyBones.Chest },
                { "spine2", HumanBodyBones.Chest },
                { "upperchest", HumanBodyBones.UpperChest },
                { "spine3", HumanBodyBones.UpperChest },
                { "neck", HumanBodyBones.Neck },
                { "head", HumanBodyBones.Head },
                { "leftshoulder", HumanBodyBones.LeftShoulder },
                { "shoulder.l", HumanBodyBones.LeftShoulder },
                { "rightshoulder", HumanBodyBones.RightShoulder },
                { "shoulder.r", HumanBodyBones.RightShoulder },
                { "leftupperarm", HumanBodyBones.LeftUpperArm },
                { "upper_arm.l", HumanBodyBones.LeftUpperArm },
                { "rightupperarm", HumanBodyBones.RightUpperArm },
                { "upper_arm.r", HumanBodyBones.RightUpperArm },
                { "leftlowerarm", HumanBodyBones.LeftLowerArm },
                { "lower_arm.l", HumanBodyBones.LeftLowerArm },
                { "rightlowerarm", HumanBodyBones.RightLowerArm },
                { "lower_arm.r", HumanBodyBones.RightLowerArm },
                { "lefthand", HumanBodyBones.LeftHand },
                { "hand.l", HumanBodyBones.LeftHand },
                { "righthand", HumanBodyBones.RightHand },
                { "hand.r", HumanBodyBones.RightHand },
                { "leftupperleg", HumanBodyBones.LeftUpperLeg },
                { "upper_leg.l", HumanBodyBones.LeftUpperLeg },
                { "rightupperleg", HumanBodyBones.RightUpperLeg },
                { "upper_leg.r", HumanBodyBones.RightUpperLeg },
                { "leftlowerleg", HumanBodyBones.LeftLowerLeg },
                { "lower_leg.l", HumanBodyBones.LeftLowerLeg },
                { "rightlowerleg", HumanBodyBones.RightLowerLeg },
                { "lower_leg.r", HumanBodyBones.RightLowerLeg },
                { "leftfoot", HumanBodyBones.LeftFoot },
                { "foot.l", HumanBodyBones.LeftFoot },
                { "rightfoot", HumanBodyBones.RightFoot },
                { "foot.r", HumanBodyBones.RightFoot },
                { "lefttoes", HumanBodyBones.LeftToes },
                { "toe.l", HumanBodyBones.LeftToes },
                { "righttoes", HumanBodyBones.RightToes },
                { "toe.r", HumanBodyBones.RightToes }
            };

            void ScanHierarchy(List<HierarchyItemData> items)
            {
                if (items == null) return;
                
                foreach (var item in items)
                {
                    if (boneNamePatterns.TryGetValue(item.name, out var bone))
                    {
                        if (!mapping.ContainsKey(item.name))
                        {
                            mapping[item.name] = bone;
                        }
                    }
                    ScanHierarchy(item.children);
                }
            }

            ScanHierarchy(hierarchy);
            return mapping;
        }
    }

    /// <summary>
    /// バグレポートテストランナー（後方互換性のため残す）
    /// 新しいテストはIntegrationTestsにBugReportTestUtilityを使って追加すること
    /// </summary>
    [TestFixture]
    public class BugReportTestRunner
    {
        /// <summary>
        /// テストケースファイルのパスを取得
        /// </summary>
        public static IEnumerable<string> GetTestCases()
        {
            var testCasesDir = PathUtility.GetTestCasesDirectoryPath();
            
            if (!Directory.Exists(testCasesDir))
            {
                yield return null;
                yield break;
            }

            var jsonFiles = Directory.GetFiles(testCasesDir, "*.json");
            if (jsonFiles.Length == 0)
            {
                yield return null;
                yield break;
            }

            foreach (var jsonFile in jsonFiles)
            {
                yield return jsonFile;
            }
        }

        /// <summary>
        /// JSONベースのテストを実行（後方互換性）
        /// 各ケースの詳細な検証はIntegrationTestsで行う
        /// </summary>
        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public void RunBugReportTest(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
            {
                Assert.Ignore("テストケースファイルが存在しません。Tests/TestCases/ ディレクトリにJSONファイルを配置してください。");
                return;
            }

            var testName = Path.GetFileNameWithoutExtension(jsonPath);
            var data = BugReportTestUtility.LoadFromFile(jsonPath);
            Assert.IsNotNull(data, $"Failed to load JSON from: {jsonPath}");
            Assert.AreEqual("1.0.0", data.version, "Unsupported JSON version");

            // ユーティリティを使って実行
            var result = BugReportTestUtility.Execute(data, testName);

            try
            {
                // 基本的な検証のみ行う（詳細な検証はIntegrationTestsで）
                if (result.MissingComponentTypes.Count == 0)
                {
                    Assert.IsTrue(result.Success, result.ErrorMessage);
                    
                    // 結果をログ出力（デバッグ用）
                    Debug.Log($"[{testName}] 実行完了 - " +
                        $"ModificationLogs: {result.ModificationLogs.Count}, " +
                        $"ObjectLogs: {result.ModificationObjectLogs.Count}");
                }
                else
                {
                    Debug.Log($"[{testName}] 一部のコンポーネント型が利用不可のため、詳細な検証をスキップ");
                }
            }
            finally
            {
                result.Cleanup();
            }
        }
    }
}

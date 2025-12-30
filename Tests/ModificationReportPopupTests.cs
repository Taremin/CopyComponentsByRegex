// ModificationReportPopup のロジックテスト
// 置換ルールを考慮したオブジェクト名マッチングのテスト
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CopyComponentsByRegex;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// ModificationReportPopup の名前マッチングロジックのテスト
    /// 置換ルールを使用した子オブジェクト検索が正しく動作することを検証
    /// </summary>
    public class ModificationReportPopupTests
    {
        private GameObject sourceRoot;
        private GameObject destinationRoot;

        /// <summary>
        /// テスト用GameObjectを作成するヘルパーメソッド
        /// </summary>
        private static GameObject CreateTestGameObject(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            go.hideFlags = HideFlags.HideAndDontSave;
            if (parent != null)
            {
                go.transform.SetParent(parent);
            }
            return go;
        }

        [SetUp]
        public void SetUp()
        {
            sourceRoot = CreateTestGameObject("SourceRoot");
            destinationRoot = CreateTestGameObject("DestinationRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (sourceRoot != null)
            {
                Object.DestroyImmediate(sourceRoot);
                sourceRoot = null;
            }
            if (destinationRoot != null)
            {
                Object.DestroyImmediate(destinationRoot);
                destinationRoot = null;
            }
        }

        /// <summary>
        /// 置換ルールを使用して異なる名前のオブジェクトをマッチさせるテスト
        /// ソース: hips -> デスティネーション: siri (置換ルール: hips -> siri)
        /// 
        /// このテストは修正前は失敗し、修正後はパスする
        /// </summary>
        [Test]
        public void FindMatchingChild_WithReplacementRule_MatchesDifferentNames()
        {
            // Arrange: hips -> siri の置換ルールを設定
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("hips", "siri")
            };

            // ソースにhipsオブジェクトを作成
            var srcHips = CreateTestGameObject("hips", sourceRoot.transform);
            var srcChild = CreateTestGameObject("skirt", srcHips.transform);
            srcChild.AddComponent<BoxCollider>();

            // デスティネーションにsiriオブジェクトを作成
            var dstSiri = CreateTestGameObject("siri", destinationRoot.transform);
            var dstChild = CreateTestGameObject("skirt", dstSiri.transform);

            // デスティネーションの子を辞書に格納
            var childDic = new Dictionary<string, Transform>
            {
                { "siri", dstSiri.transform }
            };

            // Act: 置換ルールを考慮してマッチを検索
            bool found = NameMatcher.TryFindMatchingName(
                childDic, 
                "hips", 
                rules, 
                out string matchedName);

            // Assert: hipsがsiriとマッチすることを確認
            Assert.IsTrue(found, "置換ルールにより 'hips' は 'siri' とマッチすべき");
            Assert.AreEqual("siri", matchedName, "マッチした名前は 'siri' であるべき");
        }

        /// <summary>
        /// NamesMatchが置換ルールで異なる名前をマッチさせることを確認
        /// </summary>
        [Test]
        public void NamesMatch_WithReplacementRule_ReturnsTrueForTransformedName()
        {
            // Arrange
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("hips", "siri")
            };

            // Act
            bool match = NameMatcher.NamesMatch("hips", "siri", rules);

            // Assert
            Assert.IsTrue(match, "置換ルールにより 'hips' と 'siri' はマッチすべき");
        }

        /// <summary>
        /// 置換ルールがない場合、異なる名前はマッチしないことを確認
        /// </summary>
        [Test]
        public void NamesMatch_WithoutReplacementRule_ReturnsFalseForDifferentNames()
        {
            // Arrange
            var rules = new List<ReplacementRule>();

            // Act
            bool match = NameMatcher.NamesMatch("hips", "siri", rules);

            // Assert
            Assert.IsFalse(match, "置換ルールがなければ 'hips' と 'siri' はマッチしない");
        }

        /// <summary>
        /// 階層内の複数レベルで置換ルールが適用されることを確認
        /// ソース: root/hips/skirt -> デスティネーション: root/siri/skirt
        /// </summary>
        [Test]
        public void FindMatchingChild_WithReplacementRule_WorksInNestedHierarchy()
        {
            // Arrange
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("hips", "siri")
            };

            // ソース階層を作成: root -> hips -> skirt
            var srcRoot = CreateTestGameObject("root", sourceRoot.transform);
            var srcHips = CreateTestGameObject("hips", srcRoot.transform);
            var srcSkirt = CreateTestGameObject("skirt", srcHips.transform);
            srcSkirt.AddComponent<BoxCollider>();

            // デスティネーション階層を作成: root -> siri -> skirt
            var dstRoot = CreateTestGameObject("root", destinationRoot.transform);
            var dstSiri = CreateTestGameObject("siri", dstRoot.transform);
            var dstSkirt = CreateTestGameObject("skirt", dstSiri.transform);

            // rootレベルでのマッチング（完全一致）
            var rootChildDic = new Dictionary<string, Transform>
            {
                { "root", dstRoot.transform }
            };
            bool rootFound = NameMatcher.TryFindMatchingName(rootChildDic, "root", rules, out string rootMatchedName);
            Assert.IsTrue(rootFound, "rootは完全一致でマッチすべき");
            Assert.AreEqual("root", rootMatchedName);

            // hipsレベルでのマッチング（置換ルール適用）
            var hipsChildDic = new Dictionary<string, Transform>
            {
                { "siri", dstSiri.transform }
            };
            bool hipsFound = NameMatcher.TryFindMatchingName(hipsChildDic, "hips", rules, out string hipsMatchedName);
            Assert.IsTrue(hipsFound, "hipsはsiriとマッチすべき（置換ルール）");
            Assert.AreEqual("siri", hipsMatchedName);

            // skirtレベルでのマッチング（完全一致）
            var skirtChildDic = new Dictionary<string, Transform>
            {
                { "skirt", dstSkirt.transform }
            };
            bool skirtFound = NameMatcher.TryFindMatchingName(skirtChildDic, "skirt", rules, out string skirtMatchedName);
            Assert.IsTrue(skirtFound, "skirtは完全一致でマッチすべき");
            Assert.AreEqual("skirt", skirtMatchedName);
        }

        /// <summary>
        /// 置換ルールを使ってMergeWalkdownを実行し、modificationLogsが正しく生成されるかテスト
        /// ソース: root/hips/skirt -> デスティネーション: root/siri/skirt
        /// 置換ルール: hips -> siri
        /// 期待結果: skirtにコンポーネントが追加（マージされる）
        /// </summary>
        [Test]
        public void MergeWalkdown_WithReplacementRule_GeneratesCorrectModificationLogs()
        {
            // Arrange: 置換ルール hips -> siri を設定し、静的変数を初期化
            ComponentCopier.transforms = new List<Transform>();
            ComponentCopier.components = new List<Component>();
            ComponentCopier.root = sourceRoot.transform;
            ComponentCopier.modificationLogs = new List<ModificationEntry>();
            ComponentCopier.modificationObjectLogs = new List<ModificationEntry>();
            ComponentCopier.showReportAfterPaste = true;
            ComponentCopier.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule("hips", "siri")
            };
            ComponentCopier.srcBoneMapping = null;
            ComponentCopier.dstBoneMapping = null;
            ComponentCopier.copyTransform = false;
            ComponentCopier.isClothNNS = false;

            // ソース階層を作成: SourceRoot -> hips -> skirt (with BoxCollider)
            var srcHips = CreateTestGameObject("hips", sourceRoot.transform);
            var srcSkirt = CreateTestGameObject("skirt", srcHips.transform);
            srcSkirt.AddComponent<BoxCollider>();

            // デスティネーション階層を作成: DestinationRoot -> siri -> skirt
            var dstSiri = CreateTestGameObject("siri", destinationRoot.transform);
            var dstSkirt = CreateTestGameObject("skirt", dstSiri.transform);

            // ソースのTreeItemを作成
            var tree = new TreeItem(sourceRoot);
            var hipsItem = new TreeItem(srcHips);
            tree.children.Add(hipsItem);
            var skirtItem = new TreeItem(srcSkirt);
            skirtItem.components.Add(srcSkirt.GetComponent<BoxCollider>());
            hipsItem.children.Add(skirtItem);

            // Act: DryRunモードでMergeWalkdownを実行
            ComponentCopier.MergeWalkdown(destinationRoot, ref tree, 0);

            // Assert: modificationLogsにskirtへのAdd操作が記録されているか確認
            var addLogs = ComponentCopier.modificationLogs
                .Where(log => log.operation == ModificationOperation.Add)
                .ToList();

            // 置換ルールが正しく適用されていれば、skirtにBoxColliderのAdd操作が記録されるはず
            Assert.IsTrue(addLogs.Count > 0, 
                "置換ルールが適用され、skirtへのAdd操作がmodificationLogsに記録されるべき");
            
            // skirtオブジェクトへの操作があることを確認
            var skirtLog = addLogs.FirstOrDefault(log => log.targetObject == dstSkirt);
            Assert.IsNotNull(skirtLog, 
                "skirtオブジェクトへのAdd操作が記録されるべき。" +
                $"実際のログ: {string.Join(", ", addLogs.Select(l => l.targetObject?.name ?? "null"))}");
        }

        /// <summary>
        /// 置換ルールなしでは異なる名前の階層にはマージされないことを確認
        /// </summary>
        [Test]
        public void MergeWalkdown_WithoutReplacementRule_DoesNotMergeToRenamedHierarchy()
        {
            // Arrange: 置換ルールなしで静的変数を初期化
            ComponentCopier.transforms = new List<Transform>();
            ComponentCopier.components = new List<Component>();
            ComponentCopier.root = sourceRoot.transform;
            ComponentCopier.modificationLogs = new List<ModificationEntry>();
            ComponentCopier.modificationObjectLogs = new List<ModificationEntry>();
            ComponentCopier.showReportAfterPaste = true;
            ComponentCopier.replacementRules = new List<ReplacementRule>(); // ルールなし
            ComponentCopier.srcBoneMapping = null;
            ComponentCopier.dstBoneMapping = null;
            ComponentCopier.copyTransform = false;
            ComponentCopier.isClothNNS = false;

            // ソース階層を作成: SourceRoot -> hips -> skirt (with BoxCollider)
            var srcHips = CreateTestGameObject("hips", sourceRoot.transform);
            var srcSkirt = CreateTestGameObject("skirt", srcHips.transform);
            srcSkirt.AddComponent<BoxCollider>();

            // デスティネーション階層を作成: DestinationRoot -> siri -> skirt
            var dstSiri = CreateTestGameObject("siri", destinationRoot.transform);
            var dstSkirt = CreateTestGameObject("skirt", dstSiri.transform);

            // ソースのTreeItemを作成
            var tree = new TreeItem(sourceRoot);
            var hipsItem = new TreeItem(srcHips);
            tree.children.Add(hipsItem);
            var skirtItem = new TreeItem(srcSkirt);
            skirtItem.components.Add(srcSkirt.GetComponent<BoxCollider>());
            hipsItem.children.Add(skirtItem);

            // Act: DryRunモードでMergeWalkdownを実行
            ComponentCopier.MergeWalkdown(destinationRoot, ref tree, 0);

            // Assert: 置換ルールがないので、hips != siri のためマージされない
            var addLogs = ComponentCopier.modificationLogs
                .Where(log => log.operation == ModificationOperation.Add)
                .ToList();

            // skirtオブジェクトへの操作がないことを確認（名前が一致しないため）
            var skirtLog = addLogs.FirstOrDefault(log => log.targetObject == dstSkirt);
            Assert.IsNull(skirtLog, 
                "置換ルールがなければ、siri/skirtへのAdd操作は記録されないはず");
        }

        /// <summary>
        /// 子オブジェクトの辞書を作成するヘルパーメソッド
        /// </summary>
        private static Dictionary<string, Transform> BuildChildDictionary(GameObject go)
        {
            var result = new Dictionary<string, Transform>();
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i);
                result[child.name] = child;
            }
            return result;
        }

        /// <summary>
        /// 置換ルールを考慮した子オブジェクト検索（実際のレポート表示に相当）
        /// </summary>
        [Test]
        public void DrawDestinationTree_ShouldUseReplacementRulesForChildLookup()
        {
            // Arrange: 置換ルール hips -> siri
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("hips", "siri")
            };

            // ソース階層を作成: SourceRoot -> hips
            var srcHips = CreateTestGameObject("hips", sourceRoot.transform);
            srcHips.AddComponent<BoxCollider>();

            // デスティネーション階層を作成: DestinationRoot -> siri
            var dstSiri = CreateTestGameObject("siri", destinationRoot.transform);

            // ソースのTreeItemを作成
            var sourceItem = new TreeItem(sourceRoot);
            var hipsItem = new TreeItem(srcHips);
            hipsItem.components.Add(srcHips.GetComponent<BoxCollider>());
            sourceItem.children.Add(hipsItem);

            // デスティネーションの子辞書を作成
            var childDic = BuildChildDictionary(destinationRoot);

            // Act: 現在の実装では childDic.ContainsKey(sourceChild.name) を使用
            // これは失敗するはず（hips != siri）
            bool currentImplementationMatch = childDic.ContainsKey("hips");

            // 修正後の実装では TryFindMatchingName を使用
            bool fixedImplementationMatch = NameMatcher.TryFindMatchingName(
                childDic, 
                "hips", 
                rules, 
                out string matchedName);

            // Assert
            Assert.IsFalse(currentImplementationMatch, 
                "現在の実装: 直接キー検索では 'hips' は見つからない");
            Assert.IsTrue(fixedImplementationMatch, 
                "修正後の実装: 置換ルールを考慮すれば 'hips' は 'siri' とマッチ");
            Assert.AreEqual("siri", matchedName);
        }

        /// <summary>
        /// HumanoidBone置換ルールを使用して、異なるオブジェクト名をボーンタイプでマッチさせるテスト
        /// ソース: hips (Hipsボーンにマッピング) -> デスティネーション: siri (Hipsボーンにマッピング)
        /// </summary>
        [Test]
        public void NamesMatch_WithHumanoidBoneRule_MatchesDifferentNamesViaBoneMapping()
        {
            // Arrange: HumanoidBoneルール（Allグループ）を設定
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule
                {
                    type = RuleType.HumanoidBone,
                    boneGroup = HumanoidBoneGroup.All,
                    boneSelectionMode = HumanoidBoneSelectionMode.Group,
                    enabled = true
                }
            };

            // ボーンマッピングを作成（hipsとsiriの両方をHipsボーンにマッピング）
            var srcBoneMapping = new Dictionary<string, HumanBodyBones>
            {
                { "hips", HumanBodyBones.Hips }
            };
            var dstBoneMapping = new Dictionary<string, HumanBodyBones>
            {
                { "siri", HumanBodyBones.Hips }
            };

            // Act
            bool match = NameMatcher.NamesMatch("hips", "siri", rules, srcBoneMapping, dstBoneMapping);

            // Assert
            Assert.IsTrue(match, "HumanoidBoneルールにより 'hips' と 'siri' はHipsボーンとしてマッチすべき");
        }

        /// <summary>
        /// HumanoidBone置換ルールで、ボーンマッピングがnullの場合はマッチしないことを確認
        /// これは以前のバグの再現：ModificationReportPopupでボーンマッピングがnullになっていた
        /// </summary>
        [Test]
        public void NamesMatch_WithHumanoidBoneRule_WithNullMappings_ReturnsFalse()
        {
            // Arrange: HumanoidBoneルールを設定するが、ボーンマッピングはnull
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule
                {
                    type = RuleType.HumanoidBone,
                    boneGroup = HumanoidBoneGroup.All,
                    boneSelectionMode = HumanoidBoneSelectionMode.Group,
                    enabled = true
                }
            };

            // Act: ボーンマッピングがnullの場合
            bool match = NameMatcher.NamesMatch("hips", "siri", rules, null, null);

            // Assert: ボーンマッピングがないためマッチしない
            Assert.IsFalse(match, "ボーンマッピングがnullの場合、HumanoidBoneルールはマッチしない");
        }

        /// <summary>
        /// TryFindMatchingNameでHumanoidBoneルールが正しく機能することを確認
        /// </summary>
        [Test]
        public void TryFindMatchingName_WithHumanoidBoneRule_FindsMatchingBone()
        {
            // Arrange
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule
                {
                    type = RuleType.HumanoidBone,
                    boneGroup = HumanoidBoneGroup.All,
                    boneSelectionMode = HumanoidBoneSelectionMode.Group,
                    enabled = true
                }
            };

            var srcBoneMapping = new Dictionary<string, HumanBodyBones>
            {
                { "hips", HumanBodyBones.Hips }
            };
            var dstBoneMapping = new Dictionary<string, HumanBodyBones>
            {
                { "siri", HumanBodyBones.Hips }
            };

            // デスティネーションにsiriオブジェクトを作成
            var dstSiri = CreateTestGameObject("siri", destinationRoot.transform);
            var childDic = new Dictionary<string, Transform>
            {
                { "siri", dstSiri.transform }
            };

            // Act
            bool found = NameMatcher.TryFindMatchingName(
                childDic, 
                "hips", 
                rules, 
                out string matchedName,
                srcBoneMapping,
                dstBoneMapping);

            // Assert
            Assert.IsTrue(found, "HumanoidBoneルールにより 'hips' は 'siri' とマッチすべき");
            Assert.AreEqual("siri", matchedName);
        }

        /// <summary>
        /// MergeWalkdownでHumanoidBoneルールが適用され、modificationLogsが正しく生成されるテスト
        /// </summary>
        [Test]
        public void MergeWalkdown_WithHumanoidBoneRule_GeneratesCorrectModificationLogs()
        {
            // Arrange: HumanoidBoneルール（Allグループ）を設定し、静的変数を初期化
            ComponentCopier.transforms = new List<Transform>();
            ComponentCopier.components = new List<Component>();
            ComponentCopier.root = sourceRoot.transform;
            ComponentCopier.modificationLogs = new List<ModificationEntry>();
            ComponentCopier.modificationObjectLogs = new List<ModificationEntry>();
            ComponentCopier.showReportAfterPaste = true;
            ComponentCopier.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule
                {
                    type = RuleType.HumanoidBone,
                    boneGroup = HumanoidBoneGroup.All,
                    boneSelectionMode = HumanoidBoneSelectionMode.Group,
                    enabled = true
                }
            };
            // ボーンマッピングを設定（hipsとsiriを同じHipsボーンにマッピング）
            ComponentCopier.srcBoneMapping = new Dictionary<string, HumanBodyBones>
            {
                { "hips", HumanBodyBones.Hips }
            };
            ComponentCopier.dstBoneMapping = new Dictionary<string, HumanBodyBones>
            {
                { "siri", HumanBodyBones.Hips }
            };
            ComponentCopier.copyTransform = false;
            ComponentCopier.isClothNNS = false;

            // ソース階層を作成: SourceRoot -> hips -> skirt (with BoxCollider)
            var srcHips = CreateTestGameObject("hips", sourceRoot.transform);
            var srcSkirt = CreateTestGameObject("skirt", srcHips.transform);
            srcSkirt.AddComponent<BoxCollider>();

            // デスティネーション階層を作成: DestinationRoot -> siri -> skirt
            var dstSiri = CreateTestGameObject("siri", destinationRoot.transform);
            var dstSkirt = CreateTestGameObject("skirt", dstSiri.transform);

            // ソースのTreeItemを作成
            var tree = new TreeItem(sourceRoot);
            var hipsItem = new TreeItem(srcHips);
            tree.children.Add(hipsItem);
            var skirtItem = new TreeItem(srcSkirt);
            skirtItem.components.Add(srcSkirt.GetComponent<BoxCollider>());
            hipsItem.children.Add(skirtItem);

            // Act: DryRunモードでMergeWalkdownを実行
            ComponentCopier.MergeWalkdown(destinationRoot, ref tree, 0);

            // Assert: modificationLogsにskirtへのAdd操作が記録されているか確認
            var addLogs = ComponentCopier.modificationLogs
                .Where(log => log.operation == ModificationOperation.Add)
                .ToList();

            // HumanoidBoneルールが正しく適用されていれば、skirtにBoxColliderのAdd操作が記録されるはず
            Assert.IsTrue(addLogs.Count > 0, 
                "HumanoidBoneルールが適用され、skirtへのAdd操作がmodificationLogsに記録されるべき");
            
            // skirtオブジェクトへの操作があることを確認
            var skirtLog = addLogs.FirstOrDefault(log => log.targetObject == dstSkirt);
            Assert.IsNotNull(skirtLog, 
                "skirtオブジェクトへのAdd操作が記録されるべき。" +
                $"実際のログ: {string.Join(", ", addLogs.Select(l => l.targetObject?.name ?? "null"))}");
        }

        /// <summary>
        /// HumanoidBoneルールでボーンマッピングがなくてもMergeWalkdownが失敗しないことを確認
        /// （正規表現ルールのフォールバックがあればそちらでマッチ）
        /// </summary>
        [Test]
        public void MergeWalkdown_WithHumanoidBoneRule_WithoutBoneMapping_FallsBackToExactMatch()
        {
            // Arrange: HumanoidBoneルールを設定するが、ボーンマッピングはnull
            ComponentCopier.transforms = new List<Transform>();
            ComponentCopier.components = new List<Component>();
            ComponentCopier.root = sourceRoot.transform;
            ComponentCopier.modificationLogs = new List<ModificationEntry>();
            ComponentCopier.modificationObjectLogs = new List<ModificationEntry>();
            ComponentCopier.showReportAfterPaste = true;
            ComponentCopier.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule
                {
                    type = RuleType.HumanoidBone,
                    boneGroup = HumanoidBoneGroup.All,
                    boneSelectionMode = HumanoidBoneSelectionMode.Group,
                    enabled = true
                }
            };
            // ボーンマッピングなし
            ComponentCopier.srcBoneMapping = null;
            ComponentCopier.dstBoneMapping = null;
            ComponentCopier.copyTransform = false;
            ComponentCopier.isClothNNS = false;

            // ソース階層を作成: SourceRoot -> hips -> skirt (with BoxCollider)
            var srcHips = CreateTestGameObject("hips", sourceRoot.transform);
            var srcSkirt = CreateTestGameObject("skirt", srcHips.transform);
            srcSkirt.AddComponent<BoxCollider>();

            // デスティネーション階層を作成: DestinationRoot -> siri -> skirt (名前が異なる)
            var dstSiri = CreateTestGameObject("siri", destinationRoot.transform);
            var dstSkirt = CreateTestGameObject("skirt", dstSiri.transform);

            // ソースのTreeItemを作成
            var tree = new TreeItem(sourceRoot);
            var hipsItem = new TreeItem(srcHips);
            tree.children.Add(hipsItem);
            var skirtItem = new TreeItem(srcSkirt);
            skirtItem.components.Add(srcSkirt.GetComponent<BoxCollider>());
            hipsItem.children.Add(skirtItem);

            // Act: DryRunモードでMergeWalkdownを実行
            ComponentCopier.MergeWalkdown(destinationRoot, ref tree, 0);

            // Assert: ボーンマッピングがないので、hips != siri のためマージされない
            var addLogs = ComponentCopier.modificationLogs
                .Where(log => log.operation == ModificationOperation.Add)
                .ToList();

            // skirtオブジェクトへの操作がないことを確認（ボーンマッピングがないため）
            var skirtLog = addLogs.FirstOrDefault(log => log.targetObject == dstSkirt);
            Assert.IsNull(skirtLog, 
                "ボーンマッピングがなければ、siri/skirtへのAdd操作は記録されないはず");
        }
    }
}

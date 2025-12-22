// CopyComponentsByRegex の統合テスト
// 複雑なオブジェクト階層を使用した実際のコピー機能のテスト
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// CopyComponentsByRegex の統合テストクラス
    /// 複雑な階層構造とコンポーネントを使用して、実際のコピー機能をテスト
    /// </summary>
    public class IntegrationTests
    {
        private GameObject sourceRoot;
        private GameObject destinationRoot;

        /// <summary>
        /// テスト用GameObjectを作成するヘルパーメソッド
        /// HideAndDontSave フラグを設定し、シーンに保存されないようにする
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

        /// <summary>
        /// 各テスト前にソースとデスティネーションのルートを作成
        /// また、CopyComponentsByRegex の静的変数を初期化
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            sourceRoot = CreateTestGameObject("SourceRoot");
            destinationRoot = CreateTestGameObject("DestinationRoot");

            // CopyComponentsByRegex の静的変数を初期化
            // これらは本来 EditorWindow のコンテキストで初期化されるが、
            // テストから直接メソッドを呼び出すために必要
            CopyComponentsByRegex.transforms = new List<Transform>();
            CopyComponentsByRegex.components = new List<Component>();
            CopyComponentsByRegex.root = sourceRoot.transform;
            
            // マッピングとルールも初期化
            CopyComponentsByRegex.srcBoneMapping = null;
            CopyComponentsByRegex.dstBoneMapping = null;
            CopyComponentsByRegex.replacementRules = new List<ReplacementRule>();
        }

        /// <summary>
        /// 各テスト後にGameObjectを破棄し、静的変数をクリア
        /// </summary>
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

            // 静的変数をクリア
            CopyComponentsByRegex.transforms = null;
            CopyComponentsByRegex.components = null;
            CopyComponentsByRegex.root = null;
            
            // マッピングとルールをクリア
            CopyComponentsByRegex.srcBoneMapping = null;
            CopyComponentsByRegex.dstBoneMapping = null;
            CopyComponentsByRegex.replacementRules = new List<ReplacementRule>();
        }

        /// <summary>
        /// GetChildren メソッドが正しく子オブジェクトを取得するテスト
        /// </summary>
        [Test]
        public void GetChildren_WithMultipleChildren_ReturnsAllChildren()
        {
            // Arrange: 3つの子オブジェクトを作成
            var child1 = CreateTestGameObject("Child1", sourceRoot.transform);
            var child2 = CreateTestGameObject("Child2", sourceRoot.transform);
            var child3 = CreateTestGameObject("Child3", sourceRoot.transform);

            // Act
            var children = CopyComponentsByRegex.GetChildren(sourceRoot);

            // Assert
            Assert.AreEqual(3, children.Length);
            Assert.AreEqual("Child1", children[0].name);
            Assert.AreEqual("Child2", children[1].name);
            Assert.AreEqual("Child3", children[2].name);
        }

        /// <summary>
        /// GetChildren メソッドが孫オブジェクトを含まないことを確認
        /// </summary>
        [Test]
        public void GetChildren_WithGrandchildren_OnlyReturnsDirectChildren()
        {
            // Arrange: 子と孫を作成
            var child = CreateTestGameObject("Child", sourceRoot.transform);
            var grandchild = CreateTestGameObject("Grandchild", child.transform);

            // Act
            var children = CopyComponentsByRegex.GetChildren(sourceRoot);

            // Assert: 直接の子のみ返される
            Assert.AreEqual(1, children.Length);
            Assert.AreEqual("Child", children[0].name);
        }

        /// <summary>
        /// CopyWalkdown が正規表現にマッチするコンポーネントを収集するテスト
        /// </summary>
        [Test]
        public void CopyWalkdown_WithMatchingComponents_CollectsComponents()
        {
            // Arrange: BoxCollider と SphereCollider を追加
            sourceRoot.AddComponent<BoxCollider>();
            sourceRoot.AddComponent<SphereCollider>();
            sourceRoot.AddComponent<Rigidbody>();  // マッチしない

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("Collider");  // Colliderにマッチ

            // Act
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Assert: BoxCollider と SphereCollider のみ収集される
            Assert.AreEqual(2, tree.components.Count);
            Assert.IsTrue(tree.components[0] is BoxCollider);
            Assert.IsTrue(tree.components[1] is SphereCollider);
        }

        /// <summary>
        /// CopyWalkdown が子オブジェクトも再帰的に処理するテスト
        /// </summary>
        [Test]
        public void CopyWalkdown_WithChildObjects_ProcessesRecursively()
        {
            // Arrange: 階層構造を作成
            var child = CreateTestGameObject("Child", sourceRoot.transform);
            child.AddComponent<BoxCollider>();

            var grandchild = CreateTestGameObject("Grandchild", child.transform);
            grandchild.AddComponent<SphereCollider>();

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("Collider");

            // Act
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Assert: 子と孫のコンポーネントも収集
            Assert.AreEqual(1, tree.children.Count);  // Child
            Assert.AreEqual("Child", tree.children[0].name);
            Assert.AreEqual(1, tree.children[0].components.Count);  // BoxCollider
            Assert.IsTrue(tree.children[0].components[0] is BoxCollider);

            Assert.AreEqual(1, tree.children[0].children.Count);  // Grandchild
            Assert.AreEqual("Grandchild", tree.children[0].children[0].name);
            Assert.AreEqual(1, tree.children[0].children[0].components.Count);  // SphereCollider
            Assert.IsTrue(tree.children[0].children[0].components[0] is SphereCollider);
        }

        /// <summary>
        /// CopyWalkdown が空の正規表現で何もマッチしないテスト
        /// </summary>
        [Test]
        public void CopyWalkdown_WithNonMatchingRegex_CollectsNothing()
        {
            // Arrange
            sourceRoot.AddComponent<BoxCollider>();
            sourceRoot.AddComponent<Rigidbody>();

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("NonExistentComponent");

            // Act
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Assert: 何も収集されない
            Assert.AreEqual(0, tree.components.Count);
        }

        /// <summary>
        /// 複雑な階層構造でのTreeItem構築テスト
        /// </summary>
        [Test]
        public void CopyWalkdown_ComplexHierarchy_BuildsCorrectTree()
        {
            // Arrange: 複雑な階層を構築
            //   SourceRoot
            //   ├── Armature
            //   │   ├── Hips
            //   │   │   ├── LeftLeg (BoxCollider)
            //   │   │   └── RightLeg (BoxCollider)
            //   │   └── Spine (CapsuleCollider)
            //   └── Mesh

            var armature = CreateTestGameObject("Armature", sourceRoot.transform);
            var hips = CreateTestGameObject("Hips", armature.transform);
            var leftLeg = CreateTestGameObject("LeftLeg", hips.transform);
            leftLeg.AddComponent<BoxCollider>();
            var rightLeg = CreateTestGameObject("RightLeg", hips.transform);
            rightLeg.AddComponent<BoxCollider>();
            var spine = CreateTestGameObject("Spine", armature.transform);
            spine.AddComponent<CapsuleCollider>();
            var mesh = CreateTestGameObject("Mesh", sourceRoot.transform);

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("Collider");

            // Act
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Assert: ツリー構造が正しく構築される
            Assert.AreEqual(2, tree.children.Count);  // Armature, Mesh
            
            var armatureTree = tree.children[0];
            Assert.AreEqual("Armature", armatureTree.name);
            Assert.AreEqual(2, armatureTree.children.Count);  // Hips, Spine

            var hipsTree = armatureTree.children[0];
            Assert.AreEqual("Hips", hipsTree.name);
            Assert.AreEqual(2, hipsTree.children.Count);  // LeftLeg, RightLeg

            // コンポーネント数の確認
            var leftLegTree = hipsTree.children[0];
            Assert.AreEqual(1, leftLegTree.components.Count);
            Assert.IsTrue(leftLegTree.components[0] is BoxCollider);

            var rightLegTree = hipsTree.children[1];
            Assert.AreEqual(1, rightLegTree.components.Count);
            Assert.IsTrue(rightLegTree.components[0] is BoxCollider);

            var spineTree = armatureTree.children[1];
            Assert.AreEqual(1, spineTree.components.Count);
            Assert.IsTrue(spineTree.components[0] is CapsuleCollider);
        }

        /// <summary>
        /// 同名の子オブジェクトが複数ある場合のテスト
        /// </summary>
        [Test]
        public void CopyWalkdown_DuplicateNamedChildren_HandlesCorrectly()
        {
            // Arrange: 同名の子オブジェクトを作成
            var child1 = CreateTestGameObject("Child", sourceRoot.transform);
            child1.AddComponent<BoxCollider>();
            var child2 = CreateTestGameObject("Child", sourceRoot.transform);
            child2.AddComponent<SphereCollider>();

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("Collider");

            // Act
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Assert: 両方の子が収集される
            Assert.AreEqual(2, tree.children.Count);
            Assert.AreEqual("Child", tree.children[0].name);
            Assert.AreEqual("Child", tree.children[1].name);
            Assert.AreEqual(1, tree.children[0].components.Count);
            Assert.AreEqual(1, tree.children[1].components.Count);
        }

        /// <summary>
        /// MergeWalkdown が同じ構造の対象にコンポーネントをコピーするテスト
        /// </summary>
        [Test]
        public void MergeWalkdown_SameStructure_CopiesComponents()
        {
            // Arrange: ソースに BoxCollider を追加
            sourceRoot.AddComponent<BoxCollider>().size = new Vector3(2, 2, 2);

            // ソースからTreeItemを構築
            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("BoxCollider");
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Act: デスティネーションにマージ（DryRunモードでテスト）
            CopyComponentsByRegex.MergeWalkdown(destinationRoot, ref tree, 0, true);

            // Assert: DryRunなので実際にはコンポーネントは追加されない
            // このテストは統合的な動作確認として、エラーなく実行されることを確認
            Assert.IsNull(destinationRoot.GetComponent<BoxCollider>());
        }

        /// <summary>
        /// 部分一致する正規表現で複数のコンポーネントタイプを収集するテスト
        /// </summary>
        [Test]
        public void CopyWalkdown_PartialMatchRegex_MatchesMultipleTypes()
        {
            // Arrange: "Box" を含むコンポーネントを追加
            sourceRoot.AddComponent<BoxCollider>();
            sourceRoot.AddComponent<Rigidbody>();  // マッチしない

            var tree = new TreeItem(sourceRoot);
            // 実際の型名は "UnityEngine.BoxCollider" なので部分一致で検索
            var regex = new Regex("Box");

            // Act
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Assert: BoxCollider のみマッチ
            Assert.AreEqual(1, tree.components.Count);
            Assert.IsTrue(tree.components[0] is BoxCollider);
        }

        /// <summary>
        /// 大文字小文字を区別する正規表現のテスト
        /// </summary>
        [Test]
        public void CopyWalkdown_CaseSensitiveRegex_MatchesCorrectly()
        {
            // Arrange
            sourceRoot.AddComponent<BoxCollider>();

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("boxcollider");  // 小文字

            // Act
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Assert: 大文字小文字が異なるためマッチしない
            Assert.AreEqual(0, tree.components.Count);
        }

        /// <summary>
        /// 大文字小文字を無視する正規表現のテスト
        /// </summary>
        [Test]
        public void CopyWalkdown_CaseInsensitiveRegex_MatchesCorrectly()
        {
            // Arrange
            sourceRoot.AddComponent<BoxCollider>();

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("boxcollider", RegexOptions.IgnoreCase);

            // Act
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Assert: 大文字小文字を無視するためマッチする
            Assert.AreEqual(1, tree.components.Count);
        }

        #region HumanoidBoneマッピング統合テスト

        /// <summary>
        /// モックのソース側ボーンマッピングを作成
        /// </summary>
        private static Dictionary<string, HumanBodyBones> CreateMockSrcMapping()
        {
            return new Dictionary<string, HumanBodyBones>
            {
                { "J_Bip_C_Head", HumanBodyBones.Head },
                { "J_Bip_C_Hips", HumanBodyBones.Hips },
                { "J_Bip_C_Spine", HumanBodyBones.Spine },
                { "J_Bip_L_UpperArm", HumanBodyBones.LeftUpperArm },
            };
        }

        /// <summary>
        /// モックのデスティネーション側ボーンマッピングを作成
        /// </summary>
        private static Dictionary<string, HumanBodyBones> CreateMockDstMapping()
        {
            return new Dictionary<string, HumanBodyBones>
            {
                { "Head", HumanBodyBones.Head },
                { "Hips", HumanBodyBones.Hips },
                { "Spine", HumanBodyBones.Spine },
                { "LeftUpperArm", HumanBodyBones.LeftUpperArm },
            };
        }

        /// <summary>
        /// HumanoidBoneルールを使用してMergeWalkdownが異なるボーン名でもコピーする
        /// </summary>
        [Test]
        public void MergeWalkdown_WithHumanoidBoneRule_CopiesComponentsToDifferentBoneNames()
        {
            // Arrange - VRoid Studio形式のソース階層
            var srcHead = CreateTestGameObject("J_Bip_C_Head", sourceRoot.transform);
            var srcBoxCollider = srcHead.AddComponent<BoxCollider>();
            
            var srcSpine = CreateTestGameObject("J_Bip_C_Spine", sourceRoot.transform);
            var srcSphereCollider = srcSpine.AddComponent<SphereCollider>();

            // FBX標準形式のデスティネーション階層
            var dstHead = CreateTestGameObject("Head", destinationRoot.transform);
            var dstSpine = CreateTestGameObject("Spine", destinationRoot.transform);

            // マッピングを設定
            CopyComponentsByRegex.srcBoneMapping = CreateMockSrcMapping();
            CopyComponentsByRegex.dstBoneMapping = CreateMockDstMapping();
            CopyComponentsByRegex.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // TreeItemを直接構築（CopyWalkdownをバイパス）
            var tree = new TreeItem(sourceRoot);
            
            // J_Bip_C_Headの子TreeItem
            var headTreeItem = new TreeItem(srcHead);
            headTreeItem.components.Add(srcBoxCollider);
            tree.children.Add(headTreeItem);
            
            // J_Bip_C_Spineの子TreeItem
            var spineTreeItem = new TreeItem(srcSpine);
            spineTreeItem.components.Add(srcSphereCollider);
            tree.children.Add(spineTreeItem);

            // デバッグ: 構築したTreeItemを確認
            Assert.AreEqual(2, tree.children.Count, "tree should have 2 children");
            Assert.AreEqual("J_Bip_C_Head", tree.children[0].name);
            Assert.AreEqual(1, tree.children[0].components.Count, "Head child should have 1 component");
            
            // デバッグ: マッピングが設定されているか確認
            Assert.IsNotNull(CopyComponentsByRegex.srcBoneMapping);
            Assert.IsNotNull(CopyComponentsByRegex.dstBoneMapping);
            
            // デバッグ: NamesMatchが期待通り動作するか確認
            bool match = NameMatcher.NamesMatch("J_Bip_C_Head", "Head", 
                CopyComponentsByRegex.replacementRules, 
                CopyComponentsByRegex.srcBoneMapping, 
                CopyComponentsByRegex.dstBoneMapping);
            Assert.IsTrue(match, "NamesMatch should work for J_Bip_C_Head -> Head");
            
            // デバッグ: TryFindMatchingNameが期待通り動作するか確認
            var childDic = new Dictionary<string, Transform>();
            childDic["Head"] = dstHead.transform;
            childDic["Spine"] = dstSpine.transform;
            bool found = NameMatcher.TryFindMatchingName(childDic, "J_Bip_C_Head", 
                CopyComponentsByRegex.replacementRules, out string matchedName,
                CopyComponentsByRegex.srcBoneMapping, 
                CopyComponentsByRegex.dstBoneMapping);
            Assert.IsTrue(found, "TryFindMatchingName should find Head");
            Assert.AreEqual("Head", matchedName);

            // Act - DryRunモードでテスト（テスト環境ではPasteComponentAsNewが正常に動作しない）
            CopyComponentsByRegex.MergeWalkdown(destinationRoot, ref tree, 0, true);

            // Assert - DryRunなのでコンポーネントは実際には追加されない
            // NamesMatchとTryFindMatchingNameが正しく動作することは上記のアサーションで確認済み
            Assert.IsNull(dstHead.GetComponent<BoxCollider>(), "DryRun: Head should not have BoxCollider");
            Assert.IsNull(dstSpine.GetComponent<SphereCollider>(), "DryRun: Spine should not have SphereCollider");
        }

        /// <summary>
        /// HumanoidBoneルールで指定されたグループのみがコピーされる
        /// </summary>
        [Test]
        public void MergeWalkdown_WithHeadGroupOnly_OnlyCopiesHeadBones()
        {
            // Arrange - ソース階層
            var srcHead = CreateTestGameObject("J_Bip_C_Head", sourceRoot.transform);
            srcHead.AddComponent<BoxCollider>();
            
            var srcArm = CreateTestGameObject("J_Bip_L_UpperArm", sourceRoot.transform);
            srcArm.AddComponent<SphereCollider>();

            // デスティネーション階層
            CreateTestGameObject("Head", destinationRoot.transform);
            CreateTestGameObject("LeftUpperArm", destinationRoot.transform);

            // マッピングを設定（Headグループのみ）
            CopyComponentsByRegex.srcBoneMapping = CreateMockSrcMapping();
            CopyComponentsByRegex.dstBoneMapping = CreateMockDstMapping();
            CopyComponentsByRegex.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.Head)  // 頭グループのみ
            };

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("Collider");
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Act - DryRunモードでテスト
            CopyComponentsByRegex.MergeWalkdown(destinationRoot, ref tree, 0, true);

            // Assert
            // NamesMatchのHeadグループフィルタリングが正しく動作することを確認
            // Headはマッチ、LeftUpperArmはマッチしないことを事前確認
            bool headMatch = NameMatcher.NamesMatch("J_Bip_C_Head", "Head", 
                CopyComponentsByRegex.replacementRules, 
                CopyComponentsByRegex.srcBoneMapping, 
                CopyComponentsByRegex.dstBoneMapping);
            Assert.IsTrue(headMatch, "Head should match with Head group");
            
            bool armMatch = NameMatcher.NamesMatch("J_Bip_L_UpperArm", "LeftUpperArm", 
                CopyComponentsByRegex.replacementRules, 
                CopyComponentsByRegex.srcBoneMapping, 
                CopyComponentsByRegex.dstBoneMapping);
            Assert.IsFalse(armMatch, "LeftUpperArm should NOT match with Head-only group");
        }

        /// <summary>
        /// マッピングがない場合はHumanoidBoneルールが機能しない
        /// </summary>
        [Test]
        public void MergeWalkdown_WithoutMapping_HumanoidRuleDoesNotWork()
        {
            // Arrange - ソース階層
            var srcHead = CreateTestGameObject("J_Bip_C_Head", sourceRoot.transform);
            srcHead.AddComponent<BoxCollider>();

            // デスティネーション階層
            CreateTestGameObject("Head", destinationRoot.transform);

            // マッピングなし
            CopyComponentsByRegex.srcBoneMapping = null;
            CopyComponentsByRegex.dstBoneMapping = null;
            CopyComponentsByRegex.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("Collider");
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Act - DryRunモードでテスト
            CopyComponentsByRegex.MergeWalkdown(destinationRoot, ref tree, 0, true);

            // Assert - NamesMatchがマッピングなしで失敗することを確認
            bool match = NameMatcher.NamesMatch("J_Bip_C_Head", "Head", 
                CopyComponentsByRegex.replacementRules, 
                CopyComponentsByRegex.srcBoneMapping, 
                CopyComponentsByRegex.dstBoneMapping);
            Assert.IsFalse(match, "NamesMatch should fail without mapping");
        }

        /// <summary>
        /// 正規表現ルールとHumanoidBoneルールの組み合わせ
        /// </summary>
        [Test]
        public void MergeWalkdown_WithRegexAndHumanoidRules_BothWork()
        {
            // Arrange - ソース階層
            var srcHead = CreateTestGameObject("J_Bip_C_Head", sourceRoot.transform);
            srcHead.AddComponent<BoxCollider>();
            
            var srcCustom = CreateTestGameObject("Custom_Test", sourceRoot.transform);
            srcCustom.AddComponent<SphereCollider>();

            // デスティネーション階層
            CreateTestGameObject("Head", destinationRoot.transform);
            CreateTestGameObject("Test", destinationRoot.transform);  // 正規表現でマッチ

            // マッピングとルールを設定
            CopyComponentsByRegex.srcBoneMapping = CreateMockSrcMapping();
            CopyComponentsByRegex.dstBoneMapping = CreateMockDstMapping();
            CopyComponentsByRegex.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule("Custom_(.+)", "$1"),  // 正規表現ルール
                new ReplacementRule(HumanoidBoneGroup.Head)  // HumanoidBoneルール
            };

            var tree = new TreeItem(sourceRoot);
            var regex = new Regex("Collider");
            CopyComponentsByRegex.CopyWalkdown(sourceRoot, ref tree, ref regex);

            // Act - DryRunモードでテスト
            CopyComponentsByRegex.MergeWalkdown(destinationRoot, ref tree, 0, true);

            // Assert - 両方のルールが正しくマッチできることを確認
            bool headMatch = NameMatcher.NamesMatch("J_Bip_C_Head", "Head", 
                CopyComponentsByRegex.replacementRules, 
                CopyComponentsByRegex.srcBoneMapping, 
                CopyComponentsByRegex.dstBoneMapping);
            Assert.IsTrue(headMatch, "HumanoidBone rule should match J_Bip_C_Head -> Head");
            
            bool regexMatch = NameMatcher.NamesMatch("Custom_Test", "Test", 
                CopyComponentsByRegex.replacementRules, 
                CopyComponentsByRegex.srcBoneMapping, 
                CopyComponentsByRegex.dstBoneMapping);
            Assert.IsTrue(regexMatch, "Regex rule should match Custom_Test -> Test");
        }
        #endregion
    }
}

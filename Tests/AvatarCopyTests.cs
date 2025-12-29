// AvatarTestUtilsを使用したPaste/DryRunの実践的テスト
// 様々な置換ルールの組み合わせをテスト
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// AvatarTestUtilsを使用した実践的なコピーテスト
    /// </summary>
    public class AvatarCopyTests
    {
        private GameObject sourceRoot;
        private GameObject destRoot;

        [SetUp]
        public void SetUp()
        {
            // ソース: 標準形式のアバター
            sourceRoot = AvatarTestUtils.CreateSampleAvatar();
            sourceRoot.name = "SourceAvatar";

            // デスティネーション: VRoid形式にリネームしたアバター
            destRoot = AvatarTestUtils.CloneAvatar(sourceRoot, "DestAvatar");
            AvatarTestUtils.RenameHumanoidBones(destRoot, AvatarTestUtils.GetVRoidBoneRenameMap());

            // ComponentCopierの状態を初期化
            ComponentCopier.transforms = new List<Transform>();
            ComponentCopier.components = new List<Component>();
            ComponentCopier.modificationLogs.Clear();
            ComponentCopier.modificationObjectLogs.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (sourceRoot != null) Object.DestroyImmediate(sourceRoot);
            if (destRoot != null) Object.DestroyImmediate(destRoot);

            ComponentCopier.transforms = null;
            ComponentCopier.components = null;
            ComponentCopier.root = null;
            ComponentCopier.srcBoneMapping = null;
            ComponentCopier.dstBoneMapping = null;
            ComponentCopier.replacementRules = new List<ReplacementRule>();
            ComponentCopier.modificationLogs.Clear();
            ComponentCopier.modificationObjectLogs.Clear();
        }

        #region 複合置換ルールのテスト

        /// <summary>
        /// 正規表現ルール + HumanoidBoneルールの組み合わせ
        /// 両方のルールが適用されることを確認
        /// </summary>
        [Test]
        public void DryRun_RegexAndHumanoidRules_BothApplied()
        {
            // Arrange
            var settings = new CopySettings
            {
                pattern = "VRCPhysBone|StubComponent",
                isObjectCopy = true,
                showReportAfterPaste = false,
                replacementRules = new List<ReplacementRule>
                {
                    // apron_skirt → skirt への正規表現置換
                    new ReplacementRule("apron_(.+)", "$1"),
                    // HumanoidBoneルール（全ボーン）
                    new ReplacementRule(HumanoidBoneGroup.All)
                }
            };

            ComponentCopier.srcBoneMapping = AvatarTestUtils.GetSampleBoneMapping();
            ComponentCopier.dstBoneMapping = AvatarTestUtils.GetVRoidBoneMapping();

            // Assert: 正規表現ルールでapron_skirtがマッチする
            bool regexMatch = NameMatcher.NamesMatch(
                "apron_skirt", "skirt",
                settings.replacementRules, null, null);
            Assert.IsTrue(regexMatch, "Regex rule should match apron_skirt -> skirt");

            // Assert: HumanoidBoneルールで hips → J_Bip_C_Hips がマッチ
            bool boneMatch = NameMatcher.NamesMatch(
                "hips", "J_Bip_C_Hips",
                settings.replacementRules,
                ComponentCopier.srcBoneMapping,
                ComponentCopier.dstBoneMapping);
            Assert.IsTrue(boneMatch, "HumanoidBone rule should match hips -> J_Bip_C_Hips");
        }

        /// <summary>
        /// 複数の正規表現ルールを順番に適用
        /// 最初にマッチしたルールが使われる
        /// </summary>
        [Test]
        public void DryRun_MultipleRegexRules_FirstMatchWins()
        {
            // Arrange: 同じ名前に複数のルールがマッチする場合
            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isObjectCopy = false,
                showReportAfterPaste = false,
                replacementRules = new List<ReplacementRule>
                {
                    // 最初のルール: apron_ を skirt_ に置換
                    new ReplacementRule("apron_(.+)", "skirt_$1"),
                    // 2番目のルール: apron_skirt を garment に置換（最初のルールが優先）
                    new ReplacementRule("apron_skirt", "garment"),
                }
            };

            ComponentCopier.activeObject = destRoot;

            // Act
            ComponentCopier.Copy(sourceRoot, settings);

            // Assert: 最初のルールで変換される
            bool match = NameMatcher.NamesMatch(
                "apron_skirt", "skirt_skirt",
                settings.replacementRules, null, null);
            Assert.IsTrue(match, "First rule should match apron_skirt -> skirt_skirt");
        }

        /// <summary>
        /// HumanoidBoneルールで特定のグループのみを指定
        /// 左腕のみマッチし、脚はマッチしない
        /// </summary>
        [Test]
        public void DryRun_HumanoidLeftArmOnly_LegsDoNotMatch()
        {
            // Arrange
            var settings = new CopySettings
            {
                pattern = "StubComponent",
                showReportAfterPaste = false,
                replacementRules = new List<ReplacementRule>
                {
                    new ReplacementRule(HumanoidBoneGroup.LeftArm)
                }
            };

            ComponentCopier.srcBoneMapping = AvatarTestUtils.GetSampleBoneMapping();
            ComponentCopier.dstBoneMapping = AvatarTestUtils.GetVRoidBoneMapping();

            // Act & Assert: 左腕はマッチする
            bool armMatch = NameMatcher.NamesMatch(
                "leftUpperArm", "J_Bip_L_UpperArm",
                settings.replacementRules,
                ComponentCopier.srcBoneMapping,
                ComponentCopier.dstBoneMapping);
            Assert.IsTrue(armMatch, "Left arm should match with LeftArm group");

            // 脚はマッチしない
            bool legMatch = NameMatcher.NamesMatch(
                "leftUpperLeg", "J_Bip_L_UpperLeg",
                settings.replacementRules,
                ComponentCopier.srcBoneMapping,
                ComponentCopier.dstBoneMapping);
            Assert.IsFalse(legMatch, "Legs should NOT match with LeftArm-only group");
        }

        /// <summary>
        /// 無効化されたルールはスキップされる
        /// </summary>
        [Test]
        public void DryRun_DisabledRules_AreSkipped()
        {
            // Arrange
            var settings = new CopySettings
            {
                pattern = "StubComponent",
                showReportAfterPaste = false,
                replacementRules = new List<ReplacementRule>
                {
                    // このルールは無効
                    new ReplacementRule("hips", "siri") { enabled = false },
                    // このルールは有効
                    new ReplacementRule(HumanoidBoneGroup.All) { enabled = true }
                }
            };

            ComponentCopier.srcBoneMapping = AvatarTestUtils.GetSampleBoneMapping();
            ComponentCopier.dstBoneMapping = AvatarTestUtils.GetVRoidBoneMapping();

            // Act & Assert: 無効化された正規表現ルールは適用されない
            bool regexMatch = NameMatcher.NamesMatch(
                "hips", "siri",
                settings.replacementRules, null, null);
            Assert.IsFalse(regexMatch, "Disabled regex rule should not apply");

            // HumanoidBoneルールは有効なのでマッチする
            bool boneMatch = NameMatcher.NamesMatch(
                "hips", "J_Bip_C_Hips",
                settings.replacementRules,
                ComponentCopier.srcBoneMapping,
                ComponentCopier.dstBoneMapping);
            Assert.IsTrue(boneMatch, "Enabled HumanoidBone rule should apply");
        }

        #endregion

        #region オブジェクトコピーモードのテスト

        /// <summary>
        /// isObjectCopy=true で対象オブジェクトが存在しない場合
        /// CreateObjectログが生成される
        /// </summary>
        [Test]
        public void DryRun_ObjectCopyWithMissingTarget_CreatesObject()
        {
            // Arrange: destRootから apron_skirt を削除
            var apronInDest = destRoot.transform.Find("root/J_Bip_C_Hips/J_Bip_C_Spine/apron_skirt");
            if (apronInDest != null)
            {
                Object.DestroyImmediate(apronInDest.gameObject);
            }

            var settings = new CopySettings
            {
                pattern = "VRCPhysBone",
                isObjectCopy = true,
                showReportAfterPaste = false,
                replacementRules = new List<ReplacementRule>
                {
                    new ReplacementRule(HumanoidBoneGroup.All)
                }
            };

            ComponentCopier.activeObject = destRoot;
            ComponentCopier.srcBoneMapping = AvatarTestUtils.GetSampleBoneMapping();
            ComponentCopier.dstBoneMapping = AvatarTestUtils.GetVRoidBoneMapping();

            // Act
            ComponentCopier.Copy(sourceRoot, settings);
            ComponentCopier.DryRun(destRoot, settings);

            // Assert: apron_skirt 関連のCreateObjectが発生
            var createLogs = ComponentCopier.modificationObjectLogs
                .Where(x => x.operation == ModificationOperation.CreateObject)
                .ToList();
            Assert.IsTrue(createLogs.Count > 0, "Should have CreateObject logs for missing hierarchy");
        }

        /// <summary>
        /// isRemoveBeforeCopy=true で既存コンポーネントが削除される
        /// </summary>
        [Test]
        public void DryRun_RemoveBeforeCopy_GeneratesRemoveLogs()
        {
            // Arrange: destRootにコンポーネントを追加
            var destBody = destRoot.transform.Find("Body");
            if (destBody != null)
            {
                destBody.gameObject.AddComponent<BoxCollider>();
            }

            var settings = new CopySettings
            {
                pattern = "BoxCollider|StubComponent",
                isRemoveBeforeCopy = true,
                showReportAfterPaste = false,
            };

            ComponentCopier.activeObject = destRoot;

            // Act
            ComponentCopier.Copy(sourceRoot, settings);
            ComponentCopier.DryRun(destRoot, settings);

            // Assert: Removeログがある
            var removeLogs = ComponentCopier.modificationLogs
                .Where(x => x.operation == ModificationOperation.Remove)
                .ToList();
            Assert.IsTrue(removeLogs.Count > 0, "Should have Remove logs for existing components");
        }

        #endregion

        #region エッジケースのテスト

        /// <summary>
        /// 存在しないコンポーネントパターンでは何もコピーされない
        /// </summary>
        [Test]
        public void DryRun_NonMatchingPattern_NothingCopied()
        {
            // Arrange
            var settings = new CopySettings
            {
                pattern = "NonExistentComponentXYZ123",
                showReportAfterPaste = false,
            };

            ComponentCopier.activeObject = destRoot;

            // Act
            ComponentCopier.Copy(sourceRoot, settings);
            ComponentCopier.DryRun(destRoot, settings);

            // Assert: 変更ログなし（マッチするコンポーネントがないため）
            Assert.AreEqual(0, ComponentCopier.modificationLogs.Count);
            Assert.AreEqual(0, ComponentCopier.modificationObjectLogs.Count);
        }

        /// <summary>
        /// マッピングなしでHumanoidBoneルールを使用した場合
        /// ルールは適用されない
        /// </summary>
        [Test]
        public void DryRun_HumanoidRuleWithoutMapping_RuleNotApplied()
        {
            // Arrange
            var settings = new CopySettings
            {
                pattern = "StubComponent",
                showReportAfterPaste = false,
                replacementRules = new List<ReplacementRule>
                {
                    new ReplacementRule(HumanoidBoneGroup.All)
                }
            };

            // マッピングを設定しない
            ComponentCopier.srcBoneMapping = null;
            ComponentCopier.dstBoneMapping = null;

            // Act & Assert
            bool match = NameMatcher.NamesMatch(
                "hips", "J_Bip_C_Hips",
                settings.replacementRules, null, null);
            Assert.IsFalse(match, "HumanoidBone rule should not work without mapping");
        }

        #endregion
    }
}

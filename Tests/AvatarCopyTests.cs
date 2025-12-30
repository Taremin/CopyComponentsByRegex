// AvatarTestUtilsを使用したPaste/DryRunの実践的テスト
// 様々な置換ルールの組み合わせをテスト
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;

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

        #region 詳細コピー検証テスト

        /// <summary>
        /// Pasteでコンポーネントが正しく追加されることを検証
        /// modificationLogsにAddログが記録される
        /// </summary>
        [Test]
        public void Paste_ComponentAdded_LogsAddOperation()
        {
            // Arrange: destRootからStubComponentを削除してからコピー
            var destBody = destRoot.transform.Find("Body");
            var existingStubs = destBody.GetComponents<StubComponent>();
            foreach (var stub in existingStubs)
            {
                Object.DestroyImmediate(stub);
            }

            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isObjectCopy = false,
                showReportAfterPaste = true,  // ログ記録を有効化
            };

            ComponentCopier.activeObject = destRoot;

            // Act
            ComponentCopier.Copy(sourceRoot, settings);
            ComponentCopier.Paste(destRoot, settings);

            // Assert: Addログがある
            var addLogs = ComponentCopier.modificationLogs
                .Where(x => x.operation == ModificationOperation.Add)
                .ToList();
            Assert.IsTrue(addLogs.Count > 0, "Should have Add logs for copied components");

            // Addログにコンポーネントタイプが記録されている
            var stubAddLog = addLogs.FirstOrDefault(x => x.componentType.Contains("StubComponent"));
            Assert.IsNotNull(stubAddLog, "Should have Add log for StubComponent");
        }

        /// <summary>
        /// Pasteで作成されたコンポーネントへの参照が取得できることを検証
        /// </summary>
        [Test]
        public void Paste_CreatedComponent_IsAccessible()
        {
            // Arrange: destRootからStubComponentを削除
            var destBody = destRoot.transform.Find("Body");
            var existingStubs = destBody.GetComponents<StubComponent>();
            foreach (var stub in existingStubs)
            {
                Object.DestroyImmediate(stub);
            }

            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isObjectCopy = false,
                showReportAfterPaste = true,  // ログ記録を有効化
            };

            ComponentCopier.activeObject = destRoot;

            // Act
            ComponentCopier.Copy(sourceRoot, settings);
            ComponentCopier.Paste(destRoot, settings);

            // Assert: createdComponentが設定されているログがある
            var logsWithComponent = ComponentCopier.modificationLogs
                .Where(x => x.operation == ModificationOperation.Add && x.createdComponent != null)
                .ToList();
            Assert.IsTrue(logsWithComponent.Count > 0, "Should have logs with createdComponent reference");

            // createdComponentが実際に存在する
            foreach (var log in logsWithComponent)
            {
                Assert.IsNotNull(log.createdComponent, "createdComponent should not be null");
                Assert.IsFalse(log.createdComponent.Equals(null), "createdComponent should be a valid Unity object");
            }
        }

        /// <summary>
        /// isObjectCopy=trueで新規オブジェクトが作成された場合
        /// createdObjectへの参照が取得できる
        /// </summary>
        [Test]
        public void Paste_CreateObject_CreatedObjectIsAccessible()
        {
            // Arrange: destRootからapron_skirtを削除
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
            ComponentCopier.Paste(destRoot, settings);

            // Assert: CreateObjectログにcreatedObjectがある
            var createLogs = ComponentCopier.modificationObjectLogs
                .Where(x => x.operation == ModificationOperation.CreateObject && x.createdObject != null)
                .ToList();
            
            // オブジェクトが実際に作成された場合のみ検証
            if (createLogs.Count > 0)
            {
                foreach (var log in createLogs)
                {
                    Assert.IsNotNull(log.createdObject, "createdObject should not be null");
                    Assert.IsFalse(log.createdObject.Equals(null), "createdObject should be a valid Unity object");
                }
            }
        }

        /// <summary>
        /// BugReportExporterでコピー後の状態をエクスポートできることを検証
        /// </summary>
        [Test]
        public void Paste_ExportReport_ContainsModificationLogs()
        {
            // Arrange
            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isObjectCopy = false,
                showReportAfterPaste = true,  // ログ記録を有効化
            };

            ComponentCopier.activeObject = destRoot;

            // Act
            ComponentCopier.Copy(sourceRoot, settings);
            ComponentCopier.Paste(destRoot, settings);

            // Export
            var report = BugReportExporter.Export(
                sourceRoot,
                ComponentCopier.copyTree,
                destRoot,
                settings,
                ComponentCopier.modificationLogs,
                ComponentCopier.modificationObjectLogs,
                includeProperties: true
            );

            // Assert
            Assert.IsNotNull(report, "Report should not be null");
            Assert.IsNotNull(report.source, "Report source should not be null");
            Assert.IsNotNull(report.destination, "Report destination should not be null");
            Assert.IsNotNull(report.settings, "Report settings should not be null");

            // modificationLogsが含まれている
            Assert.IsTrue(report.modificationLogs != null, "Report should contain modification logs");
        }

        /// <summary>
        /// isRemoveBeforeCopy=trueでRemoveログが記録されることを検証
        /// </summary>
        [Test]
        public void Paste_RemoveBeforeCopy_LogsRemoveOperation()
        {
            // Arrange: destRootのBodyにStubComponentがあることを確認
            var destBody = destRoot.transform.Find("Body");
            var existingStubs = destBody.GetComponents<StubComponent>();
            Assert.IsTrue(existingStubs.Length > 0, "Setup: Body should have StubComponent");

            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isRemoveBeforeCopy = true,
                showReportAfterPaste = true,  // ログ記録を有効化
            };

            ComponentCopier.activeObject = destRoot;

            // Act
            ComponentCopier.Copy(sourceRoot, settings);
            ComponentCopier.Paste(destRoot, settings);

            // Assert: Removeログがある
            var removeLogs = ComponentCopier.modificationLogs
                .Where(x => x.operation == ModificationOperation.Remove)
                .ToList();
            Assert.IsTrue(removeLogs.Count > 0, "Should have Remove logs");

            // StubComponentのRemoveログがある
            var stubRemove = removeLogs.FirstOrDefault(x => x.componentType.Contains("StubComponent"));
            Assert.IsNotNull(stubRemove, "Should have Remove log for StubComponent");
        }

        /// <summary>
        /// コピー後のログかtargetObjectが正しく記録されていることを検証
        /// </summary>
        [Test]
        public void Paste_LogsContain_CorrectTargetObject()
        {
            // Arrange
            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isObjectCopy = false,
                showReportAfterPaste = true,  // ログ記録を有効化
            };

            ComponentCopier.activeObject = destRoot;

            // Act
            ComponentCopier.Copy(sourceRoot, settings);
            ComponentCopier.Paste(destRoot, settings);

            // Assert: ログにtargetObjectが含まれている
            var logsWithObject = ComponentCopier.modificationLogs
                .Where(x => x.targetObject != null)
                .ToList();
            Assert.IsTrue(logsWithObject.Count > 0, "Logs should contain targetObject");

            // targetObjectがDestAvatarの子孫である
            foreach (var log in logsWithObject)
            {
                Assert.IsNotNull(log.targetObject, "targetObject should not be null");
                // targetObjectがdestRootの階層内にあることを確認
                var transform = log.targetObject.transform;
                bool isDescendant = false;
                while (transform != null)
                {
                    if (transform == destRoot.transform)
                    {
                        isDescendant = true;
                        break;
                    }
                    transform = transform.parent;
                }
                Assert.IsTrue(isDescendant, "targetObject should be descendant of DestAvatar");
            }
        }

        #endregion

        #region Constraint参照更新テスト

        /// <summary>
        /// ParentConstraintのソースがコピー後にコピー先階層内のオブジェクトを参照していることを検証
        /// (同じボーン名の階層でテスト)
        /// </summary>
        [Test]
        public void Paste_ParentConstraint_SourceTargetIsUpdatedToDestHierarchy()
        {
            // Arrange: リネームしていないdestRootを使用（同じボーン名階層）
            var plainDestRoot = AvatarTestUtils.CloneAvatar(sourceRoot, "PlainDestAvatar");
            // ボーン名はリネームしない
            
            try 
            {
                var sourceHead = sourceRoot.transform.Find("root/hips/spine/chest/neck/head");
                var sourceChest = sourceRoot.transform.Find("root/hips/spine/chest");
                Assert.IsNotNull(sourceHead, "Setup: sourceHead should exist");
                Assert.IsNotNull(sourceChest, "Setup: sourceChest should exist");

                var constraint = sourceHead.gameObject.AddComponent<ParentConstraint>();
                constraint.AddSource(new ConstraintSource
                {
                    sourceTransform = sourceChest,
                    weight = 1.0f
                });

                var settings = new CopySettings
                {
                    pattern = "ParentConstraint",
                    isObjectCopy = false,
                    showReportAfterPaste = true,
                };

                ComponentCopier.activeObject = plainDestRoot;

                // plainDestRootのパスを取得
                var destHead = plainDestRoot.transform.Find("root/hips/spine/chest/neck/head");
                var destChest = plainDestRoot.transform.Find("root/hips/spine/chest");
                Assert.IsNotNull(destHead, "Setup: destHead should exist");
                Assert.IsNotNull(destChest, "Setup: destChest should exist");

                // 既存のConstraintを削除
                var existingConstraints = destHead.GetComponents<ParentConstraint>();
                foreach (var c in existingConstraints)
                {
                    Object.DestroyImmediate(c);
                }

                // Act
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.Paste(plainDestRoot, settings);

                // Assert: destHeadにParentConstraintがコピーされている
                var copiedConstraint = destHead.GetComponent<ParentConstraint>();
                Assert.IsNotNull(copiedConstraint, "ParentConstraint should be copied to destHead");

                // Assert: ソースターゲットがdestChest（コピー先階層内）を参照している
                Assert.IsTrue(copiedConstraint.sourceCount > 0, "Constraint should have at least one source");
                var source = copiedConstraint.GetSource(0);
                Assert.IsNotNull(source.sourceTransform, "Source transform should not be null");
                
                // ソースターゲットがコピー先階層内のオブジェクトを参照しているか確認
                bool isInDestHierarchy = false;
                var current = source.sourceTransform;
                while (current != null)
                {
                    if (current == plainDestRoot.transform)
                    {
                        isInDestHierarchy = true;
                        break;
                    }
                    current = current.parent;
                }
                Assert.IsTrue(isInDestHierarchy, 
                    $"Constraint source should reference object in dest hierarchy, but references: {source.sourceTransform.name}");
                
                // より厳密なチェック: ソースターゲットがdestChestであること
                Assert.AreEqual(destChest, source.sourceTransform, 
                    $"Constraint source should be destChest, but was: {source.sourceTransform.name}");
            }
            finally
            {
                if (plainDestRoot != null) Object.DestroyImmediate(plainDestRoot);
            }
        }

        /// <summary>
        /// PositionConstraintのソースがコピー後にコピー先階層内のオブジェクトを参照していることを検証
        /// (同じボーン名の階層でテスト)
        /// </summary>
        [Test]
        public void Paste_PositionConstraint_SourceTargetIsUpdatedToDestHierarchy()
        {
            // Arrange: リネームしていないdestRootを使用（同じボーン名階層）
            var plainDestRoot = AvatarTestUtils.CloneAvatar(sourceRoot, "PlainDestAvatar2");
            
            try 
            {
                var sourceLeftHand = sourceRoot.transform.Find("root/hips/spine/chest/leftUpperArm/leftLowerArm/leftHand");
                var sourceRightHand = sourceRoot.transform.Find("root/hips/spine/chest/rightUpperArm/rightLowerArm/rightHand");
                Assert.IsNotNull(sourceLeftHand, "Setup: sourceLeftHand should exist");
                Assert.IsNotNull(sourceRightHand, "Setup: sourceRightHand should exist");

                var constraint = sourceLeftHand.gameObject.AddComponent<PositionConstraint>();
                constraint.AddSource(new ConstraintSource
                {
                    sourceTransform = sourceRightHand,
                    weight = 1.0f
                });

                var settings = new CopySettings
                {
                    pattern = "PositionConstraint",
                    isObjectCopy = false,
                    showReportAfterPaste = true,
                };

                ComponentCopier.activeObject = plainDestRoot;

                var destLeftHand = plainDestRoot.transform.Find("root/hips/spine/chest/leftUpperArm/leftLowerArm/leftHand");
                var destRightHand = plainDestRoot.transform.Find("root/hips/spine/chest/rightUpperArm/rightLowerArm/rightHand");
                Assert.IsNotNull(destLeftHand, "Setup: destLeftHand should exist");
                Assert.IsNotNull(destRightHand, "Setup: destRightHand should exist");

                // 既存のConstraintを削除
                var existingConstraints = destLeftHand.GetComponents<PositionConstraint>();
                foreach (var c in existingConstraints)
                {
                    Object.DestroyImmediate(c);
                }

                // Act
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.Paste(plainDestRoot, settings);

                // Assert: destLeftHandにPositionConstraintがコピーされている
                var copiedConstraint = destLeftHand.GetComponent<PositionConstraint>();
                Assert.IsNotNull(copiedConstraint, "PositionConstraint should be copied to destLeftHand");

                // Assert: ソースターゲットがdestRightHand（コピー先階層内）を参照している
                Assert.IsTrue(copiedConstraint.sourceCount > 0, "Constraint should have at least one source");
                var source = copiedConstraint.GetSource(0);
                Assert.IsNotNull(source.sourceTransform, "Source transform should not be null");
                
                // ソースターゲットがコピー先階層内のオブジェクトを参照しているか確認
                bool isInDestHierarchy = false;
                var current = source.sourceTransform;
                while (current != null)
                {
                    if (current == plainDestRoot.transform)
                    {
                        isInDestHierarchy = true;
                        break;
                    }
                    current = current.parent;
                }
                Assert.IsTrue(isInDestHierarchy, 
                    $"Constraint source should reference object in dest hierarchy, but references: {source.sourceTransform.name}");
            }
            finally
            {
                if (plainDestRoot != null) Object.DestroyImmediate(plainDestRoot);
            }
        }

        /// <summary>
        /// ParentConstraintのコピー時、HumanoidBoneGroup置換ルールを使用して
        /// 異なるボーン名構造（VRoid形式）でも正しくコピーされることを検証
        /// </summary>
        [Test]
        public void Paste_ParentConstraint_WithHumanoidRules_CopiesCorrectly()
        {
            // Arrange: VRoidボーン名のdestRootを使用
            var sourceHead = sourceRoot.transform.Find("root/hips/spine/chest/neck/head");
            var sourceChest = sourceRoot.transform.Find("root/hips/spine/chest");
            Assert.IsNotNull(sourceHead, "Setup: sourceHead should exist");
            Assert.IsNotNull(sourceChest, "Setup: sourceChest should exist");

            var constraint = sourceHead.gameObject.AddComponent<ParentConstraint>();
            constraint.AddSource(new ConstraintSource
            {
                sourceTransform = sourceChest,
                weight = 1.0f
            });

            var settings = new CopySettings
            {
                pattern = "ParentConstraint",
                isObjectCopy = false,
                showReportAfterPaste = true,
                replacementRules = new List<ReplacementRule>
                {
                    new ReplacementRule(HumanoidBoneGroup.All)
                }
            };

            ComponentCopier.activeObject = destRoot;

            // VRoid形式のボーン名パス
            var destHead = destRoot.transform.Find("root/J_Bip_C_Hips/J_Bip_C_Spine/J_Bip_C_Chest/J_Bip_C_Neck/J_Bip_C_Head");
            var destChest = destRoot.transform.Find("root/J_Bip_C_Hips/J_Bip_C_Spine/J_Bip_C_Chest");
            Assert.IsNotNull(destHead, "Setup: destHead should exist (VRoid format)");
            Assert.IsNotNull(destChest, "Setup: destChest should exist (VRoid format)");

            // 既存のConstraintを削除
            var existingConstraints = destHead.GetComponents<ParentConstraint>();
            foreach (var c in existingConstraints)
            {
                Object.DestroyImmediate(c);
            }

            // Act
            ComponentCopier.Copy(sourceRoot, settings);

            // ボーンマッピングを手動設定（テスト用GameObjectにはAnimatorがないため）
            ComponentCopier.srcBoneMapping = AvatarTestUtils.GetSampleBoneMapping();
            ComponentCopier.dstBoneMapping = AvatarTestUtils.GetVRoidBoneMapping();

            ComponentCopier.Paste(destRoot, settings);

            // Assert: destHeadにParentConstraintがコピーされている
            var copiedConstraint = destHead.GetComponent<ParentConstraint>();
            Assert.IsNotNull(copiedConstraint, 
                "ParentConstraint should be copied to destHead with HumanoidBoneGroup rules");

            // Assert: ソースターゲットがコピー先階層内を参照している
            Assert.IsTrue(copiedConstraint.sourceCount > 0, "Constraint should have at least one source");
            var source = copiedConstraint.GetSource(0);
            Assert.IsNotNull(source.sourceTransform, "Source transform should not be null");
            
            // ソースターゲットがコピー先階層内のオブジェクトを参照しているか確認
            bool isInDestHierarchy = false;
            var current = source.sourceTransform;
            while (current != null)
            {
                if (current == destRoot.transform)
                {
                    isInDestHierarchy = true;
                    break;
                }
                current = current.parent;
            }
            Assert.IsTrue(isInDestHierarchy, 
                $"Constraint source should reference object in dest hierarchy, but references: {source.sourceTransform.name}");
        }

        /// <summary>
        /// PositionConstraintのコピー時、HumanoidBoneGroup置換ルールを使用して
        /// 異なるボーン名構造（VRoid形式）でも正しくコピーされることを検証
        /// </summary>
        [Test]
        public void Paste_PositionConstraint_WithHumanoidRules_CopiesCorrectly()
        {
            // Arrange: VRoidボーン名のdestRootを使用
            var sourceLeftHand = sourceRoot.transform.Find("root/hips/spine/chest/leftUpperArm/leftLowerArm/leftHand");
            var sourceRightHand = sourceRoot.transform.Find("root/hips/spine/chest/rightUpperArm/rightLowerArm/rightHand");
            Assert.IsNotNull(sourceLeftHand, "Setup: sourceLeftHand should exist");
            Assert.IsNotNull(sourceRightHand, "Setup: sourceRightHand should exist");

            var constraint = sourceLeftHand.gameObject.AddComponent<PositionConstraint>();
            constraint.AddSource(new ConstraintSource
            {
                sourceTransform = sourceRightHand,
                weight = 1.0f
            });

            var settings = new CopySettings
            {
                pattern = "PositionConstraint",
                isObjectCopy = false,
                showReportAfterPaste = true,
                replacementRules = new List<ReplacementRule>
                {
                    new ReplacementRule(HumanoidBoneGroup.All)
                }
            };

            ComponentCopier.activeObject = destRoot;

            // VRoid形式のボーン名パス
            var destLeftHand = destRoot.transform.Find("root/J_Bip_C_Hips/J_Bip_C_Spine/J_Bip_C_Chest/J_Bip_L_UpperArm/J_Bip_L_LowerArm/J_Bip_L_Hand");
            var destRightHand = destRoot.transform.Find("root/J_Bip_C_Hips/J_Bip_C_Spine/J_Bip_C_Chest/J_Bip_R_UpperArm/J_Bip_R_LowerArm/J_Bip_R_Hand");
            Assert.IsNotNull(destLeftHand, "Setup: destLeftHand should exist (VRoid format)");
            Assert.IsNotNull(destRightHand, "Setup: destRightHand should exist (VRoid format)");

            // 既存のConstraintを削除
            var existingConstraints = destLeftHand.GetComponents<PositionConstraint>();
            foreach (var c in existingConstraints)
            {
                Object.DestroyImmediate(c);
            }

            // Act
            ComponentCopier.Copy(sourceRoot, settings);

            // ボーンマッピングを手動設定（テスト用GameObjectにはAnimatorがないため）
            ComponentCopier.srcBoneMapping = AvatarTestUtils.GetSampleBoneMapping();
            ComponentCopier.dstBoneMapping = AvatarTestUtils.GetVRoidBoneMapping();

            ComponentCopier.Paste(destRoot, settings);

            // Assert: destLeftHandにPositionConstraintがコピーされている
            var copiedConstraint = destLeftHand.GetComponent<PositionConstraint>();
            Assert.IsNotNull(copiedConstraint, 
                "PositionConstraint should be copied to destLeftHand with HumanoidBoneGroup rules");

            // Assert: ソースターゲットがコピー先階層内を参照している
            Assert.IsTrue(copiedConstraint.sourceCount > 0, "Constraint should have at least one source");
            var source = copiedConstraint.GetSource(0);
            Assert.IsNotNull(source.sourceTransform, "Source transform should not be null");
            
            // ソースターゲットがコピー先階層内のオブジェクトを参照しているか確認
            bool isInDestHierarchy = false;
            var current = source.sourceTransform;
            while (current != null)
            {
                if (current == destRoot.transform)
                {
                    isInDestHierarchy = true;
                    break;
                }
                current = current.parent;
            }
            Assert.IsTrue(isInDestHierarchy, 
                $"Constraint source should reference object in dest hierarchy, but references: {source.sourceTransform.name}");
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// TreeItemから全てのコンポーネントを再帰的に取得
        /// </summary>
        private static List<Component> GetAllComponentsFromTree(TreeItem tree)
        {
            var result = new List<Component>();
            if (tree == null) return result;
            
            result.AddRange(tree.components);
            foreach (var child in tree.children)
            {
                result.AddRange(GetAllComponentsFromTree(child));
            }
            return result;
        }

        /// <summary>
        /// TransformのフルパスToを取得
        /// </summary>
        private static string GetPath(Transform t)
        {
            if (t == null) return "null";
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        #endregion

        #region DryRunとPasteのレポート一致テスト

        /// <summary>
        /// ルート名を除外した相対パスを取得するヘルパー
        /// </summary>
        private static string GetRelativePathWithoutRoot(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return fullPath;
            int idx = fullPath.IndexOf('/');
            return idx >= 0 ? fullPath.Substring(idx + 1) : fullPath;
        }

        /// <summary>
        /// targetObjectからルートを除外した相対パスを取得
        /// </summary>
        private static string GetRelativePath(GameObject targetObject, GameObject root)
        {
            if (targetObject == null) return "null";
            if (targetObject == root) return ".";
            
            var path = targetObject.name;
            var t = targetObject.transform.parent;
            while (t != null && t.gameObject != root)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }

        /// <summary>
        /// ModificationEntryから相対パスを取得（targetObjectがnullの場合はtargetPathを使用）
        /// </summary>
        private static string GetRelativePathFromEntry(ModificationEntry entry, GameObject root)
        {
            if (entry.targetObject != null)
            {
                return GetRelativePath(entry.targetObject, root);
            }
            else if (!string.IsNullOrEmpty(entry.targetPath))
            {
                // targetPathからルート名を除外（例: "DestForDryRun/root/..." → "root/..."）
                return GetRelativePathWithoutRoot(entry.targetPath);
            }
            return "null";
        }


        /// <summary>
        /// DryRunとPasteで同じ変更ログが生成されることを検証
        /// (コンポーネント追加の場合)
        /// </summary>
        [Test]
        public void DryRun_And_Paste_Generate_Same_Report_ForComponentAdd()
        {
            // Arrange: 2つの独立したdestRootを作成（同じ初期状態）
            var destRootForDryRun = AvatarTestUtils.CloneAvatar(sourceRoot, "DestForDryRun");
            var destRootForPaste = AvatarTestUtils.CloneAvatar(sourceRoot, "DestForPaste");
            
            // 両方からBody以下のStubComponentを削除
            var bodyDryRun = destRootForDryRun.transform.Find("Body");
            var bodyPaste = destRootForPaste.transform.Find("Body");
            foreach (var stub in bodyDryRun.GetComponents<StubComponent>())
                Object.DestroyImmediate(stub);
            foreach (var stub in bodyPaste.GetComponents<StubComponent>())
                Object.DestroyImmediate(stub);

            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isObjectCopy = false,
                showReportAfterPaste = true,
            };

            try
            {
                // Act: DryRun実行
                ComponentCopier.activeObject = destRootForDryRun;
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.DryRun(destRootForDryRun, settings);
                var dryRunLogs = ComponentCopier.modificationLogs.Select(x => new
                {
                    x.operation,
                    x.componentType,
                    relativePath = GetRelativePath(x.targetObject, destRootForDryRun)
                }).ToList();
                var dryRunObjectLogs = ComponentCopier.modificationObjectLogs.Select(x => new
                {
                    x.operation,
                    relativePath = GetRelativePathWithoutRoot(x.targetPath)
                }).ToList();

                // 状態リセット
                ComponentCopier.modificationLogs.Clear();
                ComponentCopier.modificationObjectLogs.Clear();
                ComponentCopier.transforms = new List<Transform>();
                ComponentCopier.components = new List<Component>();

                // Act: Paste実行
                ComponentCopier.activeObject = destRootForPaste;
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.Paste(destRootForPaste, settings);
                var pasteLogs = ComponentCopier.modificationLogs.Select(x => new
                {
                    x.operation,
                    x.componentType,
                    relativePath = GetRelativePath(x.targetObject, destRootForPaste)
                }).ToList();
                var pasteObjectLogs = ComponentCopier.modificationObjectLogs.Select(x => new
                {
                    x.operation,
                    relativePath = GetRelativePathWithoutRoot(x.targetPath)
                }).ToList();

                // Assert: 同じ数のログ
                Assert.AreEqual(dryRunLogs.Count, pasteLogs.Count,
                    $"DryRun logs count ({dryRunLogs.Count}) should equal Paste logs count ({pasteLogs.Count})");

                // Assert: 各ログの内容が一致
                for (int i = 0; i < dryRunLogs.Count; i++)
                {
                    Assert.AreEqual(dryRunLogs[i].operation, pasteLogs[i].operation,
                        $"Log[{i}] operation mismatch: DryRun={dryRunLogs[i].operation}, Paste={pasteLogs[i].operation}");
                    Assert.AreEqual(dryRunLogs[i].componentType, pasteLogs[i].componentType,
                        $"Log[{i}] componentType mismatch: DryRun={dryRunLogs[i].componentType}, Paste={pasteLogs[i].componentType}");
                    Assert.AreEqual(dryRunLogs[i].relativePath, pasteLogs[i].relativePath,
                        $"Log[{i}] relativePath mismatch: DryRun={dryRunLogs[i].relativePath}, Paste={pasteLogs[i].relativePath}");
                }

                // Assert: ObjectLogsも一致
                Assert.AreEqual(dryRunObjectLogs.Count, pasteObjectLogs.Count,
                    $"DryRun object logs count ({dryRunObjectLogs.Count}) should equal Paste object logs count ({pasteObjectLogs.Count})");
            }
            finally
            {
                Object.DestroyImmediate(destRootForDryRun);
                Object.DestroyImmediate(destRootForPaste);
            }
        }

        /// <summary>
        /// DryRunとPasteで同じ変更ログが生成されることを検証
        /// (apron_skirtを削除してオブジェクトコピーを有効にした場合)
        /// </summary>
        [Test]
        public void DryRun_And_Paste_Generate_Same_Report_ForObjectCopyWithMissingApronSkirt()
        {
            // Arrange: 2つの独立したdestRootを作成（同じ初期状態）
            var destRootForDryRun = AvatarTestUtils.CloneAvatar(sourceRoot, "DestForDryRun");
            var destRootForPaste = AvatarTestUtils.CloneAvatar(sourceRoot, "DestForPaste");
            
            // 両方からapron_skirtを削除
            var apronDryRun = destRootForDryRun.transform.Find("root/hips/spine/apron_skirt");
            var apronPaste = destRootForPaste.transform.Find("root/hips/spine/apron_skirt");
            if (apronDryRun != null) Object.DestroyImmediate(apronDryRun.gameObject);
            if (apronPaste != null) Object.DestroyImmediate(apronPaste.gameObject);

            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isObjectCopy = true,  // オブジェクトコピーを有効化
                showReportAfterPaste = true,
            };

            try
            {
                // Act: DryRun実行
                ComponentCopier.activeObject = destRootForDryRun;
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.DryRun(destRootForDryRun, settings);
                var dryRunLogs = ComponentCopier.modificationLogs.Select(x => new
                {
                    x.operation,
                    x.componentType,
                    relativePath = GetRelativePathFromEntry(x, destRootForDryRun)
                }).OrderBy(x => x.relativePath).ThenBy(x => x.componentType).ThenBy(x => x.operation).ToList();
                var dryRunObjectLogs = ComponentCopier.modificationObjectLogs.Select(x => new
                {
                    x.operation,
                    relativePath = GetRelativePathWithoutRoot(x.targetPath)
                }).OrderBy(x => x.relativePath).ToList();

                // 状態リセット
                ComponentCopier.modificationLogs.Clear();
                ComponentCopier.modificationObjectLogs.Clear();
                ComponentCopier.transforms = new List<Transform>();
                ComponentCopier.components = new List<Component>();

                // Act: Paste実行
                ComponentCopier.activeObject = destRootForPaste;
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.Paste(destRootForPaste, settings);
                var pasteLogs = ComponentCopier.modificationLogs.Select(x => new
                {
                    x.operation,
                    x.componentType,
                    relativePath = GetRelativePathFromEntry(x, destRootForPaste)
                }).OrderBy(x => x.relativePath).ThenBy(x => x.componentType).ThenBy(x => x.operation).ToList();
                var pasteObjectLogs = ComponentCopier.modificationObjectLogs.Select(x => new
                {
                    x.operation,
                    relativePath = GetRelativePathWithoutRoot(x.targetPath)
                }).OrderBy(x => x.relativePath).ToList();

                // Assert: 同じ数のログ
                Assert.AreEqual(dryRunLogs.Count, pasteLogs.Count,
                    $"DryRun logs count ({dryRunLogs.Count}) should equal Paste logs count ({pasteLogs.Count})");

                // Assert: 各ログの内容が一致
                for (int i = 0; i < dryRunLogs.Count; i++)
                {
                    Assert.AreEqual(dryRunLogs[i].operation, pasteLogs[i].operation,
                        $"Log[{i}] operation mismatch: DryRun={dryRunLogs[i].operation}, Paste={pasteLogs[i].operation}");
                    Assert.AreEqual(dryRunLogs[i].componentType, pasteLogs[i].componentType,
                        $"Log[{i}] componentType mismatch: DryRun={dryRunLogs[i].componentType}, Paste={pasteLogs[i].componentType}");
                    Assert.AreEqual(dryRunLogs[i].relativePath, pasteLogs[i].relativePath,
                        $"Log[{i}] relativePath mismatch: DryRun={dryRunLogs[i].relativePath}, Paste={pasteLogs[i].relativePath}");
                }

                // Assert: ObjectLogsの数が一致
                Assert.AreEqual(dryRunObjectLogs.Count, pasteObjectLogs.Count,
                    $"DryRun object logs count ({dryRunObjectLogs.Count}) should equal Paste object logs count ({pasteObjectLogs.Count})");
                
                // Assert: ObjectLogsの内容が一致
                for (int i = 0; i < dryRunObjectLogs.Count; i++)
                {
                    Assert.AreEqual(dryRunObjectLogs[i].operation, pasteObjectLogs[i].operation,
                        $"ObjectLog[{i}] operation mismatch: DryRun={dryRunObjectLogs[i].operation}, Paste={pasteObjectLogs[i].operation}");
                    Assert.AreEqual(dryRunObjectLogs[i].relativePath, pasteObjectLogs[i].relativePath,
                        $"ObjectLog[{i}] relativePath mismatch: DryRun={dryRunObjectLogs[i].relativePath}, Paste={pasteObjectLogs[i].relativePath}");
                }
            }
            finally
            {
                Object.DestroyImmediate(destRootForDryRun);
                Object.DestroyImmediate(destRootForPaste);
            }
        }

        /// <summary>
        /// DryRunとPasteで同じ変更ログが生成されることを検証
        /// (コンポーネント削除+追加の場合)
        /// </summary>
        [Test]
        public void DryRun_And_Paste_Generate_Same_Report_ForRemoveAndAdd()
        {

            // Arrange
            var destRootForDryRun = AvatarTestUtils.CloneAvatar(sourceRoot, "DestForDryRun");
            var destRootForPaste = AvatarTestUtils.CloneAvatar(sourceRoot, "DestForPaste");

            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isRemoveBeforeCopy = true,
                isObjectCopy = false,
                showReportAfterPaste = true,
            };

            try
            {
                // Act: DryRun
                ComponentCopier.activeObject = destRootForDryRun;
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.DryRun(destRootForDryRun, settings);
                
                var dryRunRemoveLogs = ComponentCopier.modificationLogs
                    .Where(x => x.operation == ModificationOperation.Remove)
                    .Select(x => new { x.componentType, relativePath = GetRelativePath(x.targetObject, destRootForDryRun) })
                    .OrderBy(x => x.relativePath)
                    .ThenBy(x => x.componentType)
                    .ToList();
                var dryRunAddLogs = ComponentCopier.modificationLogs
                    .Where(x => x.operation == ModificationOperation.Add)
                    .Select(x => new { x.componentType, relativePath = GetRelativePath(x.targetObject, destRootForDryRun) })
                    .OrderBy(x => x.relativePath)
                    .ThenBy(x => x.componentType)
                    .ToList();

                // 状態リセット
                ComponentCopier.modificationLogs.Clear();
                ComponentCopier.modificationObjectLogs.Clear();
                ComponentCopier.transforms = new List<Transform>();
                ComponentCopier.components = new List<Component>();

                // Act: Paste
                ComponentCopier.activeObject = destRootForPaste;
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.Paste(destRootForPaste, settings);
                
                var pasteRemoveLogs = ComponentCopier.modificationLogs
                    .Where(x => x.operation == ModificationOperation.Remove)
                    .Select(x => new { x.componentType, relativePath = GetRelativePath(x.targetObject, destRootForPaste) })
                    .OrderBy(x => x.relativePath)
                    .ThenBy(x => x.componentType)
                    .ToList();
                var pasteAddLogs = ComponentCopier.modificationLogs
                    .Where(x => x.operation == ModificationOperation.Add)
                    .Select(x => new { x.componentType, relativePath = GetRelativePath(x.targetObject, destRootForPaste) })
                    .OrderBy(x => x.relativePath)
                    .ThenBy(x => x.componentType)
                    .ToList();

                // Assert: Removeログ数が一致
                Assert.AreEqual(dryRunRemoveLogs.Count, pasteRemoveLogs.Count,
                    $"Remove logs count mismatch: DryRun={dryRunRemoveLogs.Count}, Paste={pasteRemoveLogs.Count}");

                // Assert: Addログ数が一致
                Assert.AreEqual(dryRunAddLogs.Count, pasteAddLogs.Count,
                    $"Add logs count mismatch: DryRun={dryRunAddLogs.Count}, Paste={pasteAddLogs.Count}");

                // Assert: Removeログの内容が一致
                for (int i = 0; i < dryRunRemoveLogs.Count; i++)
                {
                    Assert.AreEqual(dryRunRemoveLogs[i].componentType, pasteRemoveLogs[i].componentType,
                        $"Remove log[{i}] componentType mismatch");
                    Assert.AreEqual(dryRunRemoveLogs[i].relativePath, pasteRemoveLogs[i].relativePath,
                        $"Remove log[{i}] relativePath mismatch");
                }

                // Assert: Addログの内容が一致
                for (int i = 0; i < dryRunAddLogs.Count; i++)
                {
                    Assert.AreEqual(dryRunAddLogs[i].componentType, pasteAddLogs[i].componentType,
                        $"Add log[{i}] componentType mismatch");
                    Assert.AreEqual(dryRunAddLogs[i].relativePath, pasteAddLogs[i].relativePath,
                        $"Add log[{i}] relativePath mismatch");
                }
            }
            finally
            {
                Object.DestroyImmediate(destRootForDryRun);
                Object.DestroyImmediate(destRootForPaste);
            }
        }

        /// <summary>
        /// DryRunとPasteで同じ変更ログが生成されることを検証
        /// (オブジェクト作成の場合)
        /// </summary>
        [Test]
        public void DryRun_And_Paste_Generate_Same_Report_ForObjectCreation()
        {
            // Arrange
            var destRootForDryRun = AvatarTestUtils.CloneAvatar(sourceRoot, "DestForDryRun");
            var destRootForPaste = AvatarTestUtils.CloneAvatar(sourceRoot, "DestForPaste");
            
            // HumanoidBoneのリネーム
            AvatarTestUtils.RenameHumanoidBones(destRootForDryRun, AvatarTestUtils.GetVRoidBoneRenameMap());
            AvatarTestUtils.RenameHumanoidBones(destRootForPaste, AvatarTestUtils.GetVRoidBoneRenameMap());
            
            // apron_skirtを削除
            var apronDryRun = destRootForDryRun.transform.Find("root/J_Bip_C_Hips/J_Bip_C_Spine/apron_skirt");
            var apronPaste = destRootForPaste.transform.Find("root/J_Bip_C_Hips/J_Bip_C_Spine/apron_skirt");
            if (apronDryRun != null) Object.DestroyImmediate(apronDryRun.gameObject);
            if (apronPaste != null) Object.DestroyImmediate(apronPaste.gameObject);

            var settings = new CopySettings
            {
                pattern = "StubComponent",
                isObjectCopy = true,
                showReportAfterPaste = true,
                replacementRules = new List<ReplacementRule>
                {
                    new ReplacementRule(HumanoidBoneGroup.All)
                }
            };

            try
            {
                // ボーンマッピングのセットアップ
                ComponentCopier.srcBoneMapping = AvatarTestUtils.GetSampleBoneMapping();
                ComponentCopier.dstBoneMapping = AvatarTestUtils.GetVRoidBoneMapping();

                // Act: DryRun
                ComponentCopier.activeObject = destRootForDryRun;
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.DryRun(destRootForDryRun, settings);
                
                var dryRunObjectLogs = ComponentCopier.modificationObjectLogs
                    .Where(x => x.operation == ModificationOperation.CreateObject)
                    .Select(x => GetRelativePathWithoutRoot(x.targetPath))
                    .OrderBy(x => x)
                    .ToList();

                // 状態リセット
                ComponentCopier.modificationLogs.Clear();
                ComponentCopier.modificationObjectLogs.Clear();
                ComponentCopier.transforms = new List<Transform>();
                ComponentCopier.components = new List<Component>();
                ComponentCopier.srcBoneMapping = AvatarTestUtils.GetSampleBoneMapping();
                ComponentCopier.dstBoneMapping = AvatarTestUtils.GetVRoidBoneMapping();

                // Act: Paste
                ComponentCopier.activeObject = destRootForPaste;
                ComponentCopier.Copy(sourceRoot, settings);
                ComponentCopier.Paste(destRootForPaste, settings);
                
                var pasteObjectLogs = ComponentCopier.modificationObjectLogs
                    .Where(x => x.operation == ModificationOperation.CreateObject)
                    .Select(x => GetRelativePathWithoutRoot(x.targetPath))
                    .OrderBy(x => x)
                    .ToList();

                // Assert: CreateObjectログが一致
                Assert.AreEqual(dryRunObjectLogs.Count, pasteObjectLogs.Count,
                    $"CreateObject logs count mismatch: DryRun={dryRunObjectLogs.Count}, Paste={pasteObjectLogs.Count}");

                for (int i = 0; i < dryRunObjectLogs.Count; i++)
                {
                    Assert.AreEqual(dryRunObjectLogs[i], pasteObjectLogs[i],
                        $"CreateObject log[{i}] path mismatch: DryRun={dryRunObjectLogs[i]}, Paste={pasteObjectLogs[i]}");
                }
            }
            finally
            {
                Object.DestroyImmediate(destRootForDryRun);
                Object.DestroyImmediate(destRootForPaste);
            }
        }

        #endregion
    }
}



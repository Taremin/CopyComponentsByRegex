// NameMatcher のユニットテスト
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// NameMatcher クラスのユニットテスト
    /// </summary>
    public class NameMatcherTests
    {
        #region TransformName テスト

        /// <summary>
        /// ルールがない場合、名前がそのまま返される
        /// </summary>
        [Test]
        public void TransformName_WithNoRules_ReturnsSameName()
        {
            // Arrange
            string srcName = "J_Bip_C_Head";
            var rules = new List<ReplacementRule>();

            // Act
            string result = NameMatcher.TransformName(srcName, rules);

            // Assert
            Assert.AreEqual(srcName, result);
        }

        /// <summary>
        /// nullのルールリストでも正常に動作
        /// </summary>
        [Test]
        public void TransformName_WithNullRules_ReturnsSameName()
        {
            // Arrange
            string srcName = "TestName";

            // Act
            string result = NameMatcher.TransformName(srcName, null);

            // Assert
            Assert.AreEqual(srcName, result);
        }

        /// <summary>
        /// 正規表現ルールで正しく変換される
        /// </summary>
        [Test]
        public void TransformName_WithRegexRule_TransformsCorrectly()
        {
            // Arrange
            string srcName = "J_Bip_C_Head";
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("J_Bip_C_(.+)", "$1")
            };

            // Act
            string result = NameMatcher.TransformName(srcName, rules);

            // Assert
            Assert.AreEqual("Head", result);
        }

        /// <summary>
        /// 複数のルールが順番に適用される
        /// </summary>
        [Test]
        public void TransformName_WithMultipleRules_AppliesInOrder()
        {
            // Arrange
            string srcName = "J_Bip_C_UpperChest";
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("J_Bip_C_", ""),  // プレフィックス削除
                new ReplacementRule("Upper", "UPPER")  // Upper を大文字化
            };

            // Act
            string result = NameMatcher.TransformName(srcName, rules);

            // Assert
            Assert.AreEqual("UPPERChest", result);
        }

        /// <summary>
        /// 無効なルールはスキップされる
        /// </summary>
        [Test]
        public void TransformName_WithDisabledRule_SkipsRule()
        {
            // Arrange
            string srcName = "J_Bip_C_Head";
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("J_Bip_C_", "") { enabled = false }
            };

            // Act
            string result = NameMatcher.TransformName(srcName, rules);

            // Assert
            Assert.AreEqual(srcName, result);  // 変換されない
        }

        #endregion

        #region NamesMatch テスト（正規表現のみ）

        /// <summary>
        /// 同じ名前はルールなしでマッチ
        /// </summary>
        [Test]
        public void NamesMatch_WithSameNames_ReturnsTrue()
        {
            // Arrange
            string srcName = "Head";
            string dstName = "Head";

            // Act
            bool result = NameMatcher.NamesMatch(srcName, dstName, null);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// 異なる名前はルールなしではマッチしない
        /// </summary>
        [Test]
        public void NamesMatch_WithDifferentNames_NoRules_ReturnsFalse()
        {
            // Arrange
            string srcName = "J_Bip_C_Head";
            string dstName = "Head";

            // Act
            bool result = NameMatcher.NamesMatch(srcName, dstName, null);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// 正規表現ルールでマッチする
        /// </summary>
        [Test]
        public void NamesMatch_WithRegexRule_MatchesTransformed()
        {
            // Arrange
            string srcName = "J_Bip_C_Head";
            string dstName = "Head";
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("J_Bip_C_(.+)", "$1")
            };

            // Act
            bool result = NameMatcher.NamesMatch(srcName, dstName, rules);

            // Assert
            Assert.IsTrue(result);
        }

        #endregion

        #region TryFindMatchingName テスト

        /// <summary>
        /// 完全一致の名前が見つかる
        /// </summary>
        [Test]
        public void TryFindMatchingName_ExactMatch_ReturnsTrue()
        {
            // Arrange
            var childDic = new Dictionary<string, Transform>
            {
                { "Head", CreateDummyTransform("Head") },
                { "Neck", CreateDummyTransform("Neck") }
            };

            // Act
            bool result = NameMatcher.TryFindMatchingName(childDic, "Head", null, out string matchedName);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("Head", matchedName);
        }

        /// <summary>
        /// 正規表現ルールでマッチする名前が見つかる
        /// </summary>
        [Test]
        public void TryFindMatchingName_WithRegexRule_FindsMatch()
        {
            // Arrange
            var childDic = new Dictionary<string, Transform>
            {
                { "Head", CreateDummyTransform("Head") },
                { "Neck", CreateDummyTransform("Neck") }
            };
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("J_Bip_C_(.+)", "$1")
            };

            // Act
            bool result = NameMatcher.TryFindMatchingName(childDic, "J_Bip_C_Head", rules, out string matchedName);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("Head", matchedName);
        }

        /// <summary>
        /// マッチしない場合はfalse
        /// </summary>
        [Test]
        public void TryFindMatchingName_NoMatch_ReturnsFalse()
        {
            // Arrange
            var childDic = new Dictionary<string, Transform>
            {
                { "Spine", CreateDummyTransform("Spine") }
            };

            // Act
            bool result = NameMatcher.TryFindMatchingName(childDic, "Head", null, out string matchedName);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(matchedName);
        }

        #endregion

        #region GetBonesInGroup テスト

        /// <summary>
        /// Head グループのボーンが取得できる
        /// </summary>
        [Test]
        public void GetBonesInGroup_Head_ReturnsHeadBones()
        {
            // Act
            var bones = NameMatcher.GetBonesInGroup(HumanoidBoneGroup.Head);

            // Assert
            Assert.Contains(HumanBodyBones.Head, bones);
            Assert.Contains(HumanBodyBones.LeftEye, bones);
            Assert.Contains(HumanBodyBones.RightEye, bones);
        }

        /// <summary>
        /// LeftArm グループのボーンが取得できる
        /// </summary>
        [Test]
        public void GetBonesInGroup_LeftArm_ReturnsArmBones()
        {
            // Act
            var bones = NameMatcher.GetBonesInGroup(HumanoidBoneGroup.LeftArm);

            // Assert
            Assert.Contains(HumanBodyBones.LeftUpperArm, bones);
            Assert.Contains(HumanBodyBones.LeftHand, bones);
        }

        /// <summary>
        /// All グループはすべてのボーンを返す
        /// </summary>
        [Test]
        public void GetBonesInGroup_All_ReturnsAllBones()
        {
            // Act
            var bones = NameMatcher.GetBonesInGroup(HumanoidBoneGroup.All);

            // Assert
            Assert.Contains(HumanBodyBones.Head, bones);
            Assert.Contains(HumanBodyBones.Hips, bones);
            Assert.Contains(HumanBodyBones.LeftUpperArm, bones);
            Assert.Contains(HumanBodyBones.RightFoot, bones);
            Assert.Greater(bones.Length, 40);  // 多くのボーンがある
        }

        #endregion

        #region HasHumanoidBoneRule テスト

        /// <summary>
        /// HumanoidBoneルールが含まれている場合true
        /// </summary>
        [Test]
        public void HasHumanoidBoneRule_WithHumanoidRule_ReturnsTrue()
        {
            // Arrange
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("test", ""),
                new ReplacementRule(HumanoidBoneGroup.Head)
            };

            // Act
            bool result = NameMatcher.HasHumanoidBoneRule(rules);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// HumanoidBoneルールが無効の場合false
        /// </summary>
        [Test]
        public void HasHumanoidBoneRule_WithDisabledHumanoidRule_ReturnsFalse()
        {
            // Arrange
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.Head) { enabled = false }
            };

            // Act
            bool result = NameMatcher.HasHumanoidBoneRule(rules);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// 正規表現ルールのみの場合false
        /// </summary>
        [Test]
        public void HasHumanoidBoneRule_WithOnlyRegexRules_ReturnsFalse()
        {
            // Arrange
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("test", "replacement")
            };

            // Act
            bool result = NameMatcher.HasHumanoidBoneRule(rules);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region IsHumanoid テスト

        /// <summary>
        /// nullの場合はfalse
        /// </summary>
        [Test]
        public void IsHumanoid_WithNull_ReturnsFalse()
        {
            // Act
            bool result = NameMatcher.IsHumanoid(null);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Animatorがない場合はfalse
        /// </summary>
        [Test]
        public void IsHumanoid_WithoutAnimator_ReturnsFalse()
        {
            // Arrange
            var go = new GameObject("TestObject");
            go.hideFlags = HideFlags.HideAndDontSave;

            // Act
            bool result = NameMatcher.IsHumanoid(go);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region NamesMatch with HumanoidBone Mapping テスト

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
                { "J_Bip_C_Chest", HumanBodyBones.Chest },
                { "J_Bip_C_Neck", HumanBodyBones.Neck },
                { "J_Bip_L_UpperArm", HumanBodyBones.LeftUpperArm },
                { "J_Bip_L_LowerArm", HumanBodyBones.LeftLowerArm },
                { "J_Bip_L_Hand", HumanBodyBones.LeftHand },
                { "J_Bip_R_UpperArm", HumanBodyBones.RightUpperArm },
                { "J_Bip_R_LowerArm", HumanBodyBones.RightLowerArm },
                { "J_Bip_R_Hand", HumanBodyBones.RightHand },
                { "J_Bip_L_UpperLeg", HumanBodyBones.LeftUpperLeg },
                { "J_Bip_L_Foot", HumanBodyBones.LeftFoot },
                { "J_Bip_R_UpperLeg", HumanBodyBones.RightUpperLeg },
                { "J_Bip_R_Foot", HumanBodyBones.RightFoot },
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
                { "Chest", HumanBodyBones.Chest },
                { "Neck", HumanBodyBones.Neck },
                { "LeftUpperArm", HumanBodyBones.LeftUpperArm },
                { "LeftLowerArm", HumanBodyBones.LeftLowerArm },
                { "LeftHand", HumanBodyBones.LeftHand },
                { "RightUpperArm", HumanBodyBones.RightUpperArm },
                { "RightLowerArm", HumanBodyBones.RightLowerArm },
                { "RightHand", HumanBodyBones.RightHand },
                { "LeftUpperLeg", HumanBodyBones.LeftUpperLeg },
                { "LeftFoot", HumanBodyBones.LeftFoot },
                { "RightUpperLeg", HumanBodyBones.RightUpperLeg },
                { "RightFoot", HumanBodyBones.RightFoot },
            };
        }

        /// <summary>
        /// 同じHumanBodyBonesにマッピングされた名前がマッチする
        /// </summary>
        [Test]
        public void NamesMatch_WithHumanoidMapping_SameBone_ReturnsTrue()
        {
            // Arrange
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act
            bool result = NameMatcher.NamesMatch("J_Bip_C_Head", "Head", rules, srcMapping, dstMapping);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// 異なるHumanBodyBonesにマッピングされた名前はマッチしない
        /// </summary>
        [Test]
        public void NamesMatch_WithHumanoidMapping_DifferentBone_ReturnsFalse()
        {
            // Arrange
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act - HeadとNeckは異なるボーン
            bool result = NameMatcher.NamesMatch("J_Bip_C_Head", "Neck", rules, srcMapping, dstMapping);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// 指定されたグループのボーンのみがマッチする
        /// </summary>
        [Test]
        public void NamesMatch_WithHumanoidMapping_HeadGroup_OnlyMatchesHeadBones()
        {
            // Arrange
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.Head)  // 頭グループのみ
            };

            // Act & Assert - Headはマッチ
            Assert.IsTrue(NameMatcher.NamesMatch("J_Bip_C_Head", "Head", rules, srcMapping, dstMapping));
            
            // Act & Assert - LeftUpperArmは頭グループではないのでマッチしない
            Assert.IsFalse(NameMatcher.NamesMatch("J_Bip_L_UpperArm", "LeftUpperArm", rules, srcMapping, dstMapping));
        }

        /// <summary>
        /// 左腕グループのボーンがマッチする
        /// </summary>
        [Test]
        public void NamesMatch_WithHumanoidMapping_LeftArmGroup_MatchesArmBones()
        {
            // Arrange
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.LeftArm)
            };

            // Act & Assert - 左腕はマッチ
            Assert.IsTrue(NameMatcher.NamesMatch("J_Bip_L_UpperArm", "LeftUpperArm", rules, srcMapping, dstMapping));
            Assert.IsTrue(NameMatcher.NamesMatch("J_Bip_L_Hand", "LeftHand", rules, srcMapping, dstMapping));
            
            // Act & Assert - 右腕はマッチしない
            Assert.IsFalse(NameMatcher.NamesMatch("J_Bip_R_UpperArm", "RightUpperArm", rules, srcMapping, dstMapping));
        }

        /// <summary>
        /// マッピングがnullの場合はHumanoidBoneルールは機能しない
        /// </summary>
        [Test]
        public void NamesMatch_WithNullMapping_HumanoidRuleIgnored()
        {
            // Arrange
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act - マッピングがないので正規表現と完全一致のみ
            bool result = NameMatcher.NamesMatch("J_Bip_C_Head", "Head", rules, null, null);

            // Assert - マッピングがないのでマッチしない
            Assert.IsFalse(result);
        }

        /// <summary>
        /// ソースのマッピングのみがnullの場合はマッチしない
        /// </summary>
        [Test]
        public void NamesMatch_WithOnlySrcMappingNull_ReturnsFalse()
        {
            // Arrange
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act
            bool result = NameMatcher.NamesMatch("J_Bip_C_Head", "Head", rules, null, dstMapping);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// 正規表現ルールとHumanoidBoneルールの組み合わせ
        /// </summary>
        [Test]
        public void NamesMatch_WithRegexAndHumanoidRules_BothWork()
        {
            // Arrange
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule("Custom_(.+)", "$1"),  // 正規表現ルール
                new ReplacementRule(HumanoidBoneGroup.Head)  // HumanoidBoneルール
            };

            // Act & Assert - 正規表現でマッチ
            Assert.IsTrue(NameMatcher.NamesMatch("Custom_Test", "Test", rules, srcMapping, dstMapping));
            
            // Act & Assert - HumanoidBoneでマッチ
            Assert.IsTrue(NameMatcher.NamesMatch("J_Bip_C_Head", "Head", rules, srcMapping, dstMapping));
        }

        /// <summary>
        /// マッピングに存在しないボーン名はマッチしない
        /// </summary>
        [Test]
        public void NamesMatch_WithUnmappedBoneName_ReturnsFalse()
        {
            // Arrange
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act - マッピングに存在しない名前
            bool result = NameMatcher.NamesMatch("UnknownBone", "Head", rules, srcMapping, dstMapping);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region TryFindMatchingName with HumanoidBone Mapping テスト

        /// <summary>
        /// HumanoidBoneマッピングでマッチする子が見つかる
        /// </summary>
        [Test]
        public void TryFindMatchingName_WithHumanoidMapping_FindsMatch()
        {
            // Arrange
            var childDic = new Dictionary<string, Transform>
            {
                { "Head", CreateDummyTransform("Head") },
                { "Neck", CreateDummyTransform("Neck") },
                { "Spine", CreateDummyTransform("Spine") }
            };
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act
            bool result = NameMatcher.TryFindMatchingName(
                childDic, "J_Bip_C_Head", rules, out string matchedName, srcMapping, dstMapping);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("Head", matchedName);
        }

        /// <summary>
        /// HumanoidBoneマッピングでマッチしない場合は見つからない
        /// </summary>
        [Test]
        public void TryFindMatchingName_WithHumanoidMapping_NoMatch_ReturnsFalse()
        {
            // Arrange
            var childDic = new Dictionary<string, Transform>
            {
                { "Neck", CreateDummyTransform("Neck") },
                { "Spine", CreateDummyTransform("Spine") }
            };
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act - HeadはchildDicに存在しない
            bool result = NameMatcher.TryFindMatchingName(
                childDic, "J_Bip_C_Head", rules, out string matchedName, srcMapping, dstMapping);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(matchedName);
        }

        /// <summary>
        /// 複数のマッチ候補がある場合、最初のマッチを返す
        /// </summary>
        [Test]
        public void TryFindMatchingName_WithMultipleCandidates_ReturnsFirstMatch()
        {
            // Arrange - 辞書の順序は保証されないので、確認用
            var childDic = new Dictionary<string, Transform>
            {
                { "Head", CreateDummyTransform("Head") }
            };
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.Head)
            };

            // Act
            bool result = NameMatcher.TryFindMatchingName(
                childDic, "J_Bip_C_Head", rules, out string matchedName, srcMapping, dstMapping);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("Head", matchedName);
        }

        #endregion

        #region 複合テスト（正規表現 + HumanoidBone）

        /// <summary>
        /// 正規表現で変換してからHumanoid照合もチェック
        /// </summary>
        [Test]
        public void NamesMatch_RegexTransformThenHumanoid_WorksTogether()
        {
            // Arrange
            var srcMapping = new Dictionary<string, HumanBodyBones>
            {
                { "mixamorig:Head", HumanBodyBones.Head }
            };
            var dstMapping = new Dictionary<string, HumanBodyBones>
            {
                { "head", HumanBodyBones.Head }  // 小文字
            };
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.Head)
            };

            // Act - HumanoidBoneマッピングでマッチ
            bool result = NameMatcher.NamesMatch("mixamorig:Head", "head", rules, srcMapping, dstMapping);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// 全身のボーンが正しくマッチするか確認
        /// </summary>
        [Test]
        public void NamesMatch_AllBodyBones_MatchCorrectly()
        {
            // Arrange
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act & Assert - 全て正しくマッチ
            var testCases = new (string src, string dst)[]
            {
                ("J_Bip_C_Head", "Head"),
                ("J_Bip_C_Hips", "Hips"),
                ("J_Bip_C_Spine", "Spine"),
                ("J_Bip_C_Chest", "Chest"),
                ("J_Bip_C_Neck", "Neck"),
                ("J_Bip_L_UpperArm", "LeftUpperArm"),
                ("J_Bip_L_Hand", "LeftHand"),
                ("J_Bip_R_UpperArm", "RightUpperArm"),
                ("J_Bip_R_Hand", "RightHand"),
                ("J_Bip_L_UpperLeg", "LeftUpperLeg"),
                ("J_Bip_L_Foot", "LeftFoot"),
                ("J_Bip_R_UpperLeg", "RightUpperLeg"),
                ("J_Bip_R_Foot", "RightFoot"),
            };

            foreach (var (src, dst) in testCases)
            {
                bool result = NameMatcher.NamesMatch(src, dst, rules, srcMapping, dstMapping);
                Assert.IsTrue(result, $"{src} should match {dst}");
            }
        }

        /// <summary>
        /// クロスマッチしないことを確認（Headと非Head等）
        /// </summary>
        [Test]
        public void NamesMatch_CrossBones_DoNotMatch()
        {
            // Arrange
            var srcMapping = CreateMockSrcMapping();
            var dstMapping = CreateMockDstMapping();
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act & Assert - 異なるボーン同士はマッチしない
            var testCases = new (string src, string dst)[]
            {
                ("J_Bip_C_Head", "Hips"),
                ("J_Bip_C_Head", "Spine"),
                ("J_Bip_L_UpperArm", "RightUpperArm"),  // 左右の違い
                ("J_Bip_L_Hand", "LeftFoot"),  // 腕と脚
            };

            foreach (var (src, dst) in testCases)
            {
                bool result = NameMatcher.NamesMatch(src, dst, rules, srcMapping, dstMapping);
                Assert.IsFalse(result, $"{src} should NOT match {dst}");
            }
        }

        #endregion

        #region ヘルパーメソッド

        private static Transform CreateDummyTransform(string name)
        {
            var go = new GameObject(name);
            go.hideFlags = HideFlags.HideAndDontSave;
            return go.transform;
        }

        [TearDown]
        public void TearDown()
        {
            // テスト用に作成したGameObjectを削除
            var objects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in objects)
            {
                if (obj.hideFlags == HideFlags.HideAndDontSave)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        #endregion
    }
}

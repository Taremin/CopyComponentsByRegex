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

        #region NamesMatch テスト

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

        /// <summary>
        /// HumanoidBoneルールでエイリアスがマッチ
        /// </summary>
        [Test]
        public void NamesMatch_WithHumanoidBoneRule_MatchesAliases()
        {
            // Arrange
            string srcName = "J_Bip_C_Head";
            string dstName = "Head";
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.Head)
            };

            // Act
            bool result = NameMatcher.NamesMatch(srcName, dstName, rules);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// HumanoidBoneルール（すべて）でマッチ
        /// </summary>
        [Test]
        public void NamesMatch_WithAllBonesRule_MatchesAnyBone()
        {
            // Arrange
            string srcName = "J_Bip_L_UpperArm";
            string dstName = "LeftUpperArm";
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.All)
            };

            // Act
            bool result = NameMatcher.NamesMatch(srcName, dstName, rules);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// 異なるボーングループのエイリアスはマッチしない
        /// </summary>
        [Test]
        public void NamesMatch_WithWrongBoneGroup_ReturnsFalse()
        {
            // Arrange
            string srcName = "J_Bip_C_Head";  // 頭
            string dstName = "LeftUpperArm";   // 左腕
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.Head)  // 頭のみ対象
            };

            // Act
            bool result = NameMatcher.NamesMatch(srcName, dstName, rules);

            // Assert
            Assert.IsFalse(result);
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
        /// ルールでマッチする名前が見つかる
        /// </summary>
        [Test]
        public void TryFindMatchingName_WithRule_FindsMatch()
        {
            // Arrange
            var childDic = new Dictionary<string, Transform>
            {
                { "Head", CreateDummyTransform("Head") },
                { "Neck", CreateDummyTransform("Neck") }
            };
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.Head)
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
            var rules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.Head)  // 頭のみ
            };

            // Act
            bool result = NameMatcher.TryFindMatchingName(childDic, "J_Bip_C_Head", rules, out string matchedName);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(matchedName);
        }

        #endregion

        #region GetHumanoidBoneAliases テスト

        /// <summary>
        /// エイリアス定義が存在する
        /// </summary>
        [Test]
        public void GetHumanoidBoneAliases_ReturnsKnownBones()
        {
            // Act
            var aliases = NameMatcher.GetHumanoidBoneAliases();

            // Assert
            Assert.IsTrue(aliases.ContainsKey("Head"));
            Assert.IsTrue(aliases.ContainsKey("Hips"));
            Assert.IsTrue(aliases.ContainsKey("LeftUpperArm"));
            Assert.IsTrue(aliases["Head"].Contains("J_Bip_C_Head"));
        }

        /// <summary>
        /// グループに属するボーンが取得できる
        /// </summary>
        [Test]
        public void GetBonesInGroup_ReturnsCorrectBones()
        {
            // Act
            var headBones = NameMatcher.GetBonesInGroup(HumanoidBoneGroup.Head);
            var leftArmBones = NameMatcher.GetBonesInGroup(HumanoidBoneGroup.LeftArm);

            // Assert
            Assert.IsTrue(headBones.Contains("Head"));
            Assert.IsTrue(leftArmBones.Contains("LeftUpperArm"));
            Assert.IsTrue(leftArmBones.Contains("LeftHand"));
        }

        /// <summary>
        /// 「すべて」グループはすべてのボーンを返す
        /// </summary>
        [Test]
        public void GetBonesInGroup_All_ReturnsAllBones()
        {
            // Act
            var allBones = NameMatcher.GetBonesInGroup(HumanoidBoneGroup.All);

            // Assert
            Assert.IsTrue(allBones.Contains("Head"));
            Assert.IsTrue(allBones.Contains("Hips"));
            Assert.IsTrue(allBones.Contains("LeftUpperArm"));
            Assert.IsTrue(allBones.Contains("RightFoot"));
            Assert.IsTrue(allBones.Count > 40);  // 多くのボーンがある
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

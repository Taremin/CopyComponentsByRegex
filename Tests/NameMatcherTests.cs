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

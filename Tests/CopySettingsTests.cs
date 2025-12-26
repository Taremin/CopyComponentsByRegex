using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// CopySettingsクラスのテスト
    /// </summary>
    public class CopySettingsTests
    {
        private const string ConfigPrefix = "CopyComponentsByRegex/";
        private string savedReplacementRulesJson;

        [SetUp]
        public void SetUp()
        {
            // 既存の設定を保存
            savedReplacementRulesJson = EditorUserSettings.GetConfigValue(ConfigPrefix + "replacementRules");
        }

        [TearDown]
        public void TearDown()
        {
            // テスト前の状態に復元
            if (savedReplacementRulesJson != null)
            {
                EditorUserSettings.SetConfigValue(ConfigPrefix + "replacementRules", savedReplacementRulesJson);
            }
            else
            {
                EditorUserSettings.SetConfigValue(ConfigPrefix + "replacementRules", null);
            }
        }

        [Test]
        public void SaveAndLoad_ReplacementRules_PersistsRegexRules()
        {
            // Arrange
            var settings = new CopySettings();
            settings.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule("src_pattern", "dst_pattern")
            };

            // Act
            settings.Save();
            var loadedSettings = new CopySettings();
            loadedSettings.Load();

            // Assert
            Assert.AreEqual(1, loadedSettings.replacementRules.Count);
            Assert.AreEqual(RuleType.Regex, loadedSettings.replacementRules[0].type);
            Assert.AreEqual("src_pattern", loadedSettings.replacementRules[0].srcPattern);
            Assert.AreEqual("dst_pattern", loadedSettings.replacementRules[0].dstPattern);
            Assert.IsTrue(loadedSettings.replacementRules[0].enabled);
        }

        [Test]
        public void SaveAndLoad_ReplacementRules_PersistsHumanoidBoneGroupRules()
        {
            // Arrange
            var settings = new CopySettings();
            settings.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanoidBoneGroup.LeftArm)
            };

            // Act
            settings.Save();
            var loadedSettings = new CopySettings();
            loadedSettings.Load();

            // Assert
            Assert.AreEqual(1, loadedSettings.replacementRules.Count);
            Assert.AreEqual(RuleType.HumanoidBone, loadedSettings.replacementRules[0].type);
            Assert.AreEqual(HumanoidBoneSelectionMode.Group, loadedSettings.replacementRules[0].boneSelectionMode);
            Assert.AreEqual(HumanoidBoneGroup.LeftArm, loadedSettings.replacementRules[0].boneGroup);
        }

        [Test]
        public void SaveAndLoad_ReplacementRules_PersistsHumanoidBoneIndividualRules()
        {
            // Arrange
            var settings = new CopySettings();
            settings.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule(HumanBodyBones.LeftHand)
            };

            // Act
            settings.Save();
            var loadedSettings = new CopySettings();
            loadedSettings.Load();

            // Assert
            Assert.AreEqual(1, loadedSettings.replacementRules.Count);
            Assert.AreEqual(RuleType.HumanoidBone, loadedSettings.replacementRules[0].type);
            Assert.AreEqual(HumanoidBoneSelectionMode.Individual, loadedSettings.replacementRules[0].boneSelectionMode);
            Assert.AreEqual(HumanBodyBones.LeftHand, loadedSettings.replacementRules[0].singleBone);
        }

        [Test]
        public void SaveAndLoad_ReplacementRules_PersistsMultipleRules()
        {
            // Arrange
            var settings = new CopySettings();
            settings.replacementRules = new List<ReplacementRule>
            {
                new ReplacementRule("regex1", "replace1"),
                new ReplacementRule(HumanoidBoneGroup.All),
                new ReplacementRule(HumanBodyBones.Head)
            };

            // Act
            settings.Save();
            var loadedSettings = new CopySettings();
            loadedSettings.Load();

            // Assert
            Assert.AreEqual(3, loadedSettings.replacementRules.Count);
            Assert.AreEqual(RuleType.Regex, loadedSettings.replacementRules[0].type);
            Assert.AreEqual(RuleType.HumanoidBone, loadedSettings.replacementRules[1].type);
            Assert.AreEqual(HumanoidBoneSelectionMode.Group, loadedSettings.replacementRules[1].boneSelectionMode);
            Assert.AreEqual(RuleType.HumanoidBone, loadedSettings.replacementRules[2].type);
            Assert.AreEqual(HumanoidBoneSelectionMode.Individual, loadedSettings.replacementRules[2].boneSelectionMode);
        }

        [Test]
        public void Load_EmptyConfig_ReturnsEmptyList()
        {
            // Arrange
            EditorUserSettings.SetConfigValue(ConfigPrefix + "replacementRules", null);

            // Act
            var settings = new CopySettings();
            settings.Load();

            // Assert
            Assert.IsNotNull(settings.replacementRules);
            Assert.AreEqual(0, settings.replacementRules.Count);
        }

        [Test]
        public void Load_InvalidJson_ReturnsEmptyList()
        {
            // Arrange
            EditorUserSettings.SetConfigValue(ConfigPrefix + "replacementRules", "invalid json");

            // Act
            var settings = new CopySettings();
            settings.Load();

            // Assert
            Assert.IsNotNull(settings.replacementRules);
            Assert.AreEqual(0, settings.replacementRules.Count);
        }

        [Test]
        public void SaveAndLoad_DisabledRule_PersistsEnabledState()
        {
            // Arrange
            var settings = new CopySettings();
            var rule = new ReplacementRule("test", "replace");
            rule.enabled = false;
            settings.replacementRules = new List<ReplacementRule> { rule };

            // Act
            settings.Save();
            var loadedSettings = new CopySettings();
            loadedSettings.Load();

            // Assert
            Assert.AreEqual(1, loadedSettings.replacementRules.Count);
            Assert.IsFalse(loadedSettings.replacementRules[0].enabled);
        }
    }
}

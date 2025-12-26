using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// ローカライゼーション機能のユニットテスト
    /// </summary>
    [TestFixture]
    public class LocalizationTests
    {
        [SetUp]
        public void SetUp()
        {
            // テスト前にローカライゼーションを再読み込み
            Localization.Reload();
        }

        /// <summary>
        /// 日本語と英語のJSONファイルのキー数が一致することを確認
        /// </summary>
        [Test]
        public void AllLanguages_HaveSameKeyCount()
        {
            int jaKeyCount = Localization.GetKeyCount("ja");
            int enKeyCount = Localization.GetKeyCount("en");

            Assert.That(jaKeyCount, Is.GreaterThan(0), "日本語のキーが読み込まれていません");
            Assert.That(enKeyCount, Is.GreaterThan(0), "英語のキーが読み込まれていません");
            Assert.That(jaKeyCount, Is.EqualTo(enKeyCount), 
                $"日本語と英語のキー数が一致しません。ja: {jaKeyCount}, en: {enKeyCount}");
        }

        /// <summary>
        /// 全てのキーが両方の言語に存在することを確認
        /// </summary>
        [Test]
        public void AllKeys_ExistInBothLanguages()
        {
            var jaKeys = new HashSet<string>(Localization.GetKeys("ja"));
            var enKeys = new HashSet<string>(Localization.GetKeys("en"));

            // 日本語にあって英語にないキー
            var missingInEn = jaKeys.Except(enKeys).ToList();
            Assert.That(missingInEn, Is.Empty, 
                $"英語に存在しないキー: {string.Join(", ", missingInEn)}");

            // 英語にあって日本語にないキー
            var missingInJa = enKeys.Except(jaKeys).ToList();
            Assert.That(missingInJa, Is.Empty, 
                $"日本語に存在しないキー: {string.Join(", ", missingInJa)}");
        }

        /// <summary>
        /// 言語切り替えが正しく動作することを確認
        /// </summary>
        [Test]
        public void LanguageSwitch_ChangesOutput()
        {
            // 英語に切り替え
            Localization.CurrentLanguageOption = LanguageOption.English;
            string enLabel = Localization.L("ActiveObject");

            // 日本語に切り替え
            Localization.CurrentLanguageOption = LanguageOption.Japanese;
            string jaLabel = Localization.L("ActiveObject");

            Assert.That(enLabel, Is.EqualTo("Active Object"));
            Assert.That(jaLabel, Is.EqualTo("アクティブオブジェクト"));
        }

        /// <summary>
        /// 存在しないキーに対してキー自体がフォールバックとして返されることを確認
        /// </summary>
        [Test]
        public void NonExistentKey_ReturnsFallback()
        {
            string result = Localization.L("NonExistentKey12345");
            Assert.That(result, Is.EqualTo("NonExistentKey12345"));
        }

        /// <summary>
        /// ツールチップが正しく取得できることを確認
        /// </summary>
        [Test]
        public void Tooltip_ReturnsCorrectValue()
        {
            Localization.CurrentLanguageOption = LanguageOption.Japanese;
            string tooltip = Localization.Tooltip("ClothNNS");
            
            Assert.That(tooltip, Does.Contain("メッシュ").Or.Contains("頂点"));
        }

        /// <summary>
        /// GUIContentが正しく生成されることを確認
        /// </summary>
        [Test]
        public void Content_ReturnsGUIContentWithLabelAndTooltip()
        {
            Localization.CurrentLanguageOption = LanguageOption.Japanese;
            var content = Localization.Content("ClothNNS");

            Assert.That(content.text, Does.Contain("Cloth NNS"));
            Assert.That(content.tooltip, Is.Not.Empty);
        }

        /// <summary>
        /// 利用可能な言語が取得できることを確認
        /// </summary>
        [Test]
        public void GetAvailableLanguages_ReturnsLanguages()
        {
            var languages = Localization.GetAvailableLanguages();

            Assert.That(languages, Contains.Item("ja"));
            Assert.That(languages, Contains.Item("en"));
        }

        /// <summary>
        /// ボーングループのキーが全て存在することを確認
        /// </summary>
        [Test]
        public void BoneGroupKeys_AllExist()
        {
            string[] boneGroupKeys = {
                "BoneGroup_All",
                "BoneGroup_Head",
                "BoneGroup_Neck",
                "BoneGroup_Chest",
                "BoneGroup_Spine",
                "BoneGroup_Hips",
                "BoneGroup_LeftArm",
                "BoneGroup_RightArm",
                "BoneGroup_LeftLeg",
                "BoneGroup_RightLeg",
                "BoneGroup_LeftFingers",
                "BoneGroup_RightFingers"
            };

            Localization.CurrentLanguageOption = LanguageOption.Japanese;
            foreach (var key in boneGroupKeys)
            {
                string label = Localization.L(key);
                Assert.That(label, Is.Not.EqualTo(key), 
                    $"ボーングループキー {key} が見つかりません");
            }
        }

        /// <summary>
        /// 言語オプションがシステムの場合、正しく言語が検出されることを確認
        /// </summary>
        [Test]
        public void SystemLanguage_DetectsCorrectly()
        {
            Localization.CurrentLanguageOption = LanguageOption.System;
            string currentLang = Localization.CurrentLanguage;

            // システム言語は日本語か英語のどちらかになるはず
            Assert.That(currentLang, Is.EqualTo("ja").Or.EqualTo("en"));
        }
    }
}

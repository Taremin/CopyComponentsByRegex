// PathUtility クラスのユニットテスト
// Unity Test Framework を使用
using System.IO;
using NUnit.Framework;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// PathUtility クラスのパス取得機能をテストするクラス
    /// </summary>
    public class PathUtilityTests
    {
        /// <summary>
        /// PackageRootPathがnullや空でないことを確認
        /// </summary>
        [Test]
        public void PackageRootPath_ReturnsNonEmptyPath()
        {
            // Act
            string rootPath = PathUtility.PackageRootPath;

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(rootPath), "PackageRootPathは空であってはいけません");
            Assert.IsTrue(Directory.Exists(rootPath), $"PackageRootPathが存在しません: {rootPath}");
        }

        /// <summary>
        /// GetEditorDirectoryPathがEditorディレクトリを返すことを確認
        /// </summary>
        [Test]
        public void GetEditorDirectoryPath_ReturnsEditorDirectory()
        {
            // Act
            string editorPath = PathUtility.GetEditorDirectoryPath();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(editorPath), "EditorDirectoryPathは空であってはいけません");
            Assert.IsTrue(editorPath.Replace('\\', '/').Contains("Editor"),
                $"パスに'Editor'が含まれていません: {editorPath}");
            Assert.IsTrue(Directory.Exists(editorPath), $"Editorディレクトリが存在しません: {editorPath}");
        }

        /// <summary>
        /// GetPackageJsonPathがpackage.jsonへの正しいパスを返すことを確認
        /// </summary>
        [Test]
        public void GetPackageJsonPath_ReturnsValidPath()
        {
            // Act
            string packageJsonPath = PathUtility.GetPackageJsonPath();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(packageJsonPath), "PackageJsonPathは空であってはいけません");
            Assert.IsTrue(packageJsonPath.EndsWith("package.json"), 
                $"パスがpackage.jsonで終わっていません: {packageJsonPath}");
            Assert.IsTrue(File.Exists(packageJsonPath), $"package.jsonが存在しません: {packageJsonPath}");
        }

        /// <summary>
        /// GetTestsDirectoryPathがTestsディレクトリへの正しいパスを返すことを確認
        /// </summary>
        [Test]
        public void GetTestsDirectoryPath_ReturnsValidPath()
        {
            // Act
            string testsPath = PathUtility.GetTestsDirectoryPath();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(testsPath), "TestsDirectoryPathは空であってはいけません");
            Assert.IsTrue(testsPath.Replace('\\', '/').EndsWith("Tests"), 
                $"パスがTestsで終わっていません: {testsPath}");
            Assert.IsTrue(Directory.Exists(testsPath), $"Testsディレクトリが存在しません: {testsPath}");
        }

        /// <summary>
        /// GetTestCasesDirectoryPathがTestCasesディレクトリへの正しいパスを返すことを確認
        /// </summary>
        [Test]
        public void GetTestCasesDirectoryPath_ReturnsValidPath()
        {
            // Act
            string testCasesPath = PathUtility.GetTestCasesDirectoryPath();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(testCasesPath), "TestCasesDirectoryPathは空であってはいけません");
            Assert.IsTrue(testCasesPath.Replace('\\', '/').EndsWith("TestCases"), 
                $"パスがTestCasesで終わっていません: {testCasesPath}");
            // TestCasesディレクトリは存在しない場合もあるのでディレクトリ存在チェックはしない
        }

        /// <summary>
        /// GetPackageAssetPathがUnityアセットパス形式を返すことを確認
        /// </summary>
        [Test]
        public void GetPackageAssetPath_ReturnsUnityStylePath()
        {
            // Act
            string assetPath = PathUtility.GetPackageAssetPath();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(assetPath), "PackageAssetPathは空であってはいけません");
            Assert.IsTrue(assetPath.StartsWith("Assets/"), 
                $"パスが'Assets/'で始まっていません: {assetPath}");
            Assert.IsFalse(assetPath.Contains("\\"), 
                $"パスにバックスラッシュが含まれています: {assetPath}");
        }

        /// <summary>
        /// GetPackageVersionがバージョン文字列を返すことを確認
        /// </summary>
        [Test]
        public void GetPackageVersion_ReturnsValidVersion()
        {
            // Act
            string version = PathUtility.GetPackageVersion();

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(version), "バージョンは空であってはいけません");
            Assert.AreNotEqual("unknown", version, "バージョンが'unknown'であってはいけません");
            // セマンティックバージョン形式（x.y.z）を確認
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+"),
                $"バージョンがセマンティックバージョン形式ではありません: {version}");
        }

        /// <summary>
        /// パスの一貫性テスト - PackageRootPathとGetPackageJsonPathの関係
        /// </summary>
        [Test]
        public void PathConsistency_PackageJsonIsInPackageRoot()
        {
            // Act
            string rootPath = PathUtility.PackageRootPath;
            string packageJsonPath = PathUtility.GetPackageJsonPath();

            // Assert
            string expectedPath = Path.Combine(rootPath, "package.json");
            Assert.AreEqual(expectedPath, packageJsonPath, 
                "GetPackageJsonPathはPackageRootPath/package.jsonと一致するべきです");
        }

        /// <summary>
        /// パスの一貫性テスト - PackageRootPathとGetTestsDirectoryPathの関係
        /// </summary>
        [Test]
        public void PathConsistency_TestsIsInPackageRoot()
        {
            // Act
            string rootPath = PathUtility.PackageRootPath;
            string testsPath = PathUtility.GetTestsDirectoryPath();

            // Assert
            string expectedPath = Path.Combine(rootPath, "Tests");
            Assert.AreEqual(expectedPath, testsPath, 
                "GetTestsDirectoryPathはPackageRootPath/Testsと一致するべきです");
        }

        /// <summary>
        /// パスの一貫性テスト - GetTestsDirectoryPathとGetTestCasesDirectoryPathの関係
        /// </summary>
        [Test]
        public void PathConsistency_TestCasesIsInTests()
        {
            // Act
            string testsPath = PathUtility.GetTestsDirectoryPath();
            string testCasesPath = PathUtility.GetTestCasesDirectoryPath();

            // Assert
            string expectedPath = Path.Combine(testsPath, "TestCases");
            Assert.AreEqual(expectedPath, testCasesPath, 
                "GetTestCasesDirectoryPathはGetTestsDirectoryPath/TestCasesと一致するべきです");
        }
    }
}

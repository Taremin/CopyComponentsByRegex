using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// パス関連のユーティリティを提供する静的クラス
    /// CallerFilePathを使用してスクリプトの相対パスを取得する機能を提供
    /// </summary>
    public static class PathUtility
    {
        /// <summary>
        /// パッケージのルートディレクトリへのパスを取得（Editorディレクトリの親）
        /// </summary>
        public static string PackageRootPath
        {
            get
            {
                string editorPath = GetEditorDirectoryPath();
                return Path.GetDirectoryName(editorPath);
            }
        }

        /// <summary>
        /// このファイル（PathUtility.cs）の絶対パスを取得
        /// [CallerFilePath]属性により、コンパイル時にこのファイルのパスが埋め込まれる
        /// </summary>
        private static string GetSelfPath([CallerFilePath] string filePath = "")
        {
            return filePath;
        }

        /// <summary>
        /// Editorディレクトリへの絶対パスを取得
        /// どこから呼んでも同じ結果（Editorディレクトリ）を返す
        /// </summary>
        public static string GetEditorDirectoryPath()
        {
            return Path.GetDirectoryName(GetSelfPath());
        }

        /// <summary>
        /// package.jsonへの絶対パスを取得
        /// </summary>
        public static string GetPackageJsonPath()
        {
            return Path.Combine(PackageRootPath, "package.json");
        }

        /// <summary>
        /// Testsディレクトリへの絶対パスを取得
        /// </summary>
        public static string GetTestsDirectoryPath()
        {
            return Path.Combine(PackageRootPath, "Tests");
        }

        /// <summary>
        /// Tests/TestCasesディレクトリへの絶対パスを取得
        /// </summary>
        public static string GetTestCasesDirectoryPath()
        {
            return Path.Combine(GetTestsDirectoryPath(), "TestCases");
        }

        /// <summary>
        /// パッケージルートのUnity用アセットパスを取得
        /// 例: "Assets/plugins/CopyComponentsByRegex"
        /// </summary>
        public static string GetPackageAssetPath()
        {
            // 絶対パスからAssetsパスに変換
            string absolutePath = PackageRootPath;
            
            // Windows/Macのパスセパレータを正規化
            absolutePath = absolutePath.Replace('\\', '/');
            
            // "Assets"以降のパスを抽出
            int assetsIndex = absolutePath.IndexOf("Assets/");
            if (assetsIndex >= 0)
            {
                return absolutePath.Substring(assetsIndex);
            }
            
            // フォールバック: 元のパスを返す
            return absolutePath;
        }

        /// <summary>
        /// package.jsonからバージョン情報を読み込む
        /// </summary>
        public static string GetPackageVersion()
        {
            string packageJsonPath = GetPackageJsonPath();
            if (File.Exists(packageJsonPath))
            {
                string json = File.ReadAllText(packageJsonPath);
                // PackageInfoはComponentCopier.csで定義済み
                var packageInfo = UnityEngine.JsonUtility.FromJson<PackageInfo>(json);
                return packageInfo?.version ?? "unknown";
            }
            return "unknown";
        }
    }
}

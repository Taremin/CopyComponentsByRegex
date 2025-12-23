// CopyComponentsByRegex の主要機能テスト
// Unity Test Framework を使用
using NUnit.Framework;
using UnityEngine;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// CopyComponentsByRegex クラスの基本機能をテストするクラス
    /// </summary>
    public class CopyComponentsByRegexTests
    {
        private GameObject testRoot;

        /// <summary>
        /// テスト用GameObjectを作成するヘルパーメソッド
        /// HideAndDontSave フラグを設定し、シーンに保存されないようにする
        /// </summary>
        private static GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            // シーンに保存せず、ヒエラルキーにも表示しない
            go.hideFlags = HideFlags.HideAndDontSave;
            return go;
        }

        /// <summary>
        /// 各テスト前にテスト用GameObjectを作成
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            testRoot = CreateTestGameObject("TestRoot");
        }

        /// <summary>
        /// 各テスト後にテスト用GameObjectを破棄
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
                testRoot = null;
            }
        }

        /// <summary>
        /// TreeItem の基本的な構築テスト
        /// </summary>
        [Test]
        public void TreeItem_Constructor_SetsNameAndType()
        {
            // Arrange
            var go = CreateTestGameObject("TestObject");

            try
            {
                // Act
                var treeItem = new TreeItem(go);

                // Assert
                Assert.AreEqual("TestObject", treeItem.name);
                Assert.AreEqual("UnityEngine.GameObject", treeItem.type);
                Assert.IsNotNull(treeItem.components);
                Assert.IsNotNull(treeItem.children);
                Assert.AreEqual(0, treeItem.components.Count);
                Assert.AreEqual(0, treeItem.children.Count);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// TreeItem が正しいGameObject参照を保持するテスト
        /// </summary>
        [Test]
        public void TreeItem_Constructor_HoldsGameObjectReference()
        {
            // Arrange
            var go = CreateTestGameObject("RefTest");

            try
            {
                // Act
                var treeItem = new TreeItem(go);

                // Assert
                Assert.AreSame(go, treeItem.gameObject);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// 子オブジェクトを持つGameObjectの階層テスト
        /// </summary>
        [Test]
        public void TreeItem_WithChildren_ChildrenListIsInitializedEmpty()
        {
            // Arrange
            var parent = CreateTestGameObject("Parent");
            var child = CreateTestGameObject("Child");
            child.transform.SetParent(parent.transform);

            try
            {
                // Act
                var treeItem = new TreeItem(parent);

                // Assert: TreeItem のコンストラクタは子を自動追加しない
                Assert.AreEqual(0, treeItem.children.Count);

                // 子GameObjectは存在する
                Assert.AreEqual(1, parent.transform.childCount);
            }
            finally
            {
                // 親を削除すれば子も削除される
                Object.DestroyImmediate(parent);
            }
        }

        /// <summary>
        /// ModificationOperation列挙型の値テスト
        /// </summary>
        [Test]
        public void ModificationOperation_HasExpectedValues()
        {
            // Assert: すべての操作タイプが定義されていることを確認
            Assert.AreEqual(0, (int)ModificationOperation.None);
            Assert.AreEqual(1, (int)ModificationOperation.Add);
            Assert.AreEqual(2, (int)ModificationOperation.Remove);
            Assert.AreEqual(3, (int)ModificationOperation.Update);
            Assert.AreEqual(4, (int)ModificationOperation.CreateObject);
        }

        /// <summary>
        /// ModificationEntry の基本プロパティテスト
        /// </summary>
        [Test]
        public void ModificationEntry_DefaultValues_AreNull()
        {
            // Act
            var entry = new ModificationEntry();

            // Assert
            Assert.IsNull(entry.targetObject);
            Assert.IsNull(entry.targetPath);
            Assert.IsNull(entry.componentType);
            Assert.AreEqual(ModificationOperation.None, entry.operation);
            Assert.IsNull(entry.message);
            Assert.IsNull(entry.createdComponent);
            Assert.IsNull(entry.createdObject);
        }

        /// <summary>
        /// ModificationEntry に値を設定できるテスト
        /// </summary>
        [Test]
        public void ModificationEntry_SetValues_StoresCorrectly()
        {
            // Arrange
            var go = CreateTestGameObject("TestTarget");

            try
            {
                // Act
                var entry = new ModificationEntry
                {
                    targetObject = go,
                    targetPath = "Root/Child",
                    componentType = "TestComponent",
                    operation = ModificationOperation.Add,
                    message = "テストメッセージ"
                };

                // Assert
                Assert.AreSame(go, entry.targetObject);
                Assert.AreEqual("Root/Child", entry.targetPath);
                Assert.AreEqual("TestComponent", entry.componentType);
                Assert.AreEqual(ModificationOperation.Add, entry.operation);
                Assert.AreEqual("テストメッセージ", entry.message);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
        /// <summary>
        /// BugReportData のデフォルト値テスト
        /// </summary>
        [Test]
        public void BugReportData_DefaultValues_AreCorrect()
        {
            // Act
            var data = new BugReportData();

            // Assert
            Assert.AreEqual("1.0.0", data.version);
            Assert.IsNull(data.timestamp);
            Assert.IsTrue(data.includeProperties);
            Assert.IsNull(data.source);
            Assert.IsNull(data.destination);
            Assert.IsNull(data.settings);
            Assert.IsNotNull(data.modificationLogs);
            Assert.AreEqual(0, data.modificationLogs.Count);
        }

        /// <summary>
        /// SimpleJsonSerializer の往復シリアライズテスト
        /// </summary>
        [Test]
        public void BugReportData_Serialization_RoundTrip()
        {
            // Arrange
            var data = new BugReportData
            {
                version = "1.0.0",
                timestamp = "2025-12-22T19:00:00+09:00",
                includeProperties = true,
                source = new ObjectData
                {
                    name = "TestSource",
                    isHumanoid = false
                },
                destination = new ObjectData
                {
                    name = "TestDest",
                    isHumanoid = true
                },
                settings = new SettingsData
                {
                    pattern = "Test.*",
                    isObjectCopy = true
                }
            };

            data.modificationLogs.Add(new ModificationLogData
            {
                targetPath = "TestSource/Child",
                componentType = "TestComponent",
                operation = "Add",
                message = "テスト"
            });

            // Act
            string json = SimpleJsonSerializer.Serialize(data, true);

            // Assert: JSONが生成されること
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"version\": \"1.0.0\""));
            Assert.IsTrue(json.Contains("\"TestSource\""));
            Assert.IsTrue(json.Contains("\"TestDest\""));
            Assert.IsTrue(json.Contains("\"Test.*\""));
            Assert.IsTrue(json.Contains("\"Add\""));
        }

        /// <summary>
        /// PropertyData のプロパティテスト
        /// </summary>
        [Test]
        public void PropertyData_SetValues_StoresCorrectly()
        {
            // Act
            var propData = new PropertyData
            {
                name = "stiffnessForce",
                type = "Float",
                value = "1.5"
            };

            // Assert
            Assert.AreEqual("stiffnessForce", propData.name);
            Assert.AreEqual("Float", propData.type);
            Assert.AreEqual("1.5", propData.value);
        }
    }
}

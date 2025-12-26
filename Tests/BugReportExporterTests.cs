using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// BugReportExporterのユニットテスト
    /// srcとdstの階層がJSONで正しく構築されることを確認
    /// </summary>
    [TestFixture]
    public class BugReportExporterTests
    {
        private GameObject srcRoot;
        private GameObject dstRoot;

        [SetUp]
        public void SetUp()
        {
            // ソース階層を作成
            srcRoot = new GameObject("SourceRoot");
            var srcChild1 = new GameObject("Child1");
            srcChild1.transform.SetParent(srcRoot.transform);
            var srcChild2 = new GameObject("Child2");
            srcChild2.transform.SetParent(srcRoot.transform);
            var srcGrandChild = new GameObject("GrandChild");
            srcGrandChild.transform.SetParent(srcChild1.transform);

            // デスティネーション階層を作成
            dstRoot = new GameObject("DestRoot");
            var dstChild1 = new GameObject("DstChild1");
            dstChild1.transform.SetParent(dstRoot.transform);
            var dstChild2 = new GameObject("DstChild2");
            dstChild2.transform.SetParent(dstRoot.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (srcRoot != null) Object.DestroyImmediate(srcRoot);
            if (dstRoot != null) Object.DestroyImmediate(dstRoot);
        }

        [Test]
        public void Export_BuildsSourceHierarchy_Correctly()
        {
            // Arrange
            var copyTree = BuildTreeItem(srcRoot);
            var settings = new CopySettings { pattern = ".*" };

            // Act
            var result = BugReportExporter.Export(
                srcRoot, copyTree, dstRoot, settings,
                new List<ModificationEntry>(), new List<ModificationEntry>(),
                includeProperties: false);

            // Assert
            Assert.IsNotNull(result.source, "source should not be null");
            Assert.AreEqual("SourceRoot", result.source.name);
            Assert.IsNotNull(result.source.hierarchy, "source.hierarchy should not be null");
            Assert.AreEqual(1, result.source.hierarchy.Count, "source.hierarchy should have 1 root item");

            var rootItem = result.source.hierarchy[0];
            Assert.AreEqual("SourceRoot", rootItem.name);
            Assert.AreEqual(2, rootItem.children.Count, "SourceRoot should have 2 children");

            // Child1を確認
            var child1 = rootItem.children.Find(c => c.name == "Child1");
            Assert.IsNotNull(child1, "Child1 should exist");
            Assert.AreEqual(1, child1.children.Count, "Child1 should have 1 child (GrandChild)");
            Assert.AreEqual("GrandChild", child1.children[0].name);

            // Child2を確認
            var child2 = rootItem.children.Find(c => c.name == "Child2");
            Assert.IsNotNull(child2, "Child2 should exist");
            Assert.AreEqual(0, child2.children.Count, "Child2 should have no children");
        }

        [Test]
        public void Export_BuildsDestinationHierarchy_Correctly()
        {
            // Arrange
            var copyTree = BuildTreeItem(srcRoot);
            var settings = new CopySettings { pattern = ".*" };

            // Act
            var result = BugReportExporter.Export(
                srcRoot, copyTree, dstRoot, settings,
                new List<ModificationEntry>(), new List<ModificationEntry>(),
                includeProperties: false);

            // Assert
            Assert.IsNotNull(result.destination, "destination should not be null");
            Assert.AreEqual("DestRoot", result.destination.name);
            Assert.IsNotNull(result.destination.hierarchy, "destination.hierarchy should not be null");
            Assert.AreEqual(1, result.destination.hierarchy.Count, "destination.hierarchy should have 1 root item");

            var rootItem = result.destination.hierarchy[0];
            Assert.AreEqual("DestRoot", rootItem.name);
            Assert.AreEqual(2, rootItem.children.Count, "DestRoot should have 2 children");

            // DstChild1を確認
            var dstChild1 = rootItem.children.Find(c => c.name == "DstChild1");
            Assert.IsNotNull(dstChild1, "DstChild1 should exist");

            // DstChild2を確認
            var dstChild2 = rootItem.children.Find(c => c.name == "DstChild2");
            Assert.IsNotNull(dstChild2, "DstChild2 should exist");
        }

        [Test]
        public void Export_WithNullSource_HandlesGracefully()
        {
            // Arrange
            var settings = new CopySettings { pattern = ".*" };

            // Act
            var result = BugReportExporter.Export(
                null, null, dstRoot, settings,
                new List<ModificationEntry>(), new List<ModificationEntry>(),
                includeProperties: false);

            // Assert
            Assert.IsNull(result.source, "source should be null when source is null");
            Assert.IsNotNull(result.destination, "destination should not be null");
            Assert.IsNotNull(result.destination.hierarchy, "destination.hierarchy should not be null");
        }

        [Test]
        public void Export_WithNullDestination_HandlesGracefully()
        {
            // Arrange
            var copyTree = BuildTreeItem(srcRoot);
            var settings = new CopySettings { pattern = ".*" };

            // Act
            var result = BugReportExporter.Export(
                srcRoot, copyTree, null, settings,
                new List<ModificationEntry>(), new List<ModificationEntry>(),
                includeProperties: false);

            // Assert
            Assert.IsNotNull(result.source, "source should not be null");
            Assert.IsNotNull(result.source.hierarchy, "source.hierarchy should not be null");
            Assert.IsNull(result.destination, "destination should be null when destination is null");
        }

        [Test]
        public void Export_IncludesPathInHierarchy()
        {
            // Arrange
            var copyTree = BuildTreeItem(srcRoot);
            var settings = new CopySettings { pattern = ".*" };

            // Act
            var result = BugReportExporter.Export(
                srcRoot, copyTree, dstRoot, settings,
                new List<ModificationEntry>(), new List<ModificationEntry>(),
                includeProperties: false);

            // Assert
            var rootItem = result.source.hierarchy[0];
            Assert.AreEqual("SourceRoot", rootItem.path);

            var child1 = rootItem.children.Find(c => c.name == "Child1");
            Assert.AreEqual("SourceRoot/Child1", child1.path);

            var grandChild = child1.children[0];
            Assert.AreEqual("SourceRoot/Child1/GrandChild", grandChild.path);
        }

        [Test]
        public void ToJson_SerializesHierarchyCorrectly()
        {
            // Arrange
            var copyTree = BuildTreeItem(srcRoot);
            var settings = new CopySettings { pattern = ".*" };
            var data = BugReportExporter.Export(
                srcRoot, copyTree, dstRoot, settings,
                new List<ModificationEntry>(), new List<ModificationEntry>(),
                includeProperties: false);

            // Act
            var json = BugReportExporter.ToJson(data, prettyPrint: false);

            // Assert
            Assert.IsTrue(json.Contains("\"SourceRoot\""), "JSON should contain SourceRoot");
            Assert.IsTrue(json.Contains("\"DestRoot\""), "JSON should contain DestRoot");
            Assert.IsTrue(json.Contains("\"Child1\""), "JSON should contain Child1");
            Assert.IsTrue(json.Contains("\"DstChild1\""), "JSON should contain DstChild1");
            Assert.IsTrue(json.Contains("\"GrandChild\""), "JSON should contain GrandChild");
        }

        /// <summary>
        /// GameObjectからTreeItemを再帰的に構築するヘルパー
        /// </summary>
        private TreeItem BuildTreeItem(GameObject go)
        {
            var item = new TreeItem(go);
            foreach (Transform child in go.transform)
            {
                item.children.Add(BuildTreeItem(child.gameObject));
            }
            return item;
        }
    }
}

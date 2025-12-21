// KDTree クラスのユニットテスト
// Unity Test Framework を使用
using NUnit.Framework;
using UnityEngine;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// KDTree クラスの近傍探索機能をテストするクラス
    /// </summary>
    public class KDTreeTests
    {
        /// <summary>
        /// 単一の点に対する最近傍探索のテスト
        /// クエリ点と同じ点が返されることを確認
        /// </summary>
        [Test]
        public void FindNearest_SinglePoint_ReturnsCorrectIndex()
        {
            // Arrange: 単一の点を持つKDTreeを作成
            var points = new Vector3[] { new Vector3(1, 2, 3) };
            var kdTree = new KDTree(points);

            // Act: その点自体をクエリ
            int nearestIndex = kdTree.FindNearest(new Vector3(1, 2, 3));

            // Assert: インデックス0が返されることを確認
            Assert.AreEqual(0, nearestIndex);
        }

        /// <summary>
        /// 複数の点から正しい最近傍点を見つけるテスト
        /// </summary>
        [Test]
        public void FindNearest_MultiplePoints_ReturnsClosestPoint()
        {
            // Arrange: 3つの点を持つKDTreeを作成
            var points = new Vector3[]
            {
                new Vector3(0, 0, 0),   // インデックス 0
                new Vector3(10, 0, 0),  // インデックス 1
                new Vector3(5, 0, 0)    // インデックス 2
            };
            var kdTree = new KDTree(points);

            // Act: (4, 0, 0) に最も近い点を探す → (5, 0, 0) が最も近い
            int nearestIndex = kdTree.FindNearest(new Vector3(4, 0, 0));

            // Assert: インデックス2（点 (5, 0, 0)）が返されることを確認
            Assert.AreEqual(2, nearestIndex);
        }

        /// <summary>
        /// 原点に最も近い点を見つけるテスト
        /// </summary>
        [Test]
        public void FindNearest_QueryAtOrigin_ReturnsClosestToOrigin()
        {
            // Arrange: 様々な位置に点を配置
            var points = new Vector3[]
            {
                new Vector3(1, 1, 1),
                new Vector3(2, 2, 2),
                new Vector3(0.5f, 0.5f, 0.5f),  // 原点に最も近い（インデックス2）
                new Vector3(-3, -3, -3)
            };
            var kdTree = new KDTree(points);

            // Act: 原点からの最近傍を探す
            int nearestIndex = kdTree.FindNearest(Vector3.zero);

            // Assert: インデックス2が返されることを確認
            Assert.AreEqual(2, nearestIndex);
        }

        /// <summary>
        /// 負の座標を持つ点でも正しく動作するテスト
        /// </summary>
        [Test]
        public void FindNearest_NegativeCoordinates_WorksCorrectly()
        {
            // Arrange: 負の座標を含む点を配置
            var points = new Vector3[]
            {
                new Vector3(-5, -5, -5),  // インデックス 0
                new Vector3(-1, -1, -1),  // インデックス 1
                new Vector3(1, 1, 1)      // インデックス 2
            };
            var kdTree = new KDTree(points);

            // Act: (-2, -2, -2) に最も近い点を探す
            int nearestIndex = kdTree.FindNearest(new Vector3(-2, -2, -2));

            // Assert: (-1, -1, -1) が最も近いのでインデックス1
            Assert.AreEqual(1, nearestIndex);
        }

        /// <summary>
        /// クエリ点が既存の点と完全に一致する場合のテスト
        /// </summary>
        [Test]
        public void FindNearest_ExactMatch_ReturnsMatchingIndex()
        {
            // Arrange
            var points = new Vector3[]
            {
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 1)
            };
            var kdTree = new KDTree(points);

            // Act: 既存の点と完全に一致するクエリ
            int nearestIndex = kdTree.FindNearest(new Vector3(0, 1, 0));

            // Assert: インデックス1が返される
            Assert.AreEqual(1, nearestIndex);
        }

        /// <summary>
        /// 大量の点でも正しく動作するパフォーマンステスト
        /// </summary>
        [Test]
        public void FindNearest_LargePointSet_ReturnsCorrectResult()
        {
            // Arrange: 1000個の点を生成
            const int pointCount = 1000;
            var points = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                points[i] = new Vector3(i, i * 2, i * 3);
            }
            var kdTree = new KDTree(points);

            // Act: 中間付近の点を探す
            var queryPoint = new Vector3(500, 1000, 1500);
            int nearestIndex = kdTree.FindNearest(queryPoint);

            // Assert: インデックス500が返される（完全一致）
            Assert.AreEqual(500, nearestIndex);
        }

        /// <summary>
        /// 3次元空間での対角方向の最近傍探索テスト
        /// </summary>
        [Test]
        public void FindNearest_DiagonalQuery_ReturnsCorrectResult()
        {
            // Arrange: 立方体の頂点に点を配置
            var points = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 1),
                new Vector3(1, 1, 0),
                new Vector3(1, 0, 1),
                new Vector3(0, 1, 1),
                new Vector3(1, 1, 1)   // インデックス 7
            };
            var kdTree = new KDTree(points);

            // Act: (1.1, 1.1, 1.1) に最も近い点を探す → (1, 1, 1)
            int nearestIndex = kdTree.FindNearest(new Vector3(1.1f, 1.1f, 1.1f));

            // Assert: インデックス7が返される
            Assert.AreEqual(7, nearestIndex);
        }

        /// <summary>
        /// 2点のみの場合のテスト
        /// </summary>
        [Test]
        public void FindNearest_TwoPoints_ReturnsCloser()
        {
            // Arrange
            var points = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(10, 10, 10)
            };
            var kdTree = new KDTree(points);

            // Act: (3, 3, 3) に最も近い点を探す → (0, 0, 0) が近い
            int nearestIndex = kdTree.FindNearest(new Vector3(3, 3, 3));

            // Assert: インデックス0が返される
            Assert.AreEqual(0, nearestIndex);
        }
    }
}

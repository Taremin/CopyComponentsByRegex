// KD Tree
// Modified by Taremin, 2018.

// Original Code:
// KDTree.cs - A Stark, September 2009.
// https://forum.unity.com/threads/point-nearest-neighbour-search-class.29923/

using System.Collections;
using UnityEngine;

namespace CopyComponentsByRegex {
    public class KDTree {
        class KDNode {
            public KDNode[] lr;
            public Vector3 pivot;
            public int pivotIdx;
            public int axis;
        }

        const int dimension = 3;
        KDNode root;

        public KDTree (Vector3[] points, int startIdx = 0, int endIdx = -1) {
            root = MakeFromPoints (points, 0, 0, endIdx < 0 ? points.Length - 1 : endIdx, CreateIndexFilledIntArray (points.Length));
        }

        // 再帰的にKDツリーの構築を行う
        KDNode MakeFromPoints (Vector3[] points, int depth, int stIdx, int enIdx, int[] inds) {
            KDNode root = new KDNode ();
            root.axis = depth % dimension;

            int splitPointIdx = SplitByAxis (points, inds, stIdx, enIdx, root.axis);

            root.lr = new KDNode[2];
            root.pivotIdx = inds[splitPointIdx];
            root.pivot = points[root.pivotIdx];

            int leftEndIdx = splitPointIdx - 1;

            if (leftEndIdx >= stIdx) {
                root.lr[0] = MakeFromPoints (points, depth + 1, stIdx, leftEndIdx, inds);
            }

            int rightStartIdx = splitPointIdx + 1;

            if (rightStartIdx <= enIdx) {
                root.lr[1] = MakeFromPoints (points, depth + 1, rightStartIdx, enIdx, inds);
            }

            return root;
        }

        static void Swap (int[] ary, int a, int b) {
            int tmp = ary[a];
            ary[a] = ary[b];
            ary[b] = tmp;
        }

        // "median of three" で大体の中央値のindexを求める
        static int FindMedianIdx (Vector3[] points, int[] inds, int startIdx, int endIdx, int axis) {
            float a = points[inds[startIdx]][axis];
            float b = points[inds[endIdx]][axis];
            int midIdx = (startIdx + endIdx) / 2;
            float m = points[inds[midIdx]][axis];

            return (
                (a > b) ?
                (m > a) ? startIdx : (b > m) ? endIdx : midIdx :
                (a > m) ? startIdx : (m > b) ? endIdx : midIdx
            );
        }

        // 指定されたindex内でソート（中央値より大きいのは右、小さいのは左にスワップ）して中央値のindexを返す
        static int SplitByAxis (Vector3[] points, int[] indices, int startIdx, int endIdx, int axis) {
            int splitPointIdx = FindMedianIdx (points, indices, startIdx, endIdx, axis);
            Vector3 pivot = points[indices[splitPointIdx]];

            Swap (indices, startIdx++, splitPointIdx);

            while (startIdx <= endIdx) {
                if ((points[indices[startIdx]][axis] > pivot[axis])) {
                    Swap (indices, startIdx, endIdx--);
                } else {
                    Swap (indices, startIdx - 1, startIdx++);
                }
            }

            return startIdx - 1;
        }

        static int[] CreateIndexFilledIntArray (int n) {
            int[] result = new int[n];

            for (int i = 0; i < n; ++i) {
                result[i] = i;
            }

            return result;
        }

        // 最近傍探索の実行
        public int FindNearest (Vector3 pt) {
            float bestSqDist = float.MaxValue;
            int bestIdx = -1;

            Search (root, pt, ref bestSqDist, ref bestIdx);

            return bestIdx;
        }

        // ツリーを再帰的に探索
        void Search (KDNode node, Vector3 pt, ref float minDist, ref int minIdx, int depth = 0) {
            var pivot = node.pivot;
            float currentDist = (pivot - pt).sqrMagnitude;
            var lr = node.lr;

            if (currentDist < minDist) {
                minDist = currentDist;
                minIdx = node.pivotIdx;
            }

            int axis = node.axis;
            float axisDist = pt[axis] - pivot[axis];
            int leftOrRight = axisDist <= 0 ? 0 : 1;

            if (lr[leftOrRight] != null) {
                Search (lr[leftOrRight], pt, ref minDist, ref minIdx, depth + 1);
            }

            leftOrRight = (leftOrRight + 1) % 2;

            if ((lr[leftOrRight] != null) && (minDist > axisDist * axisDist)) {
                Search (lr[leftOrRight], pt, ref minDist, ref minIdx, depth + 1);
            }
        }
    }
}
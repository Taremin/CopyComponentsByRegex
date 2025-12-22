using System.Collections.Generic;
using UnityEngine;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// GameObjectの階層構造とコンポーネント情報を保持するデータクラス
    /// コピー元の構造をツリー形式で記録し、コピー先に適用する際に使用
    /// </summary>
    [System.Serializable]
    public class TreeItem
    {
        /// <summary>
        /// GameObjectの名前
        /// </summary>
        public string name;

        /// <summary>
        /// GameObjectの型名（通常は "UnityEngine.GameObject"）
        /// </summary>
        public string type;

        /// <summary>
        /// 元のGameObjectへの参照
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// 子オブジェクトのツリー構造
        /// </summary>
        public List<TreeItem> children;

        /// <summary>
        /// このオブジェクトに含まれるコピー対象コンポーネント
        /// </summary>
        public List<Component> components;

        /// <summary>
        /// コンストラクタ - GameObjectからTreeItemを作成
        /// </summary>
        /// <param name="go">対象のGameObject</param>
        public TreeItem(GameObject go)
        {
            name = go.name;
            type = go.GetType().ToString();
            gameObject = go;
            components = new List<Component>();
            children = new List<TreeItem>();
        }
    }
}

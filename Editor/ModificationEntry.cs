using UnityEngine;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// コンポーネント操作の種類を表すenum
    /// </summary>
    public enum ModificationOperation
    {
        /// <summary>操作なし</summary>
        None,
        /// <summary>コンポーネントの追加</summary>
        Add,
        /// <summary>コンポーネントの削除</summary>
        Remove,
        /// <summary>コンポーネント値の更新</summary>
        Update,
        /// <summary>オブジェクトの新規作成</summary>
        CreateObject
    }

    /// <summary>
    /// コピー操作による変更のログエントリ
    /// レポート表示やUndo用に操作内容を記録
    /// </summary>
    public class ModificationEntry
    {
        /// <summary>
        /// 操作対象のGameObject
        /// </summary>
        public GameObject targetObject;

        /// <summary>
        /// 操作対象のパス（階層構造を示す文字列）
        /// </summary>
        public string targetPath;

        /// <summary>
        /// 操作対象のコンポーネント型名
        /// </summary>
        public string componentType;

        /// <summary>
        /// 実行された操作の種類
        /// </summary>
        public ModificationOperation operation;

        /// <summary>
        /// 操作の説明メッセージ
        /// </summary>
        public string message;

        /// <summary>
        /// 実際に作成されたコンポーネント（追加操作時）
        /// </summary>
        public Component createdComponent;

        /// <summary>
        /// 実際に作成されたオブジェクト（オブジェクトコピー時）
        /// </summary>
        public GameObject createdObject;
    }
}

using System;
using UnityEngine;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// 置換ルールの種類
    /// </summary>
    public enum RuleType
    {
        /// <summary>正規表現による置換</summary>
        Regex,
        /// <summary>HumanoidBoneによる置換</summary>
        HumanoidBone
    }

    /// <summary>
    /// HumanoidBoneのグループ
    /// </summary>
    public enum HumanoidBoneGroup
    {
        /// <summary>すべてのHumanoidBone</summary>
        All,
        /// <summary>頭</summary>
        Head,
        /// <summary>首</summary>
        Neck,
        /// <summary>胸</summary>
        Chest,
        /// <summary>脊椎</summary>
        Spine,
        /// <summary>ヒップ</summary>
        Hips,
        /// <summary>左腕</summary>
        LeftArm,
        /// <summary>右腕</summary>
        RightArm,
        /// <summary>左脚</summary>
        LeftLeg,
        /// <summary>右脚</summary>
        RightLeg,
        /// <summary>左手指</summary>
        LeftFingers,
        /// <summary>右手指</summary>
        RightFingers
    }

    /// <summary>
    /// HumanoidBoneの選択モード
    /// </summary>
    public enum HumanoidBoneSelectionMode
    {
        /// <summary>ボーングループで選択</summary>
        Group,
        /// <summary>個別ボーンで選択</summary>
        Individual
    }

    /// <summary>
    /// 置換ルールを表すデータ構造
    /// 正規表現の置換またはHumanoidBoneのマッピングをサポートする
    /// </summary>
    [Serializable]
    public class ReplacementRule
    {
        /// <summary>ルールの種類</summary>
        public RuleType type = RuleType.Regex;

        /// <summary>正規表現パターン（Regexタイプの場合に使用）</summary>
        public string srcPattern = "";

        /// <summary>置換後のパターン（Regexタイプの場合に使用）</summary>
        public string dstPattern = "";

        /// <summary>HumanoidBoneグループ（HumanoidBoneタイプの場合に使用）</summary>
        public HumanoidBoneGroup boneGroup = HumanoidBoneGroup.All;

        /// <summary>HumanoidBoneの選択モード（グループまたは個別）</summary>
        public HumanoidBoneSelectionMode boneSelectionMode = HumanoidBoneSelectionMode.Group;

        /// <summary>個別のHumanoidBone（Individual選択時に使用）</summary>
        public HumanBodyBones singleBone = HumanBodyBones.Hips;

        /// <summary>ルールが有効かどうか</summary>
        public bool enabled = true;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public ReplacementRule() { }

        /// <summary>
        /// 正規表現ルール用コンストラクタ
        /// </summary>
        public ReplacementRule(string srcPattern, string dstPattern)
        {
            this.type = RuleType.Regex;
            this.srcPattern = srcPattern;
            this.dstPattern = dstPattern;
            this.enabled = true;
        }

        /// <summary>
        /// HumanoidBoneルール用コンストラクタ（グループ選択）
        /// </summary>
        public ReplacementRule(HumanoidBoneGroup boneGroup)
        {
            this.type = RuleType.HumanoidBone;
            this.boneSelectionMode = HumanoidBoneSelectionMode.Group;
            this.boneGroup = boneGroup;
            this.enabled = true;
        }

        /// <summary>
        /// HumanoidBoneルール用コンストラクタ（個別ボーン選択）
        /// </summary>
        public ReplacementRule(HumanBodyBones singleBone)
        {
            this.type = RuleType.HumanoidBone;
            this.boneSelectionMode = HumanoidBoneSelectionMode.Individual;
            this.singleBone = singleBone;
            this.enabled = true;
        }
    }
}

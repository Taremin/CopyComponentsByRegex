using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// 名前変換ロジックを提供するクラス
    /// 置換ルールを使用してsrc名からdst名への変換、またはマッチングを行う
    /// </summary>
    public static class NameMatcher
    {
        /// <summary>
        /// HumanoidBoneグループの日本語表示名
        /// </summary>
        public static readonly Dictionary<HumanoidBoneGroup, string> BoneGroupDisplayNames = new Dictionary<HumanoidBoneGroup, string>
        {
            { HumanoidBoneGroup.All, "すべて" },
            { HumanoidBoneGroup.Head, "頭" },
            { HumanoidBoneGroup.Neck, "首" },
            { HumanoidBoneGroup.Chest, "胸" },
            { HumanoidBoneGroup.Spine, "脊椎" },
            { HumanoidBoneGroup.Hips, "ヒップ" },
            { HumanoidBoneGroup.LeftArm, "左腕" },
            { HumanoidBoneGroup.RightArm, "右腕" },
            { HumanoidBoneGroup.LeftLeg, "左脚" },
            { HumanoidBoneGroup.RightLeg, "右脚" },
            { HumanoidBoneGroup.LeftFingers, "左手指" },
            { HumanoidBoneGroup.RightFingers, "右手指" },
        };

        /// <summary>
        /// HumanoidBoneGroupに属するHumanBodyBonesのリスト
        /// </summary>
        private static readonly Dictionary<HumanoidBoneGroup, HumanBodyBones[]> BoneGroupMembers = new Dictionary<HumanoidBoneGroup, HumanBodyBones[]>
        {
            { HumanoidBoneGroup.Head, new[] { HumanBodyBones.Head, HumanBodyBones.LeftEye, HumanBodyBones.RightEye, HumanBodyBones.Jaw } },
            { HumanoidBoneGroup.Neck, new[] { HumanBodyBones.Neck } },
            { HumanoidBoneGroup.Chest, new[] { HumanBodyBones.Chest, HumanBodyBones.UpperChest } },
            { HumanoidBoneGroup.Spine, new[] { HumanBodyBones.Spine } },
            { HumanoidBoneGroup.Hips, new[] { HumanBodyBones.Hips } },
            { HumanoidBoneGroup.LeftArm, new[] { HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand } },
            { HumanoidBoneGroup.RightArm, new[] { HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand } },
            { HumanoidBoneGroup.LeftLeg, new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes } },
            { HumanoidBoneGroup.RightLeg, new[] { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes } },
            { HumanoidBoneGroup.LeftFingers, new[] {
                HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbDistal,
                HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal,
                HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal,
                HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal,
                HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal
            }},
            { HumanoidBoneGroup.RightFingers, new[] {
                HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal,
                HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
                HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
                HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
                HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal
            }},
        };

        /// <summary>
        /// 指定されたHumanoidBoneGroupに属するHumanBodyBonesのリストを取得
        /// </summary>
        public static HumanBodyBones[] GetBonesInGroup(HumanoidBoneGroup group)
        {
            if (group == HumanoidBoneGroup.All)
            {
                // すべてのボーンを返す
                return System.Enum.GetValues(typeof(HumanBodyBones))
                    .Cast<HumanBodyBones>()
                    .Where(b => b != HumanBodyBones.LastBone)
                    .ToArray();
            }

            if (BoneGroupMembers.TryGetValue(group, out var bones))
            {
                return bones;
            }

            return new HumanBodyBones[0];
        }

        /// <summary>
        /// AnimatorからHumanoidボーンマッピングを取得
        /// キー: ボーン名, 値: HumanBodyBones
        /// </summary>
        public static Dictionary<string, HumanBodyBones> GetBoneMapping(Animator animator)
        {
            var mapping = new Dictionary<string, HumanBodyBones>();
            
            if (animator == null || !animator.isHuman)
            {
                return mapping;
            }

            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone == HumanBodyBones.LastBone) continue;

                var transform = animator.GetBoneTransform(bone);
                if (transform != null)
                {
                    mapping[transform.name] = bone;
                }
            }

            return mapping;
        }

        /// <summary>
        /// GameObjectがHumanoidかどうかをチェック
        /// </summary>
        public static bool IsHumanoid(GameObject go)
        {
            if (go == null) return false;
            var animator = go.GetComponent<Animator>();
            return animator != null && animator.isHuman;
        }

        /// <summary>
        /// 置換ルールリストを適用してsrcNameを変換
        /// </summary>
        public static string TransformName(string srcName, List<ReplacementRule> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                return srcName;
            }

            string result = srcName;
            foreach (var rule in rules)
            {
                if (!rule.enabled)
                {
                    continue;
                }

                if (rule.type == RuleType.Regex)
                {
                    result = ApplyRegexRule(result, rule);
                }
                // HumanoidBoneルールはTransformNameには適用しない（マッピング変換はNamesMatchで行う）
            }

            return result;
        }

        /// <summary>
        /// srcNameとdstNameが（置換ルールを考慮して）マッチするかを判定
        /// </summary>
        /// <param name="srcName">ソース側の名前</param>
        /// <param name="dstName">デスティネーション側の名前</param>
        /// <param name="rules">置換ルールのリスト</param>
        /// <param name="srcBoneMapping">ソース側のHumanoidボーンマッピング（null可）</param>
        /// <param name="dstBoneMapping">デスティネーション側のHumanoidボーンマッピング（null可）</param>
        /// <returns>マッチする場合はtrue</returns>
        public static bool NamesMatch(
            string srcName, 
            string dstName, 
            List<ReplacementRule> rules,
            Dictionary<string, HumanBodyBones> srcBoneMapping = null,
            Dictionary<string, HumanBodyBones> dstBoneMapping = null)
        {
            // 完全一致の場合は即座にtrue
            if (srcName == dstName)
            {
                return true;
            }

            if (rules == null || rules.Count == 0)
            {
                return false;
            }

            // 正規表現ルールによる変換後に一致するかチェック
            string transformedSrc = TransformName(srcName, rules);
            if (transformedSrc == dstName)
            {
                return true;
            }

            // HumanoidBoneルールによるマッチをチェック
            foreach (var rule in rules)
            {
                if (!rule.enabled || rule.type != RuleType.HumanoidBone)
                {
                    continue;
                }

                if (CheckHumanoidBoneMatch(srcName, dstName, rule, srcBoneMapping, dstBoneMapping))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 辞書から置換ルールを考慮してマッチする子を検索
        /// </summary>
        public static bool TryFindMatchingName(
            Dictionary<string, Transform> childDic, 
            string srcName, 
            List<ReplacementRule> rules, 
            out string matchedName,
            Dictionary<string, HumanBodyBones> srcBoneMapping = null,
            Dictionary<string, HumanBodyBones> dstBoneMapping = null)
        {
            // まず完全一致をチェック
            if (childDic.ContainsKey(srcName))
            {
                matchedName = srcName;
                return true;
            }

            if (rules == null || rules.Count == 0)
            {
                matchedName = null;
                return false;
            }

            // 各キーに対してマッチをチェック
            foreach (var key in childDic.Keys)
            {
                if (NamesMatch(srcName, key, rules, srcBoneMapping, dstBoneMapping))
                {
                    matchedName = key;
                    return true;
                }
            }

            matchedName = null;
            return false;
        }

        /// <summary>
        /// 正規表現ルールを適用
        /// </summary>
        private static string ApplyRegexRule(string input, ReplacementRule rule)
        {
            if (string.IsNullOrEmpty(rule.srcPattern))
            {
                return input;
            }

            try
            {
                var regex = new Regex(rule.srcPattern);
                return regex.Replace(input, rule.dstPattern ?? "");
            }
            catch (ArgumentException)
            {
                // 正規表現が無効な場合は入力をそのまま返す
                return input;
            }
        }

        /// <summary>
        /// HumanoidBoneによるマッチをチェック（動的マッピング使用）
        /// </summary>
        private static bool CheckHumanoidBoneMatch(
            string srcName, 
            string dstName, 
            ReplacementRule rule,
            Dictionary<string, HumanBodyBones> srcBoneMapping,
            Dictionary<string, HumanBodyBones> dstBoneMapping)
        {
            // マッピングがない場合はマッチしない
            if (srcBoneMapping == null || dstBoneMapping == null)
            {
                return false;
            }

            // srcNameがソースのマッピングに存在するかチェック
            if (!srcBoneMapping.TryGetValue(srcName, out var srcBone))
            {
                return false;
            }

            // 選択モードに応じてフィルタリング
            if (rule.boneSelectionMode == HumanoidBoneSelectionMode.Group)
            {
                // グループ選択: 対象グループに含まれるボーンかチェック
                var bonesInGroup = GetBonesInGroup(rule.boneGroup);
                if (!bonesInGroup.Contains(srcBone))
                {
                    return false;
                }
            }
            else
            {
                // 個別選択: 指定されたボーンと一致するかチェック
                if (srcBone != rule.singleBone)
                {
                    return false;
                }
            }

            // dstNameがデスティネーションのマッピングで同じHumanBodyBonesにマッピングされているかチェック
            if (dstBoneMapping.TryGetValue(dstName, out var dstBone))
            {
                return srcBone == dstBone;
            }

            return false;
        }

        /// <summary>
        /// 置換ルールにHumanoidBoneルールが含まれているかチェック
        /// </summary>
        public static bool HasHumanoidBoneRule(List<ReplacementRule> rules)
        {
            if (rules == null) return false;
            return rules.Any(r => r.enabled && r.type == RuleType.HumanoidBone);
        }
    }
}

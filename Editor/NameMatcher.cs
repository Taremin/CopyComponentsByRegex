using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// 名前変換ロジックを提供するクラス
    /// 置換ルールを使用してsrc名からdst名への変換、またはマッチングを行う
    /// </summary>
    public static class NameMatcher
    {
        /// <summary>
        /// HumanoidBoneのエイリアス定義
        /// キー: 正規化されたボーン名, 値: エイリアスのリスト
        /// </summary>
        private static readonly Dictionary<string, List<string>> BoneAliases = new Dictionary<string, List<string>>
        {
            // 頭
            { "Head", new List<string> { "Head", "J_Bip_C_Head", "head", "mixamorig:Head" } },
            { "LeftEye", new List<string> { "LeftEye", "J_Adj_L_FaceEye", "Eye_L", "mixamorig:LeftEye" } },
            { "RightEye", new List<string> { "RightEye", "J_Adj_R_FaceEye", "Eye_R", "mixamorig:RightEye" } },
            { "Jaw", new List<string> { "Jaw", "J_Adj_C_Jaw", "jaw" } },

            // 首
            { "Neck", new List<string> { "Neck", "J_Bip_C_Neck", "neck", "mixamorig:Neck" } },

            // 胸
            { "Chest", new List<string> { "Chest", "J_Bip_C_Chest", "chest", "mixamorig:Spine1" } },
            { "UpperChest", new List<string> { "UpperChest", "J_Bip_C_UpperChest", "upper_chest", "mixamorig:Spine2" } },

            // 脊椎
            { "Spine", new List<string> { "Spine", "J_Bip_C_Spine", "spine", "mixamorig:Spine" } },

            // ヒップ
            { "Hips", new List<string> { "Hips", "J_Bip_C_Hips", "hips", "mixamorig:Hips", "Pelvis" } },

            // 左腕
            { "LeftShoulder", new List<string> { "LeftShoulder", "J_Bip_L_Shoulder", "shoulder_L", "mixamorig:LeftShoulder" } },
            { "LeftUpperArm", new List<string> { "LeftUpperArm", "J_Bip_L_UpperArm", "upper_arm_L", "mixamorig:LeftArm", "Arm_L" } },
            { "LeftLowerArm", new List<string> { "LeftLowerArm", "J_Bip_L_LowerArm", "lower_arm_L", "mixamorig:LeftForeArm", "ForeArm_L" } },
            { "LeftHand", new List<string> { "LeftHand", "J_Bip_L_Hand", "hand_L", "mixamorig:LeftHand", "Hand_L" } },

            // 右腕
            { "RightShoulder", new List<string> { "RightShoulder", "J_Bip_R_Shoulder", "shoulder_R", "mixamorig:RightShoulder" } },
            { "RightUpperArm", new List<string> { "RightUpperArm", "J_Bip_R_UpperArm", "upper_arm_R", "mixamorig:RightArm", "Arm_R" } },
            { "RightLowerArm", new List<string> { "RightLowerArm", "J_Bip_R_LowerArm", "lower_arm_R", "mixamorig:RightForeArm", "ForeArm_R" } },
            { "RightHand", new List<string> { "RightHand", "J_Bip_R_Hand", "hand_R", "mixamorig:RightHand", "Hand_R" } },

            // 左脚
            { "LeftUpperLeg", new List<string> { "LeftUpperLeg", "J_Bip_L_UpperLeg", "upper_leg_L", "mixamorig:LeftUpLeg", "Thigh_L" } },
            { "LeftLowerLeg", new List<string> { "LeftLowerLeg", "J_Bip_L_LowerLeg", "lower_leg_L", "mixamorig:LeftLeg", "Leg_L" } },
            { "LeftFoot", new List<string> { "LeftFoot", "J_Bip_L_Foot", "foot_L", "mixamorig:LeftFoot", "Foot_L" } },
            { "LeftToes", new List<string> { "LeftToes", "J_Bip_L_ToeBase", "toe_L", "mixamorig:LeftToeBase", "Toe_L" } },

            // 右脚
            { "RightUpperLeg", new List<string> { "RightUpperLeg", "J_Bip_R_UpperLeg", "upper_leg_R", "mixamorig:RightUpLeg", "Thigh_R" } },
            { "RightLowerLeg", new List<string> { "RightLowerLeg", "J_Bip_R_LowerLeg", "lower_leg_R", "mixamorig:RightLeg", "Leg_R" } },
            { "RightFoot", new List<string> { "RightFoot", "J_Bip_R_Foot", "foot_R", "mixamorig:RightFoot", "Foot_R" } },
            { "RightToes", new List<string> { "RightToes", "J_Bip_R_ToeBase", "toe_R", "mixamorig:RightToeBase", "Toe_R" } },

            // 左手指
            { "LeftThumbProximal", new List<string> { "LeftThumbProximal", "J_Bip_L_Thumb1", "thumb_01_L", "mixamorig:LeftHandThumb1" } },
            { "LeftThumbIntermediate", new List<string> { "LeftThumbIntermediate", "J_Bip_L_Thumb2", "thumb_02_L", "mixamorig:LeftHandThumb2" } },
            { "LeftThumbDistal", new List<string> { "LeftThumbDistal", "J_Bip_L_Thumb3", "thumb_03_L", "mixamorig:LeftHandThumb3" } },
            { "LeftIndexProximal", new List<string> { "LeftIndexProximal", "J_Bip_L_Index1", "index_01_L", "mixamorig:LeftHandIndex1" } },
            { "LeftIndexIntermediate", new List<string> { "LeftIndexIntermediate", "J_Bip_L_Index2", "index_02_L", "mixamorig:LeftHandIndex2" } },
            { "LeftIndexDistal", new List<string> { "LeftIndexDistal", "J_Bip_L_Index3", "index_03_L", "mixamorig:LeftHandIndex3" } },
            { "LeftMiddleProximal", new List<string> { "LeftMiddleProximal", "J_Bip_L_Middle1", "middle_01_L", "mixamorig:LeftHandMiddle1" } },
            { "LeftMiddleIntermediate", new List<string> { "LeftMiddleIntermediate", "J_Bip_L_Middle2", "middle_02_L", "mixamorig:LeftHandMiddle2" } },
            { "LeftMiddleDistal", new List<string> { "LeftMiddleDistal", "J_Bip_L_Middle3", "middle_03_L", "mixamorig:LeftHandMiddle3" } },
            { "LeftRingProximal", new List<string> { "LeftRingProximal", "J_Bip_L_Ring1", "ring_01_L", "mixamorig:LeftHandRing1" } },
            { "LeftRingIntermediate", new List<string> { "LeftRingIntermediate", "J_Bip_L_Ring2", "ring_02_L", "mixamorig:LeftHandRing2" } },
            { "LeftRingDistal", new List<string> { "LeftRingDistal", "J_Bip_L_Ring3", "ring_03_L", "mixamorig:LeftHandRing3" } },
            { "LeftLittleProximal", new List<string> { "LeftLittleProximal", "J_Bip_L_Little1", "pinky_01_L", "mixamorig:LeftHandPinky1" } },
            { "LeftLittleIntermediate", new List<string> { "LeftLittleIntermediate", "J_Bip_L_Little2", "pinky_02_L", "mixamorig:LeftHandPinky2" } },
            { "LeftLittleDistal", new List<string> { "LeftLittleDistal", "J_Bip_L_Little3", "pinky_03_L", "mixamorig:LeftHandPinky3" } },

            // 右手指
            { "RightThumbProximal", new List<string> { "RightThumbProximal", "J_Bip_R_Thumb1", "thumb_01_R", "mixamorig:RightHandThumb1" } },
            { "RightThumbIntermediate", new List<string> { "RightThumbIntermediate", "J_Bip_R_Thumb2", "thumb_02_R", "mixamorig:RightHandThumb2" } },
            { "RightThumbDistal", new List<string> { "RightThumbDistal", "J_Bip_R_Thumb3", "thumb_03_R", "mixamorig:RightHandThumb3" } },
            { "RightIndexProximal", new List<string> { "RightIndexProximal", "J_Bip_R_Index1", "index_01_R", "mixamorig:RightHandIndex1" } },
            { "RightIndexIntermediate", new List<string> { "RightIndexIntermediate", "J_Bip_R_Index2", "index_02_R", "mixamorig:RightHandIndex2" } },
            { "RightIndexDistal", new List<string> { "RightIndexDistal", "J_Bip_R_Index3", "index_03_R", "mixamorig:RightHandIndex3" } },
            { "RightMiddleProximal", new List<string> { "RightMiddleProximal", "J_Bip_R_Middle1", "middle_01_R", "mixamorig:RightHandMiddle1" } },
            { "RightMiddleIntermediate", new List<string> { "RightMiddleIntermediate", "J_Bip_R_Middle2", "middle_02_R", "mixamorig:RightHandMiddle2" } },
            { "RightMiddleDistal", new List<string> { "RightMiddleDistal", "J_Bip_R_Middle3", "middle_03_R", "mixamorig:RightHandMiddle3" } },
            { "RightRingProximal", new List<string> { "RightRingProximal", "J_Bip_R_Ring1", "ring_01_R", "mixamorig:RightHandRing1" } },
            { "RightRingIntermediate", new List<string> { "RightRingIntermediate", "J_Bip_R_Ring2", "ring_02_R", "mixamorig:RightHandRing2" } },
            { "RightRingDistal", new List<string> { "RightRingDistal", "J_Bip_R_Ring3", "ring_03_R", "mixamorig:RightHandRing3" } },
            { "RightLittleProximal", new List<string> { "RightLittleProximal", "J_Bip_R_Little1", "pinky_01_R", "mixamorig:RightHandPinky1" } },
            { "RightLittleIntermediate", new List<string> { "RightLittleIntermediate", "J_Bip_R_Little2", "pinky_02_R", "mixamorig:RightHandPinky2" } },
            { "RightLittleDistal", new List<string> { "RightLittleDistal", "J_Bip_R_Little3", "pinky_03_R", "mixamorig:RightHandPinky3" } },
        };

        /// <summary>
        /// HumanoidBoneGroupに属するボーン名のリスト
        /// </summary>
        private static readonly Dictionary<HumanoidBoneGroup, List<string>> BoneGroupMembers = new Dictionary<HumanoidBoneGroup, List<string>>
        {
            { HumanoidBoneGroup.Head, new List<string> { "Head", "LeftEye", "RightEye", "Jaw" } },
            { HumanoidBoneGroup.Neck, new List<string> { "Neck" } },
            { HumanoidBoneGroup.Chest, new List<string> { "Chest", "UpperChest" } },
            { HumanoidBoneGroup.Spine, new List<string> { "Spine" } },
            { HumanoidBoneGroup.Hips, new List<string> { "Hips" } },
            { HumanoidBoneGroup.LeftArm, new List<string> { "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand" } },
            { HumanoidBoneGroup.RightArm, new List<string> { "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand" } },
            { HumanoidBoneGroup.LeftLeg, new List<string> { "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes" } },
            { HumanoidBoneGroup.RightLeg, new List<string> { "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes" } },
            { HumanoidBoneGroup.LeftFingers, new List<string> {
                "LeftThumbProximal", "LeftThumbIntermediate", "LeftThumbDistal",
                "LeftIndexProximal", "LeftIndexIntermediate", "LeftIndexDistal",
                "LeftMiddleProximal", "LeftMiddleIntermediate", "LeftMiddleDistal",
                "LeftRingProximal", "LeftRingIntermediate", "LeftRingDistal",
                "LeftLittleProximal", "LeftLittleIntermediate", "LeftLittleDistal"
            }},
            { HumanoidBoneGroup.RightFingers, new List<string> {
                "RightThumbProximal", "RightThumbIntermediate", "RightThumbDistal",
                "RightIndexProximal", "RightIndexIntermediate", "RightIndexDistal",
                "RightMiddleProximal", "RightMiddleIntermediate", "RightMiddleDistal",
                "RightRingProximal", "RightRingIntermediate", "RightRingDistal",
                "RightLittleProximal", "RightLittleIntermediate", "RightLittleDistal"
            }},
        };

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
        /// 指定されたHumanoidBoneGroupに属するボーン名のリストを取得
        /// </summary>
        public static List<string> GetBonesInGroup(HumanoidBoneGroup group)
        {
            if (group == HumanoidBoneGroup.All)
            {
                return BoneAliases.Keys.ToList();
            }

            if (BoneGroupMembers.TryGetValue(group, out var bones))
            {
                return bones;
            }

            return new List<string>();
        }

        /// <summary>
        /// 指定したボーンのエイリアスを取得
        /// </summary>
        public static List<string> GetBoneAliases(string boneName)
        {
            if (BoneAliases.TryGetValue(boneName, out var aliases))
            {
                return aliases;
            }
            return new List<string> { boneName };
        }

        /// <summary>
        /// 置換ルールリストを適用してsrcNameを変換
        /// </summary>
        /// <param name="srcName">変換元の名前</param>
        /// <param name="rules">置換ルールのリスト</param>
        /// <returns>変換後の名前</returns>
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
                // HumanoidBoneルールはTransformNameには適用しない（マッチングのみ）
            }

            return result;
        }

        /// <summary>
        /// srcNameとdstNameが（置換ルールを考慮して）マッチするかを判定
        /// </summary>
        /// <param name="srcName">ソース側の名前</param>
        /// <param name="dstName">デスティネーション側の名前</param>
        /// <param name="rules">置換ルールのリスト</param>
        /// <returns>マッチする場合はtrue</returns>
        public static bool NamesMatch(string srcName, string dstName, List<ReplacementRule> rules)
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

                if (CheckHumanoidBoneMatch(srcName, dstName, rule.boneGroup))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 辞書から置換ルールを考慮してマッチする子を検索
        /// </summary>
        /// <param name="childDic">子オブジェクトの辞書（名前からTransformへのマッピング）</param>
        /// <param name="srcName">ソースの名前</param>
        /// <param name="rules">置換ルール</param>
        /// <param name="matchedName">マッチした名前（存在する場合）</param>
        /// <returns>マッチが見つかった場合はtrue</returns>
        public static bool TryFindMatchingName(Dictionary<string, UnityEngine.Transform> childDic, string srcName, List<ReplacementRule> rules, out string matchedName)
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
                if (NamesMatch(srcName, key, rules))
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
        /// HumanoidBoneによるマッチをチェック
        /// </summary>
        private static bool CheckHumanoidBoneMatch(string srcName, string dstName, HumanoidBoneGroup group)
        {
            var bonesInGroup = GetBonesInGroup(group);

            foreach (var bone in bonesInGroup)
            {
                var aliases = GetBoneAliases(bone);

                bool srcIsAlias = aliases.Any(alias => string.Equals(alias, srcName, StringComparison.OrdinalIgnoreCase));
                bool dstIsAlias = aliases.Any(alias => string.Equals(alias, dstName, StringComparison.OrdinalIgnoreCase));

                if (srcIsAlias && dstIsAlias)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// HumanoidBoneのエイリアス定義を取得（テスト用）
        /// </summary>
        public static Dictionary<string, List<string>> GetHumanoidBoneAliases()
        {
            return new Dictionary<string, List<string>>(BoneAliases);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using CopyComponentsByRegex;

namespace CopyComponentsByRegex.Tests
{
    /// <summary>
    /// 実行環境にないコンポーネントの代替として使用するスタブ
    /// </summary>
    public class StubComponent : MonoBehaviour
    {
        public string originalTypeName;
        public string originalTypeFullName;
        public List<PropertyData> properties = new List<PropertyData>();
    }

    /// <summary>
    /// テスト用のダミーアバターデータを生成するユーティリティクラス
    /// </summary>
    public static class AvatarTestUtils
    {
        /// <summary>
        /// サンプルアバター（tmp6 28）のGameObject階層を生成します
        /// </summary>
        public static GameObject CreateSampleAvatar()
        {
            var root = new GameObject("SampleAvatar");
            root.hideFlags = HideFlags.HideAndDontSave;

            // コンポーネントの追加（スタブ）
            AddStub(root, "PipelineManager", "VRC.Core.PipelineManager");
            AddStub(root, "VRCAvatarDescriptor", "VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            AddStub(root, "ModularAvatarConvertConstraints", "nadena.dev.modular_avatar.core.ModularAvatarConvertConstraints");
            AddStub(root, "VRCPerPlatformOverrides", "VRC.SDK3.Avatars.Components.VRCPerPlatformOverrides");

            // 子階層の作成
            var avatarNode = CreateChild(root, "Avatar");
            AddStub(avatarNode, "ModularAvatarMeshSettings", "nadena.dev.modular_avatar.core.ModularAvatarMeshSettings");

            var bodyNode = CreateChild(root, "Body");
            AddStub(bodyNode, "ModularAvatarMeshSettings", "nadena.dev.modular_avatar.core.ModularAvatarMeshSettings");

            var armatureRoot = CreateChild(root, "root");
            AddStub(armatureRoot, "FloorAdjuster", "Narazaka.VRChat.FloorAdjuster.FloorAdjuster");

            // Humanoidボーン
            var hips = CreateChild(armatureRoot, "hips");
            var spine = CreateChild(hips, "spine");
            var chest = CreateChild(spine, "chest");
            var neck = CreateChild(chest, "neck");
            var head = CreateChild(neck, "head");

            var leftUpperLeg = CreateChild(hips, "leftUpperLeg");
            var leftLowerLeg = CreateChild(leftUpperLeg, "leftLowerLeg");
            var leftFoot = CreateChild(leftLowerLeg, "leftFoot");

            var rightUpperLeg = CreateChild(hips, "rightUpperLeg");
            var rightLowerLeg = CreateChild(rightUpperLeg, "rightLowerLeg");
            var rightFoot = CreateChild(rightLowerLeg, "rightFoot");

            var leftUpperArm = CreateChild(chest, "leftUpperArm");
            var leftLowerArm = CreateChild(leftUpperArm, "leftLowerArm");
            var leftHand = CreateChild(leftLowerArm, "leftHand");

            var rightUpperArm = CreateChild(chest, "rightUpperArm");
            var rightLowerArm = CreateChild(rightUpperArm, "rightLowerArm");
            var rightHand = CreateChild(rightLowerArm, "rightHand");
            
            // PhysBoneを持つ階層
            var apron = CreateChild(spine, "apron_skirt");
            
            var apronB = CreateChild(apron, "apron_skirt.B");
            AddStub(apronB, "VRCPhysBone", "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone");
            var apronB001 = CreateChild(apronB, "apron_skirt.B.001");
            var apronB002 = CreateChild(apronB001, "apron_skirt.B.002");
            CreateChild(apronB002, "apron_skirt.B.002_end");

            var apronF = CreateChild(apron, "apron_skirt.F");
            AddStub(apronF, "VRCPhysBone", "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone");
            var apronF001 = CreateChild(apronF, "apron_skirt.F.001");
            var apronF002 = CreateChild(apronF001, "apron_skirt.F.002");
            CreateChild(apronF002, "apron_skirt.F.002_end");

            return root;
        }

        /// <summary>
        /// サンプルアバターのHumanoidボーンマッピングを取得します
        /// </summary>
        public static Dictionary<string, HumanBodyBones> GetSampleBoneMapping()
        {
            return new Dictionary<string, HumanBodyBones>
            {
                { "hips", HumanBodyBones.Hips },
                { "spine", HumanBodyBones.Spine },
                { "chest", HumanBodyBones.Chest },
                { "neck", HumanBodyBones.Neck },
                { "head", HumanBodyBones.Head },
                { "leftUpperLeg", HumanBodyBones.LeftUpperLeg },
                { "leftLowerLeg", HumanBodyBones.LeftLowerLeg },
                { "leftFoot", HumanBodyBones.LeftFoot },
                { "rightUpperLeg", HumanBodyBones.RightUpperLeg },
                { "rightLowerLeg", HumanBodyBones.RightLowerLeg },
                { "rightFoot", HumanBodyBones.RightFoot },
                { "leftUpperArm", HumanBodyBones.LeftUpperArm },
                { "leftLowerArm", HumanBodyBones.LeftLowerArm },
                { "leftHand", HumanBodyBones.LeftHand },
                { "rightUpperArm", HumanBodyBones.RightUpperArm },
                { "rightLowerArm", HumanBodyBones.RightLowerArm },
                { "rightHand", HumanBodyBones.RightHand },
            };
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetParent(parent.transform);
            return go;
        }

        private static void AddStub(GameObject go, string typeName, string typeFullName)
        {
            var stub = go.AddComponent<StubComponent>();
            stub.originalTypeName = typeName;
            stub.originalTypeFullName = typeFullName;
        }

        /// <summary>
        /// GameObjectを再帰的にクローンします
        /// </summary>
        /// <param name="source">クローン元のGameObject</param>
        /// <param name="newName">新しいルート名（nullの場合は元の名前を使用）</param>
        /// <returns>クローンされたGameObject</returns>
        public static GameObject CloneAvatar(GameObject source, string newName = null)
        {
            var clone = Object.Instantiate(source);
            clone.hideFlags = HideFlags.HideAndDontSave;
            if (!string.IsNullOrEmpty(newName))
            {
                clone.name = newName;
            }
            return clone;
        }

        /// <summary>
        /// HumanoidBoneに該当するオブジェクトをリネームします
        /// </summary>
        /// <param name="root">ルートGameObject</param>
        /// <param name="renameMap">リネームマップ（元の名前 → 新しい名前）</param>
        public static void RenameHumanoidBones(GameObject root, Dictionary<string, string> renameMap)
        {
            RenameRecursive(root.transform, renameMap);
        }

        private static void RenameRecursive(Transform current, Dictionary<string, string> renameMap)
        {
            if (renameMap.TryGetValue(current.name, out var newName))
            {
                current.name = newName;
            }

            foreach (Transform child in current)
            {
                RenameRecursive(child, renameMap);
            }
        }

        /// <summary>
        /// VRoid Studio形式のボーン名に変換するためのマップを取得します
        /// </summary>
        public static Dictionary<string, string> GetVRoidBoneRenameMap()
        {
            return new Dictionary<string, string>
            {
                { "hips", "J_Bip_C_Hips" },
                { "spine", "J_Bip_C_Spine" },
                { "chest", "J_Bip_C_Chest" },
                { "neck", "J_Bip_C_Neck" },
                { "head", "J_Bip_C_Head" },
                { "leftUpperLeg", "J_Bip_L_UpperLeg" },
                { "leftLowerLeg", "J_Bip_L_LowerLeg" },
                { "leftFoot", "J_Bip_L_Foot" },
                { "rightUpperLeg", "J_Bip_R_UpperLeg" },
                { "rightLowerLeg", "J_Bip_R_LowerLeg" },
                { "rightFoot", "J_Bip_R_Foot" },
                { "leftUpperArm", "J_Bip_L_UpperArm" },
                { "leftLowerArm", "J_Bip_L_LowerArm" },
                { "leftHand", "J_Bip_L_Hand" },
                { "rightUpperArm", "J_Bip_R_UpperArm" },
                { "rightLowerArm", "J_Bip_R_LowerArm" },
                { "rightHand", "J_Bip_R_Hand" },
            };
        }

        /// <summary>
        /// VRoid形式のボーン名に対応するHumanoidマッピングを取得します
        /// </summary>
        public static Dictionary<string, HumanBodyBones> GetVRoidBoneMapping()
        {
            return new Dictionary<string, HumanBodyBones>
            {
                { "J_Bip_C_Hips", HumanBodyBones.Hips },
                { "J_Bip_C_Spine", HumanBodyBones.Spine },
                { "J_Bip_C_Chest", HumanBodyBones.Chest },
                { "J_Bip_C_Neck", HumanBodyBones.Neck },
                { "J_Bip_C_Head", HumanBodyBones.Head },
                { "J_Bip_L_UpperLeg", HumanBodyBones.LeftUpperLeg },
                { "J_Bip_L_LowerLeg", HumanBodyBones.LeftLowerLeg },
                { "J_Bip_L_Foot", HumanBodyBones.LeftFoot },
                { "J_Bip_R_UpperLeg", HumanBodyBones.RightUpperLeg },
                { "J_Bip_R_LowerLeg", HumanBodyBones.RightLowerLeg },
                { "J_Bip_R_Foot", HumanBodyBones.RightFoot },
                { "J_Bip_L_UpperArm", HumanBodyBones.LeftUpperArm },
                { "J_Bip_L_LowerArm", HumanBodyBones.LeftLowerArm },
                { "J_Bip_L_Hand", HumanBodyBones.LeftHand },
                { "J_Bip_R_UpperArm", HumanBodyBones.RightUpperArm },
                { "J_Bip_R_LowerArm", HumanBodyBones.RightLowerArm },
                { "J_Bip_R_Hand", HumanBodyBones.RightHand },
            };
        }
    }
}

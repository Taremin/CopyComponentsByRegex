using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

// テストアセンブリからinternalメンバーへのアクセスを許可
[assembly: InternalsVisibleTo("CopyComponentsByRegex.Tests")]

namespace CopyComponentsByRegex
{
    /// <summary>
    /// パッケージ情報を格納するクラス（package.jsonの読み込み用）
    /// </summary>
    [System.Serializable]
    internal class PackageInfo
    {
        public string name;
        public string displayName;
        public string version;
        public string unity;
        public string description;
    }

    /// <summary>
    /// コンポーネントのコピー・ペースト・削除を行うロジッククラス
    /// GUI（CopyComponentsByRegexWindow）から分離された純粋なロジック
    /// </summary>
    public static class ComponentCopier
    {
        // 状態変数
        internal static GameObject activeObject;
        internal static TreeItem copyTree = null;
        internal static Transform root = null;
        internal static List<Transform> transforms = null;
        internal static List<Component> components = null;
        internal static List<ModificationEntry> modificationLogs = new List<ModificationEntry>();
        internal static List<ModificationEntry> modificationObjectLogs = new List<ModificationEntry>();

        // 設定（GUIから同期される）
        internal static bool isRemoveBeforeCopy = false;
        internal static bool isObjectCopy = false;
        internal static bool isObjectCopyMatchOnly = false;
        internal static bool isClothNNS = false;
        internal static bool copyTransform = false;
        internal static bool showReportAfterPaste = false;

        // 置換ルールとHumanoidボーンマッピング
        internal static List<ReplacementRule> replacementRules = new List<ReplacementRule>();
        internal static Dictionary<string, HumanBodyBones> srcBoneMapping = null;
        internal static Dictionary<string, HumanBodyBones> dstBoneMapping = null;

        /// <summary>
        /// コピー操作を実行
        /// </summary>
        public static void Copy(GameObject source, CopySettings settings)
        {
            // 状態の初期化
            copyTree = new TreeItem(source);
            root = source.transform;
            transforms = new List<Transform>();
            components = new List<Component>();

            // 設定の同期
            copyTransform = settings.copyTransform;

            // コピー元のHumanoidマッピングを取得
            var srcAnimator = source.GetComponent<Animator>();
            srcBoneMapping = NameMatcher.GetBoneMapping(srcAnimator);

            // 正規表現でコンポーネントを収集
            var regex = new Regex(settings.pattern);
            CopyWalkdown(source, ref copyTree, ref regex);
        }

        /// <summary>
        /// ペースト操作を実行
        /// </summary>
        public static void Paste(GameObject destination, CopySettings settings)
        {
            if (copyTree == null || root == null)
            {
                return;
            }

            // コピー先のHumanoidマッピングを取得
            var dstAnimator = destination.GetComponent<Animator>();
            dstBoneMapping = NameMatcher.GetBoneMapping(dstAnimator);

            // srcBoneMappingが設定されていない場合は再取得
            if ((srcBoneMapping == null || srcBoneMapping.Count == 0) && copyTree?.gameObject != null)
            {
                var srcAnimator = copyTree.gameObject.GetComponent<Animator>();
                srcBoneMapping = NameMatcher.GetBoneMapping(srcAnimator);
            }

            // 設定の同期
            isRemoveBeforeCopy = settings.isRemoveBeforeCopy;
            isObjectCopy = settings.isObjectCopy;
            isObjectCopyMatchOnly = settings.isObjectCopyMatchOnly;
            isClothNNS = settings.isClothNNS;
            showReportAfterPaste = settings.showReportAfterPaste;
            replacementRules = settings.replacementRules ?? new List<ReplacementRule>();

            // ログのクリア
            modificationLogs.Clear();
            modificationObjectLogs.Clear();

            // 削除処理
            if (isRemoveBeforeCopy)
            {
                RemoveWalkdown(destination, ref copyTree);
            }

            // オブジェクトコピー処理
            if (isObjectCopy)
            {
                CopyObjectWalkdown(root, ref copyTree);
            }

            // マージ処理
            MergeWalkdown(destination, ref copyTree);

            // 参照の更新
            UpdateProperties(destination.transform);

            // レポート表示
            if (showReportAfterPaste && (modificationLogs.Count > 0 || modificationObjectLogs.Count > 0))
            {
                ModificationReportPopup.Show(copyTree, destination, modificationLogs, modificationObjectLogs, isObjectCopy, false, settings);
            }
        }

        /// <summary>
        /// Dry Run（変更のプレビュー）を実行
        /// </summary>
        public static void DryRun(GameObject destination, CopySettings settings)
        {
            if (copyTree == null || root == null)
            {
                return;
            }

            // コピー先のHumanoidマッピングを取得
            var dstAnimator = destination.GetComponent<Animator>();
            dstBoneMapping = NameMatcher.GetBoneMapping(dstAnimator);

            // srcBoneMappingが設定されていない場合は再取得
            if ((srcBoneMapping == null || srcBoneMapping.Count == 0) && copyTree?.gameObject != null)
            {
                var srcAnimator = copyTree.gameObject.GetComponent<Animator>();
                srcBoneMapping = NameMatcher.GetBoneMapping(srcAnimator);
            }

            // 設定の同期
            isRemoveBeforeCopy = settings.isRemoveBeforeCopy;
            isObjectCopy = settings.isObjectCopy;
            isObjectCopyMatchOnly = settings.isObjectCopyMatchOnly;
            replacementRules = settings.replacementRules ?? new List<ReplacementRule>();

            // ログのクリア
            modificationLogs.Clear();
            modificationObjectLogs.Clear();

            // 削除処理（Dry Run）
            if (isRemoveBeforeCopy)
            {
                RemoveWalkdown(destination, ref copyTree, 0, true);
            }

            // オブジェクトコピー処理（Dry Run）
            if (isObjectCopy)
            {
                CopyObjectWalkdown(root, ref copyTree, true);
            }

            // マージ処理（Dry Run）
            MergeWalkdown(destination, ref copyTree, 0, true);

            // レポート表示
            if (modificationLogs.Count > 0 || modificationObjectLogs.Count > 0)
            {
                ModificationReportPopup.Show(copyTree, destination, modificationLogs, modificationObjectLogs, isObjectCopy, true, settings);
            }
        }

        /// <summary>
        /// 階層を走査してコンポーネントを収集
        /// </summary>
        internal static void CopyWalkdown(GameObject go, ref TreeItem tree, ref Regex regex, int depth = 0)
        {
            transforms.Add(go.transform);

            // Components
            foreach (Component component in go.GetComponents<Component>())
            {
                if (component == null || !regex.Match(component.GetType().ToString()).Success)
                {
                    continue;
                }
                if (component is Transform && !copyTransform)
                {
                    continue;
                }
                tree.components.Add(component);
            }

            // Children
            var children = GetChildren(go);
            foreach (Transform child in children)
            {
                var node = new TreeItem(child.gameObject);
                tree.children.Add(node);
                CopyWalkdown(child.gameObject, ref node, ref regex, depth + 1);
            }
        }

        /// <summary>
        /// オブジェクトコピーの階層走査
        /// </summary>
        internal static void CopyObjectWalkdown(Transform src, ref TreeItem tree, bool dryRun = false)
        {
            foreach (TreeItem child in tree.children)
            {
                var next = child;
                if (!isObjectCopyMatchOnly || child.components.Count > 0)
                {
                    var route = SearchRoute(root, src);
                    if (route == null)
                    {
                        continue;
                    }
                    route.Add(child);
                    CopyObject(root, activeObject.transform, route, dryRun);
                }
                CopyObjectWalkdown(child.gameObject.transform, ref next, dryRun);
            }
        }

        /// <summary>
        /// ルートからdstへの経路を探索
        /// </summary>
        internal static List<TreeItem> SearchRoute(Transform rootTransform, Transform dst)
        {
            List<TreeItem> down = new List<TreeItem>();

            if (rootTransform == dst)
            {
                return down;
            }

            var current = dst;
            while (rootTransform != current)
            {
                down.Add(new TreeItem(current.gameObject));
                current = current.parent;
                if (current == null)
                {
                    return null;
                }
            }
            down.Reverse();

            return down;
        }

        /// <summary>
        /// オブジェクトをコピー
        /// </summary>
        internal static Transform CopyObject(Transform srcRoot, Transform dstRoot, List<TreeItem> route, bool dryRun = false)
        {
            var src = srcRoot;
            var dst = dstRoot;

            string currentPath = dstRoot.name;

            foreach (TreeItem current in route)
            {
                var item = current;
                Transform srcChild = null;
                Transform dstChild = null;

                // 置換ルールを考慮した子検索
                foreach (Transform child in GetChildren(src.gameObject))
                {
                    if (NameMatcher.NamesMatch(current.name, child.name, replacementRules, srcBoneMapping, dstBoneMapping))
                    {
                        srcChild = child;
                        break;
                    }
                }
                foreach (Transform child in GetChildren(dst.gameObject))
                {
                    if (NameMatcher.NamesMatch(current.name, child.name, replacementRules, srcBoneMapping, dstBoneMapping))
                    {
                        dstChild = child;
                        break;
                    }
                }

                currentPath += "/" + current.name;

                if (srcChild != null && dstChild == null)
                {
                    if (dryRun || showReportAfterPaste)
                    {
                        // 既にこの作成ログが記録されているか確認
                        if (!modificationObjectLogs.Any(x => x.targetPath == currentPath && x.operation == ModificationOperation.CreateObject))
                        {
                            modificationObjectLogs.Add(new ModificationEntry
                            {
                                targetPath = currentPath,
                                operation = ModificationOperation.CreateObject,
                                message = Localization.L("Created")
                            });
                        }
                        // Dry Runではオブジェクトを作成しないので、ここで中断
                        if (dryRun)
                        {
                            return null;
                        }
                    }

                    GameObject childObject = (GameObject)Object.Instantiate(srcChild.gameObject, dst);
                    childObject.name = current.name;
                    dst = dstChild = childObject.transform;

                    // ログに作成されたオブジェクトを紐付ける
                    if (showReportAfterPaste)
                    {
                        var log = modificationObjectLogs.FirstOrDefault(x => x.targetPath == currentPath && x.operation == ModificationOperation.CreateObject);
                        if (log != null)
                        {
                            log.createdObject = childObject;
                        }
                    }

                    // コピーしたオブジェクトに対しては自動的に同種コンポーネントの削除を行う
                    RemoveWalkdown(childObject, ref item);
                }
                else
                {
                    dst = dstChild;
                }
                src = srcChild;
            }

            return dst;
        }

        /// <summary>
        /// コンポーネント操作を実行
        /// </summary>
        internal static void RunComponentOperation(GameObject go, string componentType, ModificationOperation op, string message, bool dryRun, System.Func<Component> action)
        {
            bool logAdded = false;
            if (dryRun || showReportAfterPaste)
            {
                if (!string.IsNullOrEmpty(message))
                {
                    modificationLogs.Add(new ModificationEntry
                    {
                        targetObject = go,
                        componentType = componentType,
                        operation = op,
                        message = message
                    });
                    logAdded = true;
                }
            }

            if (dryRun)
            {
                return;
            }

            Component result = action();

            if (result != null)
            {
                components.Add(result);

                if (showReportAfterPaste && logAdded)
                {
                    var lastLog = modificationLogs.LastOrDefault();
                    if (lastLog != null && lastLog.targetObject == go && lastLog.componentType == componentType)
                    {
                        lastLog.createdComponent = result;
                    }
                }
            }
        }

        /// <summary>
        /// マージ処理の階層走査
        /// </summary>
        internal static void MergeWalkdown(GameObject go, ref TreeItem tree, int depth = 0, bool dryRun = false)
        {
            // 置換ルールを考慮した名前マッチング
            if (depth > 0 && !NameMatcher.NamesMatch(tree.name, go.name, replacementRules, srcBoneMapping, dstBoneMapping))
            {
                return;
            }

            // copy components
            foreach (Component component in tree.components)
            {
                var componentType = component.GetType();
                var targetComponent = go.GetComponent(componentType);

                // 1. Transform
                if (component is Transform)
                {
                    if (!copyTransform)
                    {
                        continue;
                    }

                    ModificationOperation op = ModificationOperation.None;
                    string msg = "";

                    if (targetComponent != null && (componentType.Name == "Transform" || componentType.Name == "RectTransform"))
                    {
                        op = ModificationOperation.Update;
                        msg = Localization.L("Updated");
                    }

                    RunComponentOperation(go, componentType.Name, op, msg, dryRun, () =>
                    {
                        UnityEditorInternal.ComponentUtility.CopyComponent(component);
                        if (targetComponent != null)
                        {
                            UnityEditorInternal.ComponentUtility.PasteComponentValues(targetComponent);
                            return targetComponent;
                        }
                        return null;
                    });
                    continue;
                }

                // 2. Cloth
                if (component is Cloth)
                {
                    ModificationOperation op = targetComponent != null ? ModificationOperation.Update : ModificationOperation.Add;
                    string msg = targetComponent != null
                        ? Localization.L("Updated") + (isClothNNS ? " (NNS)" : "")
                        : Localization.L("Added");

                    RunComponentOperation(go, componentType.Name, op, msg, dryRun, () =>
                    {
                        var cloth = go.GetComponent<Cloth>() == null ? go.AddComponent<Cloth>() : go.GetComponent<Cloth>();
                        CopyProperties(component, cloth);

                        // NNS / Coefficient Copy
                        var srcCloth = (component as Cloth);
                        var dstCloth = cloth;

                        if (dstCloth != null)
                        {
                            var srcCoefficients = srcCloth.coefficients;
                            var dstCoefficients = dstCloth.coefficients;

                            if (isClothNNS)
                            {
                                var srcVertices = srcCloth.vertices;
                                var dstVertices = dstCloth.vertices;

                                // build KD-Tree
                                var kdtree = new KDTree(
                                    srcVertices,
                                    0,
                                    (srcVertices.Length < srcCoefficients.Length ? srcVertices.Length : srcCoefficients.Length) - 1
                                );

                                for (int i = 0, il = dstCoefficients.Length, ml = dstVertices.Length; i < il && i < ml; ++i)
                                {
                                    var srcIdx = kdtree.FindNearest(dstVertices[i]);
                                    dstCoefficients[i].collisionSphereDistance = srcCoefficients[srcIdx].collisionSphereDistance;
                                    dstCoefficients[i].maxDistance = srcCoefficients[srcIdx].maxDistance;
                                }
                                dstCloth.coefficients = dstCoefficients;
                            }
                            else
                            {
                                if (srcCoefficients.Length == dstCoefficients.Length)
                                {
                                    for (int i = 0, il = srcCoefficients.Length; i < il; ++i)
                                    {
                                        dstCoefficients[i].collisionSphereDistance = srcCoefficients[i].collisionSphereDistance;
                                        dstCoefficients[i].maxDistance = srcCoefficients[i].maxDistance;
                                    }
                                    dstCloth.coefficients = dstCoefficients;
                                }
                            }
                        }
                        return cloth;
                    });
                    continue;
                }

                // 3. Other Components
                {
                    ModificationOperation op = ModificationOperation.Add;
                    string msg = targetComponent != null ? Localization.L("Added") : Localization.L("Added");

                    RunComponentOperation(go, componentType.Name, op, msg, dryRun, () =>
                    {
                        UnityEditorInternal.ComponentUtility.CopyComponent(component);
                        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);

                        Component[] comps = go.GetComponents<Component>();
                        Component dstComponent = comps[comps.Length - 1];
                        return dstComponent;
                    });
                }
            }

            // children
            var children = GetChildren(go);
            var childDic = new Dictionary<string, Transform>();
            foreach (Transform child in children)
            {
                childDic[child.gameObject.name] = child;
            }
            foreach (TreeItem treeChild in tree.children)
            {
                var next = treeChild;

                // 置換ルールを考慮した子検索
                if (!NameMatcher.TryFindMatchingName(childDic, treeChild.name, replacementRules, out string matchedName, srcBoneMapping, dstBoneMapping))
                {
                    continue;
                }

                Transform child = childDic[matchedName];

                if (child.gameObject.GetType().ToString() == treeChild.type)
                {
                    MergeWalkdown(child.gameObject, ref next, depth + 1, dryRun);
                }
            }
        }

        /// <summary>
        /// 削除処理の階層走査
        /// </summary>
        internal static void RemoveWalkdown(GameObject go, ref TreeItem tree, int depth = 0, bool dryRun = false)
        {
            // 置換ルールを考慮した名前マッチング
            if (depth > 0 && !NameMatcher.NamesMatch(tree.name, go.name, replacementRules, srcBoneMapping, dstBoneMapping))
            {
                return;
            }

            var componentsTypes = tree.components.Select(component => component.GetType()).Distinct();

            // remove components
            foreach (Component component in go.GetComponents<Component>())
            {
                if (component != null && componentsTypes.Contains(component.GetType()))
                {
                    if (component is UnityEngine.Transform)
                    {
                        continue;
                    }

                    if (dryRun || showReportAfterPaste)
                    {
                        modificationLogs.Add(new ModificationEntry
                        {
                            targetObject = go,
                            componentType = component.GetType().Name,
                            operation = ModificationOperation.Remove,
                            message = Localization.L("Removed")
                        });

                        if (dryRun)
                        {
                            continue;
                        }
                    }

                    Object.DestroyImmediate(component);
                }
            }

            // children
            var children = GetChildren(go);
            foreach (Transform child in children)
            {
                TreeItem next = null;
                foreach (TreeItem treeChild in tree.children)
                {
                    // 置換ルールを考慮した名前マッチング
                    if (
                        NameMatcher.NamesMatch(treeChild.name, child.gameObject.name, replacementRules, srcBoneMapping, dstBoneMapping) &&
                        child.gameObject.GetType().ToString() == treeChild.type
                    )
                    {
                        next = treeChild;
                        RemoveWalkdown(child.gameObject, ref next, depth + 1, dryRun);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 子オブジェクトを取得
        /// </summary>
        internal static Transform[] GetChildren(GameObject go)
        {
            int count = go.transform.childCount;
            var children = new Transform[count];

            for (int i = 0; i < count; ++i)
            {
                children[i] = go.transform.GetChild(i);
            }

            return children;
        }

        /// <summary>
        /// SerializedPropertyを使用してプロパティをコピー
        /// </summary>
        internal static void CopyProperties(Component srcComponent, Component dstComponent)
        {
            var dst = new SerializedObject(dstComponent);
            var src = new SerializedObject(srcComponent);

            dst.Update();
            src.Update();

            var iter = src.GetIterator();
            while (iter.NextVisible(true))
            {
                dst.CopyFromSerializedProperty(iter);
            }
            dst.ApplyModifiedProperties();
        }

        /// <summary>
        /// コピー後の参照を更新
        /// </summary>
        internal static void UpdateProperties(Transform dstRoot)
        {
            foreach (Component dstComponent in components)
            {
                if (dstComponent == null)
                {
                    continue;
                }

                var so = new SerializedObject(dstComponent);
                so.Update();
                var iter = so.GetIterator();

                // Object Reference
                while (iter.NextVisible(true))
                {
                    if (iter.propertyType.ToString() != "ObjectReference")
                    {
                        continue;
                    }

                    SerializedProperty property = so.FindProperty(iter.propertyPath);
                    var dstObjectReference = property.objectReferenceValue;
                    if (dstObjectReference == null)
                    {
                        continue;
                    }
                    if (!(dstObjectReference is Transform || dstObjectReference is Component))
                    {
                        continue;
                    }

                    Transform srcTransform = null;
                    if (dstObjectReference is Component)
                    {
                        srcTransform = (dstObjectReference as Component).transform;
                    }
                    else if (dstObjectReference is Transform)
                    {
                        srcTransform = dstObjectReference as Transform;
                    }

                    // ObjectReferenceの参照先がコピー内に存在するか
                    if (!transforms.Contains(srcTransform))
                    {
                        continue;
                    }

                    // コピー元のルートからObjectReferenceの位置への経路を探り、コピー後のツリーから該当オブジェクトを探す
                    var routes = SearchRoute(root, srcTransform);
                    if (routes == null)
                    {
                        continue;
                    }
                    Transform current = dstRoot;
                    foreach (var route in routes)
                    {
                        // 次の子を探す(TreeItemの名前と型で経路と同じ子を探す)
                        var children = GetChildren(current.gameObject);
                        if (children.Length < 1)
                        {
                            current = null;
                            break;
                        }
                        Transform next = null;
                        foreach (Transform child in children)
                        {
                            var treeitem = new TreeItem(child.gameObject);
                            if (treeitem.name == route.name && treeitem.type == route.type)
                            {
                                next = child;
                                break;
                            }
                        }
                        if (next == null)
                        {
                            current = null;
                            break;
                        }

                        current = next;
                    }
                    if (current == null)
                    {
                        continue;
                    }

                    if (dstObjectReference is Transform)
                    {
                        property.objectReferenceValue = current;
                    }
                    else if (dstObjectReference is Component)
                    {
                        Component comp = (Component)dstObjectReference;
                        var componentChildren = current.GetComponents(dstObjectReference.GetType());
                        var index = GetReferenceIndex(ref srcTransform, ref comp);

                        if (!SearchObjectReference(ref copyTree, ref comp))
                        {
                            continue;
                        }
                        if (index < 0)
                        {
                            continue;
                        }

                        property.objectReferenceValue = componentChildren[index];
                    }
                }
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// コンポーネントのインデックスを取得
        /// </summary>
        private static int GetReferenceIndex(ref Transform current, ref Component component)
        {
            var children = current.GetComponents(component.GetType());
            int i = children.Length;

            while (--i >= 0)
            {
                if (children[i] == component)
                {
                    break;
                }
            }

            return i;
        }

        /// <summary>
        /// TreeItem内でコンポーネントを検索
        /// </summary>
        private static bool SearchObjectReference(ref TreeItem treeitem, ref Component component)
        {
            if (treeitem.components.Contains(component))
            {
                return true;
            }
            for (int i = 0, il = treeitem.children.Count(); i < il; ++i)
            {
                var child = treeitem.children[i];
                if (SearchObjectReference(ref child, ref component))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

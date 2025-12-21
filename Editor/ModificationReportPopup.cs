using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CopyComponentsByRegex
{
    public class ModificationReportPopup : EditorWindow
    {
        private TreeItem copyTree;
        private GameObject activeObject;
        private List<ModificationEntry> modificationLogs;
        private List<ModificationEntry> modificationObjectLogs;
        private bool isObjectCopy;
        private bool isDryRun;
        private Vector2 scrollPosition;
        private bool showAllComponents = false;

        public static void Show(TreeItem copyTree, GameObject activeObject, List<ModificationEntry> modificationLogs, List<ModificationEntry> modificationObjectLogs, bool isObjectCopy, bool isDryRun)
        {
            var window = CreateInstance<ModificationReportPopup>();
            string title = isDryRun ? "Dry Run Report" : "Modification Report";
            window.titleContent = new GUIContent(title);
            window.copyTree = copyTree;
            window.activeObject = activeObject;
            window.modificationLogs = modificationLogs;
            window.modificationObjectLogs = modificationObjectLogs;
            window.isObjectCopy = isObjectCopy;
            window.isDryRun = isDryRun;
            
            window.ShowUtility(); 
        }

        private Dictionary<object, Rect> sourceRects = new Dictionary<object, Rect>();

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                sourceRects.Clear();
            }

            using (new GUILayout.VerticalScope())
            {

                EditorGUILayout.LabelField(isDryRun ? "Dry Run Report" : "Modification Report", EditorStyles.boldLabel);
                showAllComponents = EditorGUILayout.ToggleLeft("全てのコンポーネントを表示", showAllComponents);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                try
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        // Source Tree
                        using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(position.width * 0.5f)))
                        {
                            if (copyTree != null)
                            {
                                EditorGUILayout.LabelField($"Source: {copyTree.name}", EditorStyles.boldLabel);
                                DrawSourceTree(copyTree, 0, activeObject != null ? activeObject.name : "");
                            }
                        }

                        // Destination Tree
                        using (new GUILayout.VerticalScope(GUI.skin.box))
                        {
                            if (activeObject != null)
                            {
                                EditorGUILayout.LabelField($"Destination: {activeObject.name}", EditorStyles.boldLabel);
                                DrawDestinationTree(activeObject, copyTree);
                            }
                        }
                    }
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawSourceTree(TreeItem item, int depth, string path)
        {
            EditorGUI.indentLevel = depth;
            var goIcon = EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image;
            var content = new GUIContent(item.name, goIcon);
            
            bool isCreated = modificationObjectLogs.Any(x => x.targetPath == path && x.operation == ModificationOperation.CreateObject);
            var oldColor = GUI.color;
            if (isCreated)
            {
                GUI.color = Color.cyan;
            }
            EditorGUILayout.LabelField(content, EditorStyles.label);
            GUI.color = oldColor;

            if (Event.current.type == EventType.Repaint)
            {
                var rect = GUILayoutUtility.GetLastRect();
                // Capture accurate text position
                sourceRects[item] = GetContentRect(rect, content, EditorStyles.label);
            }

            EditorGUI.indentLevel = depth + 1;

            if (item.gameObject != null)
            {
                var allComponents = item.gameObject.GetComponents<Component>();
                foreach (var comp in allComponents)
                {
                    if (comp == null) continue;

                    bool isCopyTarget = item.components.Contains(comp);

                    if (!isCopyTarget && !showAllComponents) continue;

                    var compIcon = EditorGUIUtility.ObjectContent(null, comp.GetType()).image;
                    if (compIcon == null)
                    {
                        compIcon = EditorGUIUtility.ObjectContent(null, typeof(Component)).image;
                    }
                    var compContent = new GUIContent(comp.GetType().Name, compIcon);

                    var oldCompColor = GUI.color;
                    if (isCopyTarget)
                    {
                        GUI.color = Color.cyan;
                    }
                    EditorGUILayout.LabelField(compContent, EditorStyles.label);
                    GUI.color = oldCompColor;

                    // Only register rect for copy targets to allow drawing links
                    if (isCopyTarget && Event.current.type == EventType.Repaint)
                    {
                        var rect = GUILayoutUtility.GetLastRect();
                        sourceRects[comp] = GetContentRect(rect, compContent, EditorStyles.label);
                    }
                }
            }
            else
            {
                foreach (var comp in item.components)
                {
                    if (comp != null)
                    {
                        var compIcon = EditorGUIUtility.ObjectContent(null, comp.GetType()).image;
                        if (compIcon == null)
                        {
                            compIcon = EditorGUIUtility.ObjectContent(null, typeof(Component)).image;
                        }
                        var compContent = new GUIContent(comp.GetType().Name, compIcon);

                        var oldCompColor = GUI.color;
                        GUI.color = Color.cyan;
                        EditorGUILayout.LabelField(compContent, EditorStyles.label);
                        GUI.color = oldCompColor;

                        if (Event.current.type == EventType.Repaint)
                        {
                            var rect = GUILayoutUtility.GetLastRect();
                            sourceRects[comp] = GetContentRect(rect, compContent, EditorStyles.label);
                        }
                    }
                }
            }

            foreach (var child in item.children)
            {
                DrawSourceTree(child, depth + 1, path + "/" + child.name);
            }
            EditorGUI.indentLevel = 0;
        }

        private void DrawDestinationTree(GameObject go, TreeItem sourceItem, int depth = 0)
        {
            if (sourceItem != null && go.name != sourceItem.name && depth > 0) return;

            EditorGUI.indentLevel = depth;

            var goIcon = EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image;
            var goContent = new GUIContent(go.name, goIcon);

            // Check if this object was created
            var createLog = modificationObjectLogs.FirstOrDefault(x => x.createdObject == go);
            if (createLog != null) {
                var oldColor = GUI.color;
                GUI.color = Color.green;
                goContent.text = $"[+] {go.name} ({createLog.message})";
                EditorGUILayout.LabelField(goContent, EditorStyles.label);
                GUI.color = oldColor;

                if (Event.current.type == EventType.Repaint && sourceItem != null && sourceRects.ContainsKey(sourceItem))
                {
                    var dstRect = GetContentRect(GUILayoutUtility.GetLastRect(), goContent, EditorStyles.label);
                    DrawLink(sourceRects[sourceItem], dstRect);
                }
            } else {
                EditorGUILayout.LabelField(goContent, EditorStyles.label);
            }

            // Don't draw link for existing GameObject containers to avoid clutter
            /*
            if (Event.current.type == EventType.Repaint) {
                var dstRect = GetContentRect(GUILayoutUtility.GetLastRect(), goContent, EditorStyles.label);
                if (sourceItem != null && sourceRects.ContainsKey(sourceItem))
                {
                    DrawLink(sourceRects[sourceItem], dstRect);
                }
            }
            */

            EditorGUI.indentLevel = depth + 1;
            var components = go.GetComponents<Component>();

            // このオブジェクトに対してコンポーネントの追加が行われるかチェック
            bool hasAddOperation = modificationLogs.Any(x => x.targetObject == go && x.operation == ModificationOperation.Add);

            // コピー先に既存のコンポーネント
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var typeName = comp.GetType().Name;

                var log = modificationLogs.FirstOrDefault(x => x.targetObject == go && x.componentType == typeName);
                
                // If this component was just created, prioritize its specific log
                var compCreateLog = modificationLogs.FirstOrDefault(x => x.createdComponent == comp);
                if (compCreateLog != null) {
                    log = compCreateLog;
                }

                GUIContent content = null;

                if (log != null && log.operation == ModificationOperation.Remove)
                {
                    var oldColor = GUI.color;
                    GUI.color = Color.red;
                    var icon = GetIcon(comp);
                    content = new GUIContent($"[-] {typeName} ({log.message})", icon);
                    EditorGUILayout.LabelField(content, EditorStyles.label);
                    GUI.color = oldColor;
                }
                else if (log != null && log.operation == ModificationOperation.Update)
                {
                    var oldColor = GUI.color;
                    GUI.color = Color.yellow;
                    var icon = GetIcon(comp);
                    content = new GUIContent($"[*] {typeName} ({log.message})", icon);
                    EditorGUILayout.LabelField(content, EditorStyles.label);
                    GUI.color = oldColor;
                }
                else if (log != null && log.operation == ModificationOperation.Add)
                {
                    var oldColor = GUI.color;
                    GUI.color = Color.green;
                    var icon = GetIcon(comp);
                    content = new GUIContent($"[+] {typeName} ({log.message})", icon);
                    EditorGUILayout.LabelField(content, EditorStyles.label);
                    GUI.color = oldColor;
                }
                else if (showAllComponents || hasAddOperation)
                {
                    var icon = GetIcon(comp);
                    content = new GUIContent(typeName, icon);
                    EditorGUILayout.LabelField(content, EditorStyles.label);
                }

                // Draw Link only if there is an operation log
                if (log != null && Event.current.type == EventType.Repaint && sourceItem != null && content != null)
                {
                    var dstRect = GetContentRect(GUILayoutUtility.GetLastRect(), content, EditorStyles.label);
                    // Match with first source component of same type
                    var srcComp = sourceItem.components.FirstOrDefault(c => c != null && c.GetType() == comp.GetType());
                    if (srcComp != null && sourceRects.ContainsKey(srcComp))
                    {
                        DrawLink(sourceRects[srcComp], dstRect);
                    }
                }
            }

            // 新規追加されるコンポーネント (Ghost)
            // DryRun時、または作成されたコンポーネントが特定できない場合のみ表示
            foreach (var log in modificationLogs.Where(x => x.targetObject == go && x.operation == ModificationOperation.Add))
            {
                if (!isDryRun && log.createdComponent != null) {
                    continue;
                }
                var oldColor = GUI.color;
                GUI.color = Color.green;
                Texture icon = EditorGUIUtility.ObjectContent(null, typeof(Component)).image;
                var content = new GUIContent($"[+] {log.componentType} ({log.message})", icon);
                EditorGUILayout.LabelField(content, EditorStyles.label);
                GUI.color = oldColor;

                // Draw Link for added component
                if (Event.current.type == EventType.Repaint && sourceItem != null)
                {
                    var dstRect = GetContentRect(GUILayoutUtility.GetLastRect(), content, EditorStyles.label);
                    var srcComp = sourceItem.components.FirstOrDefault(c => c != null && c.GetType().Name == log.componentType);
                    if (srcComp != null && sourceRects.ContainsKey(srcComp))
                    {
                        DrawLink(sourceRects[srcComp], dstRect);
                    }
                }
            }


            // 子オブジェクト
            var children = GetChildren(go);
            var childDic = children.ToDictionary(c => c.name);

            if (sourceItem != null)
            {
                foreach (var sourceChild in sourceItem.children)
                {
                    if (childDic.ContainsKey(sourceChild.name))
                    {
                        DrawDestinationTree(childDic[sourceChild.name].gameObject, sourceChild, depth + 1);
                    }
                    else
                    {
                        // この子オブジェクトはコピー元には存在するがコピー先には存在しない -> isObjectCopyがtrueの場合作成される
                        if (isObjectCopy)
                        {
                            EditorGUI.indentLevel = depth + 1;
                            var oldColor = GUI.color;
                            GUI.color = Color.green;
                            var newGoIcon = EditorGUIUtility.ObjectContent(null, typeof(GameObject)).image;
                            var content = new GUIContent($"[+] {sourceChild.name} (新規オブジェクトなど)", newGoIcon);
                            EditorGUILayout.LabelField(content, EditorStyles.label);
                            GUI.color = oldColor;
                            
                            // Draw Link for new object
                            if (Event.current.type == EventType.Repaint)
                            {
                                var dstRect = GetContentRect(GUILayoutUtility.GetLastRect(), content, EditorStyles.label);
                                if (sourceRects.ContainsKey(sourceChild))
                                {
                                    DrawLink(sourceRects[sourceChild], dstRect);
                                }
                            }
                        }
                    }
                }
            }

            EditorGUI.indentLevel = 0;
        }

        private Rect GetContentRect(Rect rect, GUIContent content, GUIStyle style)
        {
            var indentedRect = EditorGUI.IndentedRect(rect);
            
            // Calculate Text Width
            var tempStyle = new GUIStyle(style);
            tempStyle.fixedWidth = 0;
            tempStyle.stretchWidth = false;
            float textWidth = tempStyle.CalcSize(new GUIContent(content.text)).x;

            // Calculate Icon Width
            float iconWidth = 0f;
            if (content.image != null)
            {
                var iconSize = EditorGUIUtility.GetIconSize();
                // If GetIconSize is zero, it means use default (usually 16x16 in Editor)
                float width = (iconSize.x > 0) ? iconSize.x : 16f;
                iconWidth = width + 4f; // + padding
            }

            return new Rect(indentedRect.x, indentedRect.y, textWidth + iconWidth, indentedRect.height);
        }
        
        private void DrawLink(Rect src, Rect dst)
        {
            var startPos = new Vector3(src.xMax, src.center.y, 0);
            var endPos = new Vector3(dst.xMin, dst.center.y, 0);
            
            var distance = Mathf.Abs(endPos.x - startPos.x);
            var startTan = startPos + Vector3.right * (distance * 0.5f);
            var endTan = endPos + Vector3.left * (distance * 0.5f);

            Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.cyan, null, 2f);
        }

        private Transform[] GetChildren(GameObject go)
        {
            int count = go.transform.childCount;
            var children = new Transform[count];

            for (int i = 0; i < count; ++i)
            {
                children[i] = go.transform.GetChild(i);
            }

            return children;
        }

        private Texture GetIcon(Component comp)
        {
            var icon = EditorGUIUtility.ObjectContent(null, comp.GetType()).image;
            if (icon == null)
            {
                icon = EditorGUIUtility.ObjectContent(null, typeof(Component)).image;
            }
            return icon;
        }
    }
}

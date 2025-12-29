using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CopyComponentsByRegex
{
    /// <summary>
    /// Copy Components By Regex のエディタウィンドウ
    /// GUIの描画とユーザー操作を担当
    /// </summary>
    public class CopyComponentsByRegexWindow : EditorWindow
    {
        private static string version = "";
        private Vector2 scrollPosition;

        // 設定
        private CopySettings settings = new CopySettings();
        private bool exportIncludeProperties = true;

        // コンポーネント一覧表示用
        private Vector2 componentListScrollPosition;

        private void OnEnable()
        {
            // バージョン情報の読み込み
            version = PathUtility.GetPackageVersion();

            // 設定の読み込み
            settings.Load();
            
            // 置換ルールをロジッククラスと同期
            ComponentCopier.replacementRules = settings.replacementRules;
        }

        private void OnSelectionChange()
        {
            var editorEvent = EditorGUIUtility.CommandEvent("ChangeActiveObject");
            editorEvent.type = EventType.Used;
            SendEvent(editorEvent);
        }

        /// <summary>
        /// メニューからウィンドウを表示
        /// </summary>
        [MenuItem("GameObject/Copy Components By Regex", false, 20)]
        public static void ShowWindow()
        {
            ComponentCopier.activeObject = Selection.activeGameObject;
            EditorWindow.GetWindow(typeof(CopyComponentsByRegexWindow));
        }



        private void OnGUI()
        {
            ComponentCopier.activeObject = Selection.activeGameObject;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            try
            {
                // バージョン表示
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField("CopyComponentsByRegex Version: " + version);
                }
                // 言語選択ドロップダウン
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(Localization.L("Language"), GUILayout.Width(50));
                    string[] languageOptions = { Localization.L("Language_English"), Localization.L("Language_Japanese"), Localization.L("Language_System") };
                    int currentLangIndex = (int)Localization.CurrentLanguageOption;
                    int newLangIndex = EditorGUILayout.Popup(currentLangIndex, languageOptions, GUILayout.Width(100));
                    if (newLangIndex != currentLangIndex)
                    {
                        Localization.CurrentLanguageOption = (LanguageOption)newLangIndex;
                        Repaint();
                    }
                }

                // アクティブオブジェクト表示
                EditorGUILayout.LabelField(Localization.L("ActiveObject"));
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField(ComponentCopier.activeObject ? ComponentCopier.activeObject.name : "");
                }
                if (!ComponentCopier.activeObject)
                {
                    return;
                }

                // コンポーネント一覧（折りたたみ）
                bool prevShowComponentList = settings.showComponentList;
                settings.showComponentList = EditorGUILayout.Foldout(settings.showComponentList, Localization.L("ComponentList"), true);
                if (prevShowComponentList != settings.showComponentList)
                {
                    settings.SaveSetting("showComponentList", settings.showComponentList.ToString());
                }

                if (settings.showComponentList)
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        componentListScrollPosition = EditorGUILayout.BeginScrollView(
                            componentListScrollPosition,
                            GUILayout.MaxHeight(150));

                        var components = ComponentCopier.activeObject.GetComponents<Component>();
                        foreach (var component in components)
                        {
                            if (component != null)
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    string typeName = component.GetType().Name;
                                    EditorGUILayout.LabelField(typeName);

                                    // クリップボードにコピーボタン
                                    if (GUILayout.Button(Localization.L("Copy"), GUILayout.Width(50)))
                                    {
                                        GUIUtility.systemCopyBuffer = typeName;
                                    }
                                }
                            }
                        }

                        EditorGUILayout.EndScrollView();
                    }
                }

                // 正規表現パターン
                settings.pattern = EditorGUILayout.TextField(Localization.L("RegexPattern"), settings.pattern);
                settings.SaveSetting("pattern", settings.pattern);

                // Copyボタン
                if (GUILayout.Button("Copy"))
                {
                    ComponentCopier.Copy(ComponentCopier.activeObject, settings);
                }

                // コピー元オブジェクト表示
                EditorGUILayout.LabelField(Localization.L("SourceObject"));
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField(ComponentCopier.root ? ComponentCopier.root.name : "");
                }

                // オプション: Transformコピー
                settings.copyTransform = GUILayout.Toggle(settings.copyTransform, Localization.Content("CopyTransform"));
                settings.SaveSetting("copyTransform", settings.copyTransform.ToString());
                ComponentCopier.copyTransform = settings.copyTransform;

                // オプション: 削除後コピー
                settings.isRemoveBeforeCopy = GUILayout.Toggle(settings.isRemoveBeforeCopy, Localization.Content("RemoveExisting"));
                settings.SaveSetting("isRemoveBeforeCopy", settings.isRemoveBeforeCopy.ToString());

                // オプション: オブジェクトコピー
                settings.isObjectCopy = GUILayout.Toggle(settings.isObjectCopy, Localization.Content("CreateMissing"));
                settings.SaveSetting("isObjectCopy", settings.isObjectCopy.ToString());

                if (settings.isObjectCopy)
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        settings.isObjectCopyMatchOnly = GUILayout.Toggle(settings.isObjectCopyMatchOnly, Localization.Content("MatchedOnly"));
                        settings.SaveSetting("isObjectCopyMatchOnly", settings.isObjectCopyMatchOnly.ToString());
                    }
                }

                // オプション: Cloth NNS
                settings.isClothNNS = GUILayout.Toggle(settings.isClothNNS, Localization.Content("ClothNNS"));
                settings.SaveSetting("isClothNNS", settings.isClothNNS.ToString());
                ComponentCopier.isClothNNS = settings.isClothNNS;

                // オプション: レポート表示
                settings.showReportAfterPaste = GUILayout.Toggle(settings.showReportAfterPaste, Localization.Content("ShowReport"));
                settings.SaveSetting("showReportAfterPaste", settings.showReportAfterPaste.ToString());
                ComponentCopier.showReportAfterPaste = settings.showReportAfterPaste;

                // 置換リストセクション
                DrawReplacementRulesSection();

                // HumanoidBone警告表示
                DrawHumanoidBoneWarning();

                // Pasteボタン
                if (GUILayout.Button("Paste"))
                {
                    ComponentCopier.Paste(ComponentCopier.activeObject, settings);
                }

                // Dry Runボタン
                if (GUILayout.Button("Dry Run"))
                {
                    ComponentCopier.DryRun(ComponentCopier.activeObject, settings);
                }

                // Export Debug Infoセクション（折りたたみ可能）
                EditorGUILayout.Space();
                bool prevShowDebugExport = settings.showDebugExport;
                settings.showDebugExport = EditorGUILayout.Foldout(settings.showDebugExport, Localization.L("DebugExport"), true);
                if (prevShowDebugExport != settings.showDebugExport)
                {
                    settings.SaveSetting("showDebugExport", settings.showDebugExport.ToString());
                }
                if (settings.showDebugExport)
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        // src（コピー元）とdst（コピー先）が両方揃っているかチェック
                        bool hasSrc = ComponentCopier.root != null && ComponentCopier.copyTree != null;
                        bool hasDst = ComponentCopier.activeObject != null;
                        bool canExport = hasSrc && hasDst;

                        if (!canExport)
                        {
                            string message = "";
                            if (!hasSrc && !hasDst)
                            {
                                message = Localization.L("CopyAndPasteRequired");
                            }
                            else if (!hasSrc)
                            {
                                message = Localization.L("CopySourceFirst");
                            }
                            else
                            {
                                message = Localization.L("SelectDestination");
                            }
                            EditorGUILayout.HelpBox(message, MessageType.Info);
                        }

                        exportIncludeProperties = EditorGUILayout.ToggleLeft(Localization.L("IncludeProperties"), exportIncludeProperties);
                        
                        using (new EditorGUI.DisabledScope(!canExport))
                        {
                            if (GUILayout.Button("Export Debug Info"))
                            {
                                BugReportExporter.ExportAndCopy(
                                    ComponentCopier.root.gameObject,
                                    ComponentCopier.copyTree,
                                    ComponentCopier.activeObject,
                                    settings,
                                    ComponentCopier.modificationLogs,
                                    ComponentCopier.modificationObjectLogs,
                                    exportIncludeProperties
                                );
                            }
                        }
                    }
                }

                // 注意書き（折りたたみ可能）
                bool prevShowNotes = settings.showNotes;
                settings.showNotes = EditorGUILayout.Foldout(settings.showNotes, Localization.L("Notes"), true);
                if (prevShowNotes != settings.showNotes)
                {
                    settings.SaveSetting("showNotes", settings.showNotes.ToString());
                }
                if (settings.showNotes)
                {
                    GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                    labelStyle.wordWrap = true;
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label(Localization.L("NotesContent"), labelStyle);
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 置換リストセクションを描画
        /// </summary>
        private void DrawReplacementRulesSection()
        {
            bool prevShowReplacementRules = settings.showReplacementRules;
            settings.showReplacementRules = EditorGUILayout.Foldout(settings.showReplacementRules, Localization.L("NameMappingRules"), true);
            if (prevShowReplacementRules != settings.showReplacementRules)
            {
                settings.SaveSetting("showReplacementRules", settings.showReplacementRules.ToString());
            }

            if (!settings.showReplacementRules)
            {
                return;
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                // ルール追加ボタン
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Localization.L("AddRegex"), GUILayout.Width(100)))
                    {
                        settings.replacementRules.Add(new ReplacementRule("", ""));
                    }
                    if (GUILayout.Button(Localization.L("AddHumanoidBone"), GUILayout.Width(120)))
                    {
                        settings.replacementRules.Add(new ReplacementRule(HumanoidBoneGroup.All));
                    }
                }

                // 各ルールを描画
                int indexToRemove = -1;
                for (int i = 0; i < settings.replacementRules.Count; i++)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        var rule = settings.replacementRules[i];

                        // 有効/無効チェックボックス
                        rule.enabled = GUILayout.Toggle(rule.enabled, "", GUILayout.Width(20));

                        // タイプ選択
                        var newType = (RuleType)EditorGUILayout.EnumPopup(rule.type, GUILayout.Width(100));
                        if (newType != rule.type)
                        {
                            rule.type = newType;
                        }

                        if (rule.type == RuleType.Regex)
                        {
                            // 正規表現の場合
                            GUILayout.Label(Localization.L("Find"), GUILayout.Width(35));
                            rule.srcPattern = GUILayout.TextField(rule.srcPattern, GUILayout.Width(100));
                            GUILayout.Label(Localization.L("Replace"), GUILayout.Width(35));
                            rule.dstPattern = GUILayout.TextField(rule.dstPattern, GUILayout.Width(100));
                        }
                        else
                        {
                            // HumanoidBoneの場合
                            string[] modeOptions = new string[] { Localization.L("Group"), Localization.L("Single") };
                            int currentModeIndex = rule.boneSelectionMode == HumanoidBoneSelectionMode.Group ? 0 : 1;
                            int newModeIndex = EditorGUILayout.Popup(currentModeIndex, modeOptions, GUILayout.Width(65));
                            rule.boneSelectionMode = newModeIndex == 0 ? HumanoidBoneSelectionMode.Group : HumanoidBoneSelectionMode.Individual;

                            if (rule.boneSelectionMode == HumanoidBoneSelectionMode.Group)
                            {
                                // グループ選択の場合
                                var displayNames = System.Enum.GetValues(typeof(HumanoidBoneGroup));
                                string[] options = new string[displayNames.Length];
                                int currentIndex = 0;
                                for (int j = 0; j < displayNames.Length; j++)
                                {
                                    var group = (HumanoidBoneGroup)displayNames.GetValue(j);
                                    options[j] = NameMatcher.BoneGroupDisplayNames[group];
                                    if (group == rule.boneGroup)
                                    {
                                        currentIndex = j;
                                    }
                                }
                                int newIndex = EditorGUILayout.Popup(currentIndex, options, GUILayout.Width(80));
                                rule.boneGroup = (HumanoidBoneGroup)displayNames.GetValue(newIndex);
                            }
                            else
                            {
                                // 個別選択の場合
                                rule.singleBone = (HumanBodyBones)EditorGUILayout.EnumPopup(rule.singleBone, GUILayout.Width(120));
                            }
                        }

                        // 削除ボタン
                        if (GUILayout.Button("-", GUILayout.Width(25)))
                        {
                            indexToRemove = i;
                        }
                    }
                }

                // 削除処理
                if (indexToRemove >= 0)
                {
                    settings.replacementRules.RemoveAt(indexToRemove);
                }

                if (settings.replacementRules.Count == 0)
                {
                    EditorGUILayout.HelpBox(Localization.L("NoRulesInfo"), MessageType.Info);
                }
            }

            // ロジッククラスと同期
            ComponentCopier.replacementRules = settings.replacementRules;
        }

        /// <summary>
        /// HumanoidBoneルールの警告を表示
        /// </summary>
        private void DrawHumanoidBoneWarning()
        {
            // HumanoidBoneルールが有効でない場合は何も表示しない
            if (!NameMatcher.HasHumanoidBoneRule(settings.replacementRules))
            {
                return;
            }

            // コピー元のHumanoid状態
            bool srcIsHumanoid = ComponentCopier.srcBoneMapping != null && ComponentCopier.srcBoneMapping.Count > 0;

            // コピー先のHumanoid状態
            bool dstIsHumanoid = NameMatcher.IsHumanoid(ComponentCopier.activeObject);

            // 警告が必要ない場合は何も表示しない
            if (srcIsHumanoid && dstIsHumanoid)
            {
                return;
            }

            // 警告メッセージを構築
            string warningMsg = "";
            if (ComponentCopier.root == null)
            {
                warningMsg = Localization.L("HumanoidWarning_CopyFirst");
            }
            else if (!srcIsHumanoid && !dstIsHumanoid)
            {
                warningMsg = Localization.L("HumanoidWarning_NeitherHumanoid");
            }
            else if (!srcIsHumanoid)
            {
                warningMsg = Localization.L("HumanoidWarning_SrcNotHumanoid");
            }
            else
            {
                warningMsg = Localization.L("HumanoidWarning_DstNotHumanoid");
            }

            EditorGUILayout.HelpBox(warningMsg, MessageType.Warning);
        }
    }
}

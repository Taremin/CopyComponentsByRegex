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

                // アクティブオブジェクト表示
                EditorGUILayout.LabelField("アクティブなオブジェクト");
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
                settings.showComponentList = EditorGUILayout.Foldout(settings.showComponentList, "コンポーネント一覧", true);
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
                                    if (GUILayout.Button("コピー", GUILayout.Width(50)))
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
                settings.pattern = EditorGUILayout.TextField("正規表現", settings.pattern);
                settings.SaveSetting("pattern", settings.pattern);

                // Copyボタン
                if (GUILayout.Button("Copy"))
                {
                    ComponentCopier.Copy(ComponentCopier.activeObject, settings);
                }

                // コピー中のオブジェクト表示
                EditorGUILayout.LabelField("コピー中のオブジェクト");
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField(ComponentCopier.root ? ComponentCopier.root.name : "");
                }

                // オプション: Transformコピー
                settings.copyTransform = GUILayout.Toggle(settings.copyTransform, "Transformがマッチした場合値をコピー");
                settings.SaveSetting("copyTransform", settings.copyTransform.ToString());
                ComponentCopier.copyTransform = settings.copyTransform;

                // オプション: 削除後コピー
                settings.isRemoveBeforeCopy = GUILayout.Toggle(settings.isRemoveBeforeCopy, "コピー先に同じコンポーネントがあったら削除");
                settings.SaveSetting("isRemoveBeforeCopy", settings.isRemoveBeforeCopy.ToString());

                // オプション: オブジェクトコピー
                settings.isObjectCopy = GUILayout.Toggle(settings.isObjectCopy, "コピー先にオブジェクトがなかったらオブジェクトをコピー");
                settings.SaveSetting("isObjectCopy", settings.isObjectCopy.ToString());

                if (settings.isObjectCopy)
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        settings.isObjectCopyMatchOnly = GUILayout.Toggle(settings.isObjectCopyMatchOnly, "マッチしたコンポーネントを持つオブジェクトのみコピー");
                        settings.SaveSetting("isObjectCopyMatchOnly", settings.isObjectCopyMatchOnly.ToString());
                    }
                }

                // オプション: Cloth NNS
                settings.isClothNNS = GUILayout.Toggle(settings.isClothNNS, "ClothコンポーネントのConstraintsを一番近い頂点からコピー");
                settings.SaveSetting("isClothNNS", settings.isClothNNS.ToString());
                ComponentCopier.isClothNNS = settings.isClothNNS;

                // オプション: レポート表示
                settings.showReportAfterPaste = GUILayout.Toggle(settings.showReportAfterPaste, "Paste時に結果を表示");
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
                settings.showDebugExport = EditorGUILayout.Foldout(settings.showDebugExport, "デバッグ情報エクスポート", true);
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
                                message = "CopyとPaste先の選択が必要です";
                            }
                            else if (!hasSrc)
                            {
                                message = "先にCopy操作を実行してください";
                            }
                            else
                            {
                                message = "Paste先オブジェクトを選択してください";
                            }
                            EditorGUILayout.HelpBox(message, MessageType.Info);
                        }

                        exportIncludeProperties = EditorGUILayout.ToggleLeft("プロパティ値を含める", exportIncludeProperties);
                        
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
                settings.showNotes = EditorGUILayout.Foldout(settings.showNotes, "注意書き", true);
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
                        GUILayout.Label(
                            "「一番近い頂点からコピー」を利用する場合はあらかじめClothのコピー先にClothを追加するか、" +
                            "最初はチェックなしでコピーした後、別途Clothのみを対象にして「一番近い頂点からコピー」を行ってください。" +
                            "\n(UnityのClothコンポーネントの初期化時に頂点座標がずれてるのが原因のため現在は修正困難です)",
                            labelStyle
                        );
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
            settings.showReplacementRules = EditorGUILayout.Foldout(settings.showReplacementRules, "置換リスト", true);
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
                    if (GUILayout.Button("+ 正規表現", GUILayout.Width(100)))
                    {
                        settings.replacementRules.Add(new ReplacementRule("", ""));
                    }
                    if (GUILayout.Button("+ HumanoidBone", GUILayout.Width(120)))
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
                            GUILayout.Label("検索:", GUILayout.Width(35));
                            rule.srcPattern = GUILayout.TextField(rule.srcPattern, GUILayout.Width(100));
                            GUILayout.Label("置換:", GUILayout.Width(35));
                            rule.dstPattern = GUILayout.TextField(rule.dstPattern, GUILayout.Width(100));
                        }
                        else
                        {
                            // HumanoidBoneの場合
                            string[] modeOptions = new string[] { "グループ", "個別" };
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
                    EditorGUILayout.HelpBox("置換ルールが設定されていません。\nルールがない場合は名前の完全一致のみでマッチします。", MessageType.Info);
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
            bool dstIsHumanoid = false;
            if (ComponentCopier.activeObject != null)
            {
                var dstAnimator = ComponentCopier.activeObject.GetComponent<Animator>();
                dstIsHumanoid = dstAnimator != null && dstAnimator.isHuman;
            }

            // 警告が必要ない場合は何も表示しない
            if (srcIsHumanoid && dstIsHumanoid)
            {
                return;
            }

            // 警告メッセージを構築
            string warningMsg = "";
            if (ComponentCopier.root == null)
            {
                warningMsg = "HumanoidBoneルールが設定されています。\nCopyを実行してからPasteしてください。";
            }
            else if (!srcIsHumanoid && !dstIsHumanoid)
            {
                warningMsg = "コピー元とコピー先の両方がHumanoidではありません。\nHumanoidBoneルールは機能しません。";
            }
            else if (!srcIsHumanoid)
            {
                warningMsg = "コピー元がHumanoidではありません。\nHumanoidBoneルールは機能しません。";
            }
            else
            {
                warningMsg = "コピー先がHumanoidではありません。\nHumanoidBoneルールは機能しません。";
            }

            EditorGUILayout.HelpBox(warningMsg, MessageType.Warning);
        }
    }
}

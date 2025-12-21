using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

	[System.Serializable]
	public class TreeItem {
		public string name;
		public string type;
		public GameObject gameObject;
		public List<TreeItem> children;
		public List<Component> components;
		public TreeItem (GameObject go) {
			name = go.name;
			type = go.GetType ().ToString ();
			gameObject = go;
			components = new List<Component> ();
			children = new List<TreeItem> ();
		}
	}

	[System.Serializable]
	class PackageInfo {
		public string name;
		public string displayName;
		public string version;
		public string unity;
		public string description;
	}

	public enum ModificationOperation {
		None,
		Add,
		Remove,
		Update,
		CreateObject
	}

	public class ModificationEntry {
		public GameObject targetObject;
		public string targetPath;
		public string componentType;
		public ModificationOperation operation;
		public string message;
		public Component createdComponent; // 実際に作成されたコンポーネント
		public GameObject createdObject;   // 実際に作成されたオブジェクト
	}

	public class CopyComponentsByRegex : EditorWindow {
		static string version = "";
		static GameObject activeObject;
		static string pattern = "";
		static TreeItem copyTree = null;
		static Transform root = null;
		static List<Transform> transforms = null;
		static List<Component> components = null;
		static List<ModificationEntry> modificationLogs = new List<ModificationEntry>();
		static List<ModificationEntry> modificationObjectLogs = new List<ModificationEntry>();
		static bool isRemoveBeforeCopy = false;
		static bool isObjectCopy = false;
		static bool isObjectCopyMatchOnly = false;
		static bool isClothNNS = false;
		static bool copyTransform = false;
		static bool showReportAfterPaste = false;

		Vector2 scrollPosition;

		string GetSelfPath([CallerFilePath] string filepath = "") {
			return filepath;
		}

		void OnEnable () {
			var __file__ = GetSelfPath();
			string relativePath = new System.Uri(Application.dataPath).MakeRelativeUri(new System.Uri(__file__)).ToString();
			string currentDirectory = Path.GetDirectoryName(relativePath);
			var packageInfo = JsonUtility.FromJson<PackageInfo>(LoadSmallFile(currentDirectory + "/../package.json"));
			version = packageInfo.version;

			pattern = EditorUserSettings.GetConfigValue ("CopyComponentsByRegex/pattern") ?? "";
			isRemoveBeforeCopy = bool.Parse (EditorUserSettings.GetConfigValue ("CopyComponentsByRegex/isRemoveBeforeCopy") ?? isRemoveBeforeCopy.ToString ());
			isObjectCopy = bool.Parse (EditorUserSettings.GetConfigValue ("CopyComponentsByRegex/isObjectCopy") ?? isObjectCopy.ToString ());
			isObjectCopyMatchOnly = bool.Parse (EditorUserSettings.GetConfigValue ("CopyComponentsByRegex/isObjectCopyMatchOnly") ?? isObjectCopyMatchOnly.ToString ());
			isClothNNS = bool.Parse (EditorUserSettings.GetConfigValue ("CopyComponentsByRegex/isClothNNS") ?? isClothNNS.ToString ());
			copyTransform = bool.Parse (EditorUserSettings.GetConfigValue ("CopyComponentsByRegex/copyTransform") ?? copyTransform.ToString ());
			showReportAfterPaste = bool.Parse (EditorUserSettings.GetConfigValue ("CopyComponentsByRegex/showReportAfterPaste") ?? showReportAfterPaste.ToString ());
		}

		void OnSelectionChange () {
			var editorEvent = EditorGUIUtility.CommandEvent ("ChangeActiveObject");
			editorEvent.type = EventType.Used;
			SendEvent (editorEvent);
		}

		// Use this for initialization
		[MenuItem ("GameObject/Copy Components By Regex", false, 20)]
		public static void ShowWindow () {
			activeObject = Selection.activeGameObject;
			EditorWindow.GetWindow (typeof (CopyComponentsByRegex));
		}

		string LoadSmallFile(string filePath)
		{
			StreamReader reader = new StreamReader(filePath);
			string datastr = reader.ReadToEnd();
			reader.Close();

			return datastr;
		}

		static void CopyWalkdown (GameObject go, ref TreeItem tree, ref Regex regex, int depth = 0) {
			transforms.Add (go.transform);

			// Components
			foreach (Component component in go.GetComponents<Component> ()) {
				if (component == null || !regex.Match (component.GetType ().ToString ()).Success) {
					continue;
				}
				if (component is Transform && !copyTransform) {
					continue;
				}
				tree.components.Add (component);
			}

			// Children
			var children = GetChildren (go);
			foreach (Transform child in children) {
				var node = new TreeItem (child.gameObject);
				tree.children.Add (node);
				CopyWalkdown (child.gameObject, ref node, ref regex, depth + 1);
			}
		}

		static void CopyObjectWalkdown (Transform src, ref TreeItem tree, bool dryRun = false) {
			foreach (TreeItem child in tree.children) {
				var next = child;
				if (!isObjectCopyMatchOnly || child.components.Count > 0) {
					var route = SearchRoute (root, src);
					if (route == null) {
						continue;
					}
					route.Add (child);
					CopyObject (root, activeObject.transform, route, dryRun);
				}
				CopyObjectWalkdown (child.gameObject.transform, ref next, dryRun);
			}
		}

		static List<TreeItem> SearchRoute (Transform root, Transform dst) {
			List<TreeItem> down = new List<TreeItem> ();

			if (root == dst) {
				return down;
			}

			var current = dst;
			while (root != current) {
				down.Add (new TreeItem (current.gameObject));
				current = current.parent;
				if (current == null) {
					return null;
				}
			}
			down.Reverse ();

			return down;
		}

		static Transform CopyObject (Transform srcRoot, Transform dstRoot, List<TreeItem> route, bool dryRun = false) {
			var src = srcRoot;
			var dst = dstRoot;

			string currentPath = dstRoot.name;

			foreach (TreeItem current in route) {
				var item = current;
				Transform srcChild = null;
				Transform dstChild = null;
				foreach (Transform child in GetChildren (src.gameObject)) {
					if (child.name == current.name) {
						srcChild = child;
						break;
					}
				}
				foreach (Transform child in GetChildren (dst.gameObject)) {
					if (child.name == current.name) {
						dstChild = child;
						break;
					}
				}
				
				currentPath += "/" + current.name;

				if (srcChild != null && dstChild == null) {
					if (dryRun || showReportAfterPaste) {
						// 既にこの作成ログが記録されているか確認
						if (!modificationObjectLogs.Any(x => x.targetPath == currentPath && x.operation == ModificationOperation.CreateObject)) {
							modificationObjectLogs.Add(new ModificationEntry {
								targetPath = currentPath,
								operation = ModificationOperation.CreateObject,
								message = "新規オブジェクト"
							});
						}
						// Dry Runではオブジェクトを作成しないので、ここで中断
						if (dryRun) {
							return null;
						}
					}

					GameObject childObject = (GameObject)Object.Instantiate (srcChild.gameObject, dst);
					childObject.name = current.name;
					dst = dstChild = childObject.transform;

					// ログに作成されたオブジェクトを紐付ける
					if (showReportAfterPaste) {
						var log = modificationObjectLogs.FirstOrDefault(x => x.targetPath == currentPath && x.operation == ModificationOperation.CreateObject);
						if (log != null) {
							log.createdObject = childObject;
						}
					}

					// コピーしたオブジェクトに対しては自動的に同種コンポーネントの削除を行う
					RemoveWalkdown (childObject, ref item);
				} else {
					dst = dstChild;
				}
				src = srcChild;
			}

			return dst;
		}

		static void RunComponentOperation(GameObject go, string componentType, ModificationOperation op, string message, bool dryRun, System.Func<Component> action) {
			bool logAdded = false;
			if (dryRun || showReportAfterPaste) {
				if (!string.IsNullOrEmpty(message)) {
					modificationLogs.Add(new ModificationEntry {
						targetObject = go,
						componentType = componentType,
						operation = op,
						message = message
					});
					logAdded = true;
				}
			}

			if (dryRun) {
				return;
			}

			Component result = action();
			
			if (result != null) {
				components.Add(result);

				if (showReportAfterPaste && logAdded) {
					var lastLog = modificationLogs.LastOrDefault();
					if (lastLog != null && lastLog.targetObject == go && lastLog.componentType == componentType) {
						lastLog.createdComponent = result;
					}
				}
			}
		}

		internal static void MergeWalkdown (GameObject go, ref TreeItem tree, int depth = 0, bool dryRun = false) {
			if (depth > 0 && go.name != tree.name) {
				return;
			}

			// copy components
			foreach (Component component in tree.components) {
				var componentType = component.GetType();
				var targetComponent = go.GetComponent(componentType);

				// 1. Transform
				if (component is Transform) {
					if (!copyTransform) {
						continue;
					}
					
					ModificationOperation op = ModificationOperation.None;
					string msg = "";
					
					if (targetComponent != null && (componentType.Name == "Transform" || componentType.Name == "RectTransform")) {
						op = ModificationOperation.Update;
						msg = "値の更新";
					}

					RunComponentOperation(go, componentType.Name, op, msg, dryRun, () => {
						UnityEditorInternal.ComponentUtility.CopyComponent(component);
						if (targetComponent != null) {
							UnityEditorInternal.ComponentUtility.PasteComponentValues(targetComponent);
							return targetComponent;
						}
						return null;
					});
					continue;
				}

				// 2. Cloth
				if (component is Cloth) {
					ModificationOperation op = targetComponent != null ? ModificationOperation.Update : ModificationOperation.Add;
					string msg = targetComponent != null 
						? "プロパティの更新" + (isClothNNS ? " (NNS)" : "") 
						: "新規貼り付け";

					RunComponentOperation(go, componentType.Name, op, msg, dryRun, () => {
						var cloth = go.GetComponent<Cloth> () == null ? go.AddComponent<Cloth> () : go.GetComponent<Cloth> ();
						CopyProperties (component, cloth);

						// NNS / Coefficient Copy
						var srcCloth = (component as Cloth);
						var dstCloth = cloth;
						
						if (dstCloth != null) {
							var srcCoefficients = srcCloth.coefficients;
							var dstCoefficients = dstCloth.coefficients;

							if (isClothNNS) {
								var srcVertices = srcCloth.vertices;
								var dstVertives = dstCloth.vertices;

								// build KD-Tree
								var kdtree = new KDTree (
									srcVertices,
									0,
									(srcVertices.Length < srcCoefficients.Length ? srcVertices.Length : srcCoefficients.Length) - 1
								);

								for (int i = 0, il = dstCoefficients.Length, ml = dstVertives.Length; i < il && i < ml; ++i) {
									var srcIdx = kdtree.FindNearest (dstVertives[i]);
									dstCoefficients[i].collisionSphereDistance = srcCoefficients[srcIdx].collisionSphereDistance;
									dstCoefficients[i].maxDistance = srcCoefficients[srcIdx].maxDistance;
								}
								dstCloth.coefficients = dstCoefficients;
							} else {
								if (srcCoefficients.Length == dstCoefficients.Length) {
									for (int i = 0, il = srcCoefficients.Length; i < il; ++i) {
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
					string msg = targetComponent != null ? "新規貼り付け" : "コンポーネントの追加";

					RunComponentOperation(go, componentType.Name, op, msg, dryRun, () => {
						UnityEditorInternal.ComponentUtility.CopyComponent(component);
						UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);
						
						Component[] comps = go.GetComponents<Component>();
						Component dstComponent = comps[comps.Length - 1];
						return dstComponent;
					});
				}
			}

			// children
			var children = GetChildren (go);
			var childDic = new Dictionary<string, Transform> ();
			foreach (Transform child in children) {
				childDic[child.gameObject.name] = child;
			}
			foreach (TreeItem treeChild in tree.children) {
				var next = treeChild;

				if (!childDic.ContainsKey (treeChild.name)) {
					continue;
				}

				Transform child = childDic[treeChild.name];

				if (child.gameObject.GetType ().ToString () == treeChild.type) {
					MergeWalkdown (child.gameObject, ref next, depth + 1, dryRun);
				}
			}
		}

		static void RemoveWalkdown (GameObject go, ref TreeItem tree, int depth = 0, bool dryRun = false) {
			if (depth > 0 && go.name != tree.name) {
				return;
			}

			var componentsTypes = tree.components.Select (component => component.GetType ()).Distinct ();

			// remove components
			foreach (Component component in go.GetComponents<Component> ()) {
				if (component != null && componentsTypes.Contains (component.GetType ())) {
					if (component is UnityEngine.Transform) {
						continue;
					}

					if (dryRun || showReportAfterPaste) {
						modificationLogs.Add(new ModificationEntry {
							targetObject = go,
							componentType = component.GetType().Name,
							operation = ModificationOperation.Remove,
							message = "削除"
						});
						
						if (dryRun) {
							continue;
						}
					}
					
					Object.DestroyImmediate (component);
				}
			}

			// children
			var children = GetChildren (go);
			foreach (Transform child in children) {
				TreeItem next = null;
				foreach (TreeItem treeChild in tree.children) {
					if (
						child.gameObject.name == treeChild.name &&
						child.gameObject.GetType ().ToString () == treeChild.type
					) {
						next = treeChild;
						RemoveWalkdown (child.gameObject, ref next, depth + 1, dryRun);
						break;
					}
				}
			}
		}

		internal static Transform[] GetChildren (GameObject go) {
			int count = go.transform.childCount;
			var children = new Transform[count];

			for (int i = 0; i < count; ++i) {
				children[i] = go.transform.GetChild (i);
			}

			return children;
		}

		static void CopyProperties (Component srcComponent, Component dstComponent) {
			var dst = new SerializedObject (dstComponent);
			var src = new SerializedObject (srcComponent);

			dst.Update ();
			src.Update ();

			var iter = src.GetIterator ();
			while (iter.NextVisible (true)) {
				dst.CopyFromSerializedProperty (iter);
			}
			dst.ApplyModifiedProperties ();
		}

		static void UpdateProperties (Transform dstRoot) {
			foreach (Component dstComponent in components) {
				if (dstComponent == null) {
					continue;
				}

				var so = new SerializedObject (dstComponent);
				so.Update ();
				var iter = so.GetIterator ();

				// Object Reference
				while (iter.NextVisible (true)) {
					if (iter.propertyType.ToString () != "ObjectReference") {
						continue;
					}

					SerializedProperty property = so.FindProperty (iter.propertyPath);
					var dstObjectReference = property.objectReferenceValue;
					if (dstObjectReference == null) {
						continue;
					}
					if (!(dstObjectReference is Transform || dstObjectReference is Component)) {
						continue;
					}

					Transform srcTransform = null;
					if (dstObjectReference is Component) {
						srcTransform = (dstObjectReference as Component).transform;
					} else if (dstObjectReference is Transform) {
						srcTransform = dstObjectReference as Transform;
					}

					// ObjectReferenceの参照先がコピー内に存在するか
					if (!transforms.Contains (srcTransform)) {
						continue;
					}

					// コピー元のルートからObjectReferenceの位置への経路を探り、コピー後のツリーから該当オブジェクトを探す
					var routes = SearchRoute (root, srcTransform);
					if (routes == null) {
						continue;
					}
					Transform current = dstRoot;
					foreach (var route in routes) {
						// 次の子を探す(TreeItemの名前と型で経路と同じ子を探す)
						var children = GetChildren (current.gameObject);
						if (children.Length < 1) {
							current = null;
							break;
						}
						Transform next = null;
						foreach (Transform child in children) {
							var treeitem = new TreeItem (child.gameObject);
							if (treeitem.name == route.name && treeitem.type == route.type) {
								next = child;
								break;
							}
						}
						if (next == null) {
							current = null;
							break;
						}

						current = next;
					}
					if (current == null) {
						continue;
					}

					if (dstObjectReference is Transform) {
						property.objectReferenceValue = current;
					} else if (dstObjectReference is Component) {
						Component comp = (Component)dstObjectReference;
						var children = current.GetComponents(dstObjectReference.GetType());
						var index = GetReferenceIndex(ref srcTransform, ref comp);

						if (!SearchObjectReference(ref copyTree, ref comp)) {
							continue;
						}
						if (index < 0) {
							continue;
						}

						property.objectReferenceValue = children[index];
					}
				}
				so.ApplyModifiedProperties ();
			}
		}
		static private int GetReferenceIndex(ref Transform current, ref Component component) {
			var children = current.GetComponents(component.GetType());
			int i = children.Length;

			while (--i >= 0) {
				if (children[i] == component) {
					break;
				}
			}

			return i;
		}

		static private bool SearchObjectReference(ref TreeItem treeitem, ref Component component) {
			if (treeitem.components.Contains(component)) {
				return true;
			}
			for(int i = 0, il = treeitem.children.Count(); i < il; ++i) {
				var child = treeitem.children[i];
				if (SearchObjectReference(ref child, ref component)) {
					return true;
				}
			}

			return false;
		}

		private void ExecuteDryRun() {
			modificationLogs.Clear();
			modificationObjectLogs.Clear();

			if (copyTree == null || root == null) {
				return;
			}

			if (isRemoveBeforeCopy) {
				RemoveWalkdown (activeObject, ref copyTree, 0, true);
			}

			if (isObjectCopy) {
				CopyObjectWalkdown (root, ref copyTree, true);
			}
			MergeWalkdown (activeObject, ref copyTree, 0, true);
		}




		private void OnGUI () {
			activeObject = Selection.activeGameObject;

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			try {
				using (new GUILayout.VerticalScope(GUI.skin.box)) {
					EditorGUILayout.LabelField($"{typeof(CopyComponentsByRegex).Namespace} Version: " + version);
				}

				EditorGUILayout.LabelField ("アクティブなオブジェクト");
				using (new GUILayout.VerticalScope (GUI.skin.box)) {
					EditorGUILayout.LabelField (activeObject ? activeObject.name : "");
				}
				if (!activeObject) {
					return;
				}

				pattern = EditorGUILayout.TextField ("正規表現", pattern);
				EditorUserSettings.SetConfigValue ("CopyComponentsByRegex/pattern", pattern);

				if (GUILayout.Button ("Copy")) {
					// initialize class variables
					copyTree = new TreeItem (activeObject);
					root = activeObject.transform;
					transforms = new List<Transform> ();
					components = new List<Component> ();

					var regex = new Regex (pattern);
					CopyWalkdown (activeObject, ref copyTree, ref regex);
				}

				EditorGUILayout.LabelField ("コピー中のオブジェクト");
				using (new GUILayout.VerticalScope (GUI.skin.box)) {
					EditorGUILayout.LabelField (root ? root.name : "");
				}

				EditorUserSettings.SetConfigValue (
					"CopyComponentsByRegex/copyTransform",
					(copyTransform = GUILayout.Toggle (copyTransform, "Transformがマッチした場合値をコピー")).ToString ()
				);
				EditorUserSettings.SetConfigValue (
					"CopyComponentsByRegex/isRemoveBeforeCopy",
					(isRemoveBeforeCopy = GUILayout.Toggle (isRemoveBeforeCopy, "コピー先に同じコンポーネントがあったら削除")).ToString ()
				);
				EditorUserSettings.SetConfigValue (
					"CopyComponentsByRegex/isObjectCopy",
					(isObjectCopy = GUILayout.Toggle (isObjectCopy, "コピー先にオブジェクトがなかったらオブジェクトをコピー")).ToString ()
				);
				if (isObjectCopy) {
					using (new GUILayout.VerticalScope (GUI.skin.box)) {
						EditorUserSettings.SetConfigValue (
							"CopyComponentsByRegex/isObjectCopyMatchOnly",
							(isObjectCopyMatchOnly = GUILayout.Toggle (isObjectCopyMatchOnly, "マッチしたコンポーネントを持つオブジェクトのみコピー")).ToString ()
						);
					}
				}
				EditorUserSettings.SetConfigValue (
					"CopyComponentsByRegex/isClothNNS",
					(isClothNNS = GUILayout.Toggle (isClothNNS, "ClothコンポーネントのConstraintsを一番近い頂点からコピー")).ToString ()
				);

				EditorUserSettings.SetConfigValue (
					"CopyComponentsByRegex/showReportAfterPaste",
					(showReportAfterPaste = GUILayout.Toggle(showReportAfterPaste, "Paste時に結果を表示")).ToString()
				);

				if (GUILayout.Button ("Paste")) {
					if (copyTree == null || root == null) {
						return;
					}

					// Clear logs
					modificationLogs.Clear();
					modificationObjectLogs.Clear();

					if (isRemoveBeforeCopy) {
						RemoveWalkdown (activeObject, ref copyTree);
					}

					if (isObjectCopy) {
						CopyObjectWalkdown (root, ref copyTree);
					}
					MergeWalkdown (activeObject, ref copyTree);
					UpdateProperties (activeObject.transform);
					
					if (showReportAfterPaste && (modificationLogs.Count > 0 || modificationObjectLogs.Count > 0)) {
						ModificationReportPopup.Show(copyTree, activeObject, modificationLogs, modificationObjectLogs, isObjectCopy, false);
					}
				}

				if (GUILayout.Button ("Dry Run")) {
					ExecuteDryRun();
					if (modificationLogs.Count > 0 || modificationObjectLogs.Count > 0) {
						ModificationReportPopup.Show(copyTree, activeObject, modificationLogs, modificationObjectLogs, isObjectCopy, true);
					}
				}



				GUIStyle labelStyle = new GUIStyle (GUI.skin.label);
				labelStyle.wordWrap = true;
				using (new GUILayout.VerticalScope (GUI.skin.box)) {
					GUILayout.Label (
						"「一番近い頂点からコピー」を利用する場合はあらかじめClothのコピー先にClothを追加するか、" +
						"最初はチェックなしでコピーした後、別途Clothのみを対象にして「一番近い頂点からコピー」を行ってください。" +
						"\n(UnityのClothコンポーネントの初期化時に頂点座標がずれてるのが原因のため現在は修正困難です)",
						labelStyle
					);
				}
            }
            finally
            {
				EditorGUILayout.EndScrollView();
            }
		}


	}
}
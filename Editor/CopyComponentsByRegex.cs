namespace CopyComponentsByRegex {
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using System.Text.RegularExpressions;
	using System.Linq;

	[System.Serializable]
	class TreeItem {
		public string name;
		public string type;
		public List<TreeItem> children;
		public List<Component> components;
		public TreeItem(GameObject go) {
			name = go.name;
			type = go.GetType().ToString();
			components = new List<Component>();
			children = new List<TreeItem>();
		}
	}

	public class CopyComponentsByRegexWindow: EditorWindow {
		static GameObject activeObject;
		static string pattern = "";
		static TreeItem copyTree = null;
		static Transform root = null;
		static List<Transform> transforms = null;
		static List<Component> components = null;

		void OnSelectionChange() {
			var editorEvent = EditorGUIUtility.CommandEvent("ChangeActiveObject");
			editorEvent.type = EventType.Used;
			SendEvent(editorEvent);
		}

		// Use this for initialization
		[MenuItem("GameObject/Copy Components By Regex", false, 20)]
		public static void ShowWindow() {
			activeObject = Selection.activeGameObject;
			EditorWindow.GetWindow(typeof(CopyComponentsByRegexWindow));
		}

		static void CopyWalkdown(GameObject go, ref TreeItem tree, int depth=0) {
			transforms.Add(go.transform);

			// Components
			foreach (Component component in go.GetComponents<Component>()) {
				if (!new Regex(pattern).Match(component.GetType().ToString()).Success) {
					continue;
				}
				tree.components.Add(component);
			}

			// Children
			var children = go.GetComponentInChildren<Transform>();
			foreach (Transform child in children) {
				var node = new TreeItem(child.gameObject);
				tree.children.Add(node);
				CopyWalkdown(child.gameObject, ref node, depth + 1);
			}
		}

		static List<TreeItem> SearchRoute(Transform root, Transform dst) {
			List<TreeItem> down = new List<TreeItem>();

			if (root == dst) {
				return down;
			}

			var current = dst;
			while (CopyComponentsByRegexWindow.root != current) {
				down.Add(new TreeItem(current.gameObject));
				current = current.parent;
				if (current == null) {
					return null;
				}
			}
			down.Reverse();

			return down;
		}

		static void MergeWalkdown(GameObject go, ref TreeItem tree, int depth=0) {
			if (depth > 0 && go.name != tree.name) {
				return;
			}

			// copy components
			foreach (Component component in tree.components) {
				UnityEditorInternal.ComponentUtility.CopyComponent(component);
				// 同じ種類のコンポーネントがある場合、既存のコンポーネントに上書きすることも出来る。
				// しかし、一つのオブジェクトに複数のコンポーネントを設定したい場合もあるのでとりあえずコメントアウトしておく。
				// 要望などがあれば切り替えても良いかもしれない。
				/*
				var targetComponent = go.GetComponent(type);
				if (targetComponent) {
					UnityEditorInternal.ComponentUtility.PasteComponentValues(targetComponent);
				} else {
					UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);
				}
				*/
				UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);

				Component[] comps = go.GetComponents<Component>();
				components.Add(comps[comps.Length - 1]);
			}

			// children
			var children = go.GetComponentsInChildren<Transform>();
			foreach (Transform child in children) {
				TreeItem next = null;
				foreach (TreeItem treeChild in tree.children) {
					if (
						child.gameObject.name == treeChild.name &&
						child.gameObject.GetType().ToString() == treeChild.type
					) {
						next = treeChild;
						MergeWalkdown(child.gameObject, ref next, depth + 1);
						break;
					}
				}
			}
		}
		
		static void updateProperties(Transform dstRoot) {
			foreach (Component dstComponent in components) {
				var type = dstComponent.GetType();

				var so = new SerializedObject(dstComponent);
				so.Update();
				var iter = so.GetIterator();

				// Object Reference
				while (iter.NextVisible(true)) {
					if (iter.propertyType.ToString() != "ObjectReference") {
						continue;
					}

					SerializedProperty property = so.FindProperty(iter.propertyPath);
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
					} else if (dstObjectReference is Transform){
						srcTransform = dstObjectReference as Transform;
					}

					// ObjectReferenceの参照先がコピー内に存在するか
					if (!transforms.Contains(srcTransform)) {
						continue;
					}

					// コピー元のルートからObjectReferenceの位置への経路を探り、コピー後のツリーから該当オブジェクトを探す
					var routes = SearchRoute(root, srcTransform);
					if (routes == null) {
						continue;
					}
					Transform  current = dstRoot;
					foreach (var route in routes) {
						// 次の子を探す(TreeItemの名前と型で経路と同じ子を探す)
						var children = current.GetComponentsInChildren<Transform>();
						if (children.Length < 1) {
							current = null;
							break;
						}
						Transform next = null;
						foreach (Transform child in children) {
							var treeitem = new TreeItem(child.gameObject);
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
					}

					if (dstObjectReference is Component) {
						Component comp = null;
						var children = current.GetComponentsInChildren<Component>();
						foreach (Component child in children) {
							if (child.GetType() == dstObjectReference.GetType()) {
								comp = child;
								break;
							}
						}
						if (comp == null) {
							continue;
						}
						property.objectReferenceValue = comp;
					}
				}
				so.ApplyModifiedProperties();
			}
		}

		private void OnGUI() {
			activeObject = Selection.activeGameObject;
			EditorGUILayout.LabelField("アクティブなオブジェクト");
			using (new GUILayout.VerticalScope(GUI.skin.box)) {
				EditorGUILayout.LabelField(activeObject ? activeObject.name : "");
			}
			if (!activeObject) {
				return;
			}

			pattern = EditorGUILayout.TextField("正規表現", pattern);

			if (GUILayout.Button("Copy")) {
				// initialize class variables
				copyTree = new TreeItem(activeObject);
				root = activeObject.transform;
				transforms = new List<Transform>();
				components = new List<Component>();

				CopyWalkdown(activeObject, ref copyTree);
			}

			EditorGUILayout.LabelField("コピー中のオブジェクト");
			using (new GUILayout.VerticalScope(GUI.skin.box)) {
				EditorGUILayout.LabelField(root ? root.name : "");
			}

			if (GUILayout.Button("Paste")) {
				if (copyTree == null || root == null) {
					return;
				}

				MergeWalkdown(activeObject, ref copyTree);
				updateProperties(activeObject.transform);
			}
		}
	}
}
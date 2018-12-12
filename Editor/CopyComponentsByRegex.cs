namespace CopyComponentsByRegex {
	using System.Collections.Generic;
	using System.Collections;
	using System.Linq;
	using System.Text.RegularExpressions;
	using UnityEditor;
	using UnityEngine;

	[System.Serializable]
	class TreeItem {
		public string name;
		public string type;
		public List<TreeItem> children;
		public List<Component> components;
		public TreeItem (GameObject go) {
			name = go.name;
			type = go.GetType ().ToString ();
			components = new List<Component> ();
			children = new List<TreeItem> ();
		}
	}

	public class CopyComponentsByRegexWindow : EditorWindow {
		static GameObject activeObject;
		static string pattern = "";
		static TreeItem copyTree = null;
		static Transform root = null;
		static List<Transform> transforms = null;
		static List<Component> components = null;
		static bool isRemoveBeforeCopy = false;
		static bool isClothNNS = false;

		void OnEnable () {
			pattern = EditorUserSettings.GetConfigValue ("CopyComponentsByRegex/pattern") ?? "";
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
			EditorWindow.GetWindow (typeof (CopyComponentsByRegexWindow));
		}

		static void CopyWalkdown (GameObject go, ref TreeItem tree, ref Regex regex, int depth = 0) {
			transforms.Add (go.transform);

			// Components
			foreach (Component component in go.GetComponents<Component> ()) {
				if (component == null || !regex.Match (component.GetType ().ToString ()).Success) {
					continue;
				}
				tree.components.Add (component);
			}

			// Children
			var children = go.GetComponentInChildren<Transform> ();
			foreach (Transform child in children) {
				var node = new TreeItem (child.gameObject);
				tree.children.Add (node);
				CopyWalkdown (child.gameObject, ref node, ref regex, depth + 1);
			}
		}

		static List<TreeItem> SearchRoute (Transform root, Transform dst) {
			List<TreeItem> down = new List<TreeItem> ();

			if (root == dst) {
				return down;
			}

			var current = dst;
			while (CopyComponentsByRegexWindow.root != current) {
				down.Add (new TreeItem (current.gameObject));
				current = current.parent;
				if (current == null) {
					return null;
				}
			}
			down.Reverse ();

			return down;
		}

		static void MergeWalkdown (GameObject go, ref TreeItem tree, int depth = 0) {
			if (depth > 0 && go.name != tree.name) {
				return;
			}

			// copy components
			foreach (Component component in tree.components) {
				UnityEditorInternal.ComponentUtility.CopyComponent (component);
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
				if (component is Cloth && go.GetComponent<Cloth> () == null) {
					var cloth = go.AddComponent<Cloth> ();
					cloth.ClearTransformMotion ();
				}
				UnityEditorInternal.ComponentUtility.PasteComponentAsNew (go);

				Component[] comps = go.GetComponents<Component> ();
				var dstComponent = comps[comps.Length - 1];
				components.Add (dstComponent);

				if (component is Cloth) {
					var srcCloth = (component as Cloth);
					var dstCloth = (dstComponent as Cloth);
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
							var sv = srcVertices[srcIdx];
							var dv = dstVertives[i];
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
			}

			// children
			var children = go.GetComponentsInChildren<Transform> ();
			foreach (Transform child in children) {
				TreeItem next = null;
				foreach (TreeItem treeChild in tree.children) {
					if (
						child.gameObject.name == treeChild.name &&
						child.gameObject.GetType ().ToString () == treeChild.type
					) {
						next = treeChild;
						MergeWalkdown (child.gameObject, ref next, depth + 1);
						break;
					}
				}
			}
		}

		static void RemoveWalkdown (GameObject go, ref TreeItem tree, int depth = 0) {
			if (depth > 0 && go.name != tree.name) {
				return;
			}

			var componentsTypes = tree.components.Select (component => component.GetType ()).Distinct ();

			// remove components
			foreach (Component component in go.GetComponents<Component> ()) {
				if (component != null && componentsTypes.Contains (component.GetType ())) {
					Object.DestroyImmediate (component);
				}
			}

			// children
			var children = go.GetComponentsInChildren<Transform> ();
			foreach (Transform child in children) {
				TreeItem next = null;
				foreach (TreeItem treeChild in tree.children) {
					if (
						child.gameObject.name == treeChild.name &&
						child.gameObject.GetType ().ToString () == treeChild.type
					) {
						next = treeChild;
						RemoveWalkdown (child.gameObject, ref next, depth + 1);
						break;
					}
				}
			}
		}

		static void updateProperties (Transform dstRoot) {
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
						var children = current.GetComponentsInChildren<Transform> ();
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
					}

					if (dstObjectReference is Component) {
						Component comp = null;
						var children = current.GetComponentsInChildren<Component> ();
						foreach (Component child in children) {
							if (child.GetType () == dstObjectReference.GetType ()) {
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
				so.ApplyModifiedProperties ();
			}
		}

		private void OnGUI () {
			activeObject = Selection.activeGameObject;
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

			isRemoveBeforeCopy = GUILayout.Toggle (isRemoveBeforeCopy, "コピー先に同じコンポーネントがあったら削除");
			isClothNNS = GUILayout.Toggle (isClothNNS, "ClothコンポーネントのConstraintsを一番近い頂点からコピーする");

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

			if (GUILayout.Button ("Paste")) {
				if (copyTree == null || root == null) {
					return;
				}

				if (isRemoveBeforeCopy) {
					RemoveWalkdown (activeObject, ref copyTree);
				}

				MergeWalkdown (activeObject, ref copyTree);
				updateProperties (activeObject.transform);
			}
		}
	}
}
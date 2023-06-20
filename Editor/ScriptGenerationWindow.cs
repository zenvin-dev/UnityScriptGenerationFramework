using System;
using UnityEditor;
using UnityEngine;
using Zenvin.EditorUtil;
using Zenvin.ProjectPreferences;

namespace Zenvin.ScriptGeneration {
	public class ScriptGenerationWindow : EditorWindow {

		private Vector2 listScroll;
		private Vector2 editorScroll;
		private FactoryInfo[] factoryList;

		private int selected = -1;
		private object[] currentValues;
		private bool dirty;
		private GUIContent[] buttons;


		[MenuItem ("Tools/Zenvin/Script Factories")]
		private static void Init () {
			ScriptGenerationWindow win = GetWindow<ScriptGenerationWindow> ();
			win.minSize = new Vector2 (500f, 250f);
			win.titleContent = new GUIContent ("Script Factories");

			win.Show ();
		}


		private void OnGUI () {
			Rect listRect = new Rect (0f, 0f, position.width * 0.4f, position.height);
			Rect editorRect = new Rect (listRect.width, 0f, position.width - listRect.width, position.height);

			GUILayout.BeginArea (listRect);
			listScroll = GUILayout.BeginScrollView (listScroll, false, false);
			DrawFactoryList ();
			GUILayout.EndScrollView ();
			GUILayout.EndArea ();

			DrawFactory (editorRect);

			EditorDrawUtility.DrawSeparator (editorRect, EditorDrawUtility.Side.Left, 1f, UnityColors.SeparatorColor, true);
		}

		private void DrawFactoryList () {
			UpdateFactoryList ();
			for (int i = 0; i < factoryList.Length; i++) {
				if (EditorDrawUtility.Button (EditorGUILayout.GetControlRect (), new GUIContent (factoryList[i].Factory.GetType ().FullName), selected == i)) {
					SelectFactory (i);
				}
			}
		}

		private void DrawFactory (Rect editorRect) {
			if (selected >= 0 && selected < factoryList.Length) {
				var factory = factoryList[selected].Factory;
				var locked = Application.isPlaying && !factory.AllowPlaymodeChanges;

				GUILayout.BeginArea (editorRect);
				if (locked) {
					EditorGUILayout.HelpBox ("This factory's settings are not available in play mode.", MessageType.Info);
				}

				EditorGUI.BeginDisabledGroup (locked);

				EditorGUILayout.LabelField (factory.GetType ().Name, EditorStyles.boldLabel);
				GUILayout.Space (10);

				DrawFactoryButtons ();

				editorScroll = GUILayout.BeginScrollView (editorScroll, false, false);
				DrawPropertyList (factoryList[selected]);
				GUILayout.EndScrollView ();

				GUILayout.BeginHorizontal ();

				EditorGUI.BeginDisabledGroup (!dirty);
				if (GUILayout.Button ("Apply")) {
					ApplyModifiedProperties ();
				}
				if (GUILayout.Button ("Revert")) {
					UpdateCurrentValues ();
					dirty = false;
				}
				EditorGUI.EndDisabledGroup ();
				GUILayout.EndHorizontal ();

				EditorGUI.EndDisabledGroup ();
				GUILayout.EndArea ();
			}
		}

		private void SelectFactory (int i) {
			if (selected >= 0 && selected < factoryList.Length && factoryList[selected].Factory is IScriptFactoryExtension ext) {
				buttons = ext.GetFactoryButtonLabels ();
			}

			if (i == selected) {
				return;
			}
			if (dirty) {
				var choice = EditorUtility.DisplayDialog ("Warning", "The selected Script Factory has unsaved changes.", "Apply", "Cancel");
				if (!choice) {
					return;
				}
			}

			buttons = null;
			ApplyModifiedProperties ();

			selected = i;

			if (i >= 0 && i < factoryList.Length) {
				UpdateCurrentValues ();

			}
		}

		private void UpdateCurrentValues () {
			var fact = factoryList[selected];
			currentValues = new object[fact.PropertyCount];
			for (int i = 0; i < currentValues.Length; i++) {
				currentValues[i] = fact[i].GetGetMethod ().Invoke (fact.Factory, Array.Empty<object> ());
			}
		}

		private void DrawPropertyList (FactoryInfo factoryInfo) {
			if (currentValues == null) {
				UpdateCurrentValues ();
			}
			for (int i = 0; i < factoryInfo.PropertyCount; i++) {
				var prop = factoryInfo[i];
				var type = prop.PropertyType;
				var label = new GUIContent (prop.Name, prop.Tooltip);
				var prefix = prop.Decorator?.Prefix;
				var suffix = prop.Decorator?.Suffix;

				GUILayout.BeginHorizontal ();
				EditorGUILayout.PrefixLabel (label);

				if (type == typeof (bool)) {
					bool newValue = EditorGUILayout.Toggle ((bool)currentValues[i]);
					if (newValue != (bool)currentValues[i]) {
						currentValues[i] = newValue;
						dirty = true;
					}
				}
				if (type == typeof (string)) {
					if (!string.IsNullOrEmpty (prefix)) {
						EditorGUILayout.LabelField (prefix, GUILayout.ExpandWidth (false));
					}
					string newValue = EditorGUILayout.DelayedTextField ((string)currentValues[i], GUILayout.ExpandWidth (true));
					if (!string.IsNullOrEmpty (suffix)) {
						EditorGUILayout.LabelField (suffix, GUILayout.ExpandWidth (false));
					}

					if (newValue != (string)currentValues[i]) {
						currentValues[i] = newValue;
						dirty = true;
					}
				}
				if (type == typeof (int)) {
					int newValue = EditorGUILayout.IntField ((int)currentValues[i]);
					if (newValue != (int)currentValues[i]) {
						currentValues[i] = newValue;
						dirty = true;
					}
				}
				if (type == typeof (float)) {
					float newValue = EditorGUILayout.FloatField ((float)currentValues[i]);
					if (newValue != (float)currentValues[i]) {
						currentValues[i] = newValue;
						dirty = true;
					}
				}

				GUILayout.EndHorizontal ();
			}
		}

		private void DrawFactoryButtons () {
			if (buttons == null || buttons.Length == 0 || !(factoryList[selected].Factory is IScriptFactoryExtension ext)) {
				return;
			}
			GUILayout.BeginHorizontal ();
			for (int i = 0; i < buttons.Length; i++) {
				EditorGUI.BeginDisabledGroup (!ext.IsFactoryButtonInteractable (i));
				if (GUILayout.Button (buttons[i])) {
					ext.OnFactoryButtonClick (i);
				}
				EditorGUI.EndDisabledGroup ();
			}
			GUILayout.EndHorizontal ();
		}

		private void UpdateFactoryList () {
			if (factoryList == null || factoryList.Length != ScriptGenerator.FactoryCount) {
				factoryList = new FactoryInfo[ScriptGenerator.FactoryCount];
				for (int i = 0; i < factoryList.Length; i++) {
					factoryList[i] = ScriptGenerator.GetFactory (i);
				}
			}
		}

		private void ApplyModifiedProperties () {
			if (selected < 0 || selected >= factoryList.Length || !dirty) {
				return;
			}

			var fact = factoryList[selected];
			var targ = fact.Factory;
			var para = new object[1];

			for (int i = 0; i < currentValues.Length; i++) {
				para[0] = currentValues[i];
				fact[i].GetSetMethod ().Invoke (targ, para);

				if (PrefValue.TryCreate (currentValues[i], out PrefValue value)) {
					ProjectPrefs.SetValue (ScriptGenerator.GetPropertyPrefKey (targ.GetType (), fact[i].Property), value, ProjectPrefs.ValueOverrideOption.AlwaysOverride);
				} else {
					Debug.LogError ($"Could not set preference of type {fact[i].PropertyType} (Source: {targ.GetType().FullName}.{fact[i].Property}).");
				}
			}

			dirty = false;
			ProjectPrefs.Save ();
			targ.OnApply ();
		}
	}
}
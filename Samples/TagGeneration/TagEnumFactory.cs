using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Zenvin.EditorUtil;
using Zenvin.ProjectPreferences;

namespace Zenvin.ScriptGeneration.Tags {
	public class TagEnumFactory : ScriptFactory, IScriptFactoryExtension {

		/// <summary> The editor prefs key, under which the previous enum values are saved, so there is no inifinite loop when regenerating the file. </summary>
		private const string PreviousValueKey = "EDITOR_TAGS";
		/// <summary> The editor prefs key, under which the path for the script file is saved. Relative to the Assets folder. </summary>
		internal const string ScriptFileLocationKey = "Zenvin_TAG_GEN_Script_Location";
		/// <summary> The editor prefs key, under which the name for the script file is saved. </summary>
		internal const string ScriptFileNameKey = "Zenvin_TAG_GEN_Script_Name";

		private const string SYMBOL = "Zenvin_CUST_TAGS";


		private static readonly PrefKey ValueKey = new PrefKey ("$Zenvin.ScriptGeneration", "TAG_GENERATOR", PreviousValueKey);
		private string outputPath;

		[PropertyTooltip ("The path of the output file, relative to the Assets folder.")]
		public string OutputPath { get => outputPath; set => SetOutputPath (value); }


		private void SetOutputPath (string value) {
			// if the values are already equal, cancel
			if (outputPath == value) {
				return;
			}

			// get fully qualified paths
			var oldPath = GetFullPath (OutputPath);
			var newPath = GetFullPath (value);

			// if new path is not valid, cancel
			if (newPath == null || !newPath.EndsWith (".cs")) {
				return;
			}

			// make sure target directory exists
			var newFile = new FileInfo (newPath);
			if (!newFile.Directory.Exists) {
				newFile.Directory.Create ();
			}

			if (!string.IsNullOrEmpty (oldPath)) {
				// move existing file, if there is any
				var oldFile = new FileInfo (oldPath);
				if (oldFile.Exists) {
					oldFile.MoveTo (newPath);
				}
			}

			// update output path
			outputPath = value;
		}


		protected override void Setup () {
			//GenerateAndEnableTags ();
		}

		protected override void OnApply () {
			//GenerateAndEnableTags ();
		}


		private void GenerateTags () {
			// load last saved tags
			string json = ProjectPrefs.GetString (ValueKey, null);
			string[] tags = InternalEditorUtility.tags;

			// validate output path
			bool validPath = ValidatePath (out FileInfo file);

			// check if there were changes to the tags
			string[] loadedTags;
			try {
				loadedTags = JsonUtility.FromJson<string[]> (json);
			} catch {
				ProjectPrefs.DeleteKey (ValueKey);
				if (!validPath) {
					DisableTags ();
					return;
				}
				loadedTags = null;
			}
			if (json != null && Compare (loadedTags, tags)) {
				Debug.Log ("Tags did not need to be regenerated.");
				return;
			}

			ProjectPrefs.SetValue (ValueKey, json);

			// if tags were changed, regenerate
			GenerateTags (file, tags);
		}

		private bool ValidatePath (out FileInfo outputFile) {
			var fullPath = GetFullPath (OutputPath);
			outputFile = new FileInfo (fullPath);

			if (outputFile.Extension == ".cs") {
				DisableTags ();
				return false;
			}

			if (!outputFile.Directory.Exists) {
				try {
					outputFile.Directory.Create ();
				} catch { }
			}
			return true;
		}

		internal void EnableTags () {
#if !Zenvin_CUST_TAGS
			if (!SymbolUtility.HasDebugSymbol (SYMBOL)) {
				GenerateTags ();
				SymbolUtility.AddDebugSymbol (SYMBOL);
				EditorUtility.DisplayDialog ("Info", $"Tag Mask enabled.", "OK");
			} else {
				EditorUtility.DisplayDialog ("Error", $"Tag Mask is already enabled.", "OK");
			}
#endif
		}

		private void DisableTags () {
#if Zenvin_CUST_TAGS
			SymbolUtility.RemoveDebugSymbol (SYMBOL);
#endif
		}

		private bool GenerateTags (FileInfo outputFile, string[] tags) {
			// generating the content for the target file
			string content = GetEnumFileContent (tags);

			// write the file
			using (var file = File.CreateText (outputFile.FullName)) {
				file.Write (content);
				file.Flush ();
			}

			// refreshing the AssetDatabase with a small delay to make sure the tag mask is up-to-date
			EditorApplication.delayCall += () => { AssetDatabase.Refresh (ImportAssetOptions.ForceUpdate); };

			return true;
		}

		private static string GetEnumFileContent (string[] tags) {
			StringBuilder sb = new StringBuilder ();

			string info = "/*\tTHIS FILE IS AUTO-GENERATED. MANUAL CHANGES MAY RESULT IN ERRORS AND SHOULD BE AVOIDED.\t*/";
			string summary = "This BitMask enum represents the internal Unity tags as actual values. You can simply use it on any object by clicking the \"Implement Tags\" button in the Transform.";
			string warning = "| Warning: The amount of tags in the project exceeds 32. Not all tags will be included in the bitmask.";

			// create file header
			sb.Append (info);
			sb.Append ("\n\nusing System;");
			sb.Append ("\n\nnamespace UnityEngine {");

			// create enum declaration
			sb.Append ($"\n\n\t///<summary>{summary}</summary>\n\t[Flags]");
			sb.Append ($"\n\tpublic enum Tags : int\t// {tags.Length} of 32 {(tags.Length > 32 ? warning : "")}\n\t{{");

			// restrict the number of iterations so it does not wrap around
			int iterations = Mathf.Min (tags.Length, 32);

			HashSet<string> usedTags = new HashSet<string> ();

			for (int i = 0; i < iterations; i++) {

				// convert the tag index to a number that represents its bit
				int val = NumFromBit (i);

				// make sure the tag text won't cause syntax errors

				// remove leading or trailing spaces
				string tag = tags[i].Trim ();

				// replace remaining spaces with underscores
				tag = Regex.Replace (tag, @"\s+", "_", RegexOptions.Compiled);

				// remove leading digits
				tag = Regex.Replace (tag, @"^[0-9]+", "", RegexOptions.Compiled);

				// if the previous changes resulted in an empty tag, create a new one
				if (tag.Length == 0) {
					tag = $"Tag_{i}";
				}

				// make sure there are no duplicate names in the enum
				// store current tag
				string tempTag = tag;
				// init counter
				int tagNum = 0;
				// try adding stored tag to the usedTags
				while (!usedTags.Add (tempTag)) {
					// if the stored tag cannot be added, it already exists
					// so overwrite the stored tag with the original + _counter
					tempTag = tag + $"_{tagNum}";
					// increase counter
					tagNum++;
				}
				// set tag to stored value
				tag = tempTag;

				// generate a summary in case the tag in the enum is not the same as in the editor
				string tagSummary = !tag.Equals (tags[i]) ? $"\t\t/// <summary> Tag name was changed to maintain syntax compatibility. Original Name: '{tags[i]}' </summary>\n" : "";

				// append the tag to the enum
				sb.Append ($"\n{tagSummary}\t\t{tag} = {val}{(i < iterations - 1 ? "," : "")}");
			}

			// close off the enum declaration
			sb.Append ("\n\t}\n}");
			sb.Append ($"\n\n{info}");

			return sb.ToString ();
		}

		// method for calculating the bit mask value of an enum item, based on an index
		private static int NumFromBit (int bit) {
			// 2^(n - 1)
			return (int)Mathf.Pow (2, bit - 1);
		}

		// method for comparing two string arrays. used to make sure the mask file is not updated unless there are changed tags
		private static bool Compare (string[] a, string[] b) {

			// if the lengths of the arrays are unequal
			if (a.Length != b.Length) {
				return false;
			}

			// iterate over the arrays
			for (int i = 0; i < a.Length; i++) {

				// compare the individual elements
				if (!a[i].Equals (b[i])) {
					return false;
				}
			}

			return true;
		}

		private string GetFullPath (string name) {
			if (string.IsNullOrWhiteSpace (name)) {
				return null;
			}
			return Path.Combine (Application.dataPath, name);
		}


		string[] IScriptFactoryExtension.GetFactoryButtonLabels () {
			return new string[] { "Generate and Enable Tags", "Disable Tags" };
		}

		bool IScriptFactoryExtension.IsFactoryButtonInteractable (int index) {
			switch (index) {
				case 0:
					return true;
				case 1:
					return SymbolUtility.HasDebugSymbol (SYMBOL);
			}
			return false;
		}

		void IScriptFactoryExtension.OnFactoryButtonClick (int index) {
			switch (index) {
				case 0:
					EnableTags ();
					break;
				case 1:
					DisableTags ();
					break;
			}
		}
	}
}
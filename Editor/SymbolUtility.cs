using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Zenvin.ScriptGeneration {
	public static class SymbolUtility {

		private static HashSet<string> symbols = null;


		public static bool AddDebugSymbol (string symbol, BuildTargetGroup? targetGroup = null) {
			var group = GetGroup (targetGroup);
			UpdateSymbolSet (group);
			if (IsValidDebugSymbol (symbol) && symbols.Add (symbol)) {
				UpdateGroupSymbols (group);
				return true;
			}
			return false;
		}

		public static bool RemoveDebugSymbol (string symbol, BuildTargetGroup? targetGroup = null) {
			var group = GetGroup (targetGroup);
			UpdateSymbolSet (group);
			if (symbols.Remove (symbol)) {
				UpdateGroupSymbols (group);
				return true;
			}
			return false;
		}

		public static bool HasDebugSymbol (string symbol, BuildTargetGroup? targetGroup = null) {
			UpdateSymbolSet (GetGroup (targetGroup));
			return IsValidDebugSymbol (symbol) && symbols.Contains (symbol);
		}

		public static bool IsValidDebugSymbol (string symbol) {
			return Regex.IsMatch (symbol, @"[A-Za-z_][A-Za-z0-9_]+");
		}

		private static BuildTargetGroup GetGroup (BuildTargetGroup? group) {
			return group.HasValue ? group.Value : EditorUserBuildSettings.selectedBuildTargetGroup;
		}

		private static void UpdateSymbolSet (BuildTargetGroup group) {
			if (symbols == null) {
				var target = PlayerSettings.GetScriptingDefineSymbolsForGroup (group);
				symbols = new HashSet<string> (target.Split (';'));
			}
		}

		private static void UpdateGroupSymbols (BuildTargetGroup group) {
			if (symbols == null) {
				return;
			}
			var symbolString = string.Join (";", symbols);
			PlayerSettings.SetScriptingDefineSymbolsForGroup (group, symbolString);
		}

	}
}

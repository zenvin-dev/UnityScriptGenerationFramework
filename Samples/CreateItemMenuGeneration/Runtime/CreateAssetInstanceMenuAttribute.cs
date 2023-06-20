using System;

namespace Zenvin.ScriptGeneration {
	[AttributeUsage (AttributeTargets.Class)]
	public class CreateAssetInstanceMenuAttribute : Attribute {
		public readonly string Prefix;
		public readonly string Suffix;


		public CreateAssetInstanceMenuAttribute (string prefix = "", string suffix = "") {
			Prefix = prefix;
			Suffix = suffix;
		}
	}
}
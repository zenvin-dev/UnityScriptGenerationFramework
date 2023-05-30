using System;

namespace Zenvin {
	[AttributeUsage(AttributeTargets.Property)]
	public class StringPropertyDecoratorAttribute : Attribute {
		public readonly string Prefix;
		public readonly string Suffix;

		public StringPropertyDecoratorAttribute (string prefix, string suffix) {
			Prefix = prefix;
			Suffix = suffix;
		}
	}
}
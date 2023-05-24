using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using Zenvin.EditorUtil;
using Zenvin.ProjectPreferences;
using static System.Reflection.BindingFlags;
using PropList = System.Collections.Generic.List<Zenvin.ScriptGeneration.FactoryProperty>;

namespace Zenvin.ScriptGeneration {
	[InitializeOnLoad]
	public sealed class ScriptGenerator {

		private static readonly ScriptGenerator instance;

		private readonly List<ScriptFactory> factories = new List<ScriptFactory> ();
		private readonly Dictionary<Type, PropList> serializedProperties = new Dictionary<Type, PropList> ();

		public static int FactoryCount => instance.factories.Count;


		static ScriptGenerator () {
			instance = new ScriptGenerator ();
		}

		private ScriptGenerator () {
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReloadDomains;
			AssemblyReloadEvents.afterAssemblyReload += OnAfterReloadDomains;
		}


		internal static FactoryInfo GetFactory (int index) {
			if (index < 0 || index >= FactoryCount) {
				return null;
			}
			var factory = instance.factories[index];
			var properties = instance.serializedProperties.TryGetValue (factory.GetType (), out PropList value) ? value : new PropList ();
			return new FactoryInfo (factory, properties);
		}

		internal static void Save () {
			instance.SerializeFactories ();
		}


		private void OnBeforeReloadDomains () {
			SerializeFactories ();
		}

		private void OnAfterReloadDomains () {
			SetupFactories ();
		}

		private void SetupFactories () {
			var factoryTypes = TypeCache.GetTypesDerivedFrom<ScriptFactory> ();
			foreach (var type in factoryTypes) {
				if (type.IsAbstract) {
					continue;
				}
				var factory = Activator.CreateInstance (type) as ScriptFactory;
				if (factory != null) {
					factories.Add (factory);
					AnalyzeType (type);
					DeserializeFactory (factory, type);
					if (factory.Enabled) {
						factory.Setup ();
					}
				}
			}
		}

		private void AnalyzeType (Type type) {
			if (type == null || serializedProperties.ContainsKey (type)) {
				return;
			}

			var list = new PropList ();
			var properties = type.GetProperties (Instance | FlattenHierarchy | Public);
			foreach (var prop in properties) {
				var pType = prop.PropertyType;
				if (pType != typeof (bool) && pType != typeof (int) && pType != typeof (string) && pType != typeof (float)) {
					continue;
				}

				var mGet = prop.GetGetMethod ();
				var mSet = prop.GetSetMethod ();
				if (mGet == null || mSet == null || mGet.IsAbstract || mSet.IsAbstract) {
					continue;
				}

				var tooltipAttr = prop.GetCustomAttribute<PropertyTooltipAttribute> ();
				list.Insert (0, new FactoryProperty (prop, tooltipAttr?.Tooltip));
			}

			serializedProperties[type] = list;
		}

		private void DeserializeFactory (ScriptFactory factory, Type type) {
			if (!serializedProperties.TryGetValue (type, out PropList list)) {
				return;
			}

			var parameter = new object[1];
			foreach (var prop in list) {
				if (TryGetPropertyValue (type, prop.Property, out object value) && value != null && value.GetType () == prop.PropertyType) {
					parameter[0] = value;
					prop.GetSetMethod ().Invoke (factory, parameter);
				}
			}
		}

		private bool TryGetPropertyValue (Type type, PropertyInfo prop, out object value) {
			var key = GetPropertyPrefKey (type, prop);
			return ProjectPrefs.TryGetValue (key, out value);
		}

		private void SerializeFactories () {
			foreach (var factory in factories) {
				SerializeObject (factory);
			}
		}

		private void SerializeObject (object obj) {
			if (obj == null) {
				return;
			}

			var type = obj.GetType ();
			if (!serializedProperties.TryGetValue (type, out PropList list)) {
				return;
			}

			for (int i = 0; i < list.Count; i++) {
				SerializeProperty (obj, list[i].Property);
			}
		}

		private void SerializeProperty (object factory, PropertyInfo info) {
			if (info == null) {
				return;
			}

			PrefKey key = GetPropertyPrefKey (factory.GetType (), info);
			PrefValue val = null;

			object prop = info.GetValue (factory);
			switch (prop) {
				case bool @bool:
					val = @bool;
					break;
				case int @int:
					val = @int;
					break;
				case string @string:
					val = @string;
					break;
				case float @float:
					val = @float;
					break;
			}

			ProjectPrefs.SetValue (key, val);
		}

		internal static PrefKey GetPropertyPrefKey (Type type, PropertyInfo info) {
			return new PrefKey ("%Zenvin.ScriptGeneration", $"{type.FullName}", info.Name);
		}
	}

	internal class FactoryInfo {
		private readonly PropList properties;
		public readonly ScriptFactory Factory;

		public int PropertyCount => properties.Count;
		public FactoryProperty this[int index] => properties[index];


		internal FactoryInfo (ScriptFactory factory, PropList properties) {
			this.properties = properties;
			Factory = factory;
		}
	}

	internal class FactoryProperty {
		public readonly PropertyInfo Property;
		public readonly string Tooltip;


		public string Name => Property.Name;
		public Type PropertyType => Property.PropertyType;


		public FactoryProperty (PropertyInfo property, string tooltip = null) {
			Property = property;
			Tooltip = tooltip;
		}


		public MethodInfo GetSetMethod () => Property.GetSetMethod ();
		public MethodInfo GetGetMethod () => Property.GetGetMethod ();
	}
}
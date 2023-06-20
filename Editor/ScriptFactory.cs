using System;
using UnityEngine;

namespace Zenvin.ScriptGeneration {
	public abstract class ScriptFactory {
		internal event Action<string> FactoryPropertyError;

		private bool enabled;

		/// <summary> Whether the properties of this factory instance have been deserialized yet. </summary>
		public bool IsDeserialized { get; internal set; } = false;
		/// <summary> Whether the factory is enabled. <br></br> <see cref="Setup"/> will not be called if this is set to <see langword="false"/>. </summary>
		public bool Enabled { get => enabled; set => SetEnabled (value); }
		/// <summary> Whether the editor for this factory remains unlocked during play mode. </summary>
		public virtual bool AllowPlaymodeChanges => false;


		/// <summary>
		/// Called when the factory is enabled, after it was deserialized.
		/// </summary>
		internal protected virtual void Setup () { }

		/// <summary>
		/// Called after all pending property changes from the editor were applied.
		/// </summary>
		internal protected virtual void OnApply () { }

		/// <summary>
		/// Called after <see cref="Enabled"/> was set to <see langword="true"/> and applied.
		/// </summary>
		protected virtual void OnEnabled () { }

		/// <summary>
		/// Called after <see cref="Enabled"/> was set to <see langword="false"/> and applied.
		/// </summary>
		protected virtual void OnDisabled () {

		}

		/// <summary>
		/// Outputs an error message. <br></br>
		/// If NOT called during the serialization process, logged errors will appear next to the property that is being applied.
		/// </summary>
		/// <param name="message">The error message to be displayed. On console messages, prefixes will be added for clarity.</param>
		/// <param name="forceConsole">Forces console output, even if deserialization is not happening.</param>
		protected void LogError (string message, bool forceConsole = false) {
			if (string.IsNullOrEmpty(message)) {
				return;
			}
			if (IsDeserialized || forceConsole) {
				Debug.LogError ($"[ScriptGeneration] [{GetType().FullName}] " + message);
			} 
			if(!IsDeserialized) {
				FactoryPropertyError?.Invoke (message);
			}
		}


		private void SetEnabled (bool state) {
			if (state == enabled) {
				return;
			}
			enabled = state;
			if (state) {
				OnEnabled ();
			} else {
				OnDisabled ();
			}
		}
	}

	public interface IScriptFactoryExtension {
		GUIContent[] GetFactoryButtonLabels ();
		bool IsFactoryButtonInteractable (int index);
		void OnFactoryButtonClick (int index);
	}
}
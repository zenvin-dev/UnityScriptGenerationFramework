namespace Zenvin.ScriptGeneration {
	public abstract class ScriptFactory {
		private bool enabled;

		public bool Enabled { get => enabled; set => SetEnabled (value); }

		internal protected virtual void Setup () { }

		internal protected virtual void OnApply () { }

		protected virtual void OnEnabled () { }

		protected virtual void OnDisabled () {

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
		string[] GetFactoryButtonLabels ();
		bool IsFactoryButtonInteractable (int index);
		void OnFactoryButtonClick (int index);
	}
}
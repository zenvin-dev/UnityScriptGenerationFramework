using System;

namespace Zenvin.ScriptGeneration {
	[Serializable]
	public sealed class GenerationResult {
		public enum State {
			Success,
			Warning,
			Error,
		}

		public State ResultState;
		public string Message;
		public long Timestamp;


		public GenerationResult (State resultState, string message) : this (resultState) {
			ResultState = resultState;
			Message = message;
		}

		public GenerationResult (State resultState) : this () {
			ResultState = resultState;
		}

		private GenerationResult () {
			Timestamp = DateTime.Now.Ticks;
		}
	}
}

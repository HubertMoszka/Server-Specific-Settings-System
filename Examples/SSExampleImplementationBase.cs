namespace UserSettings.ServerSpecific.Examples
{
	/// <summary>
	/// Provides example implementation of a server-specific settings collection.
	/// </summary>
	public abstract class SSExampleImplementationBase
	{
		private static SSExampleImplementationBase _activeExample;

		/// <summary>
		/// All defined implementations of this class.
		/// </summary>
		public static readonly SSExampleImplementationBase[] AllExamples =
		{
			new SSFieldsDemoExample(),
			new SSAbilityExample(),
			new SSTextAreaExample(),
			new SSPrimitiveSpawnerExample()
		};

		/// <summary>
		/// Name of the example, used in a list.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Triggered via a command to activate the implementation.
		/// </summary>
		public abstract void Activate();

		/// <summary>
		/// Triggered when another example implementation is activated. 
		/// </summary>
		public abstract void Deactivate();

		/// <summary>
		/// Attempts to select an example from an index. Fails if out of range.
		/// </summary>
		public static bool TryActivateExample(int index, out string message)
		{
			if (!AllExamples.TryGet(index, out SSExampleImplementationBase ex))
			{
				message = $"Index {index} out of range.";
				return false;
			}

			if (_activeExample == ex)
			{
				message = $"Example is already active";
				return false;
			}

			TryDeactivateExample(out _);

			_activeExample = ex;
			_activeExample.Activate();

			message = ex.Name + " activated."; 
			return true;
		}

		/// <summary>
		/// Deactivates current example implementations. Fails if none is active.
		/// </summary>
		public static bool TryDeactivateExample(out string disabledName)
		{
			if (_activeExample == null)
			{
				disabledName = null;
				return false;
			}

			disabledName = _activeExample.Name;

			_activeExample.Deactivate();
			_activeExample = null;

			ServerSpecificSettingsSync.DefinedSettings = null;
			ServerSpecificSettingsSync.SendToAll();

			return true;
		}
	}
}

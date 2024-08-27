using Mirror;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Allows some entries to be updated without having to re-send all settings. Useful for dynamically-adjustable text.
	/// </summary>
	public interface ISSUpdatable
	{
		/// <summary>
		/// Used to deserialize data from an update message.
		/// </summary>
		void DeserializeUpdate(NetworkReader reader);
	}
}

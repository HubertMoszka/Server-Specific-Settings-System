using System;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Interface added to entries that are spawned in user settings window.
	/// </summary>
	public interface ISSEntry
	{
		/// <summary>
		/// Type of <see cref="ServerSpecificSettingBase"/> implementation this entry controls.
		/// </summary>
		Type SettingType { get; }

		/// <summary>
		/// Called when an entry is spawned. Used to link the specific setting to control.
		/// </summary>
		void Init(ServerSpecificSettingBase setting);
	}
}

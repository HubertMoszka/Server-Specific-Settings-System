using System;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Interface added to entries that are spawned in user settings window.
	/// </summary>
	public interface ISSEntry
	{
		/// <summary>
		/// Called on the entry template, returns true if the template can be used to represent the setting.
		/// <br /> If multiple templates return true, priority is defined by lowest index in <see cref="SSEntrySpawner._entryTemplates"/>.
		/// </summary>
		bool CheckCompatibility(ServerSpecificSettingBase setting);

		/// <summary>
		/// Called when an entry is spawned. Used to link the specific setting to control.
		/// </summary>
		void Init(ServerSpecificSettingBase setting);
	}
}

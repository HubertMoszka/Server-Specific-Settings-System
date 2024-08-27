using System;
using TMPro;
using UnityEngine;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Server-specific entry representing a header group.
	/// </summary>
	public class SSDropdownEntry : UserSettingsDropdown, ISSEntry
	{
		private SSDropdownSetting _setting;

		[SerializeField]
		private SSEntryLabel _label;

		/// <inheritdoc />
		public Type SettingType => typeof(SSDropdownSetting);

		/// <inheritdoc />
		protected override void SaveValue(int val)
		{
			PlayerPrefsSl.Set(_setting.PlayerPrefsKey, val);

			_setting.SyncSelectionIndexRaw = val;
			_setting.ClientSendValue();
		}

		/// <inheritdoc />
		protected override int ReadSavedValue()
		{
			_setting.SyncSelectionIndexRaw = PlayerPrefsSl.Get(_setting.PlayerPrefsKey, _setting.DefaultOptionIndex);
			return _setting.SyncSelectionIndexRaw;
		}

		/// <inheritdoc />
		public void Init(ServerSpecificSettingBase setting)
		{
			_setting = setting as SSDropdownSetting;
			_label.Set(_setting);

			TargetUI.options.Clear();

			foreach (string option in _setting.Options)
			{
				TargetUI.options.Add(new TMP_Dropdown.OptionData(option));
			}

			TargetUI.RefreshShownValue();
			Setup();
		}
	}
}

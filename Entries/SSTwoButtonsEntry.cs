using TMPro;
using UnityEngine;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Server-specific entry representing a bindable key.
	/// </summary>
	public class SSTwoButtonsEntry : UserSettingsTwoButtons, ISSEntry
	{
		private SSTwoButtonsSetting _setting;

		[SerializeField]
		private TMP_Text _optionA, _optionB;

		[SerializeField]
		private SSEntryLabel _label;

		/// <inheritdoc />
		protected override void SaveValue(bool val)
		{
			PlayerPrefsSl.Set(_setting.PlayerPrefsKey, val);

			_setting.SyncIsB = val;
			_setting.ClientSendValue();
		}

		/// <inheritdoc />
		protected override bool ReadSavedValue()
		{
			_setting.SyncIsB = PlayerPrefsSl.Get(_setting.PlayerPrefsKey, _setting.DefaultIsB);
			return _setting.SyncIsB;
		}

		/// <inheritdoc />
		public bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSTwoButtonsSetting;
		}

		/// <inheritdoc />
		public void Init(ServerSpecificSettingBase setting)
		{
			_setting = setting as SSTwoButtonsSetting;

			_label.Set(_setting);
			_optionA.text = _setting.OptionA;
			_optionB.text = _setting.OptionB;

			Setup();
			UpdateColors(true);
		}
	}
}

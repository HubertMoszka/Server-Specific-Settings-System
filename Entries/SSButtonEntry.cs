using System;
using TMPro;
using UnityEngine;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Server-specific entry representing a header group.
	/// </summary>
	public class SSButtonEntry : HoldableButton, ISSEntry
	{
		[SerializeField]
		private SSEntryLabel _label;

		[SerializeField]
		private TMP_Text _buttonText;

		private float _holdTime;
		private SSButton _setting;

		/// <inheritdoc />
		public Type SettingType => typeof(SSButton);

		/// <inheritdoc />
		public override float HoldTime => _holdTime;

		/// <inheritdoc />
		public void Init(ServerSpecificSettingBase settingBase)
		{
			_setting = settingBase as SSButton;

			_label.Set(_setting);
			_holdTime = _setting.HoldTimeSeconds;

			if (_holdTime > 0)
				OnHeld.AddListener(OnTrigger);
			else
				onClick.AddListener(OnTrigger);

			_buttonText.text = _setting.ButtonText;
		}

		private void OnTrigger()
		{
			_setting.ClientSendValue();
		}
	}
}

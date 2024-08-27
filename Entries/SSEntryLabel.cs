using System.Collections.Generic;
using UserSettings.GUIElements;
using Mirror.LiteNetLib4Mirror;
using UnityEngine;
using System;
using TMPro;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Utility class for any implementation of <see cref="ISSEntry"/>. Controls label and its hint.
	/// </summary>
	[Serializable]
	public class SSEntryLabel
	{
		[SerializeField]
		private TMP_Text _label;

		[SerializeField]
		private CustomUserSettingsEntryDescription _hint;

		/// <summary>
		/// Assigns label and hint for provided setting instance.
		/// </summary>
		public void Set(ServerSpecificSettingBase setting)
		{
			_label.text = setting.Label;

			if (string.IsNullOrEmpty(setting.HintDescription))
			{
				_hint.gameObject.SetActive(false);
			}
			else
			{
				_hint.gameObject.SetActive(true);
				_hint.SetCustomText(setting.HintDescription);
			}
		}
	}
}

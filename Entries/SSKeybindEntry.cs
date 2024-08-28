using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Server-specific entry representing a bindable key.
	/// </summary>
	public class SSKeybindEntry : KeycodeField, ISSEntry
	{
		private SSKeybindSetting _setting;

		[SerializeField]
		private Image _undoImage;

		[SerializeField]
		private Image _suggestionImage;

		[SerializeField]
		private SSEntryLabel _label;

		/// <summary>
		/// Applies keys suggested by the server.
		/// </summary>
		public void ApplySuggestion()
		{
			ApplyPressedKey(_setting.SuggestedKey);
		}

		/// <inheritdoc />
		public bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSKeybindSetting;
		}

		/// <inheritdoc />
		public void Init(ServerSpecificSettingBase setting)
		{
			_setting = setting as SSKeybindSetting;

			_label.Set(setting);

			_undoImage.GetComponent<Button>().onClick.AddListener(PressUndo);
			_suggestionImage.GetComponent<Button>().onClick.AddListener(ApplySuggestion);

			ApplyPressedKey((KeyCode) PlayerPrefsSl.Get(_setting.PlayerPrefsKey, (int) KeyCode.None));
		}

		/// <inheritdoc />
		protected override void ApplyPressedKey(KeyCode key)
		{
			base.ApplyPressedKey(key);

			_setting.AssignedKeyCode = key;
			PlayerPrefsSl.Set(_setting.PlayerPrefsKey, (int) key);

			_undoImage.enabled = key != KeyCode.None;
			_suggestionImage.enabled = key == KeyCode.None && _setting.SuggestedKey != KeyCode.None;
		}

		private void PressUndo()
		{
			ApplyPressedKey(KeyCode.None);
		}
	}
}

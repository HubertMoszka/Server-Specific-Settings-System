using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Server-specific entry representing a bindable key.
	/// </summary>
	public class SSPlaintextEntry : UserSettingsUIBase<TMP_InputField, string>, ISSEntry
	{
		private SSPlaintextSetting _setting;

		[SerializeField]
		private TMP_InputField _inputField;

		[SerializeField]
		private SSEntryLabel _label;

		[SerializeField]
		private TMP_Text _placeholder;

		/// <inheritdoc />
		protected override UnityEvent<string> OnValueChangedEvent => TargetUI.onEndEdit;

		/// <inheritdoc />
		public bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSPlaintextSetting;
		}

		/// <inheritdoc />
		public void Init(ServerSpecificSettingBase setting)
		{
			_setting = setting as SSPlaintextSetting;
			_setting.OnClearRequested += ClearField;

			_label.Set(_setting);

			_placeholder.text = _setting.Placeholder;
			_inputField.contentType = _setting.ContentType;
			_inputField.characterLimit = _setting.CharacterLimit;

			Setup();
		}

		/// <inheritdoc />
		protected override void Awake()
		{
			base.Awake();

			TargetUI.onEndEdit.AddListener((_) =>
			{
				EventSystem cur = EventSystem.current;

				if (!cur.alreadySelecting)
					cur.SetSelectedGameObject(null);
			});
		}

		/// <inheritdoc />
		protected override void SaveValue(string val)
		{
			PlayerPrefsSl.Set(_setting.PlayerPrefsKey, val);
			_setting.SyncInputText = val;
			_setting.ClientSendValue();
		}

		/// <inheritdoc />
		protected override string ReadSavedValue()
		{
			_setting.SyncInputText = PlayerPrefsSl.Get(_setting.PlayerPrefsKey, string.Empty);
			return _setting.SyncInputText;
		}

		/// <inheritdoc />
		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (_setting != null)
				_setting.OnClearRequested -= ClearField;
		}

		/// <inheritdoc />
		protected override void SetValueAndTriggerEvent(string val)
		{
			_inputField.text = val;
		}

		/// <inheritdoc />
		protected override void SetValueWithoutNotify(string val)
		{
			_inputField.SetTextWithoutNotify(val);
		}

		private void ClearField()
		{
			if (_inputField.isFocused)
				return;

			SetValueAndTriggerEvent(string.Empty);
		}
	}
}

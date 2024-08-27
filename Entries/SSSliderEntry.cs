using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Server-specific entry representing a slider.
	/// </summary>
	public class SSSliderEntry : UserSettingsSlider, ISSEntry, IPointerUpHandler
	{
		private SSSliderSetting _setting;

		[SerializeField]
		private SSEntryLabel _label;

		[SerializeField]
		private TMP_InputField _inputField;

		/// <inheritdoc />
		public Type SettingType => typeof(SSSliderSetting);

		private void UpdateFieldText(float value)
		{
			string valStr = value.ToString(_setting.ValueToStringFormat);
			_inputField.SetTextWithoutNotify(string.Format(_setting.FinalDisplayFormat, valStr));
		}

		private void OnDisable()
		{
			if (_setting == null || !_setting.SyncDragging)
				return;

			_setting.SyncDragging = false;
			_setting.ClientSendValue();
		}

		/// <inheritdoc />
		protected override void SaveValue(float val)
		{
			PlayerPrefsSl.Set(_setting.PlayerPrefsKey, val);

			_setting.SyncDragging = true;
			_setting.SyncFloatValue = val;
			_setting.ClientSendValue();
		}

		/// <inheritdoc />
		protected override float ReadSavedValue()
		{
			_setting.SyncFloatValue = PlayerPrefsSl.Get(_setting.PlayerPrefsKey, _setting.DefaultValue);
			return _setting.SyncFloatValue;
		}

		/// <inheritdoc />
		public void Init(ServerSpecificSettingBase setting)
		{
			_setting = setting as SSSliderSetting;

			_label.Set(_setting);

			TargetUI.minValue = _setting.MinValue;
			TargetUI.maxValue = _setting.MaxValue;
			TargetUI.wholeNumbers = _setting.Integer;

			_inputField.contentType = _setting.Integer
				? TMP_InputField.ContentType.IntegerNumber
				: TMP_InputField.ContentType.DecimalNumber;

			_inputField.onEndEdit.AddListener((str) =>
			{
				if (!float.TryParse(str, out float res))
				{
					SetValueAndTriggerEvent(StoredValue);
					return;
				}

				EventSystem cur = EventSystem.current;

				if (!cur.alreadySelecting)
					cur.SetSelectedGameObject(null);

				res = Mathf.Clamp(res, _setting.MinValue, _setting.MaxValue);
				SetValueAndTriggerEvent(res);
				UpdateFieldText(res);
			});

			_inputField.onSelect.AddListener((_) =>
			{
				_inputField.SetTextWithoutNotify(TargetUI.value.ToString());
			});

			TargetUI.onValueChanged.AddListener(UpdateFieldText);

			Setup();

			UpdateFieldText(TargetUI.value);
		}

		/// <inheritdoc />
		public void OnPointerUp(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;

			if (!_setting.SyncDragging)
				return;

			_setting.SyncDragging = false;
			_setting.ClientSendValue();
		}

	}
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EntryType = UserSettings.ServerSpecific.SSDropdownSetting.DropdownEntryType;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Server-specific entry representing a dropdown menu with arrows to the sides.
	/// </summary>
	public class SSScrollableDropdownEntry : SSDropdownEntry, ISSEntry
	{
		[SerializeField]
		private Button _prevArrow;

		[SerializeField]
		private Button _nextArrow;

		[SerializeField]
		private GameObject _hybridArrow;

		private bool _loopable;

		/// <summary>
		/// Moves to the next option if possible.
		/// </summary>
		public void Next()
		{
			int newValue = TargetUI.value + 1;
			TargetUI.value = newValue % TargetUI.options.Count;

			UpdateInteractability();
		}

		/// <summary>
		/// Moves to the previous option if possible.
		/// </summary>
		public void Prev()
		{
			if (TargetUI.value > 0)
				TargetUI.value--;
			else
				TargetUI.value = TargetUI.options.Count - 1;

			UpdateInteractability();
		}

		/// <inheritdoc />
		public override bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSDropdownSetting dropdown && dropdown.EntryType != EntryType.Regular;
		}

		/// <inheritdoc />
		public override void Init(ServerSpecificSettingBase setting)
		{
			base.Init(setting);

			EntryType type = (setting as SSDropdownSetting).EntryType;
			_loopable = type is EntryType.ScrollableLoop or EntryType.HybridLoop;

			if (type is not EntryType.Hybrid and not EntryType.HybridLoop)
			{
				_hybridArrow.SetActive(false);
				TargetUI.interactable = false;
				TargetUI.captionText.alignment = TextAlignmentOptions.Center;

				// Centerize the text
				RectTransform rt = TargetUI.captionText.rectTransform;
				rt.anchoredPosition = new Vector2(0, rt.anchoredPosition.y);
			}

			UpdateInteractability();
			TargetUI.onValueChanged.AddListener(_ => UpdateInteractability());
		}

		private void UpdateInteractability()
		{
			if (_loopable)
				return;

			_prevArrow.interactable = TargetUI.value > 0;
			_nextArrow.interactable = TargetUI.value < TargetUI.options.Count - 1;
		}
	}
}

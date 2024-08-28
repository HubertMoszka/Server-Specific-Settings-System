using UnityEngine;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Server-specific entry representing a header group.
	/// </summary>
	public class SSGroupHeaderEntry : MonoBehaviour, ISSEntry
	{
		[SerializeField]
		private SSEntryLabel _label;

		[SerializeField]
		private float _normalPadding, _shortPadding;

		/// <inheritdoc />
		public bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSGroupHeader;
		}

		/// <inheritdoc />
		public void Init(ServerSpecificSettingBase setting)
		{
			RectTransform rt = transform as RectTransform;

			bool reducedPadding = (setting as SSGroupHeader).ReducedPadding;
			float height = reducedPadding ? _shortPadding : _normalPadding;
			rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
			
			_label.Set(setting);
		}
	}
}

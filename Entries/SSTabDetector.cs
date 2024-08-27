using System;
using UnityEngine;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Simple component added to the server-specific settings tab.
	/// A relatively performance-netural way of detecting whether the tab is currently active.
	/// </summary>
	public class SSTabDetector : MonoBehaviour
	{
		private static bool _active;

		/// <summary>
		/// Event called when <see cref="IsOpen"/> changes.
		/// </summary>
		public static event Action OnStatusChanged;

		/// <summary>
		/// Returns true if user is currently looking at server-specific settings tab.
		/// </summary>
		public static bool IsOpen
		{
			get
			{
				return _active;
			}
			private set
			{
				if (_active == value)
					return;

				_active = value;
				OnStatusChanged?.Invoke();
			}
		}

		private void OnEnable()
		{
			IsOpen = true;
		}

		private void OnDisable()
		{
			IsOpen = false;
		}
	}
}

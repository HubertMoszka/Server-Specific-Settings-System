using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Allows servers to provide players with user-defined keybind fields which trigger a message upon pressing.
	/// </summary>
	public class SSKeybindSetting : ServerSpecificSettingBase
	{
		/// <summary>
		/// Returns true if the key is currently pressed by user.
		/// </summary>
		public bool SyncIsPressed { get; private set; }

		/// <summary>
		/// Returns true if key won't be pressable when any interactable UI window or text prompt is active (like inventory or RA) - highly recommended to be true.
		/// </summary>
		/// <remarks>For user privacy, input is always disabled whenever game console is active or the game is minimized.</remarks>
		public bool PreventInteractionOnGUI { get; private set; }

		/// <summary>
		/// Suggested key by the server. Optional, can be left as <see cref="KeyCode.None"/>.
		/// </summary>
		/// <remarks>
		/// For user privacy, keybinds received by the server are not automatically assigned (to prevent potential keylogging usage).
		/// This key is just a suggestion to user, but there is no way of checking which key actually ended up being assigned.
		/// </remarks>
		public KeyCode SuggestedKey { get; private set; }

		/// <summary>
		/// Keycode assigned by user.
		/// </summary>
		/// <remarks>
		/// Only valid for local player. For privacy, server does not know which key has been assigned to this action.
		/// </remarks>
		public KeyCode AssignedKeyCode { get; internal set; }

		/// <inheritdoc />
		public override string DebugValue => SyncIsPressed ? "Pressed" : "Released";

#if !HEADLESS
		private bool AllowInteraction
		{
			get
			{
				if (GameCore.Console.singleton.IsEnabled || !Application.isFocused)
					return false;

				if (!InventorySystem.GUI.InventoryGuiController.ItemsSafeForInteraction)
					return !PreventInteractionOnGUI;

				return true;
			}
		}
#endif

		/// <summary>
		/// Creates a new field where user can select one out of two options.
		/// </summary>
		/// <param name="id">Unique identifier of the setting. If there are multiple settings of the same type, the ID must be different. You can provide null to generate identifier based on label hash code (not recommended for larger systems).</param>
		/// <param name="label">Shown next to the entry. It should briefly describe what the setting is for.</param>
		/// <param name="suggestedKey">Optional, can be left as <see cref="KeyCode.None"/>. Suggested key by the server owner, but must be confirmed by the user.</param>
		/// <param name="preventInteractionOnGui">Prevents key from being pressed when any popup window is active. Recommended to leave as "true" to prevent misinput.</param>
		/// <param name="hint">Causes the "(?)" icon to appear next to the label. Can be used to provide additional information. Null or empty to disable.</param>
		public SSKeybindSetting(int? id, string label, KeyCode suggestedKey = KeyCode.None, bool preventInteractionOnGui = true, string hint = null)
		{
			SetId(id, label);
			Label = label;
			SuggestedKey = suggestedKey;
			PreventInteractionOnGUI = preventInteractionOnGui;
			HintDescription = hint;
		}

		/// <inheritdoc />
		public override void ApplyDefaultValues()
		{
			SyncIsPressed = false;
		}

		/// <inheritdoc />
		public override void DeserializeValue(NetworkReader reader)
		{
			SyncIsPressed = reader.ReadBool();
		}

		/// <inheritdoc />
		public override void SerializeValue(NetworkWriter writer)
		{
			writer.WriteBool(SyncIsPressed);
		}

		/// <inheritdoc />
		public override void DeserializeEntry(NetworkReader reader)
		{
			base.DeserializeEntry(reader);

			PreventInteractionOnGUI = reader.ReadBool();
			SuggestedKey = (KeyCode) reader.ReadInt();
		}

		/// <inheritdoc />
		public override void SerializeEntry(NetworkWriter writer)
		{
			base.SerializeEntry(writer);

			writer.WriteBool(PreventInteractionOnGUI);
			writer.WriteInt((int) SuggestedKey);
		}

#if !HEADLESS
		/// <inheritdoc />
		public override void OnUpdate()
		{
			base.OnUpdate();

			bool newPressed = AssignedKeyCode != KeyCode.None && Input.GetKey(AssignedKeyCode) && AllowInteraction;

			if (newPressed != SyncIsPressed)
			{
				SyncIsPressed = newPressed;
				ClientSendValue();
			}
		}
#endif
	}
}

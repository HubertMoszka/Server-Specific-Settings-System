using System.Diagnostics;
using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Allows servers to provide players with a button displayed inside settings which trigger a message upon pressing.
	/// </summary>
	public class SSButton : ServerSpecificSettingBase
	{
		/// <summary>
		/// Stopwatch which is triggered when user presses the button. If it's not running, user has never pressed the button.
		/// </summary>
		public readonly Stopwatch SyncLastPress = new(); 

		/// <summary>
		/// Time required for the button to remain held by user in order to trigger activation. Zero or less for instant activation on click.
		/// </summary>
		public float HoldTimeSeconds { get; private set; }

		/// <summary>
		/// Text displayed on the button.
		/// </summary>
		public string ButtonText { get; private set; }

		/// <inheritdoc />
		public override UserResponseMode ResponseMode => UserResponseMode.ChangeOnly;

		/// <inheritdoc />
		public override string DebugValue => SyncLastPress.IsRunning ? $"Pressed {SyncLastPress.Elapsed} ago" : "Never pressed";

		/// <summary>
		/// Creates a new button user can interact with in the settings.
		/// </summary> 
		/// <param name="id">Unique identifier of the setting. If there are multiple settings of the same type, the ID must be different. You can provide null to generate identifier based on label hash code (not recommended for larger systems).</param>
		/// <param name="label">Shown next to the entry. It should briefly describe what the button does.</param>
		/// <param name="buttonText">Text displayed inside the button. Usually in grammatical imperative mood (eg. "Execute", "Proceed", "Delete", "Abort", "Reset", etc).</param>
		/// <param name="holdTimeSeconds">Will require users to hold the button in order to trigger activation. Optional, null or non-positive float will result in immediate activation. Put "(Hold)" or similar message inside <paramref name="buttonText"/> for easier recognition.</param>
		/// <param name="hint">Causes the "(?)" icon to appear next to the label. Can be used to provide additional information. Null or empty to disable.</param>
		public SSButton(int? id, string label, string buttonText, float? holdTimeSeconds = null, string hint = null)
		{
			SetId(id, label);
			Label = label;
			HintDescription = hint;
			ButtonText = buttonText;
			HoldTimeSeconds = Mathf.Max(holdTimeSeconds ?? 0, 0);
		}

		/// <inheritdoc />
		public override void ApplyDefaultValues()
		{
			SyncLastPress.Reset();
		}

		/// <inheritdoc />
		public override void SerializeEntry(NetworkWriter writer)
		{
			base.SerializeEntry(writer);
			writer.WriteFloat(HoldTimeSeconds);
			writer.WriteString(ButtonText);
		}

		/// <inheritdoc />
		public override void DeserializeEntry(NetworkReader reader)
		{
			base.DeserializeEntry(reader);
			HoldTimeSeconds = reader.ReadFloat();
			ButtonText = reader.ReadString();
		}

		/// <inheritdoc />
		public override void DeserializeValue(NetworkReader reader)
		{
			base.DeserializeValue(reader);
			SyncLastPress.Restart();
		}
	}
}

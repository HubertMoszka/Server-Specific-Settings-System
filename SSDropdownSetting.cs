using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Allows servers to provide players with a dropdown field which stores an integer value that is sent to server.
	/// </summary>
	public class SSDropdownSetting : ServerSpecificSettingBase
	{
		/// <summary>
		/// List of options displayed in the dropdown.
		/// </summary>
		public string[] Options { get; private set; }

		/// <summary>
		/// Index of option selected by default.
		/// </summary>
		public int DefaultOptionIndex { get; private set; }

		/// <summary>
		/// Index selected by user, not validated.
		/// </summary>
		public int SyncSelectionIndexRaw { get; internal set; }

		/// <summary>
		/// Selected option by the user as plaintext. Prevalidated.
		/// </summary>
		/// <remarks>
		/// Returns empty string as fallback.
		/// </remarks>
		public string SyncSelectionText
		{
			get
			{
				if (OriginalDefinition is not SSDropdownSetting original)
					return string.Empty;

				int max = original.Options.Length - 1;
				int clamped = Mathf.Clamp(SyncSelectionIndexRaw, 0, max);

				return original.Options[clamped];
			}
		}

		/// <summary>
		/// More expensive method, but ensures the value will be within range of <see cref="Options"/>.
		/// </summary>
		public int SyncSelectionIndexValidated
		{
			get
			{
				if (OriginalDefinition is not SSDropdownSetting original)
					return 0;

				int max = original.Options.Length - 1;
				return Mathf.Clamp(SyncSelectionIndexRaw, 0, max);
			}
		}

		/// <inheritdoc />
		public override string DebugValue => $"{SyncSelectionIndexRaw} ({SyncSelectionText})";

		/// <summary>
		/// Creates a new field where user can select one out of any number of options.
		/// </summary>
		/// <param name="id">Unique identifier of the setting. If there are multiple settings of the same type, the ID must be different. You can provide null to generate identifier based on label hash code (not recommended for larger systems).</param>
		/// <param name="label">Shown next to the entry. It should briefly describe what the setting is for.</param>
		/// <param name="options">List of options displayed in the dropdown.</param>
		/// <param name="defaultOptionIndex">Index of <paramref name="options"/> displayed as default value.</param>
		/// <param name="hint">Causes the "(?)" icon to appear next to the label. Can be used to provide additional information. Null or empty to disable.</param>
		public SSDropdownSetting(int? id, string label, string[] options, int defaultOptionIndex = 0, string hint = null)
		{
			SetId(id, label);

			if (options == null || options.Length == 0)
				options = new string[0];

			Label = label;
			HintDescription = hint;

			Options = options;
			DefaultOptionIndex = defaultOptionIndex;
		}

		/// <inheritdoc />
		public override void ApplyDefaultValues()
		{
			SyncSelectionIndexRaw = 0;
		}

		/// <inheritdoc />
		public override void SerializeEntry(NetworkWriter writer)
		{
			base.SerializeEntry(writer);

			writer.WriteByte((byte) DefaultOptionIndex);
			writer.WriteByte((byte) Options.Length);
			Options.ForEach(writer.WriteString);
		}

		/// <inheritdoc />
		public override void DeserializeEntry(NetworkReader reader)
		{
			base.DeserializeEntry(reader);

			DefaultOptionIndex = reader.ReadByte();
			Options = new string[reader.ReadByte()];

			for (int i = 0; i < Options.Length; i++)
				Options[i] = reader.ReadString();
		}

		/// <inheritdoc />
		public override void SerializeValue(NetworkWriter writer)
		{
			base.SerializeValue(writer);
			writer.WriteByte((byte) SyncSelectionIndexRaw);
		}

		public override void DeserializeValue(NetworkReader reader)
		{
			base.DeserializeValue(reader);
			SyncSelectionIndexRaw = reader.ReadByte();
		}
	}
}

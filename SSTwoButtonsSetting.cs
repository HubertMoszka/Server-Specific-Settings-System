using Mirror;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Allows servers to provide players with a two-buttons field which store a boolean value that is sent to server.
	/// </summary>
	public class SSTwoButtonsSetting : ServerSpecificSettingBase
	{
		/// <summary>
		/// Returns true if user has selected option B instead of A.
		/// </summary>
		public bool SyncIsB { get; internal set; }

		/// <summary>
		/// Returns true if user has selected option A instead of B.
		/// </summary>
		public bool SyncIsA => !SyncIsB;

		/// <summary>
		/// Plaintext from server displayed inside the first button (to the left).
		/// </summary>
		public string OptionA { get; private set; }

		/// <summary>
		/// Plaintext from server displayed inside the second button (to the right).
		/// </summary>
		public string OptionB { get; private set; }

		/// <summary>
		/// If true, the option selected by default is B, instead of A.
		/// </summary>
		public bool DefaultIsB { get; private set; }

		/// <inheritdoc />
		public override string DebugValue => SyncIsB ? "B" : "A";

		/// <summary>
		/// Creates a new field where user can select one out of two options.
		/// </summary>
		/// <param name="id">Unique identifier of the setting. If there are multiple settings of the same type, the ID must be different. You can provide null to generate identifier based on label hash code (not recommended for larger systems).</param>
		/// <param name="label">Shown next to the entry. It should briefly describe what the setting is for.</param>
		/// <param name="optionA">Displayed inside the first button (to the left).</param>
		/// <param name="optionB">Displayed inside the second button (to the right).</param>
		/// <param name="defaultIsB">If true, the option selected by default is B, instead of A.</param>
		/// <param name="hint">Causes the "(?)" icon to appear next to the label. Can be used to provide additional information. Null or empty to disable.</param>
		public SSTwoButtonsSetting(int? id, string label, string optionA, string optionB, bool defaultIsB = false, string hint = null)
		{
			SetId(id, label);
			Label = label;
			OptionA = optionA;
			OptionB = optionB;
			DefaultIsB = defaultIsB;
			HintDescription = hint;
		}

		/// <inheritdoc />
		public override void ApplyDefaultValues()
		{
			SyncIsB = DefaultIsB;
		}

		/// <inheritdoc />
		public override void DeserializeValue(NetworkReader reader)
		{
			SyncIsB = reader.ReadBool();
		}

		/// <inheritdoc />
		public override void SerializeValue(NetworkWriter writer)
		{
			writer.WriteBool(SyncIsB);
		}

		/// <inheritdoc />
		public override void DeserializeEntry(NetworkReader reader)
		{
			base.DeserializeEntry(reader);

			OptionA = reader.ReadString();
			OptionB = reader.ReadString();
			DefaultIsB = reader.ReadBool();
		}

		/// <inheritdoc />
		public override void SerializeEntry(NetworkWriter writer)
		{
			base.SerializeEntry(writer);

			writer.WriteString(OptionA);
			writer.WriteString(OptionB);
			writer.WriteBool(DefaultIsB);
		}
	}
}

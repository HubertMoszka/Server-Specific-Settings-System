using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Allows servers to provide players with a slider field which store a float or integer value that is sent to server.
	/// </summary>
	public class SSSliderSetting : ServerSpecificSettingBase
	{
		/// <summary>
		/// Current value provided by the user.
		/// </summary>
		public float SyncFloatValue { get; set; }

		/// <summary>
		/// <see cref="SyncFloatValue"/> rounded to nearest integer.
		/// </summary>
		public int SyncIntValue => Mathf.RoundToInt(SyncFloatValue);

		/// <summary>
		/// Returns true if the player is actively dragging the slider. 
		/// <br /> This can be used to save performance by ignoring inputs that don't require immediate response to user adjusting the value.
		/// <br /> When user finishes dragging the slider, another event will be triggered with the final value (with this flag being false).
		/// </summary>
		/// <remarks>
		/// Please note that users can adjust the float value using the input field next to the slider. This will result in this flag never being set to true.
		/// </remarks>
		public bool SyncDragging { get; set; }

		/// <summary>
		/// Default value to put on the slider.
		/// </summary>
		public float DefaultValue { get; private set; }

		/// <summary>
		/// Lowest value to which the slider can be set.
		/// </summary>
		/// <remarks>
		/// Only enforced client-side. Server-side validation recommended for cheater protection.
		/// </remarks>
		public float MinValue { get; private set; }

		/// <summary>
		/// Highest value to which the slider can be set.
		/// </summary>
		/// <remarks>
		/// Only enforced client-side. Server-side validation recommended for cheater protection.
		/// </remarks>
		public float MaxValue { get; private set; }

		/// <summary>
		/// True if the slider should only allow integers.
		/// </summary>
		/// <remarks>
		/// Only enforced client-side. Server-side validation recommended for cheater protection.
		/// </remarks>
		public bool Integer { get; private set; }

		/// <summary>
		/// Format provided as parameter in the float.ToString(...) method. Please refer to https://learn.microsoft.com/en-us/dotnet/api/system.single.tostring?view=net-8.0#system-single-tostring(system-string).
		/// </summary>
		public string ValueToStringFormat { get; private set; }

		/// <summary>
		/// Allows to add units or otherwise format the shown result.
		/// </summary>
		/// <remarks>
		/// Must include a {0} somewhere. The constructor will append it to the end if not specified elsewhere.
		/// </remarks>
		public string FinalDisplayFormat { get; private set; }

		/// <inheritdoc />
		public override string DebugValue => SyncFloatValue.ToString();

		/// <summary>
		/// Creates a new entry where user can drag the slider or provide a specific numerical value.
		/// </summary>
		/// <param name="id">Unique identifier of the setting. If there are multiple settings of the same type, the ID must be different. You can provide null to generate identifier based on label hash code (not recommended for larger systems).</param>
		/// <param name="label">Shown next to the entry. It should briefly describe what the setting is for.</param>
		/// <param name="minValue">Lowest value client can set the slider to. Client-side only, please validate received value if necessary.</param>
		/// <param name="maxValue">Highest value client can set the slider to. Client-side only, please validate received value if necessary.</param>
		/// <param name="defaultValue">Value the slider will have by default. Clamped between min and max values.</param>
		/// <param name="integer">If true, client can only provide integer numbers (whole numbers, no decimals). Client-side only, please validated received value if neccessary.</param>
		/// <param name="valueToStringFormat">Provided as parameter in the float.ToString(...) method. Please refer to https://learn.microsoft.com/en-us/dotnet/api/system.single.tostring?view=net-8.0#system-single-tostring(system-string).</param>
		/// <param name="finalDisplayFormat">Formats final display string by using string.Format(..., value). Useful for adding text that appears before or after the number (like "{0} kg" suffix or "${0}" prefix).</param>
		/// <param name="hint">Causes the "(?)" icon to appear next to the label. Can be used to provide additional information. Null or empty to disable.</param>
		public SSSliderSetting(int? id, string label, float minValue, float maxValue, float defaultValue = 0, bool integer = false, string valueToStringFormat = "0.##", string finalDisplayFormat = "{0}", string hint = null)
		{
			SetId(id, label);
			Label = label;
			HintDescription = hint;

			DefaultValue = Mathf.Clamp(defaultValue, minValue, maxValue);

			MinValue = minValue;
			MaxValue = maxValue;

			Integer = integer;

			ValueToStringFormat = valueToStringFormat;
			FinalDisplayFormat = finalDisplayFormat;

			if (!finalDisplayFormat.Contains("0"))
				FinalDisplayFormat += "{0}";
		}

		/// <inheritdoc />
		public override void ApplyDefaultValues()
		{
			SyncFloatValue = DefaultValue;
		}

		/// <inheritdoc />
		public override void DeserializeValue(NetworkReader reader)
		{
			SyncFloatValue = reader.ReadFloat();
		}

		/// <inheritdoc />
		public override void SerializeValue(NetworkWriter writer)
		{
			writer.WriteFloat(SyncFloatValue);
		}

		/// <inheritdoc />
		public override void SerializeEntry(NetworkWriter writer)
		{
			base.SerializeEntry(writer);

			writer.WriteFloat(DefaultValue);
			writer.WriteFloat(MinValue);
			writer.WriteFloat(MaxValue);
			writer.WriteBool(Integer);
			writer.WriteString(ValueToStringFormat);
			writer.WriteString(FinalDisplayFormat);
		}

		/// <inheritdoc />
		public override void DeserializeEntry(NetworkReader reader)
		{
			base.DeserializeEntry(reader);

			DefaultValue = reader.ReadFloat();
			MinValue = reader.ReadFloat();
			MaxValue = reader.ReadFloat();
			Integer = reader.ReadBool();
			ValueToStringFormat = reader.ReadString();
			FinalDisplayFormat = reader.ReadString();
		}
	}
}

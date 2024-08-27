using Mirror;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Displays a big centered text, used to separate settings into sub-categories.
	/// </summary>
	public class SSGroupHeader : ServerSpecificSettingBase
	{
		/// <inheritdoc />
		public override UserResponseMode ResponseMode => UserResponseMode.None;

		/// <summary>
		/// Indicates header of reduced height, which creates a shorter padding above. Does not affect text size.
		/// </summary>
		public bool ReducedPadding { get; private set; }

		/// <inheritdoc />
		public override string DebugValue => "N/A";

		/// <summary>
		/// Creates a big centered text, used to separate settings into sub-categories.
		/// </summary>
		/// <param name="label">Text of the header. It should briefly describe the subsection of settings.</param>
		/// <param name="reducedPadding">If true, the empty space above the header is shorter. Does not affect text size.</param>
		/// <param name="hint">Causes the "(?)" icon to appear next to the text. Can be used to provide additional information. Null or empty to disable.</param>
		public SSGroupHeader(string label, bool reducedPadding = false, string hint = null)
		{
			Label = label;
			HintDescription = hint;
			ReducedPadding = reducedPadding;
		}

		/// <inheritdoc />
		public override void ApplyDefaultValues()
		{
			// No data to set.
		}
	}
}

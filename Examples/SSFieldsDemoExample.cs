namespace UserSettings.ServerSpecific.Examples
{
	/// <summary>
	/// Simplest example. Contains all available fields. Doesn't implement any functionality.
	/// </summary>
	public class SSFieldsDemoExample : SSExampleImplementationBase
	{
		/// <inheritdoc />
		public override string Name => "All fields demo (no functionality)";

		/// <inheritdoc />
		public override void Activate()
		{
			ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[]
			{
				// Without hints
				new SSGroupHeader("GroupHeader"),
				new SSTwoButtonsSetting(null, "TwoButtonsSetting", "Option A", "Option B"),
				new SSTextArea(null, "TextArea"),
				new SSTextArea(null, "Multiline collapsable TextArea.\nLorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.", SSTextArea.FoldoutMode.ExtendedByDefault),
				new SSSliderSetting(null, "SliderSetting", 0, 1),
				new SSPlaintextSetting(null, "Plaintext"),
				new SSKeybindSetting(null, "KeybindSetting"),
				new SSDropdownSetting(null, "DropdownSetting", new string[] { "Option 1", "Option 2", "Option 3", "Option 4" }),
				new SSButton(null, "Button", "Press me!"),

				// With hints
				new SSGroupHeader("Hints", hint: "Group headers are used to separate settings into subcategories."),
				new SSTwoButtonsSetting(null, "Another TwoButtonsSetting", "Option A", "Option B", hint: "Two Buttons are used to store Boolean values."),
				new SSSliderSetting(null, "Another SliderSetting", 0, 1, hint: "Sliders store a numeric value within a defined range."),
				new SSPlaintextSetting(null, "Another Plaintext", hint: "Plaintext fields store any provided text."),
				new SSKeybindSetting(null, "Another KeybindSetting", hint: "Allows checking if the player is currently holding the action key."),
				new SSDropdownSetting(null, "Another DropdownSetting", new string[] { "Option 1", "Option 2", "Option 3", "Option 4" }, hint: "Stores an integer value between 0 and the length of options minus 1."),
				new SSButton(null, "Another Button", "Press me! (again)", hint: "Triggers an event whenever it is pressed."),
			};

			ServerSpecificSettingsSync.SendToAll();
		}

		/// <inheritdoc />
		public override void Deactivate()
		{
			// Nothing to deactivate!
		}
	}
}

using System.Collections.Generic;

namespace UserSettings.ServerSpecific.Examples
{
	/// <summary>
	/// This example shows an ability to organize longer lists of entries by introducing a page selector.
	/// <para /> This example uses auto-generated IDs, since it doesn't provide additional functionality, and reliability of saving isn't important here.
	/// </summary>
	public class SSPagesExample : SSExampleImplementationBase
	{
		/// <inheritdoc />
		public override string Name => "Multiple pages demo";

		private SSDropdownSetting _pageSelectorDropdown;

		private ServerSpecificSettingBase[] _pinnedSection;
		private SettingsPage[] _pages;
		private Dictionary<ReferenceHub, int> _lastSentPages;

		/// <inheritdoc />
		public override void Activate()
		{
			ServerSpecificSettingsSync.ServerOnSettingValueReceived += ServerOnSettingValueReceived;
			ReferenceHub.OnPlayerRemoved += OnPlayerDisconnected;

			_lastSentPages = new Dictionary<ReferenceHub, int>();

			_pages = new SettingsPage[]
			{
				new SettingsPage("Page A", new ServerSpecificSettingBase[]
				{
					new SSKeybindSetting(null, "Keybind at Page A"),
					new SSPlaintextSetting(null, "Plaintext Input Field at Page A"),
					new SSTextArea(null, "Just a generic text area for page A!"),
					new SSSliderSetting(null, "Example slider at page A", 0, 1)
				}),

				new SettingsPage("Page B", new ServerSpecificSettingBase[]
				{
					new SSTwoButtonsSetting(null, "Which page is your favorite?", "Page B", "Also Page B"),
					new SSDropdownSetting(null, "Please rate this page", new string[] { "10/10", "5/5", "B", "★★★★★" }),
					new SSButton(null, "\"B\", as in \"Button\"", "BBBB"),
					new SSTextArea(null, "Page B stands for <color=red><b><i>BESTEST PAGE</i></b></color>"),
				}),

				new SettingsPage("Page C", new ServerSpecificSettingBase[]
				{
					new SSSliderSetting(null, "Slider C1", 0, 1),
					new SSSliderSetting(null, "Slider C2", 0, 1),
					new SSSliderSetting(null, "Slider C3", 0, 1),
					new SSTwoButtonsSetting(null, "Two buttons", "C1", "C2"),
					new SSGroupHeader("Subcategory", true, hint: "You can still make additional subcategories using group headers."),
					new SSDropdownSetting(null, "Dropdown C", new string[] { "C1", "C2", "C3" }, entryType: SSDropdownSetting.DropdownEntryType.Scrollable)
				}),
			};


			string[] dropdownPageOptions = new string[_pages.Length];

			for (int i = 0; i < dropdownPageOptions.Length; i++)
				dropdownPageOptions[i] = $"{_pages[i].Name} ({i + 1} out of {_pages.Length})";

			_pinnedSection = new ServerSpecificSettingBase[]
			{
				_pageSelectorDropdown = new SSDropdownSetting(null, "Page", dropdownPageOptions, entryType: SSDropdownSetting.DropdownEntryType.HybridLoop),
				new SSButton(null, "Another Pinned Element", "Do Nothing", hint: "This button doesn't do anything, but it shows you can \"pin\" multiple elements.")
			};

			_pages.ForEach(page => page.GenerateCombinedEntries(_pinnedSection));

			// All settings must be included in DefinedSettings, even if we're only sending a small part at the time.
			List<ServerSpecificSettingBase> allSettings = new(_pinnedSection);
			_pages.ForEach(page => allSettings.AddRange(page.OwnEntries));
			ServerSpecificSettingsSync.DefinedSettings = allSettings.ToArray();

			// We're technically sending ALL settings here, but clients will immediately send back the response which will allow us to re-send only the portion they're interested in.
			// You can optimize this process by only sending the page selector, but I didn't want to complicate this example more than it needs to.
			ServerSpecificSettingsSync.SendToAll();
		}

		/// <inheritdoc />
		public override void Deactivate()
		{
			ServerSpecificSettingsSync.ServerOnSettingValueReceived -= ServerOnSettingValueReceived;
			ReferenceHub.OnPlayerRemoved -= OnPlayerDisconnected;
		}

		private void ServerOnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
		{
			if (setting is SSDropdownSetting dropdown && dropdown.SettingId == _pageSelectorDropdown.SettingId)
			{
				ServerSendSettingsPage(hub, dropdown.SyncSelectionIndexValidated);
			}
		}

		private void ServerSendSettingsPage(ReferenceHub hub, int settingIndex)
		{
			// Client automatically re-sends values of all the field after reception of the settings collection.
			// This can result in triggering this event, so we want to save the previously sent value to avoid going into infinite loops.
			if (_lastSentPages.TryGetValue(hub, out int prevSent) && prevSent == settingIndex)
				return; 

			_lastSentPages[hub] = settingIndex;
			ServerSpecificSettingsSync.SendToPlayer(hub, _pages[settingIndex].CombinedEntries);
		}

		private void OnPlayerDisconnected(ReferenceHub hub)
		{
			_lastSentPages?.Remove(hub);
		}

		/// <summary>
		/// Represents a collection of settings that can be displayed one at the time.
		/// </summary>
		public class SettingsPage
		{
			/// <summary>
			/// Name of the collection, used to gennerate a header and dropdown label.
			/// </summary>
			public readonly string Name;

			/// <summary>
			/// Entries included on this page.
			/// </summary>
			public readonly ServerSpecificSettingBase[] OwnEntries;

			/// <summary>
			/// Entries to include when this page is selected. Combines the pinned page selector with its own entry.
			/// </summary>
			public ServerSpecificSettingBase[] CombinedEntries { get; private set; }

			/// <summary>
			/// Creates a new page of settings.
			/// </summary>
			/// <param name="name">Name that will be used to identify the setting.</param>
			/// <param name="entries">List of all entries included on this page.</param>
			public SettingsPage(string name, ServerSpecificSettingBase[] entries)
			{
				Name = name;
				OwnEntries = entries;
			}

			/// <summary>
			/// Generates the combined list of entries, which includes the page selector section, page name header, and the actual setting entries.
			/// </summary>
			public void GenerateCombinedEntries(ServerSpecificSettingBase[] pageSelectorSection)
			{
				int combinedLength = pageSelectorSection.Length + OwnEntries.Length + 1; // +1 to accomodate for auto-generated name header.
				CombinedEntries = new ServerSpecificSettingBase[combinedLength];

				int nextIndex = 0;

				// Include page selector section.
				foreach (ServerSpecificSettingBase entry in pageSelectorSection)
					CombinedEntries[nextIndex++] = entry;

				// Add auto-generated name header.
				CombinedEntries[nextIndex++] = new SSGroupHeader(Name);

				// Include own entries.
				foreach (ServerSpecificSettingBase entry in OwnEntries)
					CombinedEntries[nextIndex++] = entry;
			}
		}
	}
}

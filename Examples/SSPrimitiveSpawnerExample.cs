using AdminToys;
using GameCore;
using Mirror;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Utils.Networking;

namespace UserSettings.ServerSpecific.Examples
{
	/// <summary>
	/// Provides example implementation of a server-specific settings used for spawning primitives.
	/// <para /> This is the most complex example, providing alternative usage for the framework - admin tools.
	/// <br /> Analyzing this example will help you understand permission handling and appending additional settings at runtime.
	/// </summary>
	public class SSPrimitiveSpawnerExample : SSExampleImplementationBase
	{
		private static PrimitiveType[] _primitiveTypes;
		private static ColorPreset[] _colorPresets;
		private static List<ServerSpecificSettingBase> _allSettings;
		private static bool _anySpawned;
		private static SSTextArea _selectedColorTextArea;

		private const PlayerPermissions RequiredPermission = PlayerPermissions.FacilityManagement;

		/// <inheritdoc />
		public override string Name => "Primitive Spawner";

		/// <inheritdoc />
		public override void Activate()
		{
			_anySpawned = false;
			 
			_colorPresets ??= new ColorPreset[]
			{
				new ColorPreset("White", Color.white),
				new ColorPreset("Black", Color.black),
				new ColorPreset("Gray", Color.gray),
				new ColorPreset("Red", Color.red),
				new ColorPreset("Green", Color.green),
				new ColorPreset("Blue", Color.blue),
				new ColorPreset("Yellow", Color.yellow),
				new ColorPreset("Cyan", Color.cyan),
				new ColorPreset("Magenta", Color.magenta)
			};

			_primitiveTypes ??= EnumUtils<PrimitiveType>.Values;

			GenerateNewSettings();
			ResendSettings();

			ServerSpecificSettingsSync.SendOnJoinFilter = (_) => false; // Prevent all users from receiving the tools after joining the server.
			ServerSpecificSettingsSync.ServerOnSettingValueReceived += ProcessUserInput;
		}

		/// <inheritdoc />
		public override void Deactivate()
		{
			ServerSpecificSettingsSync.SendOnJoinFilter = null;
			ServerSpecificSettingsSync.ServerOnSettingValueReceived -= ProcessUserInput;
		}

		private void GenerateNewSettings()
		{
			_allSettings = new List<ServerSpecificSettingBase>()
			{
				new SSGroupHeader("Primitive Spawner"),

				new SSDropdownSetting((int) ExampleId.TypeDropdown, "Type", _primitiveTypes.Select(x => x.ToString()).ToArray()),

				new SSDropdownSetting((int)ExampleId.ColorPresetDropdown, "Color (preset)", _colorPresets.Select(x => x.Name).ToArray()),
				new SSSliderSetting((int)ExampleId.ColorAlphaSlider, "Opacity", 0, 100, 1, true, finalDisplayFormat:"{0}%"),
				new SSPlaintextSetting((int) ExampleId.ColorField, "Custom Color (R G B)", characterLimit: 10, hint: "Leave empty to use a preset."),
				(_selectedColorTextArea = new SSTextArea((int) ExampleId.ColorInfo, "Selected color: None")), // Store in a variable for easier updating.

				new SSTwoButtonsSetting((int) ExampleId.CollisionsToggle, "Collisions", "Disabled", "Enabled", true),
				new SSTwoButtonsSetting((int) ExampleId.RendererToggle, "Renderer", "Invisible", "Visible", true, "Invisible primitives can still receive collisions."),

				new SSSliderSetting((int) ExampleId.ScaleSliderX, "Scale (X)", 0, 25, 1, valueToStringFormat: "0.00", finalDisplayFormat: "x{0}"),
				new SSSliderSetting((int) ExampleId.ScaleSliderY, "Scale (Y)", 0, 25, 1, valueToStringFormat: "0.00", finalDisplayFormat: "x{0}"),
				new SSSliderSetting((int) ExampleId.ScaleSliderZ, "Scale (Z)", 0, 25, 1, valueToStringFormat: "0.00", finalDisplayFormat: "x{0}"),

				new SSButton((int) ExampleId.ConfirmButton, "Confirm Spawning", "Spawn"),
			};
		}

		private bool ValidateUser(ReferenceHub user)
		{
			return PermissionsHandler.IsPermitted(user.serverRoles.Permissions, RequiredPermission);
		}

		private void ProcessUserInput(ReferenceHub sender, ServerSpecificSettingBase setting)
		{
			if (!ValidateUser(sender)) // Always validate user input. Cheaters can claim to have pressed a button they never received.
				return;

			if (setting is SSButton potentialDestroyButton)
			{
				if (TryDestroy(potentialDestroyButton.SettingId))
					return;
			}

			switch ((ExampleId) setting.SettingId)
			{
				case ExampleId.ColorAlphaSlider:
				case ExampleId.ColorField:
				case ExampleId.ColorPresetDropdown:
					_selectedColorTextArea.SendTextUpdate(GetColorInfoForUser(sender));
					break;

				case ExampleId.DestroyAllButton:
					DestroyAll();
					break;

				case ExampleId.ConfirmButton:
					SpawnPrimitive(sender);
					break;
			}
		}

		private void SpawnPrimitive(ReferenceHub sender)
		{
			PrimitiveObjectToy primitive = null;

			foreach (GameObject pref in NetworkClient.prefabs.Values)
			{
				if (!pref.TryGetComponent(out PrimitiveObjectToy newToy))
					continue;

				primitive = GameObject.Instantiate(newToy);
				primitive.OnSpawned(sender, new ArraySegment<string>(new string[] { }));
				break;
			}

			if (primitive == null)
				return;

			int typeOption = GetSettingOfUser<SSDropdownSetting>(sender, ExampleId.TypeDropdown).SyncSelectionIndexValidated;
			primitive.PrimitiveType = (PrimitiveType) typeOption;

			Color colorOption = GetColorForUser(sender);
			primitive.MaterialColor = colorOption;

			float sizeX = GetSettingOfUser<SSSliderSetting>(sender, ExampleId.ScaleSliderX).SyncFloatValue;
			float sizeY = GetSettingOfUser<SSSliderSetting>(sender, ExampleId.ScaleSliderY).SyncFloatValue;
			float sizeZ = GetSettingOfUser<SSSliderSetting>(sender, ExampleId.ScaleSliderZ).SyncFloatValue;
			Vector3 sizeXYZ = new Vector3(sizeX, sizeY, sizeZ);
			primitive.transform.localScale = sizeXYZ;
			primitive.Scale = sizeXYZ;

			PrimitiveFlags enableCollisions = GetSettingOfUser<SSTwoButtonsSetting>(sender, ExampleId.CollisionsToggle).SyncIsB ? PrimitiveFlags.Collidable : PrimitiveFlags.None;
			PrimitiveFlags enableRenderer = GetSettingOfUser<SSTwoButtonsSetting>(sender, ExampleId.RendererToggle).SyncIsB ? PrimitiveFlags.Visible : PrimitiveFlags.None;
			primitive.PrimitiveFlags = enableCollisions | enableRenderer;

			if (!_anySpawned)
			{
				_allSettings.Add(new SSGroupHeader("Destroy Spawned Primitives"));
				_allSettings.Add(new SSButton((int) ExampleId.DestroyAllButton, "All Primitives", "Destroy All (HOLD)", 2.0f));
				_anySpawned = true;
			}

			string firstLine = $"{primitive.PrimitiveType} Color: {colorOption} Size: {sizeXYZ} SpawnPosition: {primitive.transform.position}";
			string secondLine = $"Spawned by {sender.LoggedNameFromRefHub()} at round time {RoundStart.RoundLength.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture)}";
			string combined = string.Concat(firstLine, "\n", secondLine);

			_allSettings.Add(new SSButton((int) primitive.netId, $"Primitive NetID#{primitive.netId}", "Destroy (HOLD)", 0.4f, combined));
			ResendSettings();
		}

		private void ResendSettings()
		{
			ServerSpecificSettingsSync.DefinedSettings = _allSettings.ToArray();
			ServerSpecificSettingsSync.SendToPlayersConditionally(ValidateUser);

			foreach (ReferenceHub hub in ReferenceHub.AllHubs)
			{
				if (!ValidateUser(hub))
					continue;

				_selectedColorTextArea.SendTextUpdate(GetColorInfoForUser(hub));
			}
		}

		private void DestroyAll()
		{
			if (!_anySpawned)
				return;

			for (int i = _allSettings.Count - 1; i > 0; i--) 
			{
				if (!_allSettings.TryGet(i, out ServerSpecificSettingBase setting) || setting is not SSButton)
					break;

				TryDestroy(setting.SettingId);
			}
		}

		private bool TryDestroy(int buttonId)
		{
			uint netId = (uint) buttonId;

			if (!NetworkUtils.SpawnedNetIds.TryGetValue(netId, out NetworkIdentity nid))
				return false;

			if (!nid.TryGetComponent(out PrimitiveObjectToy primitive))
				return false;

			for (int i = 0; i < _allSettings.Count; i++)
			{
				ServerSpecificSettingBase setting = _allSettings[i];

				if (setting is not SSButton)
					continue;

				if (setting.SettingId != buttonId)
					continue;

				_allSettings.RemoveAt(i);
				break;
			}

			if (_allSettings[^1] is SSButton { SettingId: (int) ExampleId.DestroyAllButton })
			{
				_anySpawned = false;

				// Restart the entire layout when the last primitive is destroyed.
				// This effectively removes the entire "Destroy Spawned Primitives" section.
				GenerateNewSettings();
			}

			NetworkServer.Destroy(primitive.gameObject);
			ResendSettings();

			return true;
		}

		private T GetSettingOfUser<T>(ReferenceHub user, ExampleId id) where T : ServerSpecificSettingBase
		{
			return ServerSpecificSettingsSync.GetSettingOfUser<T>(user, (int) id);
		}

		private string GetColorInfoForUser(ReferenceHub hub)
		{
			Color color = GetColorForUser(hub);
			return $"Selected color: <color={color.ToHex()}>███████████</color>";
		}

		private Color GetColorForUser(ReferenceHub user)
		{
			string inputText = GetSettingOfUser<SSPlaintextSetting>(user, ExampleId.ColorField).SyncInputText;
			string[] inputComponents = inputText.Split(' ');

			int colorPresetIndex = GetSettingOfUser<SSDropdownSetting>(user, ExampleId.ColorPresetDropdown).SyncSelectionIndexValidated;
			Color colorPreset = _colorPresets[colorPresetIndex].Color;

			float colorRed = inputComponents.TryGet(0, out string rStr) && float.TryParse(rStr, out float rParse) ? rParse / 255 : colorPreset.r;
			float colorGreen = inputComponents.TryGet(1, out string gStr) && float.TryParse(gStr, out float gParse) ? gParse / 255 : colorPreset.g;
			float colorBlue = inputComponents.TryGet(2, out string bStr) && float.TryParse(bStr, out float bParse) ? bParse / 255 : colorPreset.b;
			float colorAlpha = GetSettingOfUser<SSSliderSetting>(user, ExampleId.ColorAlphaSlider).SyncFloatValue / 100;

			return new Color(colorRed, colorGreen, colorBlue, colorAlpha);
		}

		private enum ExampleId
		{
			ConfirmButton,
			DestroyAllButton,
			TypeDropdown,
			ColorPresetDropdown,
			ColorField,
			ColorAlphaSlider,
			CollisionsToggle,
			RendererToggle,
			ColorInfo,
			ScaleSliderX,
			ScaleSliderY,
			ScaleSliderZ
		}

		private readonly struct ColorPreset
		{
			public readonly string Name;

			public readonly Color Color;

			public ColorPreset(string name, Color color)
			{
				Name = name;
				Color = color;
			}
		}
	}
}

using Mirror.LiteNetLib4Mirror;
using System.Text;
using Mirror;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Base class for server-specific settings. This is used by child classes to store keybinds, toggles, dropdown options, etc.
	/// </summary>
	public abstract class ServerSpecificSettingBase
	{
		private static readonly StringBuilder KeyGeneratorSb = new();

		/// <summary>
		/// Unique identifier of the setting. If there are multiple settings of the same type, the ID should be different.
		/// </summary>
		/// <remarks>
		/// Not used for clientside entries, such as headers.
		/// </remarks>
		public int SettingId { get; private set; }

		/// <summary>
		/// Plaintext from the server, shown next to the entry. It should briefly describe what the setting is for.
		/// </summary>
		public string Label { get; protected set; }

		/// <summary>
		/// Plaintext from the server. Causes the "(?)" icon to appear next to the label. Provides additional information. Null or empty to disable.
		/// </summary>
		public string HintDescription { get; protected set; }

		/// <summary>
		/// Generated player prefs key for this setting.
		/// </summary>
		public string PlayerPrefsKey { get; private set; }

		/// <summary>
		/// Defines conditions for the value to be sent to the server.
		/// </summary>
		public virtual UserResponseMode ResponseMode => UserResponseMode.AcquisitionAndChange;

		/// <summary>
		/// Value received from client, formatted as string, used for debug purposes.
		/// </summary>
		public abstract string DebugValue { get; }

		/// <summary>
		/// Used by copies (such as <see cref="ServerSpecificSettingsSync.ReceivedUserSettings"/>) to reference which setting in <see cref="ServerSpecificSettingsSync.DefinedSettings"/> they correspond to.
		/// <para /> This is useful for accessing non-synchronized values from definitions.
		/// </summary>
		public ServerSpecificSettingBase OriginalDefinition
		{
			get
			{
				foreach (ServerSpecificSettingBase setting in ServerSpecificSettingsSync.DefinedSettings)
				{
					if (setting.SettingId != SettingId)
						continue;

					if (setting.GetType() != GetType())
						continue;

					return setting;
				}

				return null;
			}
		}

		/// <summary>
		/// Called on client to send updated value to the server. This is called whenever the value changes, or server requests an update.
		/// </summary>
		public void ClientSendValue()
		{
			NetworkClient.Send(new SSSClientResponse(this));
		}

		/// <summary>
		/// Serializes information that will be used to create a settings entry on the client.
		/// </summary>
		public virtual void SerializeEntry(NetworkWriter writer)
		{
			writer.WriteInt(SettingId);
			writer.WriteString(Label);
			writer.WriteString(HintDescription);
		}

		/// <summary>
		/// Deserializes information to create an entry in the clientside settings window.
		/// </summary>
		public virtual void DeserializeEntry(NetworkReader reader)
		{
			SettingId = reader.ReadInt();
			PlayerPrefsKey = GeneratePrefsKey();
			Label = reader.ReadString();
			HintDescription = reader.ReadString();
		}

		/// <summary>
		/// Called every frame on every instance inside <see cref="ServerSpecificSettingsSync.DefinedSettings"/>
		/// </summary>
		public virtual void OnUpdate() { }

		/// <summary>
		/// Used by the client to send value of this setting.
		/// </summary>
		public virtual void SerializeValue(NetworkWriter writer) { }

		/// <summary>
		/// Used by the server to read client input of this setting. This should apply all values to this instance.
		/// </summary>
		public virtual void DeserializeValue(NetworkReader reader) { }

		/// <summary>
		/// Used to apply default values in case user has never assigned any.
		/// </summary>
		public abstract void ApplyDefaultValues();

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{GetType().Name} [ID: {SettingId}] Value: {DebugValue}";
		}

		/// <summary>
		/// Sets a specific ID or generates one from label hashcode.
		/// </summary>
		internal void SetId(int? id, string labelFallback)
		{
			if (!id.HasValue)
				id = labelFallback.GetStableHashCode();

			SettingId = id.Value;
		}

		private string GeneratePrefsKey()
		{
			KeyGeneratorSb.Clear();

			KeyGeneratorSb.Append("SrvSp_");
			KeyGeneratorSb.Append(LiteNetLib4MirrorNetworkManager.singleton.networkAddress);

			KeyGeneratorSb.Append('_');
			KeyGeneratorSb.Append(ServerSpecificSettingsSync.GetCodeFromType(GetType()));

			KeyGeneratorSb.Append('_');
			KeyGeneratorSb.Append(SettingId);

			return KeyGeneratorSb.ToString();
		}

		/// <summary>
		/// Defines when user sends back response to the server.
		/// </summary>
		public enum UserResponseMode
		{
			/// <summary>
			/// User never sends any respond to server. Receive event is never called.
			/// </summary>
			None,

			/// <summary>
			/// User only sends a response to server when value is changed. It won't send value loaded from player prefs when connecting to the server.
			/// </summary>
			ChangeOnly,

			/// <summary>
			/// Default behavior for most settings. Value is always in sync.
			/// </summary>
			AcquisitionAndChange
		}
	}
}

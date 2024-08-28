using System.Collections.Generic;
using Mirror.LiteNetLib4Mirror;
using NorthwoodLib.Pools;
using Utils.Networking;
using CentralAuth;
using UnityEngine;
using System;
using Mirror;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Synchronizes settings between clients and server.
	/// </summary>
	public static class ServerSpecificSettingsSync
	{
		/// <summary>
		/// Syncvar of all settings. Can be modified on the server, and then sent to clients using <see cref="SendToAll"/> or <see cref="SendToPlayer(ReferenceHub)"/>.
		/// </summary>
		public static ServerSpecificSettingBase[] DefinedSettings { get; set; }

		/// <summary>
		/// Allows to define a method that returns true when user is authorized to automatically receive settings when joining.
		/// <para /> This can be used to either disable automatic sending or tailor settings for users of specific permissions.
		/// </summary>
		public static Predicate<ReferenceHub> SendOnJoinFilter { get; set; }

		/// <summary>
		/// Version of the settings. Whenever a client revisits the server after this number has changed, a notification icon will be visible next to the game settings.
		/// <para /> Set value to 0 to disable the settings notification icon for this server, even to new users.
		/// </summary>
		public static int Version = 1;

		/// <summary>
		/// Called whenever server receives a settings value from the user. 
		/// </summary>
		public static event Action<ReferenceHub, ServerSpecificSettingBase> ServerOnSettingValueReceived;

		/// <summary>
		/// Called whenever server receives a self-report from the user.
		/// <para /> Users send self-report when they receive collection settings, and every time they open or close the "Server-Specific" tab in the Settings menu.
		/// </summary>
		public static event Action<ReferenceHub, SSSUserStatusReport> ServerOnStatusReceived;

		/// <summary>
		/// Identifier of the server we're connected to, used for identification in PlayerPrefs.
		/// <para /> This is equal to the ServerID (for verified servers) or IP address (as fallback)
		/// </summary>
		/// <remarks>
		/// This is only valid for a remote client connected to the server.
		/// </remarks>
		public static string CurServerPrefsKey
		{
			get
			{
				string serverId = FavoriteAndHistory.ServerIDLastJoined;

				if (string.IsNullOrEmpty(serverId))
				{
					// Fallback for non-verified servers.
					return LiteNetLib4MirrorNetworkManager.singleton.networkAddress;
				}
				else
				{
					return serverId;
				}
			}
		}

		private static Type[] _allTypes;
		private static byte[] _payloadBufferNonAlloc = new byte[NetworkWriter.DefaultCapacity];

		private static readonly Dictionary<ReferenceHub, List<ServerSpecificSettingBase>> ReceivedUserSettings = new();
		private static readonly Dictionary<ReferenceHub, SSSUserStatusReport> ReceivedUserStatuses = new();

		private static readonly Func<ServerSpecificSettingBase>[] AllSettingConstructors =
		{
			() => new SSGroupHeader(default),
			() => new SSKeybindSetting(0, default),
			() => new SSDropdownSetting(0, default, default),
			() => new SSTwoButtonsSetting(0, default, default, default),
			() => new SSSliderSetting(0, default, default, default),
			() => new SSPlaintextSetting(0, default),
			() => new SSButton(0, default, default),
			() => new SSTextArea(0, default)
		};

		private static Type[] AllSettingTypes
		{
			get
			{
				if (_allTypes != null)
					return _allTypes;

				_allTypes = new Type[AllSettingConstructors.Length];

				for (int i = 0; i < _allTypes.Length; i++)
					_allTypes[i] = AllSettingConstructors[i].Invoke().GetType();

				return _allTypes;
			}
		}

		/// <summary>
		/// Gets value that user has assigned to a setting of given type and ID. Adds a new entry with default values if user has not sent any.
		/// </summary>
		/// <remarks>
		/// Please note only the type, ID, and properties with "Sync" prefix will be valid. Other fields are not guaranteed to match values defined in <see cref="DefinedSettings"/>.
		/// <br /> Use <see cref="ServerSpecificSettingBase.OriginalDefinition"/> to reference non-synchronized parameters.
		/// </remarks>
		public static T GetSettingOfUser<T>(ReferenceHub user, int id) where T : ServerSpecificSettingBase
		{
			if (TryGetSettingOfUser(user, id, out T foundSetting))
				return foundSetting;

			T fallbackInstance = CreateInstance(typeof(T)) as T;

			fallbackInstance.SetId(id, null);
			fallbackInstance.ApplyDefaultValues();

			ReceivedUserSettings[user].Add(fallbackInstance);

			return fallbackInstance;
		}

		/// <summary>
		/// Gets value that user has assigned to a setting of given type and ID. Returns false if no value was received from the user.
		/// </summary>
		/// <remarks>
		/// Please note only the type, ID, and properties with "Sync" prefix will be valid. Other fields are not guaranteed to match values defined in <see cref="DefinedSettings"/>.
		/// <br /> Use <see cref="ServerSpecificSettingBase.OriginalDefinition"/> to reference non-synchronized parameters.
		/// </remarks>
		public static bool TryGetSettingOfUser<T>(ReferenceHub user, int id, out T result) where T : ServerSpecificSettingBase
		{
			List<ServerSpecificSettingBase> allSettings = ReceivedUserSettings.GetOrAdd(user, () => new());

			foreach (ServerSpecificSettingBase setting in allSettings)
			{
				if (setting.SettingId != id)
					continue;

				if (setting is not T castSetting)
					continue;

				result = castSetting;
				return true;
			}

			result = null;
			return false;
		}

		/// <summary>
		/// Gets the <see cref="Version"/> number from the time the user last opened the server-specific settings tab. Zero for completely new users.
		/// </summary>
		/// <remarks>
		/// Version is self-reported. Altered clients are capable of sending any integer, including versions that never existed.
		/// </remarks>
		public static int GetUserVersion(ReferenceHub user)
		{			
			return ReceivedUserStatuses.TryGetValue(user, out SSSUserStatusReport status) ? status.Version : 0;
		}

		/// <summary>
		/// Returns true if user is currently looking at the "Server-Specifc" tab in Settings menu. Useful for network optimization.
		/// </summary>
		/// <remarks>
		/// The status is self-reported. Altered clients are capable of sending untrue claims.
		/// </remarks>
		public static int IsTabOpenForUser(ReferenceHub user)
		{
			return ReceivedUserStatuses.TryGetValue(user, out SSSUserStatusReport status) ? status.Version : 0;
		}

		/// <summary>
		/// Generates a code which represents the type of implementation of the abstract <see cref="ServerSpecificSettingBase"/> for provided parameter.
		/// </summary>
		public static byte GetCodeFromType(Type type)
		{
			int code = AllSettingTypes.IndexOf(type);

			return code >= 0
				? (byte) code
				: throw new ArgumentException($"{type.FullName} is not a supported server-specific setting serializer.", nameof(type));
		}

		/// <summary>
		/// Returns implementation type of abstract <see cref="ServerSpecificSettingBase"/> from code obtained via <see cref="GetCodeFromType(ServerSpecificSettingBase)"/>.
		/// </summary>
		public static Type GetTypeFromCode(byte header)
		{
			return AllSettingTypes[header];
		}

		/// <summary>
		/// Re-sends <see cref="DefinedSettings"/> to all authenticated players.
		/// <para /> This is only required when the array has been modified. Clients automatically receive the array after connecting to the server.
		/// <para /> This will trigger all clients to send their respond messages which contain currently selected settings.
		/// </summary>
		public static void SendToAll()
		{
			if (!NetworkServer.active)
				return;

			new SSSEntriesPack(DefinedSettings, Version).SendToAuthenticated();
		}

		/// <summary>
		/// Re-sends <see cref="DefinedSettings"/> to all players matching the filter.
		/// <para /> This will trigger all matching clients to send their respond messages which contain currently selected settings.
		/// </summary>
		public static void SendToPlayersConditionally(Func<ReferenceHub, bool> filter)
		{
			if (!NetworkServer.active)
				return;

			new SSSEntriesPack(DefinedSettings, Version).SendToHubsConditionally(filter);
		}

		/// <summary>
		/// Sends the <see cref="DefinedSettings"/> array to a specific player.
		/// <para /> This is only required when the array has been modified. Clients automatically receive the array after connecting to the server.
		/// <para /> This will trigger the client to send their respond messages which contain currently selected settings.
		/// </summary>
		public static void SendToPlayer(ReferenceHub hub)
		{
			if (!NetworkServer.active)
				return;

			hub.connectionToClient.Send(new SSSEntriesPack(DefinedSettings, Version));
		}

		/// <summary>
		/// Sends a custom collection of settings to a specific player. This can be useful for sending settings that only apply to certain players (such as admins or donators).
		/// <para /> This is only required when the array has been modified. Clients automatically receive the array after connecting to the server.
		/// <para /> This will trigger the client to send their respond messages which contain currently selected settings.
		/// </summary>
		/// <remarks>
		/// The collection must only consist of entries that can be found in <see cref="DefinedSettings"/> (at least types and IDs must match), but may be partial (so it doesn't include all entries).
		/// </remarks>
		public static void SendToPlayer(ReferenceHub hub, ServerSpecificSettingBase[] collection, int? versionOverride = null)
		{
			if (!NetworkServer.active)
				return;

			hub.connectionToClient.Send(new SSSEntriesPack(collection, versionOverride ?? Version));
		}

		/// <summary>
		/// Creates an instance of provided type.
		/// </summary>
		/// <remarks>
		/// The constructor must be defined in <see cref="AllSettingConstructors"/>.
		/// </remarks>
		public static ServerSpecificSettingBase CreateInstance(Type t)
		{
			return AllSettingConstructors[AllSettingTypes.IndexOf(t)].Invoke();
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerAuthenticationManager.OnInstanceModeChanged += (hub, _) =>
			{
				if (SendOnJoinFilter != null && !SendOnJoinFilter(hub))
					return;

				SendToPlayer(hub);
			};

			CustomNetworkManager.OnClientReady += () =>
			{
				ReceivedUserSettings.Clear();
				ReceivedUserStatuses.Clear();

				NetworkClient.ReplaceHandler<SSSEntriesPack>(ClientProcessPackMsg);
				NetworkClient.ReplaceHandler<SSSUpdateMessage>(ClientProcessUpdateMsg);
				NetworkServer.ReplaceHandler<SSSClientResponse>(ServerProcessClientResponseMsg);
				NetworkServer.ReplaceHandler<SSSUserStatusReport>(ServerProcessClientStatusMsg);
			};

			ReferenceHub.OnPlayerRemoved += (hub) =>
			{
				if (!NetworkServer.active)
					return;

				ReceivedUserSettings.Remove(hub);
				ReceivedUserStatuses.Remove(hub);
			};

			StaticUnityMethods.OnUpdate += UpdateDefinedSettings;
		}

		private static void UpdateDefinedSettings()
		{
			try
			{
				if (!StaticUnityMethods.IsPlaying)
					return;

				DefinedSettings?.ForEach(x => x.OnUpdate());
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		private static void ClientProcessPackMsg(SSSEntriesPack pack)
		{
			DefinedSettings = pack.Settings;
			Version = pack.Version;

#if !HEADLESS
			Entries.SSEntrySpawner.Refresh();
#endif

			foreach (ServerSpecificSettingBase s in DefinedSettings)
			{
				if (s.ResponseMode != ServerSpecificSettingBase.UserResponseMode.AcquisitionAndChange)
					continue;

				s.ClientSendValue();
			}
		}

		private static void ClientProcessUpdateMsg(SSSUpdateMessage msg)
		{
			Type settingType = GetTypeFromCode(msg.TypeCode);
			List<byte> payload = msg.DeserializedPooledPayload;

			foreach (ServerSpecificSettingBase setting in DefinedSettings)
			{
				if (setting is not ISSUpdatable updatable)
					continue;

				if (setting.SettingId != msg.Id)
					continue;

				if (setting.GetType() != settingType)
					continue;

				if (_payloadBufferNonAlloc.Length < payload.Count)
					_payloadBufferNonAlloc = new byte[payload.Count + _payloadBufferNonAlloc.Length];

				payload.CopyTo(_payloadBufferNonAlloc);

				ArraySegment<byte> payloadSegment = new(_payloadBufferNonAlloc, 0, payload.Count);

				using (NetworkReaderPooled reader = NetworkReaderPool.Get(payloadSegment))
					updatable.DeserializeUpdate(reader);

				break;
			}

			ListPool<byte>.Shared.Return(payload);
		}

		private static bool ServerPrevalidateClientResponse(SSSClientResponse msg)
		{
			if (DefinedSettings == null)
				return false;

			foreach (ServerSpecificSettingBase setting in DefinedSettings)
			{
				if (setting.SettingId != msg.Id)
					continue;

				if (setting.GetType() != msg.SettingType)
					continue;

				return true;
			}

			return false;
		}

		private static void ServerDeserializeClientResponse(ReferenceHub sender, ServerSpecificSettingBase setting, NetworkReaderPooled reader)
		{
			if (setting.ResponseMode != ServerSpecificSettingBase.UserResponseMode.None)
			{
				setting.DeserializeValue(reader);
				ServerOnSettingValueReceived?.Invoke(sender, setting);
			}

			reader.Dispose();
		}

		private static void ServerProcessClientResponseMsg(NetworkConnection conn, SSSClientResponse msg)
		{
			if (!ReferenceHub.TryGetHub(conn.identity.gameObject, out ReferenceHub sender))
				return;

			if (!ServerPrevalidateClientResponse(msg))
				return;

			List<ServerSpecificSettingBase> allSettings = ReceivedUserSettings.GetOrAdd(sender, () => new());
			NetworkReaderPooled reader = NetworkReaderPool.Get(msg.Payload);

			foreach (ServerSpecificSettingBase setting in allSettings)
			{
				if (setting.SettingId != msg.Id)
					continue;

				if (setting.GetType() != msg.SettingType)
					continue;

				ServerDeserializeClientResponse(sender, setting, reader);
				return;
			}

			ServerSpecificSettingBase newInstance = CreateInstance(msg.SettingType);
			allSettings.Add(newInstance);

			newInstance.SetId(msg.Id, null);
			newInstance.ApplyDefaultValues();

			ServerDeserializeClientResponse(sender, newInstance, reader);
		}

		private static void ServerProcessClientStatusMsg(NetworkConnection conn, SSSUserStatusReport msg)
		{
			if (!ReferenceHub.TryGetHub(conn.identity.gameObject, out ReferenceHub sender))
				return;

			ReceivedUserStatuses[sender] = msg;
			ServerOnStatusReceived?.Invoke(sender, msg);
		}
	}
}

using Mirror;
using NorthwoodLib.Pools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Used to properly serialize an array of <see cref="ServerSpecificSettingBase"/>.
	/// </summary>
	public readonly struct SSSEntriesPack : NetworkMessage
	{
		/// <summary>
		/// Payload of this network message.
		/// </summary>
		public readonly ServerSpecificSettingBase[] Settings;

		/// <summary>
		/// Received <see cref="ServerSpecificSettingsSync.Version"/> from the server.
		/// </summary>
		public readonly int Version;

		/// <summary>
		/// Reads a message from a reader.
		/// </summary>
		public SSSEntriesPack(NetworkReader reader)
		{
			Version = reader.ReadInt();
			Settings = new ServerSpecificSettingBase[reader.ReadByte()];

			for (int i = 0; i < Settings.Length; i++)
			{
				Type settingType = ServerSpecificSettingsSync.GetTypeFromCode(reader.ReadByte());
				object newInstance = ServerSpecificSettingsSync.CreateInstance(settingType);

				ServerSpecificSettingBase unpacked = newInstance as ServerSpecificSettingBase;
				unpacked.DeserializeEntry(reader);
				Settings[i] = unpacked;
			}
		}

		/// <summary>
		/// Creates a new message.
		/// </summary>
		/// <param name="settings">Payload to serialize.</param>
		/// <param name="version">Version to serialize.</param>
		public SSSEntriesPack(ServerSpecificSettingBase[] settings, int version)
		{
			Settings = settings;
			Version = version;
		}

		/// <summary>
		/// Writes itself into a writer.
		/// </summary>
		public readonly void Serialize(NetworkWriter writer)
		{
			writer.WriteInt(Version);

			if (Settings == null)
			{
				writer.WriteByte(0);
			}
			else
			{
				writer.WriteByte((byte) Settings.Length);

				foreach (ServerSpecificSettingBase setting in Settings)
				{
					writer.WriteByte(ServerSpecificSettingsSync.GetCodeFromType(setting.GetType()));
					setting.SerializeEntry(writer);
				}
			}
		}
	}

	/// <summary>
	/// Used by clients to communicate which settings they have selected.
	/// </summary>
	public readonly struct SSSClientResponse : NetworkMessage
	{
		/// <summary>
		/// Type of class implementing the abstract <see cref="ServerSpecificSettingBase"/>.
		/// </summary>
		public readonly Type SettingType;

		/// <summary>
		/// The <see cref="ServerSpecificSettingBase.SettingId"/> user interacted with.
		/// </summary>
		public readonly int Id;

		/// <summary>
		/// Payload acquired from client via <see cref="ServerSpecificSettingBase.SerializeValue(NetworkWriter)"/>, containing selected settings.
		/// </summary>
		public readonly byte[] Payload;

		/// <summary>
		/// Reads a message from a reader.
		/// </summary>
		public SSSClientResponse(NetworkReader reader)
		{
			SettingType = ServerSpecificSettingsSync.GetTypeFromCode(reader.ReadByte());
			Id = reader.ReadInt();

			int payloadSize = reader.ReadInt();
			Payload = reader.ReadBytes(payloadSize);
		}

		/// <summary>
		/// Creates a new message related to a specific modified setting.
		/// </summary>
		public SSSClientResponse(ServerSpecificSettingBase modifiedSetting)
		{
			SettingType = modifiedSetting.GetType();
			Id = modifiedSetting.SettingId;

			using (NetworkWriterPooled writer = NetworkWriterPool.Get())
			{
				modifiedSetting.SerializeValue(writer);
				Payload = writer.ToArray();
			}
		}

		/// <summary>
		/// Writes itself into a writer.
		/// </summary>
		public readonly void Serialize(NetworkWriter writer)
		{
			writer.WriteByte(ServerSpecificSettingsSync.GetCodeFromType(SettingType));
			writer.WriteInt(Id);
			writer.WriteInt(Payload.Length);
			writer.WriteBytes(Payload, 0, Payload.Length);
		}
	}

	/// <summary>
	/// Message received from clients that self-report the version of server-specific settings tab they last accessed.  Also indicates whether their setting tab is currently open.
	/// </summary>
	public readonly struct SSSUserStatusReport : NetworkMessage
	{
		/// <summary>
		/// The last <see cref="ServerSpecificSettingsSync.Version"/> of when the user opened their server-specific settings tab.
		/// </summary>
		public readonly int Version;

		/// <summary>
		/// True if the user is claiming to have their server-specific settings tab currently open.
		/// </summary>
		public readonly bool TabOpen;

		/// <summary>
		/// Reads a message from a reader.
		/// </summary>
		public SSSUserStatusReport(NetworkReader reader)
		{
			Version = reader.ReadInt();
			TabOpen = reader.ReadBool();
		}

		/// <summary>
		/// Creates a new report message.
		/// </summary>
		public SSSUserStatusReport(int ver, bool tabOpen)
		{
			Version = ver;
			TabOpen = tabOpen;
		}

		/// <summary>
		/// Writes itself into a writer.
		/// </summary>
		public readonly void Serialize(NetworkWriter writer)
		{
			writer.WriteInt(Version);
			writer.WriteBool(TabOpen);
		}
	}

	/// <summary>
	/// Message carrying update for <see cref="ISSUpdatable"/>.
	/// </summary>
	public readonly struct SSSUpdateMessage : NetworkMessage
	{
		/// <summary>
		/// ID of setting that wants to send an update.
		/// </summary>
		public readonly int Id;

		/// <summary>
		/// Type of setting, obtained from <see cref="ServerSpecificSettingsSync.GetCodeFromType(Type)"/>
		/// </summary>
		public readonly byte TypeCode;

		/// <summary>
		/// Deserialized payload upon receiving message. Potentially large and frequent, so uses list pool.
		/// </summary>
		/// <remarks>
		/// Return to pool when processed. Not valid for server.
		/// </remarks>
		public readonly List<byte> DeserializedPooledPayload;

		/// <summary>
		/// Payload for server to serialize.
		/// </summary>
		public readonly Action<NetworkWriter> ServersidePayloadWriter;

		/// <summary>
		/// Reads a message from a reader.
		/// </summary>
		public SSSUpdateMessage(NetworkReader reader)
		{
			Id = reader.ReadInt();
			TypeCode = reader.ReadByte();

			int payloadSize = reader.ReadInt();
			DeserializedPooledPayload = ListPool<byte>.Shared.Rent(reader.ReadBytesSegment(payloadSize));

			ServersidePayloadWriter = null;
		}

		/// <summary>
		/// Creates a new update message.
		/// </summary>
		public SSSUpdateMessage(ServerSpecificSettingBase setting, Action<NetworkWriter> writerFunc)
		{
			Id = setting.SettingId;
			TypeCode = ServerSpecificSettingsSync.GetCodeFromType(setting.GetType());

			DeserializedPooledPayload = null;
			ServersidePayloadWriter = writerFunc;
		}

		/// <summary>
		/// Writes itself into a writer.
		/// </summary>
		public readonly void Serialize(NetworkWriter writer)
		{
			writer.WriteInt(Id);
			writer.WriteByte(TypeCode);

			using (NetworkWriterPooled payloadSerializer = NetworkWriterPool.Get())
			{
				ServersidePayloadWriter?.Invoke(payloadSerializer);
				writer.WriteArraySegment(payloadSerializer.ToArraySegment());
			}
		}
	}

	/// <summary>
	/// Serialize/deserialize methods for messages used by the server-specific settings system.
	/// </summary>
	public static class SSSNetworkMessageFunctions
	{
		public static void SerializeSSSEntriesPack(this NetworkWriter writer, SSSEntriesPack value) => value.Serialize(writer);

		public static SSSEntriesPack DeserializeSSSEntriesPack(this NetworkReader reader) => new SSSEntriesPack(reader);

		public static void SerializeSSSClientResponse(this NetworkWriter writer, SSSClientResponse value) => value.Serialize(writer);

		public static SSSClientResponse DeserializeSSSClientResponse(this NetworkReader reader) => new SSSClientResponse(reader);

		public static void SerializeSSSVersionSelfReport(this NetworkWriter writer, SSSUserStatusReport value) => value.Serialize(writer);

		public static SSSUserStatusReport DeserializeSSSVersionSelfReport(this NetworkReader reader) => new SSSUserStatusReport(reader);

		public static void SerializeSSSUpdateMessage(this NetworkWriter writer, SSSUpdateMessage value) => value.Serialize(writer);

		public static SSSUpdateMessage DeserializSSSUpdateMessage(this NetworkReader reader) => new SSSUpdateMessage(reader);
	}
}

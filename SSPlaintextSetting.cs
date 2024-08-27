using Utils.Networking;
using System;
using Mirror;
using TMPro;
using ContentType = TMPro.TMP_InputField.ContentType;
using UnityEngine;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Allows servers to provide players with an input field displayed inside settings. Value is synchronized.
	/// </summary>
	public class SSPlaintextSetting : ServerSpecificSettingBase, ISSUpdatable
	{
		private int? _characterLimitOriginalCache;

		/// <summary>
		/// Event called when the server request the client to clear their input.
		/// </summary>
		internal event Action OnClearRequested;

		/// <summary>
		/// Value typed out by the user.
		/// </summary>
		public string SyncInputText { get; internal set; }

		/// <summary>
		/// Text displayed when no input has been provided.
		/// </summary>
		/// <remarks>
		/// This is not the default input text, this is only used client-side when there's nothing inside.
		/// </remarks>
		public string Placeholder { get; private set; }

		/// <summary>
		/// Defines type of input field. Please refer to TextMeshPro documentation for details.
		/// </summary>
		/// <remarks>
		/// Only validated client-side, do not trust user inputs.
		/// </remarks>
		public ContentType ContentType { get; private set; }

		/// <summary>
		/// Max number of characters user can put in the field. This is validated both client- and server-side.
		/// </summary>
		public int CharacterLimit { get; private set; }

		/// <inheritdoc />
		public override string DebugValue => SyncInputText;

		/// <summary>
		/// Creates a new button user can interact with in the settings.
		/// </summary> 
		/// <param name="id">Unique identifier of the setting. If there are multiple settings of the same type, the ID must be different. You can provide null to generate identifier based on label hash code (not recommended for larger systems).</param>
		/// <param name="label">Shown next to the entry. It should briefly describe what the setting is for.</param>
		/// <param name="characterLimit">Maximum number of characters. Validated both client- and server-side.</param>
		/// <param name="contentType">Defines type of input field. Please refer to TextMeshPro documentation for details.</param>
		/// <param name="placeholder">Client-side only. Semi-transparent text displayed over the input field when it is empty.</param>
		/// <param name="hint">Causes the "(?)" icon to appear next to the label. Can be used to provide additional information. Null or empty to disable.</param>
		public SSPlaintextSetting(int? id, string label, string placeholder = "...", int characterLimit = 64, ContentType contentType = ContentType.Standard, string hint = null)
		{
			SetId(id, label);
			Label = label;
			HintDescription = hint;
			Placeholder = placeholder;
			CharacterLimit = characterLimit;
			ContentType = contentType;
		}

		/// <summary>
		/// Allows server to request player's fields to be cleared.
		/// </summary>
		/// <param name="receiveFilter">Filter for receivers. Null to send to all.</param>
		public void SendClearRequest(Func<ReferenceHub, bool> receiveFilter = null)
		{
			SSSUpdateMessage msg = new(this, null);

			if (receiveFilter == null)
				msg.SendToAuthenticated();
			else
				msg.SendToHubsConditionally(receiveFilter);
		}

		/// <inheritdoc />
		public override void ApplyDefaultValues()
		{
			SyncInputText = string.Empty;
		}

		/// <inheritdoc />
		public override void SerializeEntry(NetworkWriter writer)
		{
			base.SerializeEntry(writer);

			writer.WriteString(Placeholder);
			writer.WriteUShort((ushort) CharacterLimit);
			writer.WriteByte((byte) ContentType);
		}

		/// <inheritdoc />
		public override void DeserializeEntry(NetworkReader reader)
		{
			base.DeserializeEntry(reader);

			Placeholder = reader.ReadString();
			CharacterLimit = reader.ReadUShort();
			ContentType = (ContentType) reader.ReadByte();
		}

		/// <inheritdoc />
		public override void SerializeValue(NetworkWriter writer)
		{
			base.SerializeValue(writer);
			writer.WriteString(SyncInputText);
		}

		/// <inheritdoc />
		public override void DeserializeValue(NetworkReader reader)
		{
			base.DeserializeValue(reader);

			int maxChars;

			if (_characterLimitOriginalCache.HasValue)
			{
				maxChars = _characterLimitOriginalCache.Value;
			}
			else
			{
				maxChars = (OriginalDefinition as SSPlaintextSetting).CharacterLimit;
				_characterLimitOriginalCache = maxChars;
			}

			SyncInputText = reader.ReadString();

			if (SyncInputText.Length > maxChars)
				SyncInputText = SyncInputText.Remove(maxChars);
		}

		/// <inheritdoc />
		public void DeserializeUpdate(NetworkReader reader)
		{
			OnClearRequested?.Invoke();
		}
	}
}

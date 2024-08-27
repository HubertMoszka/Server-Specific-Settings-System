using Mirror;
using System;
using TMPro;
using Utils.Networking;
using ZXing;

namespace UserSettings.ServerSpecific
{
	/// <summary>
	/// Displays a text area in server-specific user settings, supporting rich text and multiple lines.
	/// </summary>
	public class SSTextArea : ServerSpecificSettingBase, ISSUpdatable
	{
		/// <summary>
		/// Used to update entry text without having to re-send all settings.
		/// </summary>
		internal event Action OnTextUpdated;

		/// <inheritdoc />
		public override UserResponseMode ResponseMode => UserResponseMode.None;

		/// <summary>
		/// Useful for multilines. Defines if the area can be collapsed, and how it should work.
		/// </summary>
		public FoldoutMode Foldout { get; private set; }

		/// <summary>
		/// Defines how content should be aligned.
		/// </summary>
		public TextAlignmentOptions AlignmentOptions { get; private set; }

		/// <inheritdoc />
		public override string DebugValue => "N/A";

		/// <summary>
		/// Creates a non-interactable text area. Supports rich text and multiple lines.
		/// </summary>ww
		/// <param name="id">Optional (nullable), unless you need to update the text using <see cref="SendTextUpdate"/>.</param>
		/// <param name="content">Content to display in the text area (for technical reasons, it's stored as label).</param>
		/// <param name="collapsedText">Only used when foldout mode is collapsable. Allows to override text displayed when the foldout is collapsed. Null or empty to auto-generate.</param>
		/// <param name="foldoutMode"> Useful for multilines.Defines if the area can be collapsed, and how it should work.</param>
		/// <param name="textAlignment">TextMeshPro alignment flags.</param>
		public SSTextArea(int? id, string content, FoldoutMode foldoutMode = FoldoutMode.NotCollapsable, string collapsedText = null, TextAlignmentOptions textAlignment = TextAlignmentOptions.TopLeft)
		{
			SetId(id, content);

			Label = content;
			HintDescription = collapsedText;
			Foldout = foldoutMode;
			AlignmentOptions = textAlignment;
		}

		/// <summary>
		/// Sends an updated text to clients.
		/// </summary>
		/// <param name="applyOverride">If true, new value will be saved on this field,  so future connections will receive the updated values, even if they don't match the receive filter.</param>
		/// <param name="receiveFilter">Filter for receivers. Null to send to all.</param>
		public void SendTextUpdate(string newText, bool applyOverride = true, Func<ReferenceHub, bool> receiveFilter = null)
		{
			if (applyOverride)
				Label = newText;

			SSSUpdateMessage msg = new(this, (writer) => writer.WriteString(newText));

			if (receiveFilter == null)
				msg.SendToAuthenticated();
			else
				msg.SendToHubsConditionally(receiveFilter);
		}

		/// <inheritdoc />
		public override void SerializeEntry(NetworkWriter writer)
		{
			base.SerializeEntry(writer);

			writer.WriteByte((byte) Foldout);
			writer.WriteInt((int) AlignmentOptions);
		}

		/// <inheritdoc />
		public override void DeserializeEntry(NetworkReader reader)
		{
			base.DeserializeEntry(reader);

			Foldout = (FoldoutMode) reader.ReadByte();
			AlignmentOptions = (TextAlignmentOptions) reader.ReadInt();
		}

		/// <inheritdoc />
		public override void ApplyDefaultValues()
		{
			// No data to set.
		}

		/// <inheritdoc />
		public void DeserializeUpdate(NetworkReader reader)
		{
			Label = reader.ReadString();
			OnTextUpdated?.Invoke();
		}

		/// <summary>
		/// Defines functionality of the text area foldout.
		/// </summary>
		public enum FoldoutMode
		{
			/// <summary>
			/// Area will always remain extended and show all lines.
			/// </summary>
			NotCollapsable,

			/// <summary>
			/// Collapsed every time user enters settings, but it can be extended to reveal all text.
			/// </summary>
			CollapseOnEntry,

			/// <summary>
			/// Extended every time user enters settings, but can be collapsed to occupy less space.
			/// </summary>
			ExtendOnEntry,

			/// <summary>
			/// Collapsed by default, but once extended, will stay extended until user manually collapses it.
			/// </summary>
			CollapsedByDefault,

			/// <summary>
			/// Extended by default, but once collapsed, will stay collapsed until user manually extends it.
			/// </summary>
			ExtendedByDefault
		}
	}
}

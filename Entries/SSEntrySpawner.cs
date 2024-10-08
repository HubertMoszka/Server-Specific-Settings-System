using System.Collections.Generic;
using UserSettings.GUIElements;
using UnityEngine.UI;
using UnityEngine;
using System;
using Mirror;

namespace UserSettings.ServerSpecific.Entries
{
	/// <summary>
	/// Spawns all server-specific entries received from the server.
	/// This script is added to the Game Menu Canvas.
	/// </summary>
	public class SSEntrySpawner : MonoBehaviour
	{
		private static SSEntrySpawner _singleton;
		private static int? _clientVersionCache;

		[SerializeField]
		private GameObject[] _entryTemplates;

		[SerializeField]
		private Transform _entriesParentTr;

		[SerializeField]
		private UserSettingsCategories _categoriesController;

		[SerializeField]
		private GameObject _categoryButton;

		[SerializeField]
		private GameObject _categoryRoot;

		[SerializeField]
		private GameObject _spacer;

		[SerializeField]
		private GameObject[] _newSettingsWarning;

		private ISSEntry[] _cachedComponents;
		private readonly List<GameObject> _spawnedEntries = new();
		private readonly List<VerticalLayoutGroup> _layoutGroups = new();

		private static string PrefsKey => $"SrvSp_{ServerSpecificSettingsSync.CurServerPrefsKey}_Version";

		private static int ClientVersion
		{
			get
			{
				_clientVersionCache ??= PlayerPrefs.GetInt(PrefsKey, 0);
				return _clientVersionCache.Value;
			}
			set
			{
				if (_clientVersionCache == value)
					return;

				_clientVersionCache = value;
				PlayerPrefs.SetInt(PrefsKey, value);
			}
		}

		private void Awake()
		{
			_singleton = this;
			_clientVersionCache = null;

			SSTabDetector.OnStatusChanged += OnTabChanged;
		}

		private void Update()
		{
			if (!SSTabDetector.IsOpen)
				return;

			foreach (VerticalLayoutGroup x in _layoutGroups)
			{
				// Apparently the only way to set layout dirty, according to unity forums :pepehands:
				x.enabled = false;
				x.enabled = true;
			}
		}

		private void OnDestroy()
		{
			SSTabDetector.OnStatusChanged -= OnTabChanged;
		}

		private void OnTabChanged()
		{
			if (SSTabDetector.IsOpen)
			{
				ToggleWarning(false);
				ClientVersion = ServerSpecificSettingsSync.Version;
			}

			ClientSendReport();
		}

		private void ToggleWarning(bool state)
		{
			foreach (GameObject item in _newSettingsWarning)
			{
				item.SetActive(state);
			}
		}

		private void DeleteSpawnedEntries()
		{
			foreach (GameObject entry in _spawnedEntries)
			{
				if (entry == null)
					continue;

				Destroy(entry);
			}

			_spawnedEntries.Clear();
		}

		private void SpawnAllEntries(ServerSpecificSettingBase[] settings, bool showWarning)
		{
			// Header is normally first entry and it contains a space, put one if there is no header.
			_spacer.SetActive(settings[0] is not SSGroupHeader);

			settings.ForEach(SpawnEntry);
			_categoryButton.SetActive(true);

			_layoutGroups.Clear();
			_categoryRoot.GetComponentsInChildren(_layoutGroups);

			ToggleWarning(showWarning);
		}

		private void SpawnEntry(ServerSpecificSettingBase setting)
		{
			GameObject template = GetTemplateForSetting(setting);
			GameObject instance = Instantiate(template, _entriesParentTr);

			instance.SetActive(true);
			instance.GetComponentInChildren<ISSEntry>().Init(setting);

			_spawnedEntries.Add(instance);
		}

		private GameObject GetTemplateForSetting(ServerSpecificSettingBase setting)
		{
			int len = _entryTemplates.Length;
			_cachedComponents ??= new ISSEntry[len];

			for (int i = 0; i < len; i++)
			{
				ISSEntry entry = _cachedComponents[i];

				if (entry == null)
				{
					entry = FindEntryInTemplate(_entryTemplates[i]);
					_cachedComponents[i] = entry;
				}

				if (entry.CheckCompatibility(setting))
				{
					return _entryTemplates[i];
				}
			}

			throw new InvalidOperationException("This setting does not have a compatible entry: " + setting);
		}

		private ISSEntry FindEntryInTemplate(GameObject template)
		{
			ISSEntry newComp = template.GetComponentInChildren<ISSEntry>(true);

			if ((newComp as UnityEngine.Object) == null)
				throw new InvalidOperationException($"This entry template is not valid: " + template.name);

			return newComp;
		}

		private static void ClientSendReport()
		{
			NetworkClient.Send(new SSSUserStatusReport(ClientVersion, SSTabDetector.IsOpen));
		}

		/// <summary>
		/// Called to spawn the entires.
		/// </summary>
		public static void Refresh()
		{
			if (_singleton == null)
				return;

			_singleton.DeleteSpawnedEntries();

			ServerSpecificSettingBase[] settings = ServerSpecificSettingsSync.DefinedSettings;

			if (settings == null || settings.Length == 0)
			{
				_singleton._categoriesController.ResetSelection();
				_singleton._categoryButton.SetActive(false);
				return;
			}

			int clientVersion = ClientVersion;
			int serverVersion = ServerSpecificSettingsSync.Version;

			_singleton.SpawnAllEntries(settings, serverVersion != 0 && clientVersion != serverVersion);

			ClientSendReport();
		}
	}
}

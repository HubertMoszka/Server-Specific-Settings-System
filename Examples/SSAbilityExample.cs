using CustomPlayerEffects;
using InventorySystem;
using InventorySystem.Items;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using System.Collections.Generic;
using UnityEngine;

namespace UserSettings.ServerSpecific.Examples
{
	/// <summary>
	/// Provides a example implementation of a server-specific settings that add custom abilities.
	/// <br /> This is a relatively simple implementation, good for understanding the basics of the framework.
	/// </summary>
	public class SSAbilityExample : SSExampleImplementationBase
	{
		/// <inheritdoc />
		public override string Name => "Abilities Extension";

		private const float HealAllyHp = 50;
		private const float HealAllyRange = 3.5f;
		private const byte BoostIntensity = 60;
		private const float BoostHealthDrain = 5;

		private static HashSet<ReferenceHub> _activeSpeedBoosts;

		/// <inheritdoc />
		public override void Activate()
		{
			_activeSpeedBoosts = new HashSet<ReferenceHub>();

			ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[]
			{
				new SSGroupHeader("Abilities"),
				new SSKeybindSetting((int)ExampleId.HealAlly, "Heal Ally", KeyCode.H, hint: $"Press this key while holding a medkit to instantly heal a stationary ally for {HealAllyHp} HP."),
				new SSKeybindSetting((int)ExampleId.SpeedBoostKey, "Speed Boost (Human-only)", KeyCode.Y, hint: "Increase your speed by draining your health."),
				new SSTwoButtonsSetting((int)ExampleId.SpeedBoostToggle, "Speed Boost - Activation Mode", "Hold", "Toggle")
			};

			ServerSpecificSettingsSync.SendToAll();

			ServerSpecificSettingsSync.ServerOnSettingValueReceived += ProcessUserInput;
			ReferenceHub.OnPlayerRemoved += OnPlayerDisconnected;
			PlayerRoleManager.OnRoleChanged += OnRoleChanged;
			StaticUnityMethods.OnUpdate += OnUpdate;
		}

		/// <inheritdoc />
		public override void Deactivate()
		{
			ServerSpecificSettingsSync.ServerOnSettingValueReceived -= ProcessUserInput;
			ReferenceHub.OnPlayerRemoved -= OnPlayerDisconnected;
			PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
			StaticUnityMethods.OnUpdate -= OnUpdate;
		}

		private void ProcessUserInput(ReferenceHub sender, ServerSpecificSettingBase setting)
		{
			switch ((ExampleId) setting.SettingId)
			{
				case ExampleId.HealAlly
				when setting is SSKeybindSetting keybind:
					{
						if (keybind.SyncIsPressed)
							TryHealAlly(sender);
					}
					break;

				case ExampleId.SpeedBoostKey
				when setting is SSKeybindSetting keybind:
					{
						bool toggleMode = ServerSpecificSettingsSync.GetSettingOfUser<SSTwoButtonsSetting>(sender, (int) ExampleId.SpeedBoostToggle).SyncIsB;

						if (toggleMode)
						{
							if (!keybind.SyncIsPressed)
								break;

							SetHealBoost(sender, !_activeSpeedBoosts.Contains(sender));
						}
						else
						{
							SetHealBoost(sender, keybind.SyncIsPressed);
						}
					}
					break;
			}
		}

		private void TryHealAlly(ReferenceHub sender)
		{
			ItemIdentifier heldItem = sender.inventory.CurItem;

			if (heldItem.TypeId != ItemType.Medkit)
				return;

			Vector3 origin = sender.PlayerCameraReference.position;
			Vector3 forward = sender.PlayerCameraReference.forward;

			ReferenceHub hitAlly;

			while (true)
			{
				if (!Physics.Raycast(origin, forward, out RaycastHit hit, HealAllyRange))
					return; // Nothing hit, healing failed

				if (!hit.collider.TryGetComponent(out HitboxIdentity hitbox))
					return; // Didn't hit a player, healing failed

				if (HitboxIdentity.IsEnemy(hitbox.TargetHub, sender))
					return; // It's an enemy, healing failed

				if (hitbox.TargetHub == sender)
				{
					// Player hit their own hitbox. Move origin slightly and repeat.
					const float moveAmount = 0.08f;
					origin += forward * moveAmount;
					continue;
				}
				else
				{
					// Hit a valid ally
					hitAlly = hitbox.TargetHub;
					break;
				}
			}

			hitAlly.playerStats.GetModule<HealthStat>().ServerHeal(HealAllyHp);
			sender.inventory.ServerRemoveItem(heldItem.SerialNumber, null);
		}

		private void SetHealBoost(ReferenceHub hub, bool state)
		{
			MovementBoost statusEffect = hub.playerEffectsController.GetEffect<MovementBoost>();

			if (state && hub.IsHuman())
			{
				statusEffect.ServerSetState(BoostIntensity);
				_activeSpeedBoosts.Add(hub);
			}
			else
			{
				statusEffect.ServerDisable();
				_activeSpeedBoosts.Remove(hub);
			}
		}

		private void OnPlayerDisconnected(ReferenceHub hub)
		{
			_activeSpeedBoosts.Remove(hub);
		}
		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			SetHealBoost(userHub, false);
		}

		private void OnUpdate()
		{
			if (!StaticUnityMethods.IsPlaying)
				return;

			foreach (ReferenceHub hub in _activeSpeedBoosts)
			{
				if (Mathf.Approximately(hub.GetVelocity().SqrMagnitudeIgnoreY(), 0))
					continue; // Prevent damage when stationary.

				hub.playerStats.DealDamage(new UniversalDamageHandler(Time.deltaTime * BoostHealthDrain, DeathTranslations.Scp207));
			}
		}

		private enum ExampleId
		{
			SpeedBoostKey,
			SpeedBoostToggle,
			HealAlly
		}
	}
}

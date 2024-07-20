using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace WarcraftPlugin
{
    public class EventManager
    {
        private WarcraftPlugin _plugin;

        public EventManager(WarcraftPlugin plugin)
        {
            _plugin = plugin;
        }

        public void Initialize()
        {
            _plugin.RegisterEventHandler<EventPlayerDeath>(PlayerDeathHandler);
            _plugin.RegisterEventHandler<EventPlayerSpawn>(PlayerSpawnHandler);
            _plugin.RegisterEventHandler<EventPlayerHurt>(PlayerHurtHandler);
        }

        private HookResult PlayerHurtHandler(EventPlayerHurt @event, GameEventInfo _)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;

            victim?.GetPlayerCharacter()?.GetRace()?.InvokeEvent("player_hurt", @event);
            attacker?.GetPlayerCharacter()?.GetRace()?.InvokeEvent("player_hurt_other", @event);

            return HookResult.Continue;
        }

        private HookResult PlayerSpawnHandler(EventPlayerSpawn @event, GameEventInfo _)
        {
            var player = @event.Userid;
            var race = player.GetPlayerCharacter()?.GetRace();

            if (race != null)
            {
                var name = @event.EventName;
                Server.NextFrame(() =>
                {
                    WarcraftPlugin.Instance.EffectManager.ClearEffects(player);
                    race.InvokeEvent(name, @event);
                });
            }

            return HookResult.Continue;
        }

        private HookResult PlayerDeathHandler(EventPlayerDeath @event, GameEventInfo _)
        {
            var attacker = @event.Attacker;
            var victim = @event.Userid;
            var headshot = @event.Headshot;

            if (attacker.IsValid && victim.IsValid && (attacker != victim) && !attacker.IsBot)
            {
                var weaponName = attacker
                    .PlayerPawn
                    .Value
                    .WeaponServices
                    .ActiveWeapon
                    .Value
                    .DesignerName;

                int xpToAdd = 0;
                int xpHeadshot = 0;
                int xpKnife = 0;

                xpToAdd = _plugin.XpPerKill;

                if (headshot)
                    xpHeadshot = Convert.ToInt32(_plugin.XpPerKill * _plugin.XpHeadshotModifier);

                if (weaponName == "weapon_knife")
                    xpKnife = Convert.ToInt32(_plugin.XpPerKill * _plugin.XpKnifeModifier);

                xpToAdd += xpHeadshot + xpKnife;

                _plugin.ExperienceSystem.AddExperience(attacker, xpToAdd);

                string hsBonus = "";
                if (xpHeadshot != 0)
                {
                    hsBonus = $"(+{xpHeadshot} HS bonus)";
                }

                string knifeBonus = "";
                if (xpKnife != 0)
                {
                    knifeBonus = $"(+{xpKnife} knife bonus)";
                }

                string xpString =
                    $" {ChatColors.Gold}+{xpToAdd} XP {ChatColors.Default}for killing {ChatColors.Green}{victim.PlayerName} {ChatColors.Default}{hsBonus}{knifeBonus}";

                _plugin.GetPlayerCharacter(attacker).SetStatusMessage(xpString);
                attacker.PrintToChat(xpString);
            }

            victim?.GetPlayerCharacter()?.GetRace()?.InvokeEvent("player_death", @event);
            attacker?.GetPlayerCharacter()?.GetRace()?.InvokeEvent("player_kill", @event);

            WarcraftPlugin.Instance.EffectManager.ClearEffects(victim);

            return HookResult.Continue;
        }
    }
}

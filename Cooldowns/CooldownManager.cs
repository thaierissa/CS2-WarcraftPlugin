using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace WarcraftPlugin.Cooldowns
{
    public class CooldownManager
    {
        private float _tickRate = 0.25f;

        public void Initialize()
        {
            WarcraftPlugin.Instance.AddTimer(_tickRate, CooldownTick, TimerFlags.REPEAT);
        }

        private void CooldownTick()
        {
            foreach (var player in WarcraftPlugin.Instance.Players)
            {
                if (player == null)
                    continue;
                for (int i = 0; i < 4; i++)
                {
                    if (player.AbilityCooldowns[i] >= 0)
                    {
                        var oldCooldown = player.AbilityCooldowns[i];
                        player.AbilityCooldowns[i] -= 0.25f;

                        if (oldCooldown > 0 && player.AbilityCooldowns[i] <= 0.0)
                        {
                            player.AbilityCooldowns[i] = 0.0f;
                            PlayEffects(player, i);
                        }
                    }
                }
            }
        }

        public bool IsAvailable(PlayerCharacter player, int abilityIndex)
        {
            return player.AbilityCooldowns[abilityIndex] <= 0.0f;
        }

        public void StartCooldown(PlayerCharacter player, int abilityIndex, float abilityCooldown)
        {
            player.AbilityCooldowns[abilityIndex] = abilityCooldown;
        }

        private void PlayEffects(PlayerCharacter player, int abilityIndex)
        {
            var ability = player.GetRace().GetAbility(abilityIndex);

            player.SetStatusMessage(
                $"{ChatColors.Red}{ability.DisplayName}{ChatColors.Default} ready."
            );
        }
    }
}

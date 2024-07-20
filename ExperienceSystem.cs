using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API.Core;

namespace WarcraftPlugin
{
    public class ExperienceSystem
    {
        private readonly WarcraftPlugin _plugin;
        private readonly List<int> _levelXpRequirement = new(new int[256]);

        public ExperienceSystem(WarcraftPlugin plugin)
        {
            _plugin = plugin;
        }

        public void GenerateXpCurve(int initial, float modifier, int maxLevel)
        {
            for (int i = 1; i <= maxLevel; i++)
            {
                _levelXpRequirement[i] = i == 1 ? initial : Convert.ToInt32(_levelXpRequirement[i - 1] * modifier);
            }
        }

        public int GetXpForLevel(int level) => _levelXpRequirement[level];

        public void AddExperience(CCSPlayerController attacker, int xpToAdd)
        {
            var playerCharacter = _plugin.GetPlayerCharacter(attacker);
            if (playerCharacter == null || playerCharacter.IsMaxLevel)
                return;

            playerCharacter.CurrentXp += xpToAdd;

            while (playerCharacter.CurrentXp >= playerCharacter.AmountToLevel)
            {
                playerCharacter.CurrentXp -= playerCharacter.AmountToLevel;
                GrantLevel(playerCharacter);

                if (playerCharacter.IsMaxLevel)
                    return;
            }
        }

        private void GrantLevel(PlayerCharacter playerCharacter)
        {
            if (playerCharacter.IsMaxLevel)
                return;

            playerCharacter.CurrentLevel++;
            RecalculateXpForLevel(playerCharacter);
            PerformLevelupEvents(playerCharacter);
        }

        private void PerformLevelupEvents(PlayerCharacter playerCharacter)
        {
            if (GetFreeSkillPoints(playerCharacter) > 0)
                WarcraftPlugin.Instance.ShowSkillPointMenu(playerCharacter);

            var soundToPlay = playerCharacter.IsMaxLevel ? "warcraft/ui/gamefound.mp3" : "warcraft/ui/questcompleted.mp3";
            // Sound.EmitSound(playerCharacter.Index, soundToPlay);
        }

        private void RecalculateXpForLevel(PlayerCharacter playerCharacter)
        {
            playerCharacter.AmountToLevel = playerCharacter.IsMaxLevel ? 0 : GetXpForLevel(playerCharacter.CurrentLevel);
        }

        public int GetFreeSkillPoints(PlayerCharacter playerCharacter)
        {
            var totalPointsUsed = playerCharacter.AbilityLevels.Sum();
            var level = Math.Min(playerCharacter.CurrentLevel, WarcraftPlugin.MaxLevel);
            return level - totalPointsUsed;
        }
    }
}

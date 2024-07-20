using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using WarcraftPlugin.Database;
using WarcraftPlugin.Races;

namespace WarcraftPlugin
{
    public static class PlayerExtensions
    {
        public static PlayerCharacter GetPlayerCharacter(this CCSPlayerController player) =>
            WarcraftPlugin.Instance.GetPlayerCharacter(player);
    }

    public class PlayerCharacter
    {
        public int Index { get; }
        public bool IsMaxLevel => CurrentLevel == WarcraftPlugin.MaxLevel;
        public CCSPlayerController Player { get; init; }

        public int CurrentXp;
        public int CurrentLevel;
        public int AmountToLevel;
        public string RaceName;
        public string StatusMessage;

        public List<int> AbilityLevels { get; } = new(new int[4]);
        public List<float> AbilityCooldowns { get; } = new(new float[4]);

        private CharacterRace _race;

        public PlayerCharacter(CCSPlayerController player)
        {
            Player = player;
        }

        public void LoadFromDatabase(
            RaceInformationRecord dbRace,
            ExperienceSystem experienceSystem
        )
        {
            CurrentLevel = dbRace.CurrentLevel;
            CurrentXp = dbRace.CurrentXp;
            RaceName = dbRace.RaceName;
            AmountToLevel = experienceSystem.GetXpForLevel(CurrentLevel);

            for (int i = 0; i < AbilityLevels.Count; i++)
            {
                AbilityLevels[i] = AbilityLevels[i];
            }

            _race = WarcraftPlugin.Instance.RaceManager.InstantiateRace(RaceName);
            _race.PlayerCharacter = this;
            _race.Player = Player;
        }

        public int GetAbilityLevel(int abilityIndex) =>
            abilityIndex >= 0 && abilityIndex < AbilityLevels.Count
                ? AbilityLevels[abilityIndex]
                : 0;

        public void SetAbilityLevel(int abilityIndex, int value)
        {
            if (abilityIndex >= 0 && abilityIndex < AbilityLevels.Count)
            {
                AbilityLevels[abilityIndex] = value;
            }
        }

        public CharacterRace GetRace() => _race;

        public void SetStatusMessage(string status, float duration = 2f)
        {
            StatusMessage = status;
            new Timer(duration, () => StatusMessage = null, 0);
            Player.PrintToChat(" " + status);
        }

        public void GrantAbilityLevel(int abilityIndex)
        {
            if (abilityIndex >= 0 && abilityIndex < AbilityLevels.Count)
            {
                AbilityLevels[abilityIndex]++;
            }
            else
            {
                Console.WriteLine(
                    $"Invalid ability index: {abilityIndex}. It must be between 0 and {AbilityLevels.Count - 1}."
                );
            }
        }
    }
}

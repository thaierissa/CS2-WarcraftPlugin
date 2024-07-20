using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Cooldowns;
using WarcraftPlugin.Database;
using WarcraftPlugin.Effects;
using WarcraftPlugin.Races;

namespace WarcraftPlugin
{
    public class WarcraftPlugin : BasePlugin
    {
        private static WarcraftPlugin _instance;
        public static WarcraftPlugin Instance => _instance;

        public override string ModuleName => "WarcraftPlugin";
        public override string ModuleVersion => "0.0.1";

        public static int MaxLevel { get; } = 16;
        public static int MaxSkillLevel { get; } = 5;
        public static int MaxUltimateLevel { get; } = 5;

        private readonly Dictionary<IntPtr, PlayerCharacter> _players = new();
        private EventManager _eventManager;
        public ExperienceSystem ExperienceSystem;
        public RaceManager RaceManager;
        public EffectManager EffectManager;
        public CooldownManager CooldownManager;
        private DatabaseManager _databaseManager;

        public int XpPerKill { get; } = 20;
        public float XpHeadshotModifier { get; } = 0.25f;
        public float XpKnifeModifier { get; } = 0.25f;

        public List<PlayerCharacter> Players => _players.Values.ToList();

        public PlayerCharacter GetPlayerCharacter(CCSPlayerController player)
        {
            if (_players.TryGetValue(player.Handle, out var playerCharacter))
                return playerCharacter;

            if (player.IsValid && !player.IsBot)
            {
                playerCharacter = _databaseManager.LoadClient(player, ExperienceSystem);
                _players[player.Handle] = playerCharacter;
            }

            return playerCharacter;
        }

        public void SetPlayerCharacter(
            CCSPlayerController player,
            PlayerCharacter playerCharacter
        ) => _players[player.Handle] = playerCharacter;

        public override void Load(bool hotReload)
        {
            base.Load(hotReload);
            _instance ??= this;

            ExperienceSystem = new ExperienceSystem(this);
            ExperienceSystem.GenerateXpCurve(110, 1.07f, MaxLevel);

            _databaseManager = new DatabaseManager();
            RaceManager = new RaceManager();
            RaceManager.Initialize();

            EffectManager = new EffectManager();
            EffectManager.Initialize();

            CooldownManager = new CooldownManager();
            CooldownManager.Initialize();

            RegisterCommands();
            RegisterListeners();

            if (hotReload)
            {
                OnMapStartHandler(null);
            }

            _eventManager = new EventManager(this);
            _eventManager.Initialize();

            _databaseManager.Initialize(ModuleDirectory);
        }

        private void RegisterCommands()
        {
            AddCommand("ability1", "ability1", Ability1Pressed);
            AddCommand("ability2", "ability2", Ability2Pressed);
            AddCommand("ability3", "ability3", Ability3Pressed);
            AddCommand("ultimate", "ultimate", Ability4Pressed);
            AddCommand("changerace", "changerace", CommandChangeRace);
            AddCommand("raceinfo", "raceinfo", CommandRaceInfo);
            AddCommand("resetskills", "resetskills", CommandResetSkills);
            AddCommand("addxp", "addxp", CommandAddExperience);
            AddCommand(
                "skills",
                "skills",
                (client, _) => ShowSkillPointMenu(GetPlayerCharacter(client))
            );
        }

        private void RegisterListeners()
        {
            RegisterListener<Listeners.OnClientConnect>(OnClientConnectHandler);
            RegisterListener<Listeners.OnMapStart>(OnMapStartHandler);
            RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnectHandler);
        }

        private void CommandAddExperience(CCSPlayerController client, CommandInfo commandInfo)
        {
            var playerCharacter = GetPlayerCharacter(client);
            if (playerCharacter == null)
                return;

            if (int.TryParse(commandInfo.ArgByIndex(1), out var xpToAdd))
            {
                ExperienceSystem.AddExperience(client, xpToAdd);
            }
        }

        private void CommandResetSkills(CCSPlayerController client, CommandInfo commandInfo)
        {
            var playerCharacter = GetPlayerCharacter(client);
            if (playerCharacter == null)
                return;

            if (ExperienceSystem.GetFreeSkillPoints(playerCharacter) > 0)
            {
                ShowSkillPointMenu(playerCharacter);
            }
        }

        private void OnClientDisconnectHandler(int slot)
        {
            var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));
            if (!player.IsValid || player.IsBot)
                return;

            var playerCharacter = player.GetPlayerCharacter();
            playerCharacter.GetRace().PlayerChangingToAnotherRace();
            SetPlayerCharacter(player, null);
            _databaseManager.SaveClient(player);
        }

        private void OnMapStartHandler(string mapName)
        {
            AddTimer(0.25f, StatusUpdate, TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE);
            AddTimer(
                60.0f,
                _databaseManager.SaveAllClients,
                TimerFlags.REPEAT | TimerFlags.STOP_ON_MAPCHANGE
            );

            Server.PrintToConsole("Map Load WarcraftPlugin\n");
        }

        private void StatusUpdate()
        {
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>(
                "cs_player_controller"
            );
            foreach (var player in playerEntities.Where(p => p.IsValid && p.PawnIsAlive))
            {
                var playerCharacter = GetPlayerCharacter(player);
                if (playerCharacter == null)
                    continue;

                var message =
                    $"{playerCharacter.GetRace().DisplayName} ({playerCharacter.CurrentLevel})\n"
                    + (
                        playerCharacter.IsMaxLevel
                            ? ""
                            : $"Experience: {playerCharacter.CurrentXp}/{playerCharacter.AmountToLevel}\n"
                    )
                    + $"{playerCharacter.StatusMessage}";

                player.PrintToCenter(message);
            }
        }

        private void OnClientConnectHandler(int slot, string name, string ipAddress)
        {
            var player = new CCSPlayerController(NativeAPI.GetEntityFromIndex(slot + 1));
            Console.WriteLine($"Put in server {player.Handle}");
            Console.WriteLine($"Put in server {player.Handle}");

            if (!player.IsValid || player.IsBot)
                return;

            if (!_databaseManager.ClientExists(player.SteamID))
            {
                _databaseManager.AddNewClient(player);
            }

            var playerCharacter = _databaseManager.LoadClient(player, ExperienceSystem);
            _players[player.Handle] = playerCharacter;

            Console.WriteLine("Player just connected: " + playerCharacter);
        }

        private void CommandRaceInfo(CCSPlayerController client, CommandInfo commandInfo)
        {
            var menu = new CenterHtmlMenu("Race Information", _instance);
            var races = RaceManager.GetAllRaces().OrderBy(r => r.DisplayName);

            foreach (var race in races)
            {
                menu.AddMenuOption(
                    race.DisplayName,
                    (player, option) =>
                    {
                        player.PrintToChat("--------");
                        for (int i = 0; i < 4; i++)
                        {
                            var ability = race.GetAbility(i);
                            char color = i == 3 ? ChatColors.Gold : ChatColors.Purple;

                            player.PrintToChat(
                                $" {color}{ability.DisplayName}{ChatColors.Default}: {ability.GetDescription(0)}"
                            );
                        }
                        player.PrintToChat("--------");
                    }
                );
            }

            MenuManager.OpenCenterHtmlMenu(_instance, client, menu);
        }

        private void CommandChangeRace(CCSPlayerController client, CommandInfo commandInfo)
        {
            var menu = new CenterHtmlMenu("Change Race", _instance);
            var races = RaceManager.GetAllRaces().OrderBy(r => r.DisplayName);

            foreach (var race in races)
            {
                menu.AddMenuOption(
                    race.DisplayName,
                    (player, option) =>
                    {
                        _databaseManager.SaveClient(player);

                        if (race.InternalName == player.GetPlayerCharacter().RaceName)
                            return;

                        var playerCharacter = player.GetPlayerCharacter();
                        playerCharacter.GetRace().PlayerChangingToAnotherRace();
                        playerCharacter.RaceName = race.InternalName;

                        _databaseManager.SaveCurrentRace(player);
                        _databaseManager.LoadClient(player, ExperienceSystem);

                        playerCharacter.GetRace().PlayerChangingToRace();
                        player.PlayerPawn.Value.CommitSuicide(true, true);
                    }
                );
            }

            MenuManager.OpenCenterHtmlMenu(_instance, client, menu);
        }

        private void Ability1Pressed(CCSPlayerController client, CommandInfo commandInfo) =>
            GetPlayerCharacter(client)?.GetRace()?.InvokeAbility(0);

        private void Ability2Pressed(CCSPlayerController client, CommandInfo commandInfo) =>
            GetPlayerCharacter(client)?.GetRace()?.InvokeAbility(1);

        private void Ability3Pressed(CCSPlayerController client, CommandInfo commandInfo) =>
            GetPlayerCharacter(client)?.GetRace()?.InvokeAbility(2);

        private void Ability4Pressed(CCSPlayerController client, CommandInfo commandInfo) =>
            GetPlayerCharacter(client)?.GetRace()?.InvokeAbility(3);

        public override void Unload(bool hotReload)
        {
            base.Unload(hotReload);
        }

        public void ShowSkillPointMenu(PlayerCharacter playerCharacter)
        {
            if (playerCharacter == null)
                return;

            var menu = new CenterHtmlMenu(
                $"Level up skills ({ExperienceSystem.GetFreeSkillPoints(playerCharacter)} available)",
                _instance
            );
            var race = playerCharacter.GetRace();

            for (int i = 0; i < 4; i++)
            {
                var ability = race.GetAbility(i);
                var displayString = $"{ability.DisplayName} ({playerCharacter.GetAbilityLevel(i)})";
                bool disabled =
                    i == 3
                        ? playerCharacter.CurrentLevel < MaxLevel
                            || playerCharacter.GetAbilityLevel(i) >= 1
                        : playerCharacter.GetAbilityLevel(i) >= MaxSkillLevel;

                if (ExperienceSystem.GetFreeSkillPoints(playerCharacter) == 0)
                    disabled = true;

                int abilityIndex = i; // Ensure the correct ability index is capturedd

                menu.AddMenuOption(
                    displayString,
                    (player, option) =>
                    {
                        var pc = player.GetPlayerCharacter();
                        if (pc == null)
                            return;

                        if (ExperienceSystem.GetFreeSkillPoints(pc) > 0)
                        {
                            pc.GrantAbilityLevel(abilityIndex);
                        }
                        ShowSkillPointMenu(pc);
                    },
                    disabled
                );
            }

            MenuManager.OpenCenterHtmlMenu(_instance, playerCharacter.Player, menu);
        }
    }
}

using System;
using System.IO;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Dapper;
using Microsoft.Data.Sqlite;

namespace WarcraftPlugin.Database
{
    public class DatabaseManager
    {
        private SqliteConnection _connection;

        public void Initialize(string directory)
        {
            _connection = new SqliteConnection(
                $"Data Source={Path.Join(directory, "database.db")}"
            );

            _connection.Execute(
                @"
                CREATE TABLE IF NOT EXISTS `players` (
                    `steamid` UNSIGNED BIG INT NOT NULL,
                    `currentRace` VARCHAR(32) NOT NULL DEFAULT 'undead_scourge',
                    `name` VARCHAR(64),
                    PRIMARY KEY (`steamid`));"
            );

            _connection.Execute(
                @"
                CREATE TABLE IF NOT EXISTS `raceinformation` (
                    `steamid` UNSIGNED BIG INT NOT NULL,
                    `racename` VARCHAR(32) NOT NULL,
                    `currentXP` INT NULL DEFAULT 0,
                    `currentLevel` INT NULL DEFAULT 1,
                    `amountToLevel` INT NULL DEFAULT 100,
                    `ability1level` TINYINT NULL DEFAULT 0,
                    `ability2level` TINYINT NULL DEFAULT 0,
                    `ability3level` TINYINT NULL DEFAULT 0,
                    `ability4level` TINYINT NULL DEFAULT 0,
                    PRIMARY KEY (`steamid`, `racename`));
"
            );
        }

        public bool ClientExists(ulong steamid) =>
            _connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM players WHERE steamid = @steamid",
                new { steamid }
            ) > 0;

        public void AddNewClient(CCSPlayerController player)
        {
            Console.WriteLine($"Adding client to database {player.SteamID}");
            _connection.Execute(
                @"
                INSERT INTO players (`steamid`, `currentRace`)
                VALUES(@steamid, 'undead_scourge')",
                new { steamid = player.SteamID }
            );
        }

        public PlayerCharacter LoadClient(
            CCSPlayerController player,
            ExperienceSystem ExperienceSystem
        )
        {
            var dbPlayer = _connection.QueryFirstOrDefault<PlayerRecord>(
                @"
                SELECT * FROM `players` WHERE `steamid` = @steamid",
                new { steamid = player.SteamID }
            );

            if (dbPlayer == null)
            {
                AddNewClient(player);
                dbPlayer = new PlayerRecord
                {
                    SteamId = player.SteamID,
                    CurrentRace = "undead_scourge"
                };
            }

            EnsureRaceInformationExists(player.SteamID, dbPlayer.CurrentRace);

            var raceInfo = _connection.QueryFirst<RaceInformationRecord>(
                @"
                SELECT * FROM `raceinformation` WHERE `steamid` = @steamid AND `racename` = @racename",
                new { steamid = player.SteamID, racename = dbPlayer.CurrentRace }
            );

            var playerCharacter = new PlayerCharacter(player);
            playerCharacter.LoadFromDatabase(raceInfo, ExperienceSystem);
            WarcraftPlugin.Instance.SetPlayerCharacter(player, playerCharacter);

            return playerCharacter;
        }

        private void EnsureRaceInformationExists(ulong steamid, string raceName)
        {
            var exists =
                _connection.ExecuteScalar<int>(
                    @"
                SELECT COUNT(*) FROM `raceinformation` WHERE `steamid` = @steamid AND `racename` = @racename",
                    new { steamid, racename = raceName }
                ) > 0;

            if (!exists)
            {
                _connection.Execute(
                    @"
                    INSERT INTO `raceinformation` (`steamid`, `racename`)
                    VALUES (@steamid, @racename);",
                    new { steamid, racename = raceName }
                );
            }
        }

        public void SaveClient(CCSPlayerController player)
        {
            var playerCharacter = WarcraftPlugin.Instance.GetPlayerCharacter(player);
            Server.PrintToConsole($"Saving {player.PlayerName} to database...");

            EnsureRaceInformationExists(player.SteamID, playerCharacter.RaceName);

            _connection.Execute(
                @"
                UPDATE `raceinformation` SET
                    `currentXP` = @currentXp,
                    `currentLevel` = @currentLevel,
                    `ability1level` = @ability1Level,
                    `ability2level` = @ability2Level,
                    `ability3level` = @ability3Level,
                    `ability4level` = @ability4Level,
                    `amountToLevel` = @amountToLevel
                WHERE `steamid` = @steamid AND `racename` = @racename;",
                new
                {
                    currentXp = playerCharacter.CurrentXp,
                    currentLevel = playerCharacter.CurrentLevel,
                    ability1Level = playerCharacter.GetAbilityLevel(0),
                    ability2Level = playerCharacter.GetAbilityLevel(1),
                    ability3Level = playerCharacter.GetAbilityLevel(2),
                    ability4Level = playerCharacter.GetAbilityLevel(3),
                    amountToLevel = playerCharacter.AmountToLevel,
                    steamid = player.SteamID,
                    racename = playerCharacter.RaceName
                }
            );
        }

        public void SaveAllClients()
        {
            var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>(
                "cs_player_controller"
            );
            foreach (var player in playerEntities)
            {
                if (!player.IsValid)
                    continue;

                var playerCharacter = WarcraftPlugin.Instance.GetPlayerCharacter(player);
                if (playerCharacter == null)
                    continue;

                SaveClient(player);
            }
        }

        public void SaveCurrentRace(CCSPlayerController player)
        {
            var playerCharacter = WarcraftPlugin.Instance.GetPlayerCharacter(player);

            _connection.Execute(
                @"
                UPDATE `players` SET
                    `currentRace` = @currentRace,
                    `name` = @name
                WHERE `steamid` = @steamid;",
                new
                {
                    currentRace = playerCharacter.RaceName,
                    name = player.PlayerName,
                    steamid = player.SteamID
                }
            );
        }
    }
}

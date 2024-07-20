namespace WarcraftPlugin.Database
{
    public class RaceInformationRecord
    {
        public ulong SteamId { get; set; }
        public string RaceName { get; set; }
        public int CurrentXp { get; set; }
        public int CurrentLevel { get; set; }
        public int AmountToLevel { get; set; }
        public int Ability1Level { get; set; }
        public int Ability2Level { get; set; }
        public int Ability3Level { get; set; }
        public int Ability4Level { get; set; }
    }
}

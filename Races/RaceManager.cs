using System;
using System.Collections.Generic;
using System.Linq;

namespace WarcraftPlugin.Races
{
    public class RaceManager
    {
        private Dictionary<string, Type> _races = new Dictionary<string, Type>();
        private Dictionary<string, CharacterRace> _raceObjects =
            new Dictionary<string, CharacterRace>();

        public void Initialize()
        {
            RegisterRace<RaceUndeadScourge>();
            RegisterRace<RaceHumanAlliance>();
        }

        private void RegisterRace<T>()
            where T : CharacterRace, new()
        {
            var race = new T();
            race.Register();
            _races[race.InternalName] = typeof(T);
            _raceObjects[race.InternalName] = race;
        }

        public CharacterRace InstantiateRace(string name)
        {
            if (!_races.ContainsKey(name))
                throw new Exception("Race not found: " + name);

            var race = (CharacterRace)Activator.CreateInstance(_races[name]);
            race.Register();

            return race;
        }

        public CharacterRace[] GetAllRaces()
        {
            return _raceObjects.Values.ToArray();
        }

        public CharacterRace GetRace(string name)
        {
            return _raceObjects.ContainsKey(name) ? _raceObjects[name] : null;
        }
    }
}

using System.Drawing;
using CounterStrikeSharp.API.Core;

namespace WarcraftPlugin.Effects
{
    public abstract class WarcraftEffect
    {
        protected WarcraftEffect(
            CCSPlayerController owner,
            CCSPlayerController target,
            float duration
        )
        {
            Owner = owner;
            Target = target;
            Duration = duration;
            RemainingDuration = duration;
        }

        public CCSPlayerController Owner { get; }
        public CCSPlayerController Target { get; }

        public float Duration { get; }
        public float RemainingDuration { get; set; }

        public virtual void OnStart() { }

        public virtual void OnTick() { }

        public virtual void OnFinish() { }
    }
}

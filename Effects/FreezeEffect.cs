using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace WarcraftPlugin.Effects
{
    public class FreezeEffect : WarcraftEffect
    {
        public FreezeEffect(CCSPlayerController owner, CCSPlayerController target, float duration)
            : base(owner, target, duration) { }

        public override void OnStart()
        {
            Target.GetPlayerCharacter()?.SetStatusMessage($"{ChatColors.Blue}[FROZEN]{ChatColors.Default}", Duration);
            // Target.MoveType = MoveType.MOVETYPE_NONE;
            // Target.RenderMode = RenderMode.RENDER_TRANSCOLOR;
            // Target.Color = Color.FromArgb(255, 50, 50, 255);
            Console.WriteLine("Added freeze");
        }

        public override void OnTick()
        {
            base.OnTick();
            Console.WriteLine("Freeze tick");
        }

        public override void OnFinish()
        {
            // Target.MoveType = MoveType.MOVETYPE_ISOMETRIC;
            // Target.RenderMode = RenderMode.RENDER_NORMAL;
            // Target.Color = Color.FromArgb(255, 255, 255, 255);
            Console.WriteLine("Freeze finished");
        }
    }
}

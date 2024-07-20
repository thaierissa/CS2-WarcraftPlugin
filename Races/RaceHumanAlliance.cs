using System;
using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using WarcraftPlugin.Effects;

namespace WarcraftPlugin.Races
{
    public class RaceHumanAlliance : CharacterRace
    {
        public override string InternalName => "human_alliance";
        public override string DisplayName => "Human Alliance";

        private Timer _invisiblityTimer = null;
        private bool _isInvisible = false;

        public override void Register()
        {
            AddAbility(
                new SimpleWarcraftAbility(
                    "invisibility",
                    "Invisibility",
                    i =>
                        $"Every 1 second, you will become invisible for {ChatColors.Green}0.15/0.30/0.45/0.60/0.75{ChatColors.Default} seconds"
                )
            );
            AddAbility(
                new SimpleWarcraftAbility(
                    "devotion",
                    "Devotion",
                    i =>
                        $"Spawn with an extra {ChatColors.Yellow}10/20/30/40/50{ChatColors.Default} HP."
                )
            );
            AddAbility(
                new SimpleWarcraftAbility(
                    "freeze",
                    "Freeze",
                    i =>
                        $"When you hit an enemy, there is a {ChatColors.Blue}5/10/15/20/25%{ChatColors.Default} chance you will freeze them in place for 1 second."
                )
            );
            AddAbility(
                new SimpleCooldownAbility(
                    "dash",
                    "Dash",
                    i =>
                        $"When you press your ultimate key, you will be launched at {ChatColors.Red}extreme speed{ChatColors.Default} in the direction you are facing.",
                    3.0f
                )
            );

            HookEvent<EventPlayerDeath>("player_death", PlayerDeath);
            HookEvent<EventPlayerSpawn>("player_spawn", PlayerSpawn);
            HookEvent<EventPlayerHurt>("player_hurt_other", PlayerHurtOther);

            HookAbility(3, Ultimate);
        }

        private void Ultimate()
        {
            if (PlayerCharacter.GetAbilityLevel(3) < 1)
                return;

            if (IsAbilityReady(3))
            {
                Dash(1000f);
                StartCooldown(3);
            }
        }

        private void Dash(float distance)
        {
            var directionVec = new Vector();

            NativeAPI.AngleVectors(
                Player.PlayerPawn.Value.EyeAngles.Handle,
                directionVec.Handle,
                IntPtr.Zero,
                IntPtr.Zero
            );

            // Always shoot us up a little bit if were on the ground and not aiming up.
            if (Player.GroundEntity.IsValid != false && directionVec.Z < 0.275)
            {
                directionVec.Z = 0.275f;
            }

            directionVec *= distance;

            Player.PlayerPawn.Value.AbsVelocity.X = directionVec.X;
            Player.PlayerPawn.Value.AbsVelocity.Y = directionVec.Y;
            Player.PlayerPawn.Value.AbsVelocity.Z = directionVec.Z;
        }

        private void PlayerHurtOther(GameEvent obj)
        {
            var @event = (EventPlayerHurt)obj;
            if (!@event.Userid.IsValid || !@event.Userid.PawnIsAlive)
                return;

            double rolledValue = new Random().NextDouble();
            float chanceToStun = PlayerCharacter.GetAbilityLevel(2) * 0.05f;

            if (rolledValue < chanceToStun)
            {
                var victim = @event.Userid;
                DispatchEffect(new FreezeEffect(Player, victim, 1.0f));
            }
        }

        private void PlayerSpawn(GameEvent obj)
        {
            Player.PlayerPawn.Value.Health = 100 + (10 * PlayerCharacter.GetAbilityLevel(1));

            if (_invisiblityTimer != null)
            {
                _invisiblityTimer.Kill();
            }

            _invisiblityTimer = WarcraftPlugin.Instance.AddTimer(
                PlayerCharacter.GetAbilityLevel(0) * 0.15f,
                ToggleInvisiblity,
                TimerFlags.REPEAT
            );

            ResetColor();
            _isInvisible = false;
        }

        public override void PlayerChangingToAnotherRace()
        {
            base.PlayerChangingToAnotherRace();
            _invisiblityTimer?.Kill();
            MakeVisible();
        }

        private void ToggleInvisiblity()
        {
            if (_isInvisible)
                MakeVisible();
            else
                MakeInvisible();

            _isInvisible = !_isInvisible;
        }

        private void MakeInvisible()
        {
            if (!Player.PawnIsAlive)
            {
                ResetColor();
                return;
            }
        }

        private void MakeVisible()
        {
            if (!Player.PawnIsAlive)
            {
                ResetColor();
                return;
            }

            ResetColor();
        }

        private void PlayerDeath(GameEvent obj)
        {
            ResetColor();
        }

        private void ResetColor()
        {
            // Player.RenderMode = RenderMode.RENDER_NORMAL;
            // Player.Color = Color.FromArgb(255, 255, 255, 255);
        }
    }
}

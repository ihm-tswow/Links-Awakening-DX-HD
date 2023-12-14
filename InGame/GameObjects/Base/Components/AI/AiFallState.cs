using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components.AI
{
    class AiFallState
    {
        public delegate void HoleAbsorbFunction();

        private readonly AiComponent _aiComponent;
        private readonly BodyComponent _body;
        private readonly HoleAbsorbFunction _onAbsorb;
        private readonly HoleAbsorbFunction _onDeath;

        public AiFallState(AiComponent aiComponent, BodyComponent body, HoleAbsorbFunction onAbsorb = null, HoleAbsorbFunction onDeath = null, int fallTime = 600)
        {
            _aiComponent = aiComponent;

            _body = body;
            _body.HoleAbsorb = OnHoleAbsorb;

            _onAbsorb = onAbsorb;
            _onDeath = onDeath;

            var fallingState = new AiState();
            fallingState.Trigger.Add(new AiTriggerCountdown(fallTime, null, FallDeath));
            _aiComponent.States.Add("falling", fallingState);
        }

        public void OnHoleAbsorb()
        {
            if (_aiComponent.CurrentStateId == "falling")
                return;

            _aiComponent.ChangeState("falling");

            _body.Drag = 0.0f;
            _body.VelocityTarget = Vector2.Zero;

            _onAbsorb?.Invoke();
        }

        private void FallDeath()
        {
            _aiComponent.Owner.Map.Objects.DeleteObjects.Add(_aiComponent.Owner);

            // play sound effect
            Game1.GameManager.PlaySoundEffect("D360-24-18");

            var fallAnimation = new ObjAnimator(_aiComponent.Owner.Map, 0, 0, Values.LayerBottom, "Particles/fall", "idle", true);
            fallAnimation.EntityPosition.Set(new Vector2(
                _body.Position.X + _body.OffsetX + _body.Width / 2.0f - 5,
                _body.Position.Y + _body.OffsetY + _body.Height / 2.0f - 5));
            _aiComponent.Owner.Map.Objects.SpawnObject(fallAnimation);

            _onDeath?.Invoke();
        }
    }
}

using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components.AI
{
    class AiDeepWaterState
    {
        private readonly BodyComponent _body;
        private readonly int _fallTime;

        private float _deepWaterCounter;
        private double _lastDeepWaterCollision;

        public AiDeepWaterState(BodyComponent body, int fallTime = 250)
        {
            _body = body;
            _body.OnDeepWaterFunction = OnDeepWaterCollision;

            _fallTime = fallTime;
        }

        public void OnDeepWaterCollision()
        {
            if (_lastDeepWaterCollision != Game1.TotalGameTimeLast)
                _deepWaterCounter = _fallTime;

            _deepWaterCounter -= Game1.DeltaTime;

            _lastDeepWaterCollision = Game1.TotalGameTime;

            if (_deepWaterCounter < 0)
            {
                FallDeath();
            }
        }

        private void FallDeath()
        {
            // play sound effect
            Game1.GameManager.PlaySoundEffect("D360-24-18");

            // spawn splash effect
            var fallAnimation = new ObjAnimator(_body.Owner.Map,
                (int)(_body.Position.X + _body.OffsetX + _body.Width / 2.0f),
                (int)(_body.Position.Y + _body.OffsetY + _body.Height / 2.0f),
                Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
            _body.Owner.Map.Objects.SpawnObject(fallAnimation);

            _body.Owner.Map.Objects.DeleteObjects.Add(_body.Owner);
        }
    }
}

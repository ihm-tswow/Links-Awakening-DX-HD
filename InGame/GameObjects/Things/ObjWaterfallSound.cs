using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjWaterfallSound : GameObject
    {
        private const int Range = 128;

        private static Vector2 _maxPosition;
        private static float _maxDistance;
        private static double _lastTime;
        private static float _soundTimer;

        public ObjWaterfallSound() : base("editor shore sound") { }

        public ObjWaterfallSound(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            var distance = (EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position).Length();
            if (distance < _maxDistance)
            {
                _maxDistance = distance;
                _maxPosition = EntityPosition.Position;
            }

            // @HACK: only update the sound from one object
            if (_lastTime == Game1.TotalGameTime)
                return;

            _lastTime = Game1.TotalGameTime;
            _soundTimer -= Game1.DeltaTime;
            if (_soundTimer < 0)
            {
                _soundTimer += Game1.RandomNumber.Next(64, 80);
                Game1.GameManager.PlaySoundEffect("D378-30-1E", true, _maxPosition, Range);
                _maxDistance = float.MaxValue;
            }
        }
    }
}

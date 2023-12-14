using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using System;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjShoreSound : GameObject
    {
        private readonly int _positionY;
        private float _shoreTimer;

        public ObjShoreSound() : base("editor shore sound") { }

        public ObjShoreSound(Map.Map map, int posX, int posY) : base(map)
        {
            _positionY = posY;
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            if (MapManager.ObjLink.PosY < _positionY)
                return;

            _shoreTimer += Game1.DeltaTime;

            if (_shoreTimer > 2000)
            {
                _shoreTimer -= 2000;
                Game1.GameManager.PlaySoundEffect("D378-15-0F", true, new Vector2(MathF.Min(160 * 5.5f, MapManager.ObjLink.PosX), _positionY + 200));
            }
        }
    }
}

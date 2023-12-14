using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjColorShift : GameObject
    {
        private Rectangle _collisionRectangle;
        private readonly int _colorDirection;

        public ObjColorShift() : base("editor color shift") { }

        public ObjColorShift(Map.Map map, int posX, int posY, int colorDirection, int width, int height) : base(map)
        {
            _collisionRectangle = new Rectangle(posX, posY, width, height);
            _colorDirection = colorDirection;

            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(_collisionRectangle, OnCollision));
        }

        public void OnCollision(GameObject gameObject)
        {
            if (_colorDirection == 1)
                Game1.GameManager.ForestColorState = 1 - (MapManager.ObjLink.PosX - _collisionRectangle.X) / _collisionRectangle.Width;
            else if (_colorDirection == 2)
                Game1.GameManager.ForestColorState = 1 - (MapManager.ObjLink.PosY - _collisionRectangle.Y) / _collisionRectangle.Height;
            else if (_colorDirection == 3)
                Game1.GameManager.ForestColorState = (MapManager.ObjLink.PosY - _collisionRectangle.Y) / _collisionRectangle.Height;
            else
                Game1.GameManager.ForestColorState = MathHelper.Clamp(Game1.GameManager.ForestColorState, 0, 1) * 0.55f;
        }
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjRollBand : GameObject
    {
        private readonly List<GameObject> _collidingObjects = new List<GameObject>();

        private readonly Box _collisionBox;
        private readonly Rectangle _sourceRectangle;
        private readonly Vector2 _vecRollBand;
        private readonly Point _drawPosition;

        private readonly float _direction;
        private int _animationCount;
        private int _animationSpeed = 100;

        public ObjRollBand() : base("rollband_0") { }

        public ObjRollBand(Map.Map map, int posX, int posY, int direction) : base(map)
        {
            _sourceRectangle = Resources.SourceRectangle("rollband_0");

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _drawPosition.X = posX + 8;
            _drawPosition.Y = posY + 8;

            _direction = (float)(direction * Math.PI / 2f);

            _vecRollBand = AnimationHelper.DirectionOffset[(direction + 3) % 4] * 0.25f;// 10.0f / 60.0f; // 10 pixels/second

            _collisionBox = new Box(posX + 3, posY + 3, 0, 10, 10, 8);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        public void Update()
        {
            _animationCount = ((int)(Game1.TotalGameTime) % (16 * _animationSpeed)) / _animationSpeed;

            // get and move the components colliding with the rollband
            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects,
                (int)_collisionBox.Left, (int)_collisionBox.Back, (int)_collisionBox.Width, (int)_collisionBox.Height, BodyComponent.Mask);

            foreach (var gameObject in _collidingObjects)
            {
                var gameObjectBody = ((BodyComponent)gameObject.Components[BodyComponent.Index]);
                if (_collisionBox.Contains(gameObjectBody.BodyBox.Box))
                    gameObjectBody.AdditionalMovementVT = _vecRollBand;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var sourceY = -_animationCount % 16;
            spriteBatch.Draw(Resources.SprObjects, new Rectangle(
                    _drawPosition.X, _drawPosition.Y, _sourceRectangle.Width, _sourceRectangle.Height),
                new Rectangle(_sourceRectangle.X, _sourceRectangle.Y + sourceY, _sourceRectangle.Width, _sourceRectangle.Height),
                Color.White, _direction, new Vector2(8, 8), SpriteEffects.None, 0);
        }
    }
}

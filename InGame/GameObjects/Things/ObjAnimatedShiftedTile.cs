using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjAnimatedShiftedTile : GameObject
    {
        private readonly CSprite _sprite;
        private readonly Rectangle _sourceRectangle;
        private readonly int _offsetX;
        private readonly int _offsetY;

        private readonly int _frames;
        private readonly int _animationSpeed;

        public ObjAnimatedShiftedTile(Map.Map map, int posX, int posY,
            Rectangle sourceRectangle, int offsetX, int offsetY, int animationSpeed, int spriteEffect) : base(map)
        {
            SprEditorImage = Resources.SprObjects;
            EditorIconSource = sourceRectangle;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, sourceRectangle.Width, sourceRectangle.Height);

            _sourceRectangle = sourceRectangle;
            _offsetX = offsetX;
            _offsetY = offsetY;
            _animationSpeed = animationSpeed;
            
            _sprite = new CSprite(Resources.SprObjects, EntityPosition, sourceRectangle, Vector2.Zero)
            {
                SpriteEffect = (SpriteEffects)spriteEffect
            };

            _frames = Math.Min(
                _sourceRectangle.Width / Math.Max(1, Math.Abs(offsetX)), 
                _sourceRectangle.Height / Math.Max(1, Math.Abs(offsetY)));

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerBottom));
        }

        private void Update()
        {
            // all the animations are in sync
            var currentFrame = (int)Game1.TotalGameTime % (_frames * _animationSpeed) / _animationSpeed;

            _sprite.SourceRectangle = new Rectangle(
                _sourceRectangle.X + _offsetX * currentFrame,
                _sourceRectangle.Y + _offsetY * currentFrame,
                _sourceRectangle.Width, _sourceRectangle.Height);
        }
    }
}
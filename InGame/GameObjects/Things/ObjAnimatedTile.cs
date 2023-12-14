using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjAnimatedTile : GameObject
    {
        public readonly CSprite Sprite;

        private readonly Rectangle _sourceRectangle;

        private int _currentFrame;
        private float _timeCounter;

        private readonly int _frames;
        private readonly int _animationSpeed;

        // TODO_OPT: should probably only switch the tilemap values
        // one object could update a lot of tiles in the tilemap
        // would be better for performance
        public ObjAnimatedTile(Map.Map map, int posX, int posY,
            string spriteId, int frames, int animationSpeed, bool sync, int spriteEffects, int drawLayer) : base(map)
        {
            var sprite = Resources.GetSprite(spriteId);

            _sourceRectangle = sprite.ScaledRectangle;

            SprEditorImage = sprite.Texture;
            EditorIconSource = _sourceRectangle;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, _sourceRectangle.Width, _sourceRectangle.Height);

            _frames = frames;
            _animationSpeed = animationSpeed;
            Sprite = new CSprite(sprite.Texture, EntityPosition, _sourceRectangle, Vector2.Zero)
            {
                Scale = sprite.Scale,
                SpriteEffect = (SpriteEffects)spriteEffects
            };

            AddComponent(UpdateComponent.Index, new UpdateComponent(sync ? UpdateSync : UpdateNoSync));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(Sprite, drawLayer));

            // randomize the starting state of the animation
            if (!sync) 
                RandomizeStartFrame();
        }

        public void RandomizeStartFrame()
        {
            var pX = EntityPosition.X / 200f;
            var pY = EntityPosition.Y / 200f;
            _timeCounter = (int)((pX * pX + pY * pY) * 30f);

            while (_timeCounter >= _animationSpeed)
            {
                _currentFrame++;
                if (_currentFrame >= _frames)
                    _currentFrame = 0;

                _timeCounter -= _animationSpeed;
            }
        }

        public void UpdateSync()
        {
            // all the animations are in sync
            _currentFrame = (int)Game1.TotalGameTime %
                            (_frames * _animationSpeed) / _animationSpeed;

            UpdateSourceRectangle();
        }

        public void UpdateNoSync()
        {
            _timeCounter += Game1.DeltaTime;

            if (_timeCounter > _animationSpeed)
            {
                _currentFrame++;
                _timeCounter -= _animationSpeed;

                if (_currentFrame >= _frames)
                    _currentFrame = 0;

                UpdateSourceRectangle();
            }
        }

        public void UpdateSourceRectangle()
        {
            Sprite.SourceRectangle.X = _sourceRectangle.X + _sourceRectangle.Width * _currentFrame;
        }
    }
}
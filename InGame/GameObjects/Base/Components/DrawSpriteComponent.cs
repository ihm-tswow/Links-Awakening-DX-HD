using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    public class DrawSpriteComponent : DrawComponent
    {
        public CSprite Sprite;

        public DrawSpriteComponent(string spriteId, CPosition position, int layer)
            : base(layer, position)
        {
            var sprite = Resources.GetSprite(spriteId);
            Sprite = new CSprite(sprite, position);
            Draw = DrawFunction;
        }

        public DrawSpriteComponent(string spriteId, CPosition position, Vector2 offset, int layer)
            : base(layer, position)
        {
            Sprite = new CSprite(spriteId, position, offset);
            Draw = DrawFunction;
        }

        public DrawSpriteComponent(Texture2D sprite, CPosition position, Rectangle sourceRectangle, Vector2 offset, int layer)
             : base(layer, position)
        {
            Sprite = new CSprite(sprite, position, sourceRectangle, offset);
            Draw = DrawFunction;
        }

        private void DrawFunction(SpriteBatch spriteBatch)
        {
            if (!IsActive)
                return;

            Sprite.Draw(spriteBatch);
        }
    }
}

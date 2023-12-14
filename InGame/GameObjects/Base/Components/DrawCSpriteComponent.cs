using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.CObjects;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    public class DrawCSpriteComponent : DrawComponent
    {
        public CSprite Sprite;

        public DrawCSpriteComponent(CSprite sprite, int layer)
            : base(layer, sprite.Position)
        {
            Sprite = sprite;
            Draw = DrawFunction;
        }

        public void DrawFunction(SpriteBatch spriteBatch)
        {
            if (!IsActive)
                return;

            Sprite.Draw(spriteBatch);
        }
    }
}

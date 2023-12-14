using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.CObjects;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class DrawShadowCSpriteComponent : DrawShadowComponent
    {
        public CSprite Sprite;
        public Color Color = Color.White;

        public float? Height;
        public float? Rotation;

        public DrawShadowCSpriteComponent(CSprite sprite)
        {
            Sprite = sprite;
            Draw = SpriteDrawFunction;
        }
        
        public void SpriteDrawFunction(SpriteBatch spriteBatch)
        {
            if (!IsActive)
                return;

            Sprite.DrawShadow(spriteBatch, Color, -1, Height ?? Owner.Map.ShadowHeight, Rotation ?? Owner.Map.ShadowRotation);
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class ShadowBodyDrawComponent : DrawShadowComponent
    {
        private readonly CPosition _position;
        private readonly Vector2 _offset = new Vector2(0, -1.5f);

        public int ShadowWidth = 8;
        public int ShadowHeight = 4;
        
        public float Transparency = 1;

        public ShadowBodyDrawComponent(CPosition position)
        {
            _position = position;
            Draw = SpriteDrawFunction;
        }

        public void SpriteDrawFunction(SpriteBatch spriteBatch)
        {
            if (!IsActive)
                return;

            // draw the shadow
            DrawHelper.DrawShadow(Resources.SprItem, new Vector2(_position.X - ShadowWidth / 2 + _offset.X, _position.Y - ShadowHeight / 2 + _offset.Y),
                new Rectangle(1, 218, 8, 4), ShadowWidth, ShadowHeight, false, 1, 0, Color.White * ((_position.Z + 10) / 20f) * Transparency);
        }
    }
}

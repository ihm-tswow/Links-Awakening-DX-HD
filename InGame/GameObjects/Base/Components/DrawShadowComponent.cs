using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class DrawShadowComponent : Component
    {
        public new static int Index = 6;
        public static int Mask = 0x01 << Index;

        public delegate void DrawTemplate(SpriteBatch spriteBatch);
        public DrawTemplate Draw;

        public bool IsActive = true;

        protected DrawShadowComponent() { }

        public DrawShadowComponent(DrawTemplate draw)
        {
            Draw = draw;
        }
    }
}

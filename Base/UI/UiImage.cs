using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.Base.UI
{
    public class UiImage : UiElement
    {
        public Texture2D SprImage;
        public Rectangle SourceRectangle;

        public UiImage(Texture2D sprImage, Rectangle drawRectangle, Rectangle sourceRectangle, string elementId, string screen, Color color, UiFunction update)
            : base(elementId, screen)
        {
            SprImage = sprImage;
            Rectangle = drawRectangle;
            SourceRectangle = sourceRectangle;
            BackgroundColor = color;
            UpdateFunction = update;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (SprImage != null)
                spriteBatch.Draw(SprImage, Rectangle, SourceRectangle, BackgroundColor);
        }
    }
}
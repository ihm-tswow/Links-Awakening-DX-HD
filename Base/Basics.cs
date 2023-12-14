using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.Base
{
    internal class Basics
    {
        public static void DrawStringCenter(SpriteFont font, string text, Rectangle position, Color color)
        {
            var textSize = font.MeasureString(text);
            var drawPosition = new Vector2(
                (int)(position.X + position.Width / 2 - textSize.X / 2), 
                (int)(position.Y + position.Height / 2 - textSize.Y / 2));
            Game1.SpriteBatch.DrawString(font, text, drawPosition, color);
        }
    }
}

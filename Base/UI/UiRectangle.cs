using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiRectangle : UiElement
    {
        public Color BlurColor;
        public float Radius = 0;

        public UiRectangle(Rectangle rectangle, string elementId, string screen, Color color, Color blurColor, UiFunction update)
            : base(elementId, screen)
        {
            Rectangle = rectangle;
            BackgroundColor = color;
            BlurColor = blurColor;
            UpdateFunction = update;
        }

        public override void DrawBlur(SpriteBatch spriteBatch)
        {
            Resources.RoundedCornerBlurEffect.Parameters["scale"].SetValue(Game1.UiScale);
            Resources.RoundedCornerBlurEffect.Parameters["blurColor"].SetValue(BlurColor.ToVector4());
            Resources.RoundedCornerBlurEffect.Parameters["radius"].SetValue(Radius);
            Resources.RoundedCornerBlurEffect.Parameters["width"].SetValue(Rectangle.Width / Game1.UiScale);
            Resources.RoundedCornerBlurEffect.Parameters["height"].SetValue(Rectangle.Height / Game1.UiScale);

            // draw the blur texture
            spriteBatch.Draw(Resources.SprWhite, Rectangle, BackgroundColor);
        }
    }
}
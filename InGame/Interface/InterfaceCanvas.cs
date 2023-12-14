using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.Interface
{
    public class InterfaceCanvas : InterfaceElement
    {
        public InterfaceElement InsideElement;

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, float scale, float transparency)
        {
            base.Draw(spriteBatch, drawPosition, scale, transparency);

            // draw the embedded element
            InsideElement?.Draw(spriteBatch, drawPosition, scale, transparency);
        }
    }
}
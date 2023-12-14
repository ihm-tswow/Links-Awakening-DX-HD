using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Interface
{
    public class InterfaceButton : InterfaceElement
    {
        public InterfaceElement InsideElement;

        public delegate void BFunction(InterfaceElement element);
        public BFunction ClickFunction;

        public InterfaceButton()
        {
            Selectable = true;

            Color = Values.MenuButtonColor;
            SelectionColor = Values.MenuButtonColorSelected;
        }

        public InterfaceButton(Point size, Point margin, InterfaceElement insideElement, BFunction clickFunction) : this()
        {
            Size = size;
            Margin = margin;
            InsideElement = insideElement;
            ClickFunction = clickFunction;
        }

        public InterfaceButton(Point size, Point margin, string text, BFunction clickFunction) : this()
        {
            Size = size;
            Margin = margin;

            var label = new InterfaceLabel(text, size, Point.Zero);

            InsideElement = label;
            ClickFunction = clickFunction;
        }

        public override void Select(Directions direction, bool animate)
        {
            InsideElement?.Select(direction, animate);

            base.Select(direction, animate);
        }

        public override void Deselect(bool animate)
        {
            InsideElement?.Deselect(animate);

            base.Deselect(animate);
        }

        public override InputEventReturn PressedButton(CButtons pressedButton)
        {
            if (pressedButton != CButtons.A)
                return InputEventReturn.Nothing;

            if (ClickFunction != null)
            {
                Game1.GameManager.PlaySoundEffect("D360-19-13");

                ClickFunction(this);
                return InputEventReturn.Something;
            }

            return InputEventReturn.Nothing;
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, float scale, float transparency)
        {
            base.Draw(spriteBatch, drawPosition, scale, transparency);

            // draw the embedded element
            InsideElement?.Draw(spriteBatch, drawPosition, scale, transparency);
        }
    }
}
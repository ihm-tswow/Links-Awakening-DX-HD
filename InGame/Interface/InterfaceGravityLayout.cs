using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.Interface
{
    public class InterfaceGravityLayout : InterfaceElement
    {
        public List<InterfaceElement> Elements = new List<InterfaceElement>();

        public InterfaceGravityLayout()
        {
            Selectable = true;
        }

        public override void Update()
        {
            base.Update();

            foreach (var element in Elements)
                element.Update();
        }

        public override void Deselect(bool animate)
        {
            foreach (var element in Elements)
                if (element.Selectable)
                    element.Deselect(animate);

            base.Deselect(animate);
        }

        public InterfaceElement AddElement(InterfaceElement element)
        {
            Recalculate = true;
            Elements.Add(element);
            return element;
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, float scale, float transparency)
        {
            if (Recalculate)
                CalculatePosition();

            base.Draw(spriteBatch, drawPosition, scale, transparency);

            // draw all elements that are inside the layout
            foreach (var element in Elements)
            {
                element.Draw(spriteBatch, element.Position.ToVector2() * scale + drawPosition, scale, transparency);
            }
        }

        public override void CalculatePosition()
        {
            Recalculate = false;

            var centerX = Size.X / 2;
            var centerY = Size.Y / 2;

            var left = 0;
            var right = Size.X;
            var top = 0;
            var down = Size.Y;

            foreach (var element in Elements)
            {
                if (element.ChangeUp)
                    element.CalculatePosition();

                var elementPosition = Point.Zero;

                if (element.Gravity == Gravities.Center)
                {
                    elementPosition = new Point(
                        centerX - element.Size.X / 2, centerY - element.Size.Y / 2);
                }
                else if (element.Gravity == Gravities.Left)
                {
                    elementPosition = new Point(
                        left + element.Margin.X, centerY - element.Size.Y / 2);

                    left += element.Size.X + element.Margin.X * 2;
                }
                else if (element.Gravity == Gravities.Right)
                {
                    elementPosition = new Point(
                        right - element.Size.X - element.Margin.X, centerY - element.Size.Y / 2);

                    left += element.Size.X + element.Margin.X * 2;
                }

                element.Position = elementPosition;
            }
        }

    }
}
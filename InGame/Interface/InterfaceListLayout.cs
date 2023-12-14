using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;

namespace ProjectZ.InGame.Interface
{
    public class InterfaceListLayout : InterfaceElement
    {
        public List<InterfaceElement> Elements = new List<InterfaceElement>();

        public Gravities ContentAlignment = Gravities.Center;

        public bool HorizontalMode;
        public bool AutoSize;
        public bool PreventSelection;

        private int _selectionIndex;
        private int _width;
        private int _height;

        public InterfaceListLayout() { }

        public override void Update()
        {
            base.Update();

            foreach (var element in Elements)
                element.Update();
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 drawPosition, float scale, float transparency)
        {
            // look for changes
            foreach (var element in Elements)
            {
                if (element.ChangeUp)
                {
                    Recalculate = true;
                    element.ChangeUp = false;
                }
            }

            // recalculate the position of the elements if needed
            if (Recalculate)
                CalculatePosition();

            base.Draw(spriteBatch, drawPosition, scale, transparency);

            // draw all the visible elements inside the layout
            foreach (var element in Elements)
            {
                if (element.Visible && !element.Hidden)
                    element.Draw(spriteBatch, element.Position.ToVector2() * scale + drawPosition, scale, transparency);
            }
        }

        public override InputEventReturn PressedButton(CButtons pressedButton)
        {
            var eValue = Elements[_selectionIndex].PressedButton(pressedButton);

            // return if the upper element reacted to the button press
            if (eValue != InputEventReturn.Nothing)
                return eValue;

            var direction = 0;

            if (HorizontalMode ? ControlHandler.MenuButtonPressed(CButtons.Left) : ControlHandler.MenuButtonPressed(CButtons.Up))
                direction = -1;

            if (HorizontalMode ? ControlHandler.MenuButtonPressed(CButtons.Right) : ControlHandler.MenuButtonPressed(CButtons.Down))
                direction = 1;

            if (direction == 0)
                return InputEventReturn.Nothing;

            // move selections
            Elements[_selectionIndex].Deselect(true);

            var rValue = InputEventReturn.Something;

            do
            {
                _selectionIndex += direction;

                if (_selectionIndex < 0)
                {
                    rValue = PreventSelection ? InputEventReturn.Something : InputEventReturn.Nothing;
                    _selectionIndex = Elements.Count - 1;
                }
                else if (_selectionIndex >= Elements.Count)
                {
                    rValue = PreventSelection ? InputEventReturn.Something : InputEventReturn.Nothing;
                    _selectionIndex = 0;
                }

            } while (!Elements[_selectionIndex].Selectable || !Elements[_selectionIndex].Visible);

            if (direction < 0)
                Elements[_selectionIndex].Select(HorizontalMode ? Directions.Right : Directions.Down, true);
            else
                Elements[_selectionIndex].Select(HorizontalMode ? Directions.Left : Directions.Top, true);

            Game1.GameManager.PlaySoundEffect("D360-10-0A");

            return rValue;
        }

        public override void Select(Directions direction, bool animate)
        {
            if (!Selectable)
                return;

            var dir = 1;

            if (!HorizontalMode && direction == Directions.Down ||
                HorizontalMode && direction == Directions.Right)
            {
                _selectionIndex = Elements.Count - 1;
                dir = -1;
            }
            else if (!HorizontalMode && direction == Directions.Top ||
                HorizontalMode && direction == Directions.Left)
            {
                _selectionIndex = 0;
                dir = 1;
            }

            // find a selectable item in the list
            while (!Elements[_selectionIndex].Selectable || !Elements[_selectionIndex].Visible)
                _selectionIndex += dir;

            Elements[_selectionIndex].Select(direction, animate);

            base.Select(direction, animate);
        }

        public void SetSelectionIndex(int index)
        {
            _selectionIndex = MathHelper.Clamp(index, 0, Elements.Count - 1);
        }

        public void Select(int index, bool animate)
        {
            _selectionIndex = index;
            Elements[_selectionIndex].Select(0, animate);
        }

        public override void Deselect(bool animate)
        {
            if (!Selectable)
                return;

            Elements[_selectionIndex].Deselect(animate);

            base.Deselect(animate);
        }

        public InterfaceElement AddElement(InterfaceElement element)
        {
            Recalculate = true;
            Elements.Add(element);
            return element;
        }

        public override void CalculatePosition()
        {
            Recalculate = false;

            _width = 0;
            _height = 0;

            // calculate the width
            foreach (var element in Elements)
            {
                if (element.Hidden)
                    continue;

                if (element.Recalculate)
                    element.CalculatePosition();

                if (HorizontalMode)
                {
                    _width += element.Size.X + element.Margin.X * 2;
                    _height = MathHelper.Max(element.Size.Y + element.Margin.Y * 2, _height);
                }
                else
                {
                    _width = MathHelper.Max(element.Size.X + element.Margin.X * 2, _width);
                    _height += element.Size.Y + element.Margin.Y * 2;
                }
            }

            // set the size of the layout
            if (AutoSize)
            {
                Size.X = _width;
                Size.Y = _height;
                ChangeUp = true;
            }

            var centerX = Size.X / 2;
            var centerY = Size.Y / 2;

            var currentX = centerX - _width / 2;
            var currentY = centerY - _height / 2;

            // align content left/right
            if ((ContentAlignment & Gravities.Left) != 0)
                currentX = 0;
            else if ((ContentAlignment & Gravities.Right) != 0)
                currentX = Size.X - _width;

            // align content top/bottom
            if ((ContentAlignment & Gravities.Top) != 0)
                currentY = 0;
            else if ((ContentAlignment & Gravities.Bottom) != 0)
                currentY = Size.Y - _height;

            foreach (var element in Elements)
            {
                if (element.Hidden)
                    continue;

                Point elementPosition;

                if (HorizontalMode)
                {
                    currentX += element.Margin.X;
                    elementPosition = new Point(currentX, centerY - element.Size.Y / 2);
                    currentX += element.Size.X + element.Margin.X;
                }
                else
                {
                    currentY += element.Margin.Y;
                    elementPosition = new Point(centerX - element.Size.X / 2, currentY);
                    currentY += element.Size.Y + element.Margin.Y;
                }

                element.Position = elementPosition;
            }
        }
    }
}
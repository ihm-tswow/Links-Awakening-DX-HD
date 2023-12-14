using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiEditList<T> : UiElement
    {
        public int SelectedEntry;

        private readonly List<T> _list;

        private const int Padding = 5;
        private const int ButtonDistance = 1;
        private const int ButtonHeight = 16;

        private int _currentSelection = -1;
        private int _startSelection = -1;

        private int _scrollPosition;

        private bool _isSwapping;

        public UiEditList(Rectangle rectangle, SpriteFont font, List<T> list, string elementId, string screen, UiFunction update)
            : base(elementId, screen)
        {
            Rectangle = rectangle;
            _list = list;

            UpdateFunction = update;

            Font = font;
        }

        public override void Update()
        {
            var listEntrySize = (ButtonHeight + ButtonDistance);

            _currentSelection = -1;
            if (InputHandler.MouseIntersect(Rectangle))
            {
                // scroll through the list
                {
                    if (InputHandler.MouseWheelUp())
                        _scrollPosition--;
                    if (InputHandler.MouseWheelDown())
                        _scrollPosition++;

                    var maxVisibleEntries = Rectangle.Height / listEntrySize;
                    var maxScrollPosition = maxVisibleEntries >= _list.Count ? 0 : _list.Count - maxVisibleEntries;

                    _scrollPosition = Math.Clamp(_scrollPosition, 0, maxScrollPosition);
                }

                _currentSelection = (InputHandler.MouseState.Y - Rectangle.Y + _scrollPosition * listEntrySize) / listEntrySize;

                if (_currentSelection < _list.Count && InputHandler.MouseLeftStart())
                {
                    _isSwapping = true;
                    SelectedEntry = _currentSelection;
                    _startSelection = _currentSelection;
                }

                if (_isSwapping && InputHandler.MouseLeftDown())
                {
                    // swap entries?
                    if (_startSelection != _currentSelection && _currentSelection < _list.Count)
                    {
                        var copy = _list[_startSelection];
                        _list[_startSelection] = _list[_currentSelection];
                        _list[_currentSelection] = copy;

                        _startSelection = _currentSelection;
                        SelectedEntry = _currentSelection;
                    }
                }
            }

            if (!InputHandler.MouseLeftDown())
                _isSwapping = false;

            base.Update();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var posY = Rectangle.Y;
            for (var i = _scrollPosition; i < _list.Count; i++)
            {
                var marked = _currentSelection == i || SelectedEntry == i;

                // draw the background
                var buttonRectangle = new Rectangle(Rectangle.X + Padding, posY + Padding, Rectangle.Width - Padding * 2, ButtonHeight);
                spriteBatch.Draw(Resources.SprWhite, buttonRectangle, marked ? FontColor : BackgroundColor);

                // draw the text
                var text = _list[i].ToString();
                var labelSize = Font.MeasureString(text);
                var textPosition = new Vector2((int)(Rectangle.X + Rectangle.Width / 2 - labelSize.X / 2), (int)(posY + ButtonDistance + ButtonHeight / 2 - labelSize.Y / 2 + 2));
                spriteBatch.DrawString(Font, text, textPosition, marked ? BackgroundColor : FontColor);

                posY += ButtonHeight + ButtonDistance;
            }

            // draw the scrollbar
            if(_list.Count > 0)
            {
                var listEntrySize = (ButtonHeight + ButtonDistance);
                var maxVisibleEntries = Rectangle.Height / listEntrySize;
                var maxScrollPosition = maxVisibleEntries >= _list.Count ? 0 : _list.Count - maxVisibleEntries;
                var scrollBarHeight = (int)((maxVisibleEntries / (float)_list.Count) * Rectangle.Height);

                if (maxScrollPosition > 0)
                {
                    // draw the bar background
                    spriteBatch.Draw(Resources.SprWhite,
                        new Rectangle(Rectangle.Right - Padding + 1, Rectangle.Y, Padding - 2, Rectangle.Height), Values.ColorBackgroundLight);

                    // draw the bar
                    spriteBatch.Draw(Resources.SprWhite,
                        new Rectangle(Rectangle.Right - Padding + 1, 
                            Rectangle.Y + (int)((_scrollPosition / (float)maxScrollPosition) * (Rectangle.Height - scrollBarHeight)), Padding - 2, scrollBarHeight), BackgroundColor);
                }
            }
        }
    }
}
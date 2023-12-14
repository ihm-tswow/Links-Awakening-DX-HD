using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay
{
    // TODO: add button hints and maybe dialog box with the original text?
    class PhotoOverlay
    {
        private DictAtlasEntry _spriteBook;
        private DictAtlasEntry _spriteCursor;
        private DictAtlasEntry _spriteNop;
        private DictAtlasEntry _spriteOk;

        private DictAtlasEntry[] _spritePhotos = new DictAtlasEntry[12];
        private bool[] _unlockState = new bool[12];

        private int _cursorIndex;

        private float _transitionValue;
        private float _transitionCounter;
        private const float TransitionTimeOpen = 125;
        private const float TransitionTimeClose = 125;
        private bool _isShowingImage;

        private float _cursorState;
        private float _cursorCounter;
        private float _cursorTime = 200f;
        private bool _cursorPressed;

        public void Load()
        {
            _spriteBook = Resources.GetSprite("photo_book");
            _spriteCursor = Resources.GetSprite("photo_cursor");
            _spriteNop = Resources.GetSprite("photo_no");
            _spriteOk = Resources.GetSprite("photo_ok");

            for (var i = 0; i < 12; i++)
                _spritePhotos[i] = Resources.GetSprite("photo_" + (i + 1));
        }

        public void OnOpen()
        {
            // check the state of the discovered photos
            _isShowingImage = false;
            _transitionCounter = 0;
            _transitionValue = 0;

            for (var i = 0; i < 12; i++)
                _unlockState[i] = !string.IsNullOrEmpty(Game1.GameManager.SaveManager.GetString("photo_" + (i + 1)));

            // set to alt image or not?
            var altPhoto = Game1.GameManager.SaveManager.GetString("photo_1_alt");
            var useAltPhoto = !string.IsNullOrEmpty(altPhoto);
            _spritePhotos[0] = Resources.GetSprite(useAltPhoto ? "photo_1_alt" : "photo_1");
        }

        public void Update()
        {
            // convert the index into a 2d position
            var cursorPoint = CursorPosition(_cursorIndex);

            if (!_isShowingImage)
            {
                if (ControlHandler.ButtonPressed(CButtons.A))
                {
                    _cursorPressed = true;

                    // very important if the player is spamming the a button to have a nice animation
                    if (_cursorCounter > _cursorTime / 2)
                        _cursorCounter = _cursorTime - _cursorCounter;

                    if (_unlockState[_cursorIndex])
                        _isShowingImage = true;
                }
                else
                {
                    if (ControlHandler.ButtonPressed(CButtons.Left))
                        cursorPoint.X--;
                    if (ControlHandler.ButtonPressed(CButtons.Right))
                        cursorPoint.X++;
                    if (ControlHandler.ButtonPressed(CButtons.Up))
                        cursorPoint.Y--;
                    if (ControlHandler.ButtonPressed(CButtons.Down))
                        cursorPoint.Y++;

                    if (cursorPoint.X < 0)
                        cursorPoint.X += 4;
                    if (cursorPoint.X > 3)
                        cursorPoint.X -= 4;
                    if (cursorPoint.Y < 0)
                        cursorPoint.Y += 3;
                    if (cursorPoint.Y > 2)
                        cursorPoint.Y -= 3;
                }

                // close the page
                if (ControlHandler.ButtonPressed(CButtons.B))
                    Game1.GameManager.InGameOverlay.CloseOverlay();
            }
            else
            {
                if (ControlHandler.ButtonPressed(CButtons.B))
                {
                    _isShowingImage = false;
                    _transitionCounter = TransitionTimeClose;
                }
            }

            if (_isShowingImage && _transitionCounter < TransitionTimeOpen)
            {
                _transitionCounter += Game1.DeltaTime;
                if (_transitionCounter > TransitionTimeOpen)
                    _transitionCounter = TransitionTimeOpen;

                _transitionValue = Math.Clamp(_transitionCounter / TransitionTimeOpen, 0, 1);
            }
            else if (!_isShowingImage && _transitionCounter > 0)
            {
                _transitionCounter -= Game1.DeltaTime;
                if (_transitionCounter < 0)
                    _transitionCounter = 0;

                _transitionValue = _transitionCounter / TransitionTimeClose;
                _cursorState = MathF.Sin(_transitionValue * MathF.PI * 0.5f);
            }

            // cursor animation
            if (_cursorPressed)
            {
                _cursorCounter += Game1.DeltaTime;
                if (_cursorCounter >= _cursorTime)
                {
                    _cursorCounter = 0;
                    _cursorPressed = false;
                }

                _cursorState = MathF.Sin(_cursorCounter / _cursorTime * MathF.PI);
            }

            // converts back into index space
            _cursorIndex = CursorIndex(cursorPoint);

        }

        private Point CursorPosition(int index)
        {
            return new Point(index % 2 + (index / 6) * 2, (index % 6) / 2);
        }

        private int CursorIndex(Point position)
        {
            return position.X % 2 + position.X / 2 * 6 + position.Y * 2;
        }

        public void Draw(SpriteBatch spriteBatch, float transparency)
        {
            // draw the book
            var bookPosition = new Vector2(
                Game1.WindowWidth / 2 - (_spriteBook.SourceRectangle.Width * Game1.UiScale) / 2,
                Game1.WindowHeight / 2 - (_spriteBook.SourceRectangle.Height * Game1.UiScale) / 2);
            spriteBatch.Draw(_spriteBook.Texture, bookPosition, _spriteBook.SourceRectangle,
                Color.White * transparency, 0, Vector2.Zero, new Vector2(Game1.UiScale), SpriteEffects.None, 0);

            // draw the images
            for (var i = 0; i < 12; i++)
            {
                var imageSprite = _unlockState[i] ? _spriteOk : _spriteNop;
                var position = bookPosition +
                               new Vector2(27 + (i % 2) * 32 + (i / 6) * 88, 19 + ((i % 6) / 2) * 32) * Game1.UiScale -
                               new Vector2(imageSprite.SourceRectangle.Width / 2, 0) * Game1.UiScale;
                spriteBatch.Draw(imageSprite.Texture, position, imageSprite.SourceRectangle,
                    Color.White * transparency, 0, Vector2.Zero, new Vector2(Game1.UiScale), SpriteEffects.None, 0);
            }

            // draw the cursor
            var cursorPosition = bookPosition +
                           new Vector2(12 + (_cursorIndex % 2) * 32 + (_cursorIndex / 6) * 88, 8 + ((_cursorIndex % 6) / 2) * 32) * Game1.UiScale +
                           new Vector2(21, 21) * Game1.UiScale -
                           new Vector2(2, 2) * Game1.UiScale * _cursorState;
            spriteBatch.Draw(_spriteCursor.Texture, cursorPosition, _spriteCursor.SourceRectangle,
                Color.White * transparency, 0, Vector2.Zero, new Vector2(Game1.UiScale), SpriteEffects.None, 0);

            // draw the selected image
            if (_transitionValue != 0)
            {
                var pictureStartPosition = bookPosition + new Vector2(27 + (_cursorIndex % 2) * 32 + (_cursorIndex / 6) * 88, 27 + ((_cursorIndex % 6) / 2) * 32) * Game1.UiScale;
                var picturePosition = Vector2.Lerp(pictureStartPosition, new Vector2(Game1.WindowWidth / 2, Game1.WindowHeight / 2), _transitionValue);

                spriteBatch.Draw(_spritePhotos[_cursorIndex].Texture, picturePosition, _spritePhotos[_cursorIndex].SourceRectangle,
                    Color.White * transparency * _transitionValue, 0,
                    new Vector2(_spritePhotos[_cursorIndex].SourceRectangle.Width / 2, _spritePhotos[_cursorIndex].SourceRectangle.Height / 2f),
                    new Vector2(Game1.UiScale * (0.1f + _transitionValue * 0.9f)), SpriteEffects.None, 0);
            }
        }
    }
}

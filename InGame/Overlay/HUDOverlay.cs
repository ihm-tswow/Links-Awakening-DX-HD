using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base.UI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay
{
    public class HudOverlay
    {
        private readonly ItemSlotOverlay _itemSlotOverlay = new ItemSlotOverlay();

        private readonly UiRectangle _heartBackground;
        private readonly UiRectangle _rubeeBackground;

        private readonly DictAtlasEntry _saveIcon;

        private Rectangle _gameUiWindow;

        private Point _heartPosition;
        private Point _rubeePosition;
        private Vector2 _saveIconPosition;

        private const int FadeOffsetBackground = 10;
        private const int FadeOffset = 13;

        private const int SaveIconTime = 1000;
        private float _saveIconTransparency;
        private float _saveIconCounter;

        public HudOverlay()
        {
            _heartBackground = new UiRectangle(Rectangle.Empty, "heart", Values.ScreenNameGame, Values.OverlayBackgroundColor, Values.OverlayBackgroundBlurColor, null) { Radius = Values.UiBackgroundRadius };
            Game1.EditorUi.AddElement(_heartBackground);

            _rubeeBackground = new UiRectangle(Rectangle.Empty, "rubee", Values.ScreenNameGame, Values.OverlayBackgroundColor, Values.OverlayBackgroundBlurColor, null) { Radius = Values.UiBackgroundRadius };
            Game1.EditorUi.AddElement(_rubeeBackground);

            _saveIcon = Resources.GetSprite("save_icon");
        }

        public void Update(float fadePercentage, float transparency)
        {
            _saveIconCounter -= Game1.DeltaTime;
            if (_saveIconCounter < 0)
                _saveIconCounter = 0;
            _saveIconTransparency = Math.Min(Math.Clamp(_saveIconCounter / 100, 0, 1), Math.Clamp((SaveIconTime - _saveIconCounter) / 100, 0, 1));

            // TODO_Opt: maybe add settings for wide screen positioning
            var scale = Math.Min(Game1.WindowWidth / (float)Values.MinWidth, Game1.WindowHeight / (float)Values.MinHeight);

            // not so gud
            _gameUiWindow.Width = (int)(Values.MinWidth * scale);
            _gameUiWindow.Height = (int)(Values.MinHeight * scale);

            var ar = MathHelper.Clamp(Game1.WindowWidth / (float)Game1.WindowHeight, 1, 2);

            _gameUiWindow.Width = MathHelper.Clamp((int)(Game1.WindowHeight * ar), 0, Game1.WindowWidth);
            _gameUiWindow.Height = MathHelper.Clamp((int)(Game1.WindowWidth / ar), 0, Game1.WindowHeight);
            _gameUiWindow.X = Game1.WindowWidth / 2 - _gameUiWindow.Width / 2;
            _gameUiWindow.Y = Game1.WindowHeight / 2 - _gameUiWindow.Height / 2;

            // top left
            _heartPosition = new Point(_gameUiWindow.X + 16 * Game1.UiScale, _gameUiWindow.Y + 16 * Game1.UiScale);
            _heartBackground.Rectangle = ItemDrawHelper.GetHeartRectangle(_heartPosition, Game1.UiScale);
            _heartBackground.Rectangle.X -= (int)(fadePercentage * FadeOffsetBackground * Game1.UiScale);
            _heartBackground.BackgroundColor = Values.OverlayBackgroundColor * transparency;
            _heartBackground.BlurColor = Values.OverlayBackgroundBlurColor * transparency;

            // top right
            _rubeePosition = new Point(
                _gameUiWindow.X + _gameUiWindow.Width - ItemDrawHelper.RubeeSize.X * Game1.UiScale - 16 * Game1.UiScale,
                _gameUiWindow.Y + 16 * Game1.UiScale);
            _rubeeBackground.Rectangle = ItemDrawHelper.GetRubeeRectangle(new Point(_rubeePosition.X, _rubeePosition.Y), Game1.UiScale);
            _rubeeBackground.Rectangle.X += (int)(fadePercentage * FadeOffsetBackground * Game1.UiScale);
            _rubeeBackground.BackgroundColor = Values.OverlayBackgroundColor * transparency;
            _rubeeBackground.BlurColor = Values.OverlayBackgroundBlurColor * transparency;

            // bottom left
            _itemSlotOverlay.UpdatePositions(_gameUiWindow, new Point(-(int)(fadePercentage * FadeOffsetBackground * Game1.UiScale), 0), Game1.UiScale);
            _itemSlotOverlay.SetTransparency(transparency);

            // bottom right
            _saveIconPosition = new Vector2(
                _gameUiWindow.X + _gameUiWindow.Width - _saveIcon.SourceRectangle.Width * Game1.UiScale - 16 * Game1.UiScale,
                _gameUiWindow.Y + _gameUiWindow.Height - _saveIcon.SourceRectangle.Height * Game1.UiScale - 16 * Game1.UiScale);
        }

        public void DrawTop(SpriteBatch spriteBatch, float fadePercentage, float transparency)
        {
            // draw the item slots
            ItemSlotOverlay.Draw(spriteBatch, _itemSlotOverlay.ItemSlotPosition - new Point((int)(fadePercentage * FadeOffset * Game1.UiScale), 0), Game1.UiScale, transparency);

            //DrawHelper.DrawSmallKeys(spriteBatch, _keyPosition, Game1.UiScale, Color.White * transparency);

            // draw the rubees
            ItemDrawHelper.DrawRubee(spriteBatch, _rubeePosition + new Point((int)(fadePercentage * FadeOffset * Game1.UiScale), 0), Game1.UiScale, Color.Black * transparency);

            // draw the heart position
            ItemDrawHelper.DrawHearts(spriteBatch, _heartPosition - new Point((int)(fadePercentage * FadeOffset * Game1.UiScale), 0), Game1.UiScale, Color.White * transparency);
        }

        public void DrawBlur(SpriteBatch spriteBatch)
        {
            // draw the save icon
            Resources.RoundedCornerBlurEffect.Parameters["blurColor"].SetValue((Values.OverlayBackgroundBlurColor * _saveIconTransparency).ToVector4());
            DrawHelper.DrawNormalized(spriteBatch, _saveIcon.Texture, _saveIconPosition, _saveIcon.ScaledRectangle, Values.OverlayBackgroundColor * _saveIconTransparency, Game1.UiScale);
        }

        public void ShowSaveIcon()
        {
            _saveIconCounter = SaveIconTime;
        }
    }
}

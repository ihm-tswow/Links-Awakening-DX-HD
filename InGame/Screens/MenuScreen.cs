using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Pages;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Screens
{
    public class MenuScreen : Screen
    {
        private Matrix _animationMatrix => Game1.GetMatrix * Matrix.CreateScale(Game1.UiScale);
        private Animator _linkAnimation = new Animator();
        private Texture2D _sprBackground;
        private Rectangle _menuRectangle;

        private Vector2 _linkPosition;
        private bool _linkVisible;

        private int _scale = 3;
        private int _menuWidth;
        private int _menuHeight;
        private int _backgroundWidth;

        private int _leftBar;
        private int _rightBar;
        private int _topBar;
        private int _bottomBar;
        private int _posX;

        public MenuScreen(string screenId) : base(screenId) { }

        public override void Load(ContentManager content)
        {
            _sprBackground = content.Load<Texture2D>("Menu/menuBackground");

            _linkAnimation = AnimatorSaveLoad.LoadAnimator("menu_link");
            _linkAnimation.Play("idle");

            _menuWidth = Values.MinWidth - 32;
            _menuHeight = Values.MinHeight - 32;
        }

        public override void OnLoad()
        {
            Game1.UiPageManager.ClearStack();
            Game1.UiPageManager.ChangePage(typeof(MainMenuPage), null, PageManager.TransitionAnimation.TopToBottom, PageManager.TransitionAnimation.TopToBottom);

            Game1.GameManager.ResetMusic();
            Game1.GameManager.SetMusic(16, 0);

            Game1.GbsPlayer.SetVolumeMultiplier(1.0f);
            Game1.GbsPlayer.Play();
        }

        public override void Update(GameTime gameTime)
        {
            _scale = Game1.UiScale;

            if (_scale <= 0)
                _scale = 1;

            _backgroundWidth = (int)Math.Ceiling(Game1.WindowWidth / (double)(32 * _scale) + 1) * 32 * _scale;

            _menuRectangle = new Rectangle(
                Game1.WindowWidth / 2 - _menuWidth * _scale / 2,
                Game1.WindowHeight / 2 - _menuHeight * _scale / 2, _menuWidth * _scale, _menuHeight * _scale);

            _menuRectangle.X = _menuRectangle.X / _scale * _scale;
            _menuRectangle.Y = _menuRectangle.Y / _scale * _scale;

            _topBar = (int)Math.Ceiling((Game1.WindowHeight / 2 - _menuHeight * _scale / 2) / (float)_scale / _sprBackground.Height) * _sprBackground.Height;
            _bottomBar = (int)Math.Ceiling((Game1.WindowHeight / 2 - _menuHeight * _scale / 2) / (float)_scale / _sprBackground.Height) * _sprBackground.Height;

            _posX = (int)Math.Ceiling(_menuRectangle.X / (float)_scale / 32) * 32 - _menuRectangle.X / _scale;

            _leftBar = (int)Math.Ceiling((Game1.WindowWidth / 2 - _menuWidth * _scale / 2) / (float)_scale / _sprBackground.Width) * _sprBackground.Width;
            _rightBar = _leftBar;

            {
                // update the animation
                _linkAnimation.Update();

                _linkVisible = false;
                var mainMenuPage = (MainMenuPage)Game1.UiPageManager.GetPage(typeof(MainMenuPage));

                if (Game1.UiPageManager.PageStack.Count == 1)
                    foreach (var saveButton in mainMenuPage.SaveEntries)
                    {
                        if (saveButton.Selected)
                        {
                            _linkVisible = true;
                            _linkPosition = new Vector2(saveButton.Position.X + 22, saveButton.Position.Y + 22);
                        }
                    }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, Game1.GetMatrix);

            // draw the black background
            spriteBatch.Draw(Resources.SprWhite, _menuRectangle, Color.Black);

            // input helper
            {
                var backStr = "";
                if (ControlHandler.LastKeyboardDown && ControlHandler.ButtonDictionary[CButtons.B].Keys.Length > 0)
                    backStr = ControlHandler.ButtonDictionary[CButtons.B].Keys[0].ToString();
                if (!ControlHandler.LastKeyboardDown && ControlHandler.ButtonDictionary[CButtons.B].Buttons.Length > 0)
                    backStr = ControlHandler.ButtonDictionary[CButtons.B].Buttons[0].ToString();
                var backHelp = backStr + " Back";

                var backTextSize = Resources.GameFont.MeasureString(backHelp);
                spriteBatch.DrawString(Resources.GameFont, backHelp,
                    new Vector2(_menuRectangle.X + 2 * _scale, _menuRectangle.Bottom - backTextSize.Y * _scale), Color.White, 0, Vector2.Zero, _scale, SpriteEffects.None, 0);
            }

            {
                var selectStr = "";
                if (ControlHandler.LastKeyboardDown && ControlHandler.ButtonDictionary[CButtons.A].Keys.Length > 0)
                    selectStr = ControlHandler.ButtonDictionary[CButtons.A].Keys[0].ToString();
                if (!ControlHandler.LastKeyboardDown && ControlHandler.ButtonDictionary[CButtons.A].Buttons.Length > 0)
                    selectStr = ControlHandler.ButtonDictionary[CButtons.A].Buttons[0].ToString();
                var inputHelper = selectStr + " Select";

                var selectTextSize = Resources.GameFont.MeasureString(inputHelper);
                spriteBatch.DrawString(Resources.GameFont, inputHelper,
                    new Vector2(_menuRectangle.Right - (selectTextSize.X + 2) * _scale, _menuRectangle.Bottom - 9 * _scale), Color.White, 0, Vector2.Zero, _scale, SpriteEffects.None, 0);
            }

            spriteBatch.End();
        }

        public override void DrawTop(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, Game1.GetMatrix);

            // top
            spriteBatch.Draw(_sprBackground, new Rectangle(
                    -_posX * _scale, _menuRectangle.Y - _topBar * _scale, _backgroundWidth, _topBar * _scale),
                new Rectangle(0, 0, _backgroundWidth / _scale, _topBar), Color.White);
            // bottom
            spriteBatch.Draw(_sprBackground, new Rectangle(
                    -_posX * _scale, _menuRectangle.Bottom, _backgroundWidth, _bottomBar * _scale),
                new Rectangle(0, 0, _backgroundWidth / _scale, _bottomBar), Color.White);

            // left
            spriteBatch.Draw(_sprBackground, new Rectangle(
                    _menuRectangle.X - _leftBar * _scale, _menuRectangle.Y, _leftBar * _scale, _menuHeight * _scale),
                new Rectangle(0, 0, _leftBar, _menuHeight), Color.White);
            // right
            spriteBatch.Draw(_sprBackground, new Rectangle(
                    _menuRectangle.Right, _menuRectangle.Y, _rightBar * _scale, _menuHeight * _scale),
                new Rectangle(0, 0, _rightBar, _menuHeight), Color.White);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, _animationMatrix);

            //if (_linkVisible)
            //    _linkAnimation.Draw(spriteBatch, new Vector2(
            //        _menuRectangle.X / _scale + _linkPosition.X + 8, _menuRectangle.Y / _scale + _linkPosition.Y + 32), Color.White);

            spriteBatch.End();
            spriteBatch.Begin();
        }
    }
}
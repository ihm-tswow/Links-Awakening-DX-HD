using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    public class PageManager
    {
        public enum TransitionAnimation
        {
            Fade,
            LeftToRight,
            RightToLeft,
            TopToBottom,
            BottomToTop
        }

        public Dictionary<Type, InterfacePage> InsideElement = new Dictionary<Type, InterfacePage>();
        public List<Type> PageStack = new List<Type>();

        private TransitionAnimation _transitionOutAnimation;
        private TransitionAnimation _transitionInAnimation;

        private Vector2 _menuPosition;

        private double _transitionCount;

        private float _transitionState;

        private int _width;
        private int _height;
        private int _currentPage;
        private int _nextPage;
        private int _transitionTime;
        private int _transitionDirection;

        private const int TransitionFade = 125;
        private const int TransitionNormal = 200;

        private bool _isTransitioning;

        public void Load()
        {
            _width = Values.MinWidth - 32;
            _height = Values.MinHeight - 32;

            AddPage(new MainMenuPage(_width, _height));
            AddPage(new CopyPage(_width, _height));
            AddPage(new CopyConfirmationPage(_width, _height));
            AddPage(new DeleteSaveSlotPage(_width, _height));
            AddPage(new NewGamePage(_width, _height));
            AddPage(new SettingsPage(_width, _height));
            AddPage(new GameSettingsPage(_width, _height));
            //AddPage(new AudioSettingsPage(_width, _height));
            AddPage(new ControlSettingsPage(_width, _height));
            AddPage(new GraphicSettingsPage(_width, _height));
            AddPage(new GameMenuPage(_width, _height));
            AddPage(new ExitGamePage(_width, _height));
            AddPage(new GameOverPage(_width, _height));
            AddPage(new QuitGamePage(_width, _height));
        }

        public virtual void Update(GameTime gameTime)
        {
            // not a good place
            _menuPosition = new Vector2(
                (Game1.WindowWidth / 2 - _width * Game1.UiScale / 2) / Game1.UiScale * Game1.UiScale,
                (Game1.WindowHeight / 2 - _height * Game1.UiScale / 2) / Game1.UiScale * Game1.UiScale);

            if (_isTransitioning)
            {
                _transitionCount += Game1.DeltaTime;

                if (_transitionCount >= _transitionTime)
                {
                    _transitionCount = 0;
                    _isTransitioning = false;

                    // remove the old page after finishing the transition
                    if (_transitionDirection == 1)
                    {
                        if (PageStack.Count > 0)
                            PageStack.RemoveAt(0);
                    }

                    _currentPage = 0;
                }
            }

            if (!_isTransitioning && PageStack.Count > _currentPage)
                InsideElement[PageStack[_currentPage]].Update(ControlHandler.GetPressedButtons(), gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _transitionState = (float)(Math.Sin(_transitionCount / _transitionTime * Math.PI - Math.PI / 2) + 1) / 2f;

            // draw the current page
            if (PageStack.Count > _currentPage)
            {
                var directionX =
                    _transitionOutAnimation == TransitionAnimation.RightToLeft ? _transitionDirection :
                    _transitionOutAnimation == TransitionAnimation.LeftToRight ? -_transitionDirection : 0;
                var directionY =
                    _transitionOutAnimation == TransitionAnimation.TopToBottom ? -_transitionDirection :
                    _transitionOutAnimation == TransitionAnimation.BottomToTop ? _transitionDirection : 0;
                var transitionOffset = new Vector2(
                    _width * 0.65f * _transitionState * directionX * Game1.UiScale,
                    _height * 0.65f * _transitionState * directionY * Game1.UiScale);

                InsideElement[PageStack[_currentPage]].Draw(spriteBatch,
                    _menuPosition + transitionOffset, Game1.UiScale, 1 - _transitionState);
            }

            if (!_isTransitioning || PageStack.Count <= _nextPage)
                return;

            // draw the next page while transitioning
            var directionXNext =
                _transitionInAnimation == TransitionAnimation.RightToLeft ? -_transitionDirection :
                _transitionInAnimation == TransitionAnimation.LeftToRight ? _transitionDirection : 0;
            var directionYNext =
                _transitionInAnimation == TransitionAnimation.TopToBottom ? _transitionDirection :
                _transitionInAnimation == TransitionAnimation.BottomToTop ? -_transitionDirection : 0;
            var transitionOffsetNext = new Vector2(
                _width * 0.65f * (1 - _transitionState) * directionXNext * Game1.UiScale,
                _height * 0.65f * (1 - _transitionState) * directionYNext * Game1.UiScale);

            InsideElement[PageStack[_nextPage]].Draw(spriteBatch,
                _menuPosition + transitionOffsetNext, Game1.UiScale, _transitionState);
        }

        private void AddPage(InterfacePage element)
        {
            InsideElement.Add(element.GetType(), element);
        }

        public bool ChangePage(Type nextPage, Dictionary<string, object> intent, TransitionAnimation animationIn = TransitionAnimation.RightToLeft, TransitionAnimation animationOut = TransitionAnimation.RightToLeft)
        {
            // do not add the page/restart the animation if it is transitioning out of the page
            if (!_isTransitioning || PageStack.Count <= 0 || nextPage != PageStack[0])
            {
                PageStack.Insert(0, nextPage);

                _transitionCount = 0;
                _transitionState = 0;
            }
            else
            {
                _transitionCount = _transitionTime - _transitionCount;
            }

            _isTransitioning = true;
            _transitionDirection = -1;

            _currentPage = 1;
            _nextPage = 0;

            // onload
            InsideElement[nextPage].OnLoad(intent);

            _transitionInAnimation = animationIn;
            _transitionOutAnimation = animationOut;

            // @HACK
            _transitionTime = _transitionInAnimation == TransitionAnimation.Fade ? TransitionFade : TransitionNormal;

            return true;
        }

        public InterfacePage GetPage(Type pageType)
        {
            return InsideElement[pageType];
        }

        public InterfacePage GetCurrentPage()
        {
            if (PageStack.Count <= 0)
                return null;

            return InsideElement[PageStack[0]];
        }

        public bool ChangePage(Type nextPage)
        {
            return ChangePage(nextPage, null);
        }

        public void PopPage(Dictionary<string, object> intent = null, TransitionAnimation animationIn = TransitionAnimation.RightToLeft, TransitionAnimation animationOut = TransitionAnimation.RightToLeft)
        {
            if (PageStack.Count <= 0)
                return;

            if (PageStack.Count > 0)
                InsideElement[PageStack[0]].OnPop(intent);

            if (!_isTransitioning)
            {
                _transitionCount = 0;
                _isTransitioning = true;
            }
            else
            {
                PageStack.RemoveAt(0);
                _transitionCount = _transitionTime - _transitionCount;
            }

            _transitionDirection = 1;

            _currentPage = 0;
            _nextPage = 1;

            // onload
            if (PageStack.Count > 1)
                InsideElement[PageStack[1]].OnReturn(intent);

            _transitionInAnimation = animationIn;
            _transitionOutAnimation = animationOut;

            // @HACK
            _transitionTime = _transitionInAnimation == TransitionAnimation.Fade ? TransitionFade : TransitionNormal;
        }

        public void PopAllPages(TransitionAnimation animationIn = TransitionAnimation.RightToLeft, TransitionAnimation animationOut = TransitionAnimation.RightToLeft)
        {
            PopPage(null, animationIn, animationOut);

            // remove everything but the current page
            if (PageStack.Count > 1)
            {
                for (var i = 0; i < PageStack.Count; i++)
                    InsideElement[PageStack[i]].OnPop(null);

                PageStack.RemoveRange(1, PageStack.Count - 1);
            }
        }

        public void ClearStack()
        {
            PageStack.Clear();
        }
    }
}

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
#if WINDOWS
using System.Windows.Forms;
#endif

namespace ProjectZ.Editor
{
    internal class AnimationScreen : InGame.Screens.Screen
    {
        private readonly EditorCamera _camera = new EditorCamera();

        private Animator _animator = new Animator();
        private Animator _overlayAnimator;
        private Texture2D _sprAnimator;

        private Point _currentPosition, _selectionStart, _selectionEnd;
        private UiNumberInput _animationInput, _frameInput, _numberInputX, _numberInputY, _numberInputWidth, _numberInputHeight;
        private UiNumberInput _niOffsetX, _niOffsetY, _niAnOffsetX, _niAnOffsetY, _fpsInput, _loopCountInput;
        private UiTextInput _animationName, _nextAnimationName;
        private UiLabel _animationUiLabel, _frameUiLabel;
        private UiCheckBox _checkBoxLoop, _cbFrameMirroredV, _cbFrameMirroredH;

        private string _sprPath;
        private string _lastFileName;
        private const float MinScale = 1;
        private const float MaxScale = 10;
        private const int LeftBarWidth = 200;
        private const int RightBarWidth = 300;
        private const int TileSize = 2;
        private int _selectedAnimation, _selectedFrame;
        private bool _selecting;
        private bool _collisionRectangleMode;
        private bool _isPlaying;
        private bool _showAllSelections;

        public AnimationScreen(string screenId) : base(screenId) { }

        public override void Load(ContentManager content)
        {
            var buttonDist = 5;
            var buttonWidth = LeftBarWidth - buttonDist * 2;
            var buttonWidthHalf = (LeftBarWidth - buttonDist * 3) / 2;
            var buttonHeight = 30;
            var labelHeight = Resources.EditorFontHeight;
            var buttonQWidth = LeftBarWidth - 15 - buttonHeight;
            var posY = Values.ToolBarHeight + buttonDist;

            var screenId = Values.EditorUiAnimation;

            Game1.EditorUi.AddElement(new UiRectangle(new Rectangle(0, 0, 0, 0), "leftBackground", screenId, Values.ColorBackgroundLight, Color.White,
                ui => { ui.Rectangle = new Rectangle(0, Values.ToolBarHeight, LeftBarWidth, Game1.WindowHeight - Values.ToolBarHeight); }));

            Game1.EditorUi.AddElement(new UiRectangle(new Rectangle(0, 0, 0, 0), "rightBackground", screenId, Values.ColorBackgroundLight, Color.White,
                ui => { ui.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth, Values.ToolBarHeight, RightBarWidth, Game1.WindowHeight - Values.ToolBarHeight - RightBarWidth); }));
            // animation background
            Game1.EditorUi.AddElement(new UiRectangle(new Rectangle(0, 0, 0, 0), "rightAnimationBackground", screenId, Color.White * 0.5f, Color.White,
                ui => { ui.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth, Game1.WindowHeight - RightBarWidth, RightBarWidth, RightBarWidth); }));

            // right toolbar
            Game1.EditorUi.AddElement(new UiButton(Rectangle.Empty, Resources.EditorFont, "load overlay", "bt1", screenId,
                ui => { ui.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth + 5, Values.ToolBarHeight + buttonDist, buttonWidth, buttonHeight); },
                ui => { LoadOverlayAnimation(); }));

            Game1.EditorUi.AddElement(new UiButton(Rectangle.Empty, Resources.EditorFont, "play/pause", "bt1", screenId,
                ui => { ui.Rectangle = new Rectangle(Game1.WindowWidth - RightBarWidth + 5, Values.ToolBarHeight + buttonDist * 2 + buttonHeight, buttonWidth, buttonHeight); },
                ui => { _isPlaying = !_isPlaying; }));

            posY = Values.ToolBarHeight + buttonDist;

            // load button
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY, buttonWidth, buttonHeight), Resources.EditorFont,
                "load", "bt1", screenId, null, ui => { LoadAnimation(); }));

            // save button
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "save as...", "bt1", screenId, null, ui => { SaveAnimationDialog(); }));
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "save...", "bt1", screenId, null, ui => { SaveAnimation(); }));

            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "updated animations", "bt1", screenId, null, ui => { UpdateAnimations(); }));

            // load image
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "load image", "bt1", screenId, null, ui => { LoadImage(); }));
            // clear animation
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "clear animator", "bt1", screenId, null, ui => { CreateAnimator(); }));

            Game1.EditorUi.AddElement(new UiCheckBox(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight),
                Resources.EditorFont, "show all selections", "showAllSelections", screenId, false, null,
                ui => { _showAllSelections = ((UiCheckBox)ui).CurrentState; }));

            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist * 3, buttonWidth, labelHeight),
                Resources.EditorFont, "Animation", "frameHeader", screenId, null));

            _animationUiLabel = new UiLabel(new Rectangle(buttonDist, posY += labelHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, "animation", "flable", screenId, ui => { ui.Label = "[" + (_selectedAnimation + 1) + "/" + _animator.Animations.Count + "]"; });
            Game1.EditorUi.AddElement(_animationUiLabel);
            _animationInput = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 1, 1, 1, 1, "numberX", screenId,
                ui => { ((UiNumberInput)ui).MaxValue = _animator.Animations.Count; },
                ui => { ChangeAnimation((int)((UiNumberInput)ui).Value - 1); });
            Game1.EditorUi.AddElement(_animationInput);

            // add animation
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist, posY += buttonHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, "add", "bt1", screenId, null, ui => { AddAnimation(); }));
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, "remove", "bt1", screenId, null, ui => { RemoveAnimation(); }));

            // animation name
            var animationNameLabel = new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist, buttonWidth, labelHeight),
                Resources.EditorFont, "name", "lableX", screenId, null);
            Game1.EditorUi.AddElement(animationNameLabel);
            _animationName = new UiTextInput(new Rectangle(buttonDist, posY += labelHeight, buttonWidth, buttonHeight),
                Resources.EditorFontMonoSpace, 20, "animationName", screenId, null, ui => { _animator.Animations[_selectedAnimation].Id = ((UiTextInput)ui).StrValue; });
            Game1.EditorUi.AddElement(_animationName);

            // next animation
            var nextAnimationLabel = new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist, buttonWidth, labelHeight),
                Resources.EditorFont, "next animation", "lableX", screenId, null);
            Game1.EditorUi.AddElement(nextAnimationLabel);
            _nextAnimationName = new UiTextInput(new Rectangle(buttonDist, posY += labelHeight, buttonWidth, buttonHeight),
                Resources.EditorFontMonoSpace, 20, "nextAnimationName", screenId, null,
                ui => { _animator.Animations[_selectedAnimation].NextAnimation = ((UiTextInput)ui).StrValue; });
            Game1.EditorUi.AddElement(_nextAnimationName);

            // collision
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
                "collision mode", "bt1", screenId, ui => { ((UiButton)ui).Marked = _collisionRectangleMode; },
                ui => { _collisionRectangleMode = !_collisionRectangleMode; }));

            //// loop checkbox
            //_checkBoxLoop = new CheckBox(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight), Resources.EditorFont,
            //    "loop", "cbloop", screenId, false, ui => { ((CheckBox)ui).CurrentState = _animator.CurrentAnimation.Looping; },
            //    ui => { _animator.CurrentAnimation.Looping = ((CheckBox)ui).CurrentState; });
            //Game1.EditorUi.AddElement(_checkBoxLoop);

            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, "Loops", "flable", screenId, null));
            _loopCountInput = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 1, -1, 999, 1, "loopCount", screenId, null,
                ui => _animator.Animations[_selectedAnimation].LoopCount = (int)((UiNumberInput)ui).Value);
            Game1.EditorUi.AddElement(_loopCountInput);


            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist * 3, buttonWidth, labelHeight),
                Resources.EditorFont, "Frame", "frameHeader", screenId, null));

            // frame information
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += labelHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, "frame", "flable", screenId,
                ui => { ui.Label = "frame [" + (_selectedFrame + 1) + "/" + _animator.Animations[_selectedAnimation].Frames.Length + "]"; }));
            _frameInput = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 1, 1, 1, 1, "numberX", screenId,
                ui => { ((UiNumberInput)ui).MaxValue = _animator.Animations[_selectedAnimation].Frames.Length; },
                ui => { ChangeFrame((int)((UiNumberInput)ui).Value - 1); });
            Game1.EditorUi.AddElement(_frameInput);

            // add remove frame
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist, posY += buttonHeight + buttonDist, buttonWidthHalf, buttonHeight), Resources.EditorFont,
                "add", "addFrame", screenId, null, ui => { AddFrame(); }));
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight), Resources.EditorFont,
                "delete", "deleteFrame", screenId, null, ui => { DeleteFrame(); }));

            _cbFrameMirroredV = (UiCheckBox)Game1.EditorUi.AddElement(new UiCheckBox(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight),
                Resources.EditorFont, "mirrored V", "cbmirrored", screenId, false, null,
                ui => { _animator.Animations[_selectedAnimation].Frames[_selectedFrame].MirroredV = ((UiCheckBox)ui).CurrentState; }));
            _cbFrameMirroredH = (UiCheckBox)Game1.EditorUi.AddElement(new UiCheckBox(new Rectangle(5, posY += buttonHeight + buttonDist, buttonWidth, buttonHeight),
                Resources.EditorFont, "mirrored H", "cbmirrored", screenId, false, null,
                ui => { _animator.Animations[_selectedAnimation].Frames[_selectedFrame].MirroredH = ((UiCheckBox)ui).CurrentState; }));

            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist * 2, buttonWidth, labelHeight),
                Resources.EditorFont, "fps", "flable", screenId, null));
            _fpsInput = new UiNumberInput(new Rectangle(buttonDist, posY += labelHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 1, 1, 1000, 1, "numberX", screenId, null,
                ui => { _animator.SetFrameFps(_selectedAnimation, _selectedFrame, (int)((UiNumberInput)ui).Value); });
            Game1.EditorUi.AddElement(_fpsInput);
            Game1.EditorUi.AddElement(new UiButton(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, "to all", "toall", screenId, null,
                ui => { _animator.SetAnimationFps(_selectedAnimation, (int)_fpsInput.Value); }));

            // animation offset
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist,
                buttonWidth, labelHeight), Resources.EditorFont, "animation offset", "lableX", screenId, null));
            //Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += Resources.EditorFontHeight + buttonDist,
            //    buttonWidthHalf, labelHeight), Resources.EditorFont, "x", "lableX", screenId, null));
            //Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY,
            //    buttonWidthHalf, labelHeight), Resources.EditorFont, "y", "lableX", screenId, null));
            _niAnOffsetX = new UiNumberInput(new Rectangle(buttonDist, posY += labelHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, -100, 100, 1, "animationWidth", screenId, null,
                ui => { _animator.Animations[_selectedAnimation].Offset.X = (int)((UiNumberInput)ui).Value; });
            _niAnOffsetY = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, -100, 100, 1, "animationWidth", screenId, null,
                ui => { _animator.Animations[_selectedAnimation].Offset.Y = (int)((UiNumberInput)ui).Value; });
            Game1.EditorUi.AddElement(_niAnOffsetX);
            Game1.EditorUi.AddElement(_niAnOffsetY);

            // rectangle
            // labels
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist,
                buttonWidth, labelHeight), Resources.EditorFont, "rectangle", "rectangle", screenId, null));
            //Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY,
            //    buttonWidthHalf, labelHeight), Resources.EditorFont, "y", "lableX", screenId, null));
            // number picker
            _numberInputX = new UiNumberInput(new Rectangle(buttonDist, posY += labelHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, 0, 1000, 1, "numberX", screenId, null,
                ui =>
                {
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.X = (int)((UiNumberInput)ui).Value;
                    UpdateCurrentFrame();
                });
            _numberInputY = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, 0, 1000, 1, "numberY", screenId, null,
                ui =>
                {
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.Y = (int)((UiNumberInput)ui).Value;
                    UpdateCurrentFrame();
                });
            Game1.EditorUi.AddElement(_numberInputX);
            Game1.EditorUi.AddElement(_numberInputY);
            //// labels
            //Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist,
            //    buttonWidthHalf, labelHeight), Resources.EditorFont, "width", "lableX", screenId, null));
            //Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY,
            //    buttonWidthHalf, labelHeight), Resources.EditorFont, "height", "lableX", screenId, null));
            // number picker
            _numberInputWidth = new UiNumberInput(new Rectangle(buttonDist, posY += buttonHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, 0, 1000, 1, "numberWidth", screenId, null, ui =>
                {
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.Width = (int)((UiNumberInput)ui).Value;
                    UpdateCurrentFrame();
                });
            _numberInputHeight = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, 0, 1000, 1, "numberHeight", screenId, null, ui =>
                {
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.Height = (int)((UiNumberInput)ui).Value;
                    UpdateCurrentFrame();
                });
            Game1.EditorUi.AddElement(_numberInputWidth);
            Game1.EditorUi.AddElement(_numberInputHeight);

            // offset
            // labels
            Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight + buttonDist,
                buttonWidth, labelHeight), Resources.EditorFont, "frame offset", "lableX", screenId, null));
            //Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist, posY += buttonHeight / 2,
            //    buttonWidthHalf, labelHeight), Resources.EditorFont, "x", "lableX", screenId, null));
            //Game1.EditorUi.AddElement(new UiLabel(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY,
            //    buttonWidthHalf, labelHeight), Resources.EditorFont, "y", "lableX", screenId, null));
            // number picker
            _niOffsetX = new UiNumberInput(new Rectangle(buttonDist, posY += labelHeight + buttonDist, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, -100, 100, 1, "numberX", screenId, null, ui =>
                {
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].Offset.X = (int)((UiNumberInput)ui).Value;
                    UpdateCurrentFrame();
                });
            _niOffsetY = new UiNumberInput(new Rectangle(buttonDist * 2 + buttonWidthHalf, posY, buttonWidthHalf, buttonHeight),
                Resources.EditorFont, 0, -100, 100, 1, "numberY", screenId, null, ui =>
                {
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].Offset.Y = (int)((UiNumberInput)ui).Value;
                    UpdateCurrentFrame();
                });
            Game1.EditorUi.AddElement(_niOffsetX);
            Game1.EditorUi.AddElement(_niOffsetY);

            // create empty animation
            CreateAnimator();
        }

        public override void Update(GameTime gameTime)
        {
            Game1.EditorUi.CurrentScreen = Values.EditorUiAnimation;

            if (_sprAnimator == null)
                return;

            var mousePosition = InputHandler.MousePosition();

            if (_isPlaying && !_animator.IsPlaying)
            {
                _animator.Play(_selectedAnimation);
            }

            if (_isPlaying)
                _animator.Update();
            else
            {
                _animator.SetFrame(_selectedFrame);
            }

            // update tileset scale
            if (InputHandler.MouseIntersect(new Rectangle(LeftBarWidth, Values.ToolBarHeight,
                Game1.WindowWidth - LeftBarWidth - RightBarWidth, Game1.WindowHeight - Values.ToolBarHeight)))
            {
                _currentPosition = new Point(
                    (int)((InputHandler.MousePosition().X - _camera.Location.X) / _camera.Scale),
                    (int)((InputHandler.MousePosition().Y - _camera.Location.Y) / _camera.Scale));

                if (InputHandler.MouseLeftStart())
                {
                    _selecting = true;
                    _selectionStart = _currentPosition;
                }
                if (InputHandler.MouseLeftDown() && _selecting)
                {
                    _selectionEnd = _currentPosition;

                    var selectionStart1 = new Point(Math.Min(_selectionStart.X, _selectionEnd.X), Math.Min(_selectionStart.Y, _selectionEnd.Y));
                    var selectionEnd1 = new Point(Math.Max(_selectionStart.X, _selectionEnd.X) + 1, Math.Max(_selectionStart.Y, _selectionEnd.Y) + 1);

                    if (_collisionRectangleMode)
                        _animator.Animations[_selectedAnimation].Frames[_selectedFrame].CollisionRectangle =
                            new Rectangle(
                                selectionStart1.X - _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.X,
                                selectionStart1.Y - _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.Y,
                                selectionEnd1.X - selectionStart1.X, selectionEnd1.Y - selectionStart1.Y);
                    else
                    {
                        _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle =
                            new Rectangle(selectionStart1.X, selectionStart1.Y, selectionEnd1.X - selectionStart1.X,
                                selectionEnd1.Y - selectionStart1.Y);
                        UpdateCurrentFrame();
                    }

                    UpdateInputUi();
                }
                if (InputHandler.MouseLeftReleased())
                {
                    _selecting = false;
                }

                if (InputHandler.MouseRightPressed())
                {
                    if (_collisionRectangleMode)
                        _animator.Animations[_selectedAnimation].Frames[_selectedFrame].CollisionRectangle = Rectangle.Empty;
                }

                if (InputHandler.MouseWheelUp())
                    _camera.Zoom(1, mousePosition);
                if (InputHandler.MouseWheelDown())
                    _camera.Zoom(-1, mousePosition);
            }

            if (!InputHandler.MouseMiddleStart() && InputHandler.MouseMiddleDown())
                _camera.Location += mousePosition - InputHandler.LastMousePosition();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_sprAnimator == null)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, _camera.TransformMatrix);

            // draw the tiled background
            spriteBatch.Draw(Resources.SprTiledBlock, new Rectangle(0, 0, _sprAnimator.Width, _sprAnimator.Height),
                new Rectangle(0, 0,
                    (int)(_sprAnimator.Width / (float)TileSize * 2),
                    (int)(_sprAnimator.Height / (float)TileSize * 2)), Color.White);

            // draw the sprite
            spriteBatch.Draw(_sprAnimator, new Rectangle(0, 0, _sprAnimator.Width, _sprAnimator.Height), Color.White);

            // draw current position of the mouse
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(_currentPosition.X, _currentPosition.Y, 1, 1), Color.Red * 0.5f);

            // show all source rectangles of all animations
            if (_showAllSelections)
            {
                foreach (var animation in _animator.Animations)
                    for (var i = 0; i < animation.Frames.Length; i++)
                        spriteBatch.Draw(Resources.SprWhite, animation.Frames[i].SourceRectangle, Color.HotPink * 0.25f);
            }

            // draw the selection
            for (var i = 0; i < _animator.Animations[_selectedAnimation].Frames.Length; i++)
            {
                spriteBatch.Draw(Resources.SprWhite, _animator.Animations[_selectedAnimation].Frames[i].SourceRectangle, _selectedFrame == i ? Color.Red * 0.5f : Color.Red * 0.25f);
            }

            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].CollisionRectangle.X +
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.X,
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].CollisionRectangle.Y +
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.Y,
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].CollisionRectangle.Width,
                    _animator.Animations[_selectedAnimation].Frames[_selectedFrame].CollisionRectangle.Height), Color.Green * 0.5f);

            spriteBatch.End();
        }

        public override void DrawTop(SpriteBatch spriteBatch)
        {
            if (_sprAnimator == null)
                return;

            float drawScale = 2;

            if (_animator.CurrentAnimation.AnimationWidth > 0 && _animator.CurrentAnimation.AnimationHeight > 0)
            {
                if (_animator.CurrentAnimation.AnimationWidth > _animator.CurrentAnimation.AnimationHeight)
                    drawScale = (int)(RightBarWidth / (float)(_animator.CurrentAnimation.AnimationWidth + 2));
                else
                    drawScale = (int)(RightBarWidth / (float)(_animator.CurrentAnimation.AnimationHeight + 2));
            }

            var drawOffsetX = Game1.WindowWidth - RightBarWidth / 2 -
                              (int)((_animator.CurrentAnimation.AnimationWidth + 2) * drawScale) / 2;
            var drawOffsetY = Game1.WindowHeight - RightBarWidth / 2 -
                              (int)((_animator.CurrentAnimation.AnimationHeight + 2) * drawScale) / 2;

            var drawPositionX = drawOffsetX + (int)drawScale +
                                (int)((_animator.CurrentFrame.Offset.X - _animator.CurrentAnimation.AnimationLeft) * drawScale);
            var drawPositionY = drawOffsetY + (int)drawScale +
                                (int)((_animator.CurrentFrame.Offset.Y - _animator.CurrentAnimation.AnimationTop) * drawScale);

            // draw the tiled background
            spriteBatch.Draw(Resources.SprTiledBlock, new Rectangle(
                    drawOffsetX, drawOffsetY,
                    (int)((_animator.CurrentAnimation.AnimationWidth + 2) * drawScale),
                    (int)((_animator.CurrentAnimation.AnimationHeight + 2) * drawScale)),
                new Rectangle(0, 0,
                    (int)((_animator.CurrentAnimation.AnimationWidth + 2) / (float)TileSize * 2),
                    (int)((_animator.CurrentAnimation.AnimationHeight + 2) / (float)TileSize * 2)), Color.White * 0.5f);

            DrawOverlayAnimation(spriteBatch, drawOffsetX, drawOffsetY, drawScale);

            // draw the current animation sprite
            spriteBatch.Draw(_sprAnimator, new Rectangle(
                drawPositionX, drawPositionY, (int)(drawScale * _animator.FrameWidth), (int)(drawScale * _animator.FrameHeight)),
                _animator.CurrentFrame.SourceRectangle, Color.White, 0, Vector2.Zero,
                (_animator.CurrentFrame.MirroredV ? SpriteEffects.FlipVertically : SpriteEffects.None) |
                (_animator.CurrentFrame.MirroredH ? SpriteEffects.FlipHorizontally : SpriteEffects.None), 0);

            // draw the origin xy axis
            var originPosition = new Vector2(
                drawPositionX + (-_animator.CurrentAnimation.Offset.X - _animator.CurrentFrame.Offset.X) * drawScale,
                drawPositionY + (-_animator.CurrentAnimation.Offset.Y - _animator.CurrentFrame.Offset.Y) * drawScale);
            spriteBatch.Draw(Resources.SprWhite, new Vector2(originPosition.X - 1, originPosition.Y - 10), new Rectangle(0, 0, 2, 20), Color.Green);
            spriteBatch.Draw(Resources.SprWhite, new Vector2(originPosition.X - 10, originPosition.Y - 1), new Rectangle(0, 0, 20, 2), Color.Red);
        }

        private void DrawOverlayAnimation(SpriteBatch spriteBatch, int drawOffsetX, int drawOffsetY, float drawScale)
        {
            if (_overlayAnimator != null && _overlayAnimator.GetAnimationIndex(_animator.CurrentAnimation.Id) >= 0)
            {
                _overlayAnimator.Play(_animator.CurrentAnimation.Id);
                // find the matching frame
                var frameIndex = GetOverlayFrame();

                _overlayAnimator.SetFrame(frameIndex);

                var drawPositionX = drawOffsetX + (int)drawScale +
                                    (int)((_overlayAnimator.CurrentFrame.Offset.X -
                                           _animator.CurrentAnimation.AnimationLeft +
                                           _overlayAnimator.CurrentAnimation.Offset.X -
                                           _animator.CurrentAnimation.Offset.X) * drawScale);
                var drawPositionY = drawOffsetY + (int)drawScale +
                                    (int)((_overlayAnimator.CurrentFrame.Offset.Y -
                                           _animator.CurrentAnimation.AnimationTop +
                                           _overlayAnimator.CurrentAnimation.Offset.Y -
                                           _animator.CurrentAnimation.Offset.Y) * drawScale);

                spriteBatch.Draw(_overlayAnimator.SprTexture, new Rectangle(
                        drawPositionX, drawPositionY, (int)(drawScale * _overlayAnimator.FrameWidth),
                        (int)(drawScale * _overlayAnimator.FrameHeight)),
                    _overlayAnimator.CurrentFrame.SourceRectangle, Color.White, 0, Vector2.Zero,
                    (_overlayAnimator.CurrentFrame.MirroredV ? SpriteEffects.FlipVertically : SpriteEffects.None) |
                    (_overlayAnimator.CurrentFrame.MirroredH ? SpriteEffects.FlipHorizontally : SpriteEffects.None), 0);
            }
        }

        private int GetOverlayFrame()
        {
            if (_overlayAnimator.CurrentAnimation.Frames.Length <= 0)
                return 0;

            // time to reach the selected frame
            var frameTime = 0;
            for (var i = 0; i < _animator.CurrentFrameIndex; i++)
                frameTime += _animator.CurrentAnimation.Frames[i].FrameTime;

            var index = 0;
            while (frameTime >= 0)
            {
                frameTime -= _overlayAnimator.CurrentAnimation.Frames[index].FrameTime;
                if (frameTime < 0)
                    return index;

                if (_overlayAnimator.CurrentAnimation.Frames.Length > index + 1)
                    index++;
                // loop animation
                else
                    index = 0;
            }

            return 0;
        }

        private void UpdateCurrentFrame()
        {
            // update the size of the animation
            _animator.RecalculateAnimationSize(_selectedAnimation);
        }

        public void CreateAnimator()
        {
            // create empty animator with one animation and frame
            _animator = new Animator();
            AddAnimation();

            _animator.SpritePath = _sprPath;
        }

        public void AddAnimation()
        {
            var newAnimation = new Animation("a" + _animator.Animations.Count) { NextAnimation = "" };

            _animator.AddAnimation(newAnimation);
            _animator.AddFrame(_animator.Animations.Count - 1, 0, new Frame() { FrameTimeFps = 5 });

            ChangeAnimation(_animator.Animations.Count - 1);
        }

        private void RemoveAnimation()
        {
            if (_animator.Animations.Count <= 1)
                return;

            _animator.Animations.Remove(_animator.Animations[_selectedAnimation]);
            ChangeAnimation(_selectedAnimation % _animator.Animations.Count);
        }

        public void AddFrame()
        {
            var newFrame = new Frame()
            {
                SourceRectangle = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle,
                Offset = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].Offset,
                FrameTimeFps = 5
            };

            _animator.AddFrame(_selectedAnimation, _selectedFrame + 1, newFrame);

            _selectedFrame++;
            UpdateInputUi();
        }

        public void DeleteFrame()
        {
            // cant delete the last frame
            if (_animator.Animations[_selectedAnimation].Frames.Length <= 1)
                return;

            _animator.Stop();

            // create new frame array without the selected frame
            var newFrames = new Frame[_animator.Animations[_selectedAnimation].Frames.Length - 1];
            var newIndex = 0;
            for (var i = 0; i < _animator.Animations[_selectedAnimation].Frames.Length; i++)
                if (i != _selectedFrame)
                {
                    newFrames[newIndex] = _animator.Animations[_selectedAnimation].Frames[i];
                    newIndex++;
                }
            _animator.Animations[_selectedAnimation].Frames = newFrames;

            if (_selectedFrame > 0)
                _selectedFrame--;

            UpdateInputUi();
        }

        public void ChangeAnimation(int nextAnimation)
        {
            _selectedAnimation = nextAnimation;
            _selectedFrame = 0;
            _animator.Play(_selectedAnimation);

            UpdateInputUi();
        }

        public void ChangeFrame(int nextFrame)
        {
            _selectedFrame = nextFrame;
            UpdateInputUi();
        }

        public void UpdateInputUi()
        {
            _animationInput.Value = _selectedAnimation + 1;
            _frameInput.Value = _selectedFrame + 1;

            _loopCountInput.Value = _animator.Animations[_selectedAnimation].LoopCount;
            _fpsInput.Value = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].FrameTimeFps;

            _animationName.StrValue = _animator.Animations[_selectedAnimation].Id;
            _nextAnimationName.StrValue = _animator.Animations[_selectedAnimation].NextAnimation;

            _niAnOffsetX.Value = _animator.CurrentAnimation.Offset.X;
            _niAnOffsetY.Value = _animator.CurrentAnimation.Offset.Y;

            _numberInputX.Value = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.X;
            _numberInputY.Value = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.Y;
            _numberInputWidth.Value = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.Width;
            _numberInputHeight.Value = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].SourceRectangle.Height;

            _niOffsetX.Value = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].Offset.X;
            _niOffsetY.Value = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].Offset.Y;

            _cbFrameMirroredV.CurrentState = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].MirroredV;
            _cbFrameMirroredH.CurrentState = _animator.Animations[_selectedAnimation].Frames[_selectedFrame].MirroredH;
        }

        public void SaveAnimationDialog()
        {
#if WINDOWS
            var saveFileDialog = new SaveFileDialog()
            {
                RestoreDirectory = true,
                Filter = "animator files (*.ani)|*.ani",
            };

            if (_lastFileName != null)
            {
                saveFileDialog.FileName = Path.GetFileName(_lastFileName);
                saveFileDialog.InitialDirectory = Path.GetFullPath(Path.GetDirectoryName(_lastFileName));
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                AnimatorSaveLoad.SaveAnimator(saveFileDialog.FileName, _animator);
#endif
        }

        public void SaveAnimation()
        {
            AnimatorSaveLoad.SaveAnimator(_lastFileName, _animator);
        }

        public void LoadAnimation()
        {
#if WINDOWS
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "animator files (*.ani)|*.ani"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            EditorLoadAnimation(openFileDialog.FileName);
#endif
        }

        public void EditorLoadAnimation(string filePath)
        {
            _selectedAnimation = 0;
            _selectedFrame = 0;
            _lastFileName = filePath;
            _animator = AnimatorSaveLoad.LoadAnimatorFile(filePath);
            _sprAnimator = _animator.SprTexture;

            UpdateInputUi();
        }

        private void LoadOverlayAnimation()
        {
#if WINDOWS
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "animator files (*.ani)|*.ani"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            _overlayAnimator = AnimatorSaveLoad.LoadAnimatorFile(openFileDialog.FileName);
#endif
        }

        public void UpdateAnimations()
        {
#if WINDOWS
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "animator files (*.ani)|*.ani",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            foreach (var fileName in openFileDialog.FileNames)
            {
                _animator = AnimatorSaveLoad.LoadAnimatorFile(fileName);
                AnimatorSaveLoad.SaveAnimator(fileName, _animator);
            }
#endif
        }

        public void LoadImage()
        {
#if WINDOWS
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "png files (*.png)|*.png"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                using (var stream = File.OpenRead(openFileDialog.FileName))
                {
                    _sprAnimator = Texture2D.FromStream(Game1.Graphics.GraphicsDevice, stream);
                    _sprPath = Path.GetFileName(openFileDialog.FileName);
                    _animator.SpritePath = _sprPath;
                }
            }
            catch { }
#endif
        }

    }
}

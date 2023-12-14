using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Screens
{
    public class IntroScreen : Screen
    {
        private Texture2D _sprOcean;
        private Texture2D _sprRain;
        private Texture2D _sprIntro;
        private Texture2D _sprWaves;
        private Texture2D _sprCloud;

        private Animator _loadingAnimator;
        private Animator _marinAnimator;
        private Animator _linkAnimator;
        private Animator _lightAnimation;
        private Animator _linkBoatAnimator;

        private Animator[] _thunder = new Animator[2];
        private Vector2[] _thunderPositions = new Vector2[2];
        private float[] _thunderCounts = { 1000, 2000 };

        private enum States
        {
            OceanCamera,
            OceanPicture,
            OceanThunder,
            StrandFading,
            StrandCamera,
            StrandMarin,
            StrandLogo
        };

        private States _currentState;

        private int _scale = 1;
        private Vector2 _cameraCenter;
        private Matrix TransformMatrix =>
                                Matrix.CreateTranslation(new Vector3(
                                    -(float)(Math.Round(_cameraCenter.X * _scale) / _scale),
                                    -(float)(Math.Round(_cameraCenter.Y * _scale) / _scale), 0)) *
                                Matrix.CreateScale(_scale) *
                                Matrix.CreateTranslation(new Vector3((int)(Game1.WindowWidth * 0.5f), (int)(Game1.WindowHeight * 0.5f), 0)) * Game1.GetMatrix;

        private float _cameraState;
        private Vector2 _cameraStart;
        private Vector2 _cameraTarget;

        private enum MarinState
        {
            WalkSlow,
            Walk,
            Run,
            Stand,
            Hold,
            Push,
            End
        };

        private MarinState marinState;

        private MarinState[] _marinStates =
        {
            MarinState.WalkSlow, MarinState.Stand, MarinState.Run, MarinState.Stand, MarinState.Walk, MarinState.Stand, MarinState.Walk, MarinState.Stand, MarinState.Walk, MarinState.Stand,
            MarinState.Hold, MarinState.Push, MarinState.Hold, MarinState.Push, MarinState.Hold, MarinState.Push, MarinState.Hold, MarinState.Push, MarinState.End,
        };
        private int[] _marinTimes = {
            1000, 1100, 900, 1300, 200, // stand times
            2000, 500, 2100, 200, 400, 200, 250, 750 // push hold times
        };

        private Vector2[] _marinGoalPositions =
        {
            new Vector2(-150, 219), new Vector2(-94, 219), new Vector2(-72, 219), new Vector2(-64, 219),
            new Vector2(-18, 219)
        };

        private float _marinStateCounter;
        private int _marinIndex;
        private int _marinWalkIndex;
        private int _marinTimeIndex;

        private readonly Rectangle _oceanCloudRectangle = new Rectangle(0, 0, 32, 32);
        private int _thunderIndex;
        private float _thunderCount = 250;
        private float _thunderTransition;

        private readonly Rectangle _ocean0Rectangle = new Rectangle(0, 288, 32, 16);
        private readonly Rectangle _ocean1Rectangle = new Rectangle(0, 304, 32, 16);
        private readonly Rectangle _ocean2Rectangle = new Rectangle(0, 320, 32, 16);
        private readonly Rectangle _oceanRectangle = new Rectangle(0, 144, 32, 32);

        private DictAtlasEntry _spriteOceanBoat;

        private Vector2 _oceanBoatPosition;
        private Vector2 _oceanPosition0;
        private Vector2 _oceanPosition1;
        private Vector2 _oceanPosition2;
        private Vector2 _oceanPosition3;

        private readonly int[] _thunderFrame = { 1, 0, 1, 2, 1, 0, 1, 2, 1, 0, 1, 2, 1, 0 };
        private Color[] _oceanColor = { new Color(0, 32, 168), new Color(32, 32, 168), new Color(96, 96, 232) };

        private DictAtlasEntry _spriteBackground;
        private DictAtlasEntry _spriteMountain;
        private DictAtlasEntry _spriteLogo0;
        private DictAtlasEntry _spriteLogo1;

        private readonly Rectangle _treesRectangle = new Rectangle(0, 320, 32, 46);
        private readonly Rectangle _sandRectangle = new Rectangle(0, 364, 32, 16);
        private readonly Rectangle _waveRectangle = new Rectangle(0, 0, 32, 24);

        private Vector2 _logoPosition;
        private float _logoState;
        private float _logoCounter;

        private Vector2 _ligthPosition;
        private Vector2[] _lightPositions =
        {
            new Vector2(7, 0),
            new Vector2(41, 0),
            new Vector2(2, 19),
            new Vector2(0, 56),
            new Vector2(34, 9),
            new Vector2(55, 9),
            new Vector2(72, 9),
            new Vector2(96, 9),
            new Vector2(34, 47),
            new Vector2(55, 47),
            new Vector2(72, 47),
            new Vector2(90, 47),
            new Vector2(119, 47),
        };
        private int _lightIndex;

        private Vector2 _treePosition;
        private Vector2 _mountainRightPosition;
        private Vector2 _mountainLeftPosition;
        private Vector2 _cloundLeftPosition;
        private Vector2 _wavePosition;
        private Vector2 _marinPosition;
        private Vector2 _marinGoal;

        private int _currentFrame;
        private float _waveCounter;

        private int _oceanFrameIndex;

        private float _oceanShoreCounter = 2000;

        private int _screenWidth = 160;
        private int _screenHeight = 144;

        private float StrandFadeTime = 600;
        private float _strandFadeCount;
        private float _loadingTransparency = 0.75f;

        private const int Slow = 500;
        private const int Fast = 200;
        private readonly int[] _waveTimes = { Slow, Fast, Fast, Fast, Slow, Fast, Fast, Fast, Fast, Fast };

        private bool _loaded;

        public IntroScreen(string screenId) : base(screenId) { }

        public override void Load(ContentManager content)
        {
            if (_loaded)
                return;
            _loaded = true;

            _spriteOceanBoat = Resources.GetSprite("intro_boat");

            _spriteBackground = Resources.GetSprite("intro_background");
            _spriteMountain = Resources.GetSprite("intro_mountain");
            _spriteLogo0 = Resources.GetSprite("intro_logo_0");
            _spriteLogo1 = Resources.GetSprite("intro_logo_1");

            _mountainLeftPosition.X = -_spriteBackground.SourceRectangle.Width / 2 - _spriteMountain.SourceRectangle.Width;
            _mountainLeftPosition.Y = 0;

            _mountainRightPosition.X = _spriteBackground.SourceRectangle.Width / 2;
            _mountainRightPosition.Y = 0;

            _treePosition.X = 0;
            _treePosition.Y = _spriteBackground.SourceRectangle.Height - 16;

            _wavePosition.X = _treePosition.X;
            _wavePosition.Y = _treePosition.Y + _treesRectangle.Height + 16;

            _logoPosition.X = -_spriteBackground.SourceRectangle.Width / 2 + 16;
            _logoPosition.Y = 3;

            _sprOcean = Resources.GetTexture("ocean.png");
            _sprRain = Resources.GetTexture("rain.png");
            _sprIntro = Resources.GetTexture("intro.png");
            _sprCloud = Resources.GetTexture("cloud.png");
            _sprWaves = Resources.GetTexture("waves.png");

            _loadingAnimator = AnimatorSaveLoad.LoadAnimator("Intro/loading");
            _loadingAnimator.Play("idle");

            _thunder[0] = AnimatorSaveLoad.LoadAnimator("Intro/thunder");
            _thunder[1] = AnimatorSaveLoad.LoadAnimator("Intro/thunder");

            _linkBoatAnimator = AnimatorSaveLoad.LoadAnimator("Intro/link_boat");
            _marinAnimator = AnimatorSaveLoad.LoadAnimator("Intro/maria");
            _linkAnimator = AnimatorSaveLoad.LoadAnimator("Intro/link");
            _lightAnimation = AnimatorSaveLoad.LoadAnimator("Intro/light");
        }

        public override void OnLoad()
        {
            Init();
        }

        private void Init()
        {
            _linkAnimator.Play("idle");
            _marinAnimator.Play("walk");
            _lightAnimation.Stop();

            _currentState = States.OceanCamera;
            _cameraCenter = new Vector2(-220, 55);

            // start playing the prologue music
            Game1.GameManager.ResetMusic();
            Game1.GameManager.StopMusic();
            Game1.GameManager.SetMusic(25, 0);

            Game1.GbsPlayer.SetVolumeMultiplier(1.0f);
            Game1.GbsPlayer.Play();
            // play track for 52sec
#if WINDOWS
            Game1.GbsPlayer.SoundGenerator.SetStopTime(48.75f);
#endif
        }

        public override void Update(GameTime gameTime)
        {
            if (Game1.FinishedLoading && Game1.LoadFirstSave)
            {
                Game1.LoadFirstSave = false;
                if (SaveManager.FileExists(Values.PathSaveFolder + "/" + SaveGameSaveLoad.SaveFileName + "0"))
                {
                    // change to the game screen
                    Game1.ScreenManager.ChangeScreen(Values.ScreenNameGame);
                    // load the save
                    Game1.GameManager.LoadSaveFile(0);
                }
            }

#if WINDOWS
            if (Game1.GbsPlayer.SoundGenerator.WasStopped && Game1.GbsPlayer.SoundGenerator.FinishedPlaying())
            {
                Game1.GameManager.SetMusic(0, 0);
                Game1.GbsPlayer.Play();
            }
#endif

            if (Game1.FinishedLoading &&
                (ControlHandler.ButtonPressed(CButtons.A) || ControlHandler.ButtonPressed(CButtons.Start)))
                Game1.ScreenManager.ChangeScreen(Values.ScreenNameMenu);

            if (!Game1.FinishedLoading)
                _loadingAnimator.Update();
            if (Game1.FinishedLoading)
                _loadingTransparency = AnimationHelper.MoveToTarget(_loadingTransparency, 0, 0.125f * Game1.TimeMultiplier);

            UpdateOcean();

            UpdateBeach();

            _scale = MathHelper.Clamp(Math.Min(Game1.WindowWidth / _screenWidth, Game1.WindowHeight / _screenHeight), 1, 10);
        }

        private void UpdateOcean()
        {
            if (_currentState == States.OceanCamera)
            {
                // move the camera to the center
                var goalPosition = new Vector2(0, 55);
                var direction = goalPosition - _cameraCenter;
                if (direction.Length() < Game1.TimeMultiplier * 0.25f)
                {
                    _currentState = States.OceanPicture;

                    _linkBoatAnimator.Stop();
                    _linkBoatAnimator.Play("run");
                }
                else
                {
                    direction.Normalize();
                    _cameraCenter += direction * Game1.TimeMultiplier * 0.25f;
                }

                _thunderIndex = 0;
                for (var i = 0; i < _thunder.Length; i++)
                {
                    _thunder[i].Update();
                    if (_thunder[i].IsPlaying)
                        UpdateThunderIndex(_thunder[i].FrameCounter);

                    _thunderCounts[i] -= Game1.DeltaTime;
                    if (_thunderCounts[i] <= 0)
                    {
                        var animation = Game1.RandomNumber.Next(0, 4);
                        _thunder[i].Play("thunder" + animation);
                        _thunderCounts[i] = Game1.RandomNumber.Next(2000, 4000);

                        // play sound effect
                        Game1.GameManager.PlaySoundEffect("D378-12-0C", true);

                        var randomX = (int)_cameraCenter.X - 150 + Game1.RandomNumber.Next(0, 300);
                        if (animation < 2)
                            _thunderPositions[i] = new Vector2(randomX, 28);
                        else
                            _thunderPositions[i] = new Vector2(randomX, 14);
                    }
                }

                _oceanPosition0 = new Vector2(_cameraCenter.X * 0.1f, 60 + (int)Math.Round(Math.Sin(Game1.TotalTime / 500) * 1));
                _oceanPosition1 = new Vector2(_cameraCenter.X * 0.05f, 60 + (int)Math.Round(Math.Sin(Game1.TotalTime / 500 + 0.1) * 2));
                _oceanPosition2 = new Vector2(-_cameraCenter.X * 0.05f, 60 + (int)Math.Round(Math.Sin(Game1.TotalTime / 500 + 0.2) * 3));
                _oceanPosition3 = new Vector2(-_cameraCenter.X * 0.25f, 60 + (int)Math.Round(Math.Sin(Game1.TotalTime / 500 + 0.2) * 3));
                _oceanBoatPosition = new Vector2(-16 + _cameraCenter.X * 0.05f, 47 + (int)Math.Round(Math.Sin(Game1.TotalTime / 500) * 2.5));
            }
            else if (_currentState == States.OceanPicture)
            {
                _cameraCenter = new Vector2(0, 0);

                _linkBoatAnimator.Update();

                if (_oceanFrameIndex != _linkBoatAnimator.CurrentFrameIndex &&
                    (_linkBoatAnimator.CurrentFrameIndex == 1 || _linkBoatAnimator.CurrentFrameIndex == 10))
                {
                    Game1.GameManager.PlaySoundEffect("D378-12-0C", true);
                }

                _oceanFrameIndex = _linkBoatAnimator.CurrentFrameIndex;

                // transition to next state
                if (!_linkBoatAnimator.IsPlaying)
                {
                    _currentState = States.OceanThunder;
                    _thunderTransition = 0;
                    _thunderCount = 250;
                    _thunder[0].Play("null");
                }
            }
            else if (_currentState == States.OceanThunder)
            {
                if (_thunderCount < 0)
                {
                    _thunder[0].Play("thunderboat");

                    var counter = _thunder[0].FrameCounter;
                    UpdateThunderIndex(counter);

                    if (counter > 500)
                    {
                        _thunderTransition = ((float)counter - 500) / 700f;
                        if (_thunderTransition >= 1)
                        {
                            InitBeach();
                            return;
                        }
                    }
                }
                else
                    _thunderCount -= Game1.DeltaTime;

                _thunder[0].Update();

                // center the camera
                _cameraCenter = new Vector2(0, 55);

                _oceanPosition0 = new Vector2(_cameraCenter.X * 0.1f, 60);
                _oceanPosition1 = new Vector2(_cameraCenter.X * 0.05f, 60);
                _oceanPosition2 = new Vector2(-_cameraCenter.X * 0.05f, 60);
                _oceanPosition3 = new Vector2(-_cameraCenter.X * 0.25f, 60);
                _oceanBoatPosition = new Vector2(-16 + _cameraCenter.X * 0.05f, 47);

                _thunderPositions[0] = new Vector2(-2, -17);
            }
        }

        private void UpdateThunderIndex(double time)
        {
            var index = (int)(time / (2000 / 60.0));
            if (index < _thunderFrame.Length)
                _thunderIndex = _thunderFrame[index];
        }

        private void InitBeach()
        {
            _currentState = States.StrandFading;

            _cameraCenter = new Vector2(-400, 210);
            _marinPosition = new Vector2(-250, 219);

            _marinAnimator.Play("stand");
            _marinAnimator.SpeedMultiplier = 1.0f;

            _logoState = 0;
            _strandFadeCount = 3700 + StrandFadeTime;
            _marinIndex = 0;
            _marinWalkIndex = 0;
            _marinTimeIndex = 0;

            NextState();
        }

        private void UpdateBeach()
        {
            if (_currentState != States.StrandFading && _currentState != States.StrandCamera &&
                _currentState != States.StrandMarin && _currentState != States.StrandLogo)
                return;

            if (_currentState == States.StrandFading)
            {
                _strandFadeCount -= Game1.DeltaTime;

                if (_strandFadeCount < StrandFadeTime)
                {
                    UpdateMarin();
                }

                if (_strandFadeCount <= 0)
                {
                    _strandFadeCount = 0;
                    _currentState = States.StrandCamera;
                }
            }
            // move camera to marin
            else if (_currentState == States.StrandCamera)
            {
                UpdateMarin();

                // reached marin?
                if (_cameraCenter.X >= _marinPosition.X + 18)
                {
                    _currentState = States.StrandMarin;
                }
            }
            // camera follows marin directly
            else if (_currentState == States.StrandMarin)
            {
                UpdateMarin();

                if (_marinIndex == _marinStates.Length - 1)
                {
                    _cameraState = 0;
                    _cameraStart = _cameraCenter;
                    _cameraTarget = new Vector2(_cameraCenter.X, _logoPosition.Y + _spriteLogo0.ScaledRectangle.Height + 5);

                    _logoCounter = 0;
                    _currentState = States.StrandLogo;
                }
            }
            else if (_currentState == States.StrandLogo)
            {
                if (!MoveCamera(0.65f))
                {
                    _logoCounter += Game1.DeltaTime;

                    if (_logoCounter > 750 && _logoState != 1)
                    {
                        _logoState = AnimationHelper.MoveToTarget(_logoState, 1, 0.05f * Game1.TimeMultiplier);

                        if (_logoState == 1)
                            Game1.GameManager.PlaySoundEffect("D378-25-19");
                    }

                    if (_logoCounter > 1500)
                    {
                        if (!_lightAnimation.IsPlaying)
                        {
                            _lightAnimation.Play("idle");

                            _lightIndex = (_lightIndex + Game1.RandomNumber.Next(1, _lightPositions.Length)) % _lightPositions.Length;
                            _ligthPosition = _lightPositions[_lightIndex];
                        }
                    }
                }
            }
            else
            {
                _oceanShoreCounter -= Game1.DeltaTime;
                if (_oceanShoreCounter <= 0)
                {
                    _oceanShoreCounter += 3500;
                    Game1.GameManager.PlaySoundEffect("D378-15-0F");
                }
            }

            if (_currentState != States.StrandLogo && _strandFadeCount < StrandFadeTime)
            {
                UpdateCamera(new Vector2(_marinPosition.X + 18, 210), _currentState == States.StrandMarin ? 1.0f : 0.75f);
            }

            _marinAnimator.Update();
            _linkAnimator.Update();
            _lightAnimation.Update();

            _cloundLeftPosition = new Vector2(-_sprCloud.Width, 47);

            // update wave animation
            _waveCounter += Game1.DeltaTime;
            if (_waveCounter > _waveTimes[_currentFrame])
            {
                _waveCounter -= _waveTimes[_currentFrame];
                _currentFrame = (_currentFrame + 1) % 10;
            }
        }

        private void UpdateCamera(Vector2 tragetPosition, float maxSpeed)
        {
            var direction = tragetPosition - _cameraCenter;
            if (direction != Vector2.Zero)
            {
                var distance = direction.Length();

                if (distance <= 0.1f * Game1.TimeMultiplier)
                    _cameraCenter = tragetPosition;
                else
                {
                    var speedMult = Math.Clamp(CameraFunction(distance / 12.5f), 0, maxSpeed);

                    direction.Normalize();
                    var cameraSpeed = direction * speedMult * Game1.TimeMultiplier;

                    _cameraCenter += cameraSpeed;
                }
            }
        }

        private float CameraFunction(float x)
        {
            var y = MathF.Atan(x);

            if (x > 2)
                y += (x - 2) / 2;

            return y + 0.1f;
        }

        private bool MoveCamera(float speed)
        {
            if (_cameraCenter == _cameraTarget)
                return false;

            _cameraState += 0.005f * Game1.TimeMultiplier;
            _cameraState = Math.Clamp(_cameraState, 0, 1);

            _cameraCenter = Vector2.Lerp(_cameraStart, _cameraTarget, 0.5f + MathF.Sin(-MathF.PI * 0.5f + _cameraState * MathF.PI) * 0.5f);

            return true;
        }

        private void UpdateMarin()
        {
            if (marinState == MarinState.Stand)
            {
                _marinAnimator.Play("stand");

                _marinStateCounter -= Game1.DeltaTime;
                if (_marinStateCounter <= 0)
                    NextState();
            }
            else if (marinState == MarinState.Walk || marinState == MarinState.WalkSlow)
            {
                _marinAnimator.Play("move");

                if (_marinPosition.X < _marinGoal.X)
                {
                    _marinPosition.X += Game1.TimeMultiplier * (marinState == MarinState.Walk ? 0.5f : 0.25f);
                }
                else
                {
                    _marinPosition = _marinGoal;
                    NextState();
                }
            }
            else if (marinState == MarinState.Run)
            {
                _marinAnimator.SpeedMultiplier = 2.0f;
                _marinAnimator.Play("move");

                if (_marinPosition.X < _marinGoal.X)
                {
                    _marinPosition.X += Game1.TimeMultiplier * 1.0f;
                }
                else
                {
                    _marinPosition = _marinGoal;
                    _marinAnimator.SpeedMultiplier = 1.0f;
                    NextState();
                }
            }
            else if (marinState == MarinState.Hold)
            {
                _marinAnimator.Play("hold");
                _linkAnimator.Play("idle");

                _marinStateCounter -= Game1.DeltaTime;
                if (_marinStateCounter <= 0)
                    NextState();
            }
            else if (marinState == MarinState.Push)
            {
                _marinAnimator.Play("push");
                _linkAnimator.Play("pushed");

                _marinStateCounter -= Game1.DeltaTime;
                if (_marinStateCounter <= 0)
                    NextState();
            }
        }

        private void NextState()
        {
            marinState = _marinStates[_marinIndex];
            _marinIndex++;

            if (marinState == MarinState.Stand || marinState == MarinState.Hold || marinState == MarinState.Push)
            {
                _marinStateCounter = _marinTimes[_marinTimeIndex];
                _marinTimeIndex++;
            }
            else if (marinState == MarinState.Walk || marinState == MarinState.WalkSlow || marinState == MarinState.Run)
            {
                _marinGoal = _marinGoalPositions[_marinWalkIndex];
                _marinWalkIndex++;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Game1.Graphics.GraphicsDevice.Clear(_currentState == States.OceanPicture ? Color.Black : new Color(104, 96, 248));
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, TransformMatrix);

            DrawOcean(spriteBatch);

            DrawBeach(spriteBatch);

            spriteBatch.End();

            // draw the loading animation
            if (_loadingTransparency > 0)
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, Game1.GetMatrix);
                _loadingAnimator.DrawBasic(spriteBatch, new Vector2(
                    Game1.WindowWidth - 2 * _scale, Game1.WindowHeight - 2 * _scale), Color.White * _loadingTransparency, _scale);
                spriteBatch.End();
            }
        }

        private void DrawOcean(SpriteBatch spriteBatch)
        {
            if (_currentState != States.OceanCamera && _currentState != States.OceanPicture && _currentState != States.OceanThunder)
                return;

            var screenLeft = (int)Math.Floor(_cameraCenter.X) - (int)Math.Ceiling(Game1.WindowWidth / (double)_scale / 2.0);
            var screenRight = (int)Math.Ceiling(_cameraCenter.X) + (int)Math.Ceiling(Game1.WindowWidth / (double)_scale / 2.0);
            var screenTop = (int)Math.Floor(_cameraCenter.Y) - (int)Math.Ceiling(Game1.WindowHeight / (double)_scale / 2.0);
            var screenBottom = (int)Math.Ceiling(_cameraCenter.Y) + (int)Math.Ceiling(Game1.WindowHeight / (double)_scale / 2.0);
            var width = screenRight - screenLeft + 2;
            var height = screenBottom - screenTop + 2;
            var cloudOffset = _cameraCenter.X * 0.05f;

            if (_currentState == States.OceanCamera || _currentState == States.OceanThunder)
            {
                // draw the dark cloud
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(screenLeft, screenTop, width, -screenTop), new Color(24, 56, 40));

                // draw the clouds
                spriteBatch.Draw(_sprOcean,
                    new Rectangle(screenLeft, 0, width, _oceanCloudRectangle.Height),
                    new Rectangle(_oceanCloudRectangle.X + screenLeft - (int)cloudOffset,
                        _oceanCloudRectangle.Y + _thunderIndex * (_oceanCloudRectangle.Height + 16), width,
                        _oceanCloudRectangle.Height),
                    Color.White, 0, new Vector2(1 - cloudOffset % 1, 0), SpriteEffects.None, 0);

                // draw the dark sky
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(screenLeft, _oceanCloudRectangle.Height, width, 64), _oceanColor[_thunderIndex]);

                // draw the ocean top
                var oceanAnimationOffset = _thunderIndex * 64;
                spriteBatch.Draw(_sprOcean,
                    new Rectangle(screenLeft, (int)_oceanPosition0.Y, width, _ocean0Rectangle.Height),
                    new Rectangle(_ocean0Rectangle.X + screenLeft - (int)_oceanPosition0.X,
                        _ocean0Rectangle.Y + oceanAnimationOffset, width, _ocean0Rectangle.Height),
                    Color.White, 0, new Vector2(1 - _oceanPosition0.X % 1, 0), SpriteEffects.None, 0);

                // draw the ocean middle
                spriteBatch.Draw(_sprOcean,
                    new Rectangle(screenLeft, (int)_oceanPosition1.Y, width, _ocean1Rectangle.Height),
                    new Rectangle(_ocean1Rectangle.X + screenLeft - (int)_oceanPosition1.X,
                        _ocean1Rectangle.Y + oceanAnimationOffset, width, _ocean1Rectangle.Height),
                    Color.White, 0, new Vector2(1 - _oceanPosition1.X % 1, 0), SpriteEffects.None, 0);

                // draw the boat
                DrawHelper.DrawNormalized(spriteBatch, _spriteOceanBoat, _oceanBoatPosition, Color.White);

                // draw the ocean middle
                spriteBatch.Draw(_sprOcean,
                    new Rectangle(screenLeft, (int)_oceanPosition2.Y, width, _ocean2Rectangle.Height),
                    new Rectangle(_ocean2Rectangle.X + screenLeft - (int)_oceanPosition2.X,
                        _ocean2Rectangle.Y + oceanAnimationOffset, width, _ocean2Rectangle.Height),
                    Color.White, 0, new Vector2(1 - _oceanPosition2.X % 1, 0), SpriteEffects.None, 0);

                // draw the ocean
                spriteBatch.Draw(_sprOcean,
                    new Rectangle(screenLeft, (int)_oceanPosition3.Y + 16, width, _oceanRectangle.Height),
                    new Rectangle(_oceanRectangle.X + screenLeft - (int)_oceanPosition3.X,
                        _oceanRectangle.Y + _thunderIndex * (_oceanRectangle.Height + 16), width, _oceanRectangle.Height),
                    Color.White, 0, new Vector2(1 - _oceanPosition3.X % 1, 0), SpriteEffects.None, 0);

                // draw the dark ocean
                spriteBatch.Draw(Resources.SprWhite,
                    new Rectangle(screenLeft, (int)_oceanPosition3.Y + 48, width,
                        screenBottom - ((int)_oceanPosition3.Y + 48)), new Color(16, 0, 104));
            }

            if (_currentState == States.OceanPicture)
            {
                _linkBoatAnimator.Draw(spriteBatch, Vector2.Zero, Color.White);
            }

            // draw the rain
            var rainOffset = new Vector2((float)(Game1.TotalTime / 2.5f + Math.Sin(Game1.TotalTime / 500) * 5), (float)Game1.TotalTime / 2.3f);
            spriteBatch.Draw(_sprRain,
                new Rectangle(screenLeft, screenTop, width, height),
                new Rectangle(screenLeft - (int)rainOffset.X, screenTop - (int)rainOffset.Y, width, height),
                Color.White, 0, new Vector2(1 - rainOffset.X % 1, 1 - rainOffset.Y % 1), SpriteEffects.None, 0);

            if (_currentState == States.OceanCamera)
            {
                // draw the thunder
                for (var i = 0; i < _thunder.Length; i++)
                    _thunder[i].DrawBasic(spriteBatch, _thunderPositions[i] + new Vector2(cloudOffset, 0), Color.White);
            }
            else if (_currentState == States.OceanThunder)
            {
                // draw the thunder on top of the boat
                _thunder[0].DrawBasic(spriteBatch, _thunderPositions[0] + new Vector2(cloudOffset, 0), Color.White);

                if (_thunderTransition > 0)
                {
                    var white = MathHelper.Clamp(_thunderTransition * 1.5f, 0, 1);
                    spriteBatch.Draw(Resources.SprWhite, new Rectangle(screenLeft, screenTop, width, height), Color.White * white);

                    // draw the boat
                    var boatWhite = MathHelper.Clamp(1.5f - _thunderTransition * 1.5f, 0, 1);
                    DrawHelper.DrawNormalized(spriteBatch, _spriteOceanBoat, _oceanBoatPosition, Color.White * boatWhite);
                }
            }

            //spriteBatch.Draw(Resources.SprWhite, new Rectangle(-2, 72, 4, 4), Color.Red);
        }

        private void DrawBeach(SpriteBatch spriteBatch)
        {
            if (_currentState != States.StrandFading && _currentState != States.StrandCamera &&
                _currentState != States.StrandMarin && _currentState != States.StrandLogo)
                return;

            var screenLeft = (int)Math.Floor(_cameraCenter.X) - (int)Math.Ceiling(Game1.WindowWidth / (double)_scale / 2.0);
            var screenRight = (int)Math.Ceiling(_cameraCenter.X) + (int)Math.Ceiling(Game1.WindowWidth / (double)_scale / 2.0);
            var screenTop = (int)Math.Floor(_cameraCenter.Y) - (int)Math.Ceiling(Game1.WindowHeight / (double)_scale / 2.0);
            var screenBottom = (int)Math.Ceiling(_cameraCenter.Y) + (int)Math.Ceiling(Game1.WindowHeight / (double)_scale / 2.0);
            var width = screenRight - screenLeft + 2;
            var height = screenBottom - screenTop + 1;

            // draw the sky white
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(
                screenLeft, screenTop, width, -screenTop + 47), new Color(248, 248, 248));

            var mountainOffset = new Vector2((float)(Math.Round(_cameraCenter.X * 0.5f * _scale) / _scale), 0);

            // draw the clouds on the left
            var cloudLeft = -_spriteBackground.ScaledRectangle.Width / 2;
            if (screenLeft < cloudLeft + (int)mountainOffset.X)
                spriteBatch.Draw(_sprCloud,
                    new Rectangle(screenLeft, (int)_cloundLeftPosition.Y, (cloudLeft + (int)mountainOffset.X) - screenLeft, _sprCloud.Height),
                    new Rectangle(screenLeft - (int)mountainOffset.X, 0, (cloudLeft + (int)mountainOffset.X) - screenLeft, _sprCloud.Height),
                    Color.White, 0, new Vector2(-mountainOffset.X % 1, 0), SpriteEffects.None, 0);

            // draw the clouds on the right
            var cloudRight = _spriteBackground.ScaledRectangle.Width / 2;
            if (cloudRight + (int)mountainOffset.X < screenRight)
                spriteBatch.Draw(_sprCloud,
                    new Rectangle(cloudRight + (int)mountainOffset.X, (int)_cloundLeftPosition.Y, screenRight - (cloudRight + (int)mountainOffset.X), _sprCloud.Height),
                    new Rectangle(0, 0, screenRight - (cloudRight + (int)mountainOffset.X), _sprCloud.Height),
                    Color.White, 0, new Vector2(-mountainOffset.X % 1, 0), SpriteEffects.None, 0);

            // draw the top of the mountain
            spriteBatch.Draw(_sprIntro, new Vector2(-_spriteBackground.ScaledRectangle.Width / 2, 0) + mountainOffset, _spriteBackground.ScaledRectangle, Color.White);

            // draw the left side of the mountain
            spriteBatch.Draw(_sprIntro, _mountainLeftPosition + mountainOffset, _spriteMountain.SourceRectangle,
                Color.White, 0, Vector2.Zero, 1, SpriteEffects.FlipHorizontally, 0);

            // draw the right side of the mountain
            spriteBatch.Draw(_sprIntro, _mountainRightPosition + mountainOffset, _spriteMountain.SourceRectangle, Color.White);

            // draw the trees
            var treeOffset = new Vector2(_cameraCenter.X * 0.075f, 0);
            spriteBatch.Draw(_sprWaves,
                new Rectangle(screenLeft, (int)_treePosition.Y, width, _treesRectangle.Height),
                new Rectangle(_treesRectangle.X + screenLeft - (int)treeOffset.X, _treesRectangle.Y, width, _treesRectangle.Height),
                Color.White, 0, new Vector2(1 - treeOffset.X % 1, 0), SpriteEffects.None, 0);

            // draw the strand
            var strandOffset = new Vector2(-_cameraCenter.X * 0.025f, 0);
            spriteBatch.Draw(_sprWaves,
                new Rectangle(screenLeft, (int)_treePosition.Y + _treesRectangle.Height, width, _sandRectangle.Height),
                new Rectangle(_sandRectangle.X + screenLeft - (int)strandOffset.X, _sandRectangle.Y, width, _sandRectangle.Height),
                Color.White, 0, new Vector2(1 - strandOffset.X % 1, 0), SpriteEffects.None, 0);

            // draw the waves
            spriteBatch.Draw(_sprWaves,
                new Rectangle(screenLeft, (int)_wavePosition.Y, width, _waveRectangle.Height),
                new Rectangle(_waveRectangle.X + screenLeft - (int)strandOffset.X, _waveRectangle.Y + _currentFrame * 32, width, _waveRectangle.Height),
                Color.White, 0, new Vector2(1 - strandOffset.X % 1, 0), SpriteEffects.None, 0);

            // draw marin
            _marinAnimator.DrawBasic(spriteBatch, _marinPosition, Color.White);

            // draw link
            _linkAnimator.DrawBasic(spriteBatch, new Vector2(-8, 225), Color.White);

            // draw the logo
            {
                var logoHeight = (int)(_spriteLogo0.SourceRectangle.Height * (MathF.Sin(_logoState * MathF.PI - MathF.PI / 2) * 0.5f + 0.5f));
                logoHeight += logoHeight % 2;

                spriteBatch.Draw(_spriteLogo0.Texture,
                    new Rectangle((int)_logoPosition.X, (int)_logoPosition.Y + _spriteLogo0.SourceRectangle.Height / 2 - logoHeight / 2,
                    _spriteLogo0.SourceRectangle.Width, logoHeight), _spriteLogo0.ScaledRectangle, Color.White);

                var textTransparency = Math.Clamp((_logoState - 0.5f) * 2, 0, 1);
                DrawHelper.DrawNormalized(spriteBatch, _spriteLogo1, _logoPosition, Color.White * textTransparency);
            }

            var lightPosition = _logoPosition + _ligthPosition;

            // draw the light around the logo
            if (_lightAnimation.IsPlaying)
                _lightAnimation.DrawBasic(spriteBatch, lightPosition, Color.White);

            // draw the white forground for the fadein
            if (_strandFadeCount > 0)
            {
                var white = MathHelper.Clamp(_strandFadeCount / StrandFadeTime, 0, 1);
                spriteBatch.Draw(Resources.SprWhite, new Rectangle(screenLeft, screenTop, width, height), Color.White * white);
            }
        }
    }
}
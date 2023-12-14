using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.Editor;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Pages;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Screens;
using ProjectZ.InGame.Things;

#if WINDOWS
using Forms = System.Windows.Forms;
#endif

#if DEBUG
using ProjectZ.InGame.Tests;
#endif

namespace ProjectZ
{
    public class Game1 : Game
    {
        public static GraphicsDeviceManager Graphics;
        public static SpriteBatch SpriteBatch;

        public static UiManager EditorUi = new UiManager();
        public static ScreenManager ScreenManager = new ScreenManager();
        public static PageManager UiPageManager = new PageManager();
        public static GameManager GameManager = new GameManager();
        public static Language LanguageManager = new Language();

        public static GbsPlayer.GbsPlayer GbsPlayer = new GbsPlayer.GbsPlayer();

        public static StopWatchTracker StopWatchTracker = new StopWatchTracker(120);
        public static Random RandomNumber = new Random();
        public static RenderTarget2D MainRenderTarget;

        public static Matrix GetMatrix => Matrix.CreateScale(new Vector3(
            (float)Graphics.PreferredBackBufferWidth / WindowWidth,
            (float)Graphics.PreferredBackBufferHeight / WindowHeight, 0));

        private static float gameScale;
        private static float gameScaleStart;

        public static float GameScaleChange => gameScale / gameScaleStart;

        public static string DebugText;

        public static float TimeMultiplier;
        public static float DeltaTime;
        public static double TotalTime;

        public static double TotalGameTime;
        public static double TotalGameTimeLast;

        public static float DebugTimeScale = 1.0f;

        public static int WindowWidth;
        public static int WindowHeight;
        public static int WindowWidthEnd;
        public static int WindowHeightEnd;
        public static int ScreenScale;
        public static int UiScale;
        public static int UiRtScale;

        public static int RenderWidth;
        public static int RenderHeight;

        public static bool ScaleSettingChanged;

        private bool _wasMinimized;
        private static DoubleAverage _avgTotalMs = new DoubleAverage(30);
        private static DoubleAverage _avgTimeMult = new DoubleAverage(30);
        public static int DebugLightMode;
        public static int DebugBoxMode;
        public static bool DebugMode;
        public static bool ShowDebugText;

        public static double FreezeTime;

        public static bool WasActive;
        public static bool UpdateGame;
        public static bool ForceDialogUpdate;
        public static bool FpsSettingChanged;
        public static bool DebugStepper;
        public static bool EditorMode;

#if WINDOWS
        private static Forms.Form _windowForm;
        private static Forms.FormWindowState _lastWindowState;
#endif

        private static System.Drawing.Rectangle _lastWindowBounds;
        private static System.Drawing.Rectangle _lastWindowRestoreBounds;
        private static int _lastWindowWidth;
        private static int _lastWindowHeight;
        private static bool _isFullscreen;
        private bool _isResizing;

        private static RenderTarget2D _renderTarget1;
        private static RenderTarget2D _renderTarget2;

        private float _blurValue = 0.2f;

        private readonly SimpleFps _fpsCounter = new SimpleFps();
        private Vector2 _debugTextSize;

        private string _lastGameScreen = Values.ScreenNameGame;
        private string _lastEditorScreen = Values.ScreenNameEditor;

        private string _debugLog;

        private int _currentFrameTimeIndex;
        private double[] _debugFrameTimes =
        {
            1000 / 30.0,
            1000 / 60.0,
            1000 / 90.0,
            1000 / 120.0,
            1000 / 144.0,
            1000 / 288.0,
            1
        };

        private string _consoleLine;
        private bool _stopConsoleThread;

        private static bool _finishedLoading;
        private static bool _initRenderTargets;
        public static bool FinishedLoading => _finishedLoading;
        public static bool LoadFirstSave;

#if DEBUG
        private MapTest _mapTest;
        private SequenceTester _sequenceTester;
        private DialogTester _dialogTester;
#endif

        public Game1(bool editorMode, bool loadFirstSave)
        {
#if WINDOWS
            _windowForm = (Forms.Form)Forms.Control.FromHandle(Window.Handle);
            _windowForm.Icon = Properties.Resources.Icon;

            // set the min size of the game
            // not sure why you can not simply set the min size of the client size directly...
            var deltaWidth = _windowForm.Width - _windowForm.ClientSize.Width;
            var deltaHeight = _windowForm.Height - _windowForm.ClientSize.Height;
            _windowForm.MinimumSize = new System.Drawing.Size(Values.MinWidth + deltaWidth, Values.MinHeight + deltaHeight);
#endif

            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Graphics.PreferredBackBufferWidth = 1500;
            Graphics.PreferredBackBufferHeight = 1000;

#if MACOSX
            Window.ClientSizeChanged += ClientSizeChanged;
#endif

            Window.AllowUserResizing = true;
            IsMouseVisible = editorMode;

            EditorMode = editorMode;
            LoadFirstSave = loadFirstSave;

            var thread = new Thread(ConsoleReaderThread);
            thread.Start();
        }

        private void ClientSizeChanged(object sender, EventArgs e)
        {
            OnResize();
            Graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            Graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
        }

        private void ConsoleReaderThread()
        {
            while (true)
            {
                if (_stopConsoleThread)
                    return;

                if (Console.In.Peek() != -1)
                    _consoleLine = Console.ReadLine();

                Thread.Sleep(20);
            }
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            _stopConsoleThread = true;
            GbsPlayer.OnExit();

            base.OnExiting(sender, args);
        }

        protected override void LoadContent()
        {
#if MACOSX
            // not sure how to copy the files in the correct directory...
            Content.RootDirectory += "/bin/MacOSX";
#endif

            // load game settings
            SettingsSaveLoad.LoadSettings();

            // init gbs player; load gbs file
            GbsPlayer.LoadFile(Values.PathContentFolder + "Music/awakening.gbs");
            GbsPlayer.StartThread();

            // start loading the resources that are needed after the intro
            ThreadPool.QueueUserWorkItem(LoadContentThreaded);

            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            // Input Handler
            Components.Add(new InputHandler(this));

            // game control stuff
            ControlHandler.Initialize();

            // load the intro screen + the resources needed for it
            Resources.LoadIntro(Graphics.GraphicsDevice, Content);
            ScreenManager.LoadIntro(Content);

            // toggle fullscreen
            if (GameSettings.IsFullscreen)
            {
                GameSettings.IsFullscreen = false;
                ToggleFullscreen();
            }

            // set the fps settings of the game
            UpdateFpsSettings();

#if WINDOWS
            _windowForm.ResizeBegin += OnResizeBegin;
            _windowForm.Resize += OnResize;
            _windowForm.ResizeEnd += OnResizeEnd;
#endif

#if DEBUG
            SaveCondition.TestCondition();
            _mapTest = new MapTest();
            _sequenceTester = new SequenceTester();
#endif
        }

        private void LoadContentThreaded(Object obj)
        {
            // load resources
            Resources.LoadTextures(Graphics.GraphicsDevice, Content);
            Resources.LoadSounds(Content);

            GameManager.Load(Content);

            GameObjectTemplates.SetUpGameObjects();

            ScreenManager.Load(Content);

            // load the language files
            LanguageManager.Load();

            UiPageManager.Load();

            if (EditorMode)
                SetUpEditorUi();

#if DEBUG
            _dialogTester = new DialogTester();
#endif

            _finishedLoading = true;
        }

        private void UpdateConsoleInput()
        {
            if (_consoleLine == null)
                return;

            // open file in map editor
            if (_consoleLine.Contains(".map"))
            {
                SaveLoadMap.EditorLoadMap(_consoleLine, Game1.GameManager.MapManager.CurrentMap);
            }
            // open file in animation editor
            else if (_consoleLine.Contains(".ani"))
            {
                var animationScreen = (AnimationScreen)ScreenManager.GetScreen(Values.ScreenNameEditorAnimation);
                animationScreen.EditorLoadAnimation(_consoleLine);
            }
            // open file in sprite atlas editor
            else if (_consoleLine.Contains(".png"))
            {
                var spriteAtlasScreen = (SpriteAtlasScreen)ScreenManager.GetScreen(Values.ScreenNameSpriteAtlasEditor);
                spriteAtlasScreen.LoadSpriteEditor(_consoleLine);
            }

            _consoleLine = null;
        }

        protected override void Update(GameTime gameTime)
        {
            WasActive = IsActive;

            // mute the music if the window is not focused
            //if (!IsActive)
            //    GbsPlayer.SetVolumeMultiplier(0);
            //else
            //    GbsPlayer.SetVolumeMultiplier(1);

            UpdateConsoleInput();

            // SetTransparency _fpsCounter counter
            _fpsCounter.Update(gameTime);

            // toggle fullscreen
            if (InputHandler.KeyDown(Keys.LeftAlt) && InputHandler.KeyPressed(Keys.Enter))
            {
                ToggleFullscreen();
                InputHandler.ResetInputState();
                SettingsSaveLoad.SaveSettings();
            }

            if(_finishedLoading && !_initRenderTargets)
            {
                _initRenderTargets = true;

                // @HACK to update the rendertargets
                WindowWidth = 0;
                WindowHeightEnd = 0;
            }

            // check if the window is resized
            if (WindowWidth != Window.ClientBounds.Width ||
                WindowHeight != Window.ClientBounds.Height)
                OnResize();

            UpdateRenderTargets();

            if (FpsSettingChanged)
            {
                UpdateFpsSettings();
                FpsSettingChanged = false;
            }

            if (ScaleSettingChanged)
            {
                ScaleSettingChanged = false;
                OnUpdateScale();
            }

            ControlHandler.Update();

            if (EditorMode && InputHandler.KeyPressed(Values.DebugToggleDebugText))
                ShowDebugText = !ShowDebugText;

            if (!DebugStepper)
            {
                TimeMultiplier = gameTime.ElapsedGameTime.Ticks / 166667f * DebugTimeScale;
                TotalGameTimeLast = TotalGameTime;

                // limit the game time so that it slows down if the steps are bigger than they would be for 30fps
                // if the timesteps get too big it would be hard (wast of time) to make the logic still function 100% correctly
                if (TimeMultiplier > 2.0f)
                {
                    TimeMultiplier = 2.0f;
                    DeltaTime = (TimeMultiplier * 1000.0f) / 60.0f;
                    TotalTime += (TimeMultiplier * 1000.0) / 60.0;
                    DebugText += "\nLow Framerate";

                    if (UpdateGame)
                        TotalGameTime += (TimeMultiplier * 1000.0) / 60.0;
                }
                else
                {
                    DeltaTime = (float)gameTime.ElapsedGameTime.TotalMilliseconds * DebugTimeScale;
                    TotalTime += gameTime.ElapsedGameTime.TotalMilliseconds * DebugTimeScale;
                    if (UpdateGame)
                        TotalGameTime += gameTime.ElapsedGameTime.TotalMilliseconds * DebugTimeScale;
                }
            }

            if (_finishedLoading)
            {

                if (EditorMode)
                {
                    // update the ui
                    // need to be at the first place to be able to block input from the screen
                    EditorUi.Update();

                    EditorUpdate(gameTime);
                }

                EditorUi.CurrentScreen = "";

                // update the game ui
                UiPageManager.Update(gameTime);
            }

#if DEBUG
            _mapTest.Update();
            _sequenceTester.Update();
            if (_finishedLoading)
                _dialogTester.Update();
#endif

            // update the screen manager
            UpdateGame = true;
            if (!DebugStepper || InputHandler.KeyPressed(Keys.M))
                ScreenManager.Update(gameTime);

            if (_finishedLoading)
            {
                DebugText += _fpsCounter.Msg;

                _avgTotalMs.AddValue(gameTime.ElapsedGameTime.TotalMilliseconds);
                _avgTimeMult.AddValue(TimeMultiplier);
                DebugText += $"\ntotal ms:      {_avgTotalMs.Average,6:N3}" +
                             $"\ntime mult:     {_avgTimeMult.Average,6:N3}" +
                             $"\ntime scale:    {DebugTimeScale}" +
                             $"\ntime:          {TotalGameTime}";

                DebugText += "\nHistory Enabled: " + GameManager.SaveManager.HistoryEnabled + "\n";
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (!_finishedLoading)
            {
                ScreenManager.Draw(SpriteBatch);
                return;
            }

            _fpsCounter.CountDraw();

            ScreenManager.DrawRT(SpriteBatch);

            Graphics.GraphicsDevice.SetRenderTarget(MainRenderTarget);
            GraphicsDevice.Clear(Color.CadetBlue);

            // draw the current screen
            ScreenManager.Draw(SpriteBatch);

            BlurImage();

            {
                Graphics.GraphicsDevice.SetRenderTarget(null);

                SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap);

                //draw the original image
                SpriteBatch.Draw(MainRenderTarget, new Rectangle(0, 0, MainRenderTarget.Width, MainRenderTarget.Height), Color.White);

                SpriteBatch.End();
            }

            {
                Resources.BlurEffect.Parameters["sprBlur"].SetValue(_renderTarget2);
                Resources.RoundedCornerBlurEffect.Parameters["sprBlur"].SetValue(_renderTarget2);

                SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.AnisotropicClamp, null, null, Resources.RoundedCornerBlurEffect, GetMatrix);

                // blurred ui parts
                EditorUi.DrawBlur(SpriteBatch);

                // blured stuff
                GameManager.InGameOverlay.InGameHud.DrawBlur(SpriteBatch);

                // background for the debug text
                DebugTextBackground();

                SpriteBatch.End();
            }

            {
                // draw the top part
                SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, GetMatrix);

                // draw the ui part
                EditorUi.Draw(SpriteBatch);

                // draw the game ui
                UiPageManager.Draw(SpriteBatch);

                // draw the screen tops
                ScreenManager.DrawTop(SpriteBatch);

                // draw the debug text
                DrawDebugText();
                DebugText = "";

#if DEBUG
                if (GameManager.SaveManager.HistoryEnabled)
                    SpriteBatch.Draw(Resources.SprWhite, new Rectangle(0, WindowHeight - 6, WindowWidth, 6), Color.Red);
#endif

                SpriteBatch.End();
            }

            base.Draw(gameTime);
        }

        private void BlurImage()
        {
            Resources.BlurEffectH.Parameters["pixelX"].SetValue(1.0f / _renderTarget1.Width);
            Resources.BlurEffectV.Parameters["pixelY"].SetValue(1.0f / _renderTarget1.Height);

            var mult0 = _blurValue;
            var mult1 = (1 - _blurValue * 2) / 2;
            Resources.BlurEffectH.Parameters["mult0"].SetValue(mult0);
            Resources.BlurEffectH.Parameters["mult1"].SetValue(mult1);
            Resources.BlurEffectV.Parameters["mult0"].SetValue(mult0);
            Resources.BlurEffectV.Parameters["mult1"].SetValue(mult1);

            // resize
            Graphics.GraphicsDevice.SetRenderTarget(_renderTarget2);
            SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.AnisotropicClamp, null, null, null, null);
            SpriteBatch.Draw(MainRenderTarget, new Rectangle(0, 0, _renderTarget2.Width, _renderTarget2.Height), Color.White);
            SpriteBatch.End();

            for (var i = 0; i < 2; i++)
            {
                // v blur
                Graphics.GraphicsDevice.SetRenderTarget(_renderTarget1);
                SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.AnisotropicClamp, null, null, Resources.BlurEffectV, null);
                SpriteBatch.Draw(_renderTarget2, Vector2.Zero, Color.White);
                SpriteBatch.End();

                // h blur
                Graphics.GraphicsDevice.SetRenderTarget(_renderTarget2);
                SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.AnisotropicClamp, null, null, Resources.BlurEffectH, null);
                SpriteBatch.Draw(_renderTarget1, Vector2.Zero, Color.White);
                SpriteBatch.End();
            }
        }

        private void SetUpEditorUi()
        {
            var strScreen = $"{Values.EditorUiObjectEditor}:" +
                            $"{Values.EditorUiObjectSelection}:" +
                            $"{Values.EditorUiTileEditor}:" +
                            $"{Values.EditorUiTileSelection}:" +
                            $"{Values.EditorUiDigTileEditor}:" +
                            $"{Values.EditorUiMusicTileEditor}:" +
                            $"{Values.EditorUiTileExtractor}:" +
                            $"{Values.EditorUiTilesetEditor}:" +
                            $"{Values.EditorUiAnimation}:" +
                            $"{Values.EditorUiSpriteAtlas}";

            EditorUi.AddElement(new UiRectangle(new Rectangle(0, 0, WindowWidth, Values.ToolBarHeight),
                "top", strScreen, Values.ColorBackgroundDark, Color.White,
                ui => { ui.Rectangle = new Rectangle(0, 0, WindowWidth, Values.ToolBarHeight); }));

            var pos = 0;
            EditorUi.AddElement(new UiButton(new Rectangle(0, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Editor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = ScreenManager.CurrentScreenId == Values.ScreenNameEditor; },
                element => { ScreenManager.ChangeScreen(Values.ScreenNameEditor); }));

            EditorUi.AddElement(new UiButton(new Rectangle(pos += 205, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Tileset Editor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = ScreenManager.CurrentScreenId == Values.ScreenNameEditorTileset; },
                element => { ScreenManager.ChangeScreen(Values.ScreenNameEditorTileset); }));

            EditorUi.AddElement(new UiButton(new Rectangle(pos += 205, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Tileset Extractor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = ScreenManager.CurrentScreenId == Values.ScreenNameEditorTilesetExtractor; },
                element => { ScreenManager.ChangeScreen(Values.ScreenNameEditorTilesetExtractor); }));

            EditorUi.AddElement(new UiButton(new Rectangle(pos += 205, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Animation Editor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = ScreenManager.CurrentScreenId == Values.ScreenNameEditorAnimation; },
                element => { ScreenManager.ChangeScreen(Values.ScreenNameEditorAnimation); }));

            EditorUi.AddElement(new UiButton(new Rectangle(pos += 205, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Sprite Atlas Editor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = ScreenManager.CurrentScreenId == Values.ScreenNameSpriteAtlasEditor; },
                element => { ScreenManager.ChangeScreen(Values.ScreenNameSpriteAtlasEditor); }));
        }

        private void EditorUpdate(GameTime gameTime)
        {
            if (InputHandler.KeyPressed(Keys.N))
                DebugStepper = !DebugStepper;
            if (ScreenManager.CurrentScreenId != Values.ScreenNameGame)
                DebugStepper = false;

            // debug step
            if (DebugStepper && InputHandler.KeyPressed(Keys.M))
            {
                TimeMultiplier = TargetElapsedTime.Ticks / 166667f;
                DeltaTime = (float)TargetElapsedTime.TotalMilliseconds;

                TotalGameTimeLast = TotalTime;
                TotalTime += TargetElapsedTime.Milliseconds;
                TotalGameTime += TargetElapsedTime.Milliseconds;
            }

            // reload all objects
            if (InputHandler.KeyPressed(Keys.Q))
                GameManager.MapManager.ReloadMap();

            // slow down or speed up the game
            if (InputHandler.KeyPressed(Keys.Add))
                DebugTimeScale += 0.125f;
            if (InputHandler.KeyPressed(Keys.Subtract) && DebugTimeScale > 0)
                DebugTimeScale -= 0.125f;

            if (InputHandler.KeyPressed(Values.DebugShadowKey))
                GameSettings.EnableShadows = !GameSettings.EnableShadows;

            if (ScreenManager.CurrentScreenId != Values.ScreenNameEditor &&
                ScreenManager.CurrentScreenId != Values.ScreenNameEditorTileset &&
                ScreenManager.CurrentScreenId != Values.ScreenNameEditorTilesetExtractor &&
                ScreenManager.CurrentScreenId != Values.ScreenNameEditorAnimation &&
                ScreenManager.CurrentScreenId != Values.ScreenNameSpriteAtlasEditor)
            {
                if (InputHandler.KeyPressed(Keys.D0))
                    TriggerFpsSettings();

                if (InputHandler.KeyPressed(Keys.D1))
                {
                    _currentFrameTimeIndex--;
                    if (_currentFrameTimeIndex < 0)
                        _currentFrameTimeIndex = _debugFrameTimes.Length - 1;
                    TargetElapsedTime = new TimeSpan((long)Math.Ceiling(_debugFrameTimes[_currentFrameTimeIndex] * 10000));
                }

                if (InputHandler.KeyPressed(Keys.D2))
                {
                    _currentFrameTimeIndex = (_currentFrameTimeIndex + 1) % _debugFrameTimes.Length;
                    TargetElapsedTime = new TimeSpan((long)Math.Ceiling(_debugFrameTimes[_currentFrameTimeIndex] * 10000));
                }
            }

            if (InputHandler.KeyPressed(Keys.Escape) || InputHandler.KeyPressed(Keys.OemPeriod))
            {
                // open the editor
                if (ScreenManager.CurrentScreenId != Values.ScreenNameEditor &&
                    ScreenManager.CurrentScreenId != Values.ScreenNameEditorTileset &&
                    ScreenManager.CurrentScreenId != Values.ScreenNameEditorTilesetExtractor &&
                    ScreenManager.CurrentScreenId != Values.ScreenNameEditorAnimation &&
                    ScreenManager.CurrentScreenId != Values.ScreenNameSpriteAtlasEditor)
                {
                    UiPageManager.PopAllPages(PageManager.TransitionAnimation.TopToBottom, PageManager.TransitionAnimation.TopToBottom);

                    _lastGameScreen = ScreenManager.CurrentScreenId;
                    ScreenManager.ChangeScreen(_lastEditorScreen);
                }
                // go back to the game
                else
                {
                    _lastEditorScreen = ScreenManager.CurrentScreenId;
                    ScreenManager.ChangeScreen(_lastGameScreen);

                    // set the player position
                    var editorScreen = (MapEditorScreen)ScreenManager.GetScreen(Values.ScreenNameEditor);

                    if (_lastEditorScreen == Values.ScreenNameEditor)
                        MapManager.ObjLink.SetPosition(new Vector2(
                            editorScreen.MousePixelPosition.X,
                            editorScreen.MousePixelPosition.Y));
                }
            }

            if (InputHandler.KeyPressed(Values.DebugToggleDebugModeKey))
                DebugMode = !DebugMode;

            if (InputHandler.KeyPressed(Values.DebugBox))
                DebugBoxMode = (DebugBoxMode + 1) % 6;

            // save/load
            if (InputHandler.KeyPressed(Values.DebugSaveKey))
            {
                MapManager.ObjLink.SaveMap = GameManager.MapManager.CurrentMap.MapName;
                MapManager.ObjLink.SavePosition = MapManager.ObjLink.EntityPosition.Position;
                MapManager.ObjLink.SaveDirection = MapManager.ObjLink.Direction;

                SaveGameSaveLoad.SaveGame(GameManager);
                GameManager.InGameOverlay.InGameHud.ShowSaveIcon();
            }
            if (InputHandler.KeyPressed(Values.DebugLoadKey))
                GameManager.LoadSaveFile(GameManager.SaveSlot);

            // save the debug log to the clipboard
            if (InputHandler.KeyDown(Keys.H))
                _debugLog += "\n" + DebugText;
#if WINDOWS
            else if (InputHandler.KeyReleased(Keys.H))
                Forms.Clipboard.SetText(_debugLog);
#endif
        }

        private void TriggerFpsSettings()
        {
            if (!IsFixedTimeStep)
            {
                IsFixedTimeStep = true;
                Graphics.SynchronizeWithVerticalRetrace = false;
            }
            else
            {
                IsFixedTimeStep = false;
                Graphics.SynchronizeWithVerticalRetrace = true;
            }

            Graphics.ApplyChanges();
        }

        public static void SwitchFullscreenWindowedSetting()
        {
            // switch from hardware fullscreen to borderless windows
            if (!GameSettings.BorderlessWindowed && Graphics.IsFullScreen ||
                GameSettings.BorderlessWindowed && _isFullscreen)
            {
                ToggleFullscreen();
                GameSettings.BorderlessWindowed = !GameSettings.BorderlessWindowed;
                ToggleFullscreen();
            }
            else
            {
                GameSettings.BorderlessWindowed = !GameSettings.BorderlessWindowed;
            }
        }

        public static void ToggleFullscreen()
        {
#if WINDOWS
            GameSettings.IsFullscreen = !GameSettings.IsFullscreen;

            var screenBounds = System.Windows.Forms.Screen.GetBounds(_windowForm);

            if (!GameSettings.BorderlessWindowed)
            {
                if (!Graphics.IsFullScreen)
                {
                    _lastWindowWidth = Graphics.PreferredBackBufferWidth;
                    _lastWindowHeight = Graphics.PreferredBackBufferHeight;

                    _lastWindowRestoreBounds = _windowForm.RestoreBounds;

                    Graphics.PreferredBackBufferWidth = screenBounds.Width;
                    Graphics.PreferredBackBufferHeight = screenBounds.Height;

                    _lastWindowState = _windowForm.WindowState;
                }
                else
                {
                    if (_lastWindowState != Forms.FormWindowState.Maximized)
                    {
                        Graphics.PreferredBackBufferWidth = _lastWindowWidth;
                        Graphics.PreferredBackBufferHeight = _lastWindowHeight;
                    }
                }

                Graphics.ToggleFullScreen();

                if (_lastWindowState == Forms.FormWindowState.Maximized)
                {
                    // restore the window size of the normal sized window
                    _windowForm.Bounds = _lastWindowRestoreBounds;

                    _windowForm.WindowState = _lastWindowState;
                }
            }
            else
            {
                _isFullscreen = !_isFullscreen;

                // change to fullscreen
                if (_isFullscreen)
                {
                    _lastWindowState = _windowForm.WindowState;
                    _lastWindowBounds = _windowForm.Bounds;

                    _windowForm.FormBorderStyle = Forms.FormBorderStyle.None;
                    _windowForm.WindowState = Forms.FormWindowState.Normal;
                    _windowForm.Bounds = screenBounds;
                }
                else
                {
                    _windowForm.FormBorderStyle = Forms.FormBorderStyle.Sizable;

                    if (_lastWindowState == Forms.FormWindowState.Maximized)
                    {
                        // this is set to not loose the old state because fullscreen and windowed are both using the "Normal" state
                        _windowForm.Bounds = _lastWindowRestoreBounds;

                        _windowForm.WindowState = _lastWindowState;
                    }
                    else
                    {
                        _windowForm.WindowState = _lastWindowState;
                        _windowForm.Bounds = _lastWindowBounds;
                    }
                }
            }
#endif
        }

        public void DebugTextBackground()
        {
            if (!ShowDebugText)
                return;

            _debugTextSize = Resources.GameFont.MeasureString(DebugText);

            // draw the background
            SpriteBatch.Draw(_renderTarget2, new Rectangle(0, 0,
                (int)(_debugTextSize.X * 2) + 20, (int)(_debugTextSize.Y * 2) + 20), Color.White);
        }

        public void DrawDebugText()
        {
            if (!ShowDebugText)
                return;

            SpriteBatch.Draw(Resources.SprWhite, new Rectangle(0, 0,
                    (int)(_debugTextSize.X * 2) + 20, (int)(_debugTextSize.Y * 2) + 20), Color.Black * 0.75f);

            SpriteBatch.DrawString(Resources.GameFont, DebugText, new Vector2(10), Color.White,
                0, Vector2.Zero, new Vector2(2f), SpriteEffects.None, 0);
        }

        public void UpdateFpsSettings()
        {
            IsFixedTimeStep = false;
            Graphics.SynchronizeWithVerticalRetrace = GameSettings.LockFps;
            Graphics.ApplyChanges();
        }

        private void OnResizeBegin(object? sender, EventArgs e)
        {
            _isResizing = true;
            gameScaleStart = gameScale;
        }

        private void OnResize(object? sender, EventArgs e)
        {
#if WINDOWS
            // save the restore bounds when going into borderless fullscreen mode from an maximized state
            if (_isFullscreen && _windowForm.WindowState == Forms.FormWindowState.Maximized)
                _lastWindowRestoreBounds = _windowForm.RestoreBounds;

            // minimize the fullscreen window
            if (!GameSettings.BorderlessWindowed && Graphics.IsFullScreen && _windowForm.WindowState == Forms.FormWindowState.Minimized && !_wasMinimized)
            {
                _wasMinimized = true;

                Graphics.ToggleFullScreen();
                _windowForm.WindowState = Forms.FormWindowState.Minimized;
            }
            // reopen the fullscreen window
            if (!GameSettings.BorderlessWindowed && _windowForm.WindowState == Forms.FormWindowState.Normal && _wasMinimized)
            {
                _wasMinimized = false;
                ToggleFullscreen();
            }
#endif
        }

        private void OnResizeEnd(object? sender, EventArgs e)
        {
            _isResizing = false;
            gameScaleStart = gameScale;
        }

        private void OnResize()
        {
            if (Window.ClientBounds.Width <= 0 &&
                Window.ClientBounds.Height <= 0)
                return;

            WindowWidth = Window.ClientBounds.Width;
            WindowHeight = Window.ClientBounds.Height;

            OnUpdateScale();
        }

        private void OnUpdateScale()
        {
            // scale of the game
            ScreenScale = MathHelper.Clamp(Math.Min(WindowWidth / Values.MinWidth, WindowHeight / Values.MinHeight), 1, 25);

            // float scale
            gameScale = MathHelper.Clamp(Math.Min(WindowWidth / (float)Values.MinWidth, WindowHeight / (float)Values.MinHeight), 1, 25);

            // autoscale or size set in the menu
            MapManager.Camera.Scale = GameSettings.GameScale == 11 ? MathF.Ceiling(gameScale) : GameSettings.GameScale;
            if (MapManager.Camera.Scale < 1)
            {
                MapManager.Camera.Scale = 1 / (2 - MapManager.Camera.Scale);
                GameManager.SetGameScale(1);
            }
            else
            {
                GameManager.SetGameScale(GameSettings.GameScale == 11 ? gameScale : GameSettings.GameScale);
            }

            UiScale = GameSettings.UiScale == 0 ? ScreenScale : MathHelper.Clamp(GameSettings.UiScale, 1, ScreenScale);

            // update the ui manager
            EditorUi.SizeChanged();

            ScreenManager.OnResize(WindowWidth, WindowHeight);
        }

        private void UpdateRenderTargets()
        {
            if (_isResizing ||
                WindowWidthEnd == WindowWidth && WindowHeightEnd == WindowHeight)
                return;

            UiRtScale = UiScale;

            WindowWidthEnd = WindowWidth;
            WindowHeightEnd = WindowHeight;

            UpdateRenderTargetSizes(WindowWidth, WindowHeight);

            ScreenManager.OnResizeEnd(WindowWidth, WindowHeight);
        }

        private void UpdateRenderTargetSizes(int width, int height)
        {
            // @TODO: width must be bigger than 0

            MainRenderTarget?.Dispose();
            MainRenderTarget = new RenderTarget2D(Graphics.GraphicsDevice, width, height);
            Resources.BlurEffect.Parameters["width"].SetValue(width);
            Resources.BlurEffect.Parameters["height"].SetValue(height);

            Resources.RoundedCornerBlurEffect.Parameters["textureWidth"].SetValue(width);
            Resources.RoundedCornerBlurEffect.Parameters["textureHeight"].SetValue(height);

            // update the blur rendertargets
            var blurScale = MathHelper.Clamp(MapManager.Camera.Scale / 2, 1, 10);
            var blurRtWidth = (int)(width / blurScale);
            var blurRtHeight = (int)(height / blurScale);

            _renderTarget1?.Dispose();
            _renderTarget2?.Dispose();

            _renderTarget1 = new RenderTarget2D(Graphics.GraphicsDevice, blurRtWidth, blurRtHeight);
            _renderTarget2 = new RenderTarget2D(Graphics.GraphicsDevice, blurRtWidth, blurRtHeight);
        }
    }
}

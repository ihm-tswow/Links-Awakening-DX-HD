using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Overlay;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.InGame.Things
{
    public class GameManager
    {
        public struct MiniMapTile
        {
            public int TileIndex;
            public bool DiscoveryState;

            public int HintTileIndex;
            public string HintKey;
        }

        public struct MiniMapOverrides
        {
            public string SaveKey;
            public int PosX;
            public int PosY;
            public int TileIndex;
        }

        public class MiniMap
        {
            public int OffsetX;
            public int OffsetY;

            public MiniMapTile[,] Tiles;

            public MiniMapOverrides[] Overrides;
        }

        private class PlayingSoundEffect
        {
            public bool LowerMusicVolume;
            
            public float Volume;
            public double EndTime;

            public SoundEffectInstance Instance;
        }

        // _activeRenderTarget == null ???
        public Matrix GetMatrix => Matrix.CreateScale(new Vector3(
            (float)_activeRenderTarget.Width / (int)(Game1.WindowWidth * _scaleMultiplier),
            (float)_activeRenderTarget.Height / (int)(Game1.WindowHeight * _scaleMultiplier), 0));

        public int CurrentRenderWidth;
        public int CurrentRenderHeight;
        public float CurrentRenderScale;

        public int BlurRenderTargetWidth => (int)(Game1.RenderWidth / MapManager.Camera.Scale / 2) + 8;
        public int BlurRenderTargetHeight => (int)(Game1.RenderHeight / MapManager.Camera.Scale / 2) + 8;

        public int SideBlurRenderTargetWidth => BlurRenderTargetWidth * 2;
        public int SideBlurRenderTargetHeight => BlurRenderTargetHeight * 2;

        public MapManager MapManager = new MapManager();
        public OverlayManager InGameOverlay = new OverlayManager();
        public SaveManager SaveManager = new SaveManager();
        public ItemManager ItemManager = new ItemManager();

        public float ForestColorState;
        public bool UseShockEffect;

        public const int EquipmentSlots = 12;
        public GameItemCollected[] Equipment = new GameItemCollected[EquipmentSlots];
        public List<GameItemCollected> CollectedItems = new List<GameItemCollected>();

        // sound effects that are currently playing
        private Dictionary<string, PlayingSoundEffect> CurrentSoundEffects = new Dictionary<string, PlayingSoundEffect>();

        // dungeon maps
        public Dictionary<string, MiniMap> DungeonMaps = new Dictionary<string, MiniMap>();

        public Dictionary<Type, GameSystem> GameSystems = new Dictionary<Type, GameSystem>();

        public Point PlayerDungeonPosition;

        // can be null if the player never left the house in the beginning
        public Point? PlayerMapPosition;

        public bool[,] MapVisibility;

        public string SaveName = "Link";

        public float DrawPlayerOnTopPercentage;

        public bool FreezeWorldAroundPlayer;

        // save game data
        public string LoadedMap;
        public int SavePositionX;
        public int SavePositionY;
        public int SaveDirection;
        public int SaveSlot;

        private float _shakeCountX;
        private float _shakeCountY;
        private float _shakeSpeedX;
        private float _shakeSpeedY;
        private int _maxOffsetX;
        private int _maxOffsetY;

        public int DeathCount;

        public int MaxHearths = 3;
        public int CurrentHealth = 4 * 3;

        public int SwordLevel;
        public int ShieldLevel;
        public int StoneGrabberLevel;

        public bool HasMagnifyingLens;

        public bool DebugMode;

        // 0: marin
        // 1: mambo
        // 2: frog
        public int[] OcarinaSongs = new int[3];
        public int SelectedOcarinaSong = 0;

        public static int CloakGreen = 0;
        public static int CloakBlue = 1;
        public static int CloakRed = 2;

        public int CloakType;
        public Color CloakColor => ItemDrawHelper.CloakColors[CloakType];

        public bool GuardianAcornIsActive;
        public int GuardianAcornCount;
        public int GuardianAcornDamageCount;

        public bool PieceOfPowerIsActive;
        public int PieceOfPowerCount;
        public int PieceOfPowerDamageCount;

        private readonly Dictionary<string, List<DialogPath>> _dialogPaths = new Dictionary<string, List<DialogPath>>();
        private DialogPath _currentDialogPath;
        private readonly Queue<string> _dialogPathQueue = new Queue<string>();

        private RenderTarget2D _activeRenderTarget;
        private RenderTarget2D _inactiveRenderTarget1;
        private RenderTarget2D _inactiveRenderTarget2;
        private RenderTarget2D _shadowRenderTarget;
        private RenderTarget2D _shadowRenderTargetBlur;
        private RenderTarget2D _lightRenderTarget;

        // used for the blured tile layer; use usage of the render targets should probably be optimized
        public RenderTarget2D TempRT0;
        public RenderTarget2D TempRT1;
        public RenderTarget2D TempRT2;

        public float _scaleMultiplier;
        private int _currentDialogPathState;

        // 0: map music, 1: guardian acorn/piece of power, 2: maria singing
        private const int MusicChannels = 3;
        private int[] _musicArray = new int[MusicChannels];
        // counters used to stop music
        private float[] _musicCounter = new float[MusicChannels];

        public GameManager()
        {
            ResetMusic();

            GameSystems.Add(typeof(MapTransitionSystem), new MapTransitionSystem(MapManager));
            GameSystems.Add(typeof(GameOverSystem), new GameOverSystem());
            GameSystems.Add(typeof(EndingSystem), new EndingSystem());
            GameSystems.Add(typeof(MapShowSystem), new MapShowSystem());
        }

        public void Load(ContentManager content)
        {
            ItemDrawHelper.Load();

            InGameOverlay.Load(content);
            MapManager.Load();
            ItemManager.Load();

            // load the dialog paths
            DialogPathLoader.LoadScripts(Values.PathContentFolder + "scripts.zScript", _dialogPaths);
        }

        public void OnLoad()
        {
            InGameOverlay.OnLoad();

            _currentDialogPath = null;
            _dialogPathQueue.Clear();

            // this leads to the music not starting after switching from edit mode and reloading objects
            // not so sure if this is a problem or not
            ResetMusic();

            foreach (var gameSystem in GameSystems)
                gameSystem.Value.OnLoad();
        }

        public void UpdateGame()
        {
            InGameOverlay.Update();

            UpdateSoundEffects();

            UpdateMusic();

            ItemDrawHelper.Update();

            // update the dialogs; forced dialog update is used in sequences where the dialog should be updated but not the normal game
            // needs to come after the ingame overlay update because Game1.UpdateGame can be set to false by it
            if (Game1.UpdateGame || Game1.ForceDialogUpdate)
            {
                UpdateDialog();
                Game1.ForceDialogUpdate = false;
            }

            if (Game1.UpdateGame && Game1.TotalGameTime > Game1.FreezeTime)
            {
                // update the game-systems
                foreach (var gameSystem in GameSystems)
                    gameSystem.Value.Update();

                if (Game1.UpdateGame)
                    MapManager.Update(false);
            }
            else if (Game1.GameManager.InGameOverlay.UpdateCameraAndAnimation())
            {
                MapManager.Update(true);
                MapManager.UpdateAnimation();
            }

            // update screen shake
            if (Game1.UpdateGame)
                UpdateShake();
        }

        public void DrawGame(SpriteBatch spriteBatch)
        {
            if (GameSettings.EnableShadows && MapManager.CurrentMap.UseShadows && !UseShockEffect)
            {
                // render the shadows
                RenderShadows(spriteBatch);

                Resources.BlurEffectH.Parameters["pixelX"].SetValue(1.0f / _shadowRenderTarget.Width);
                Resources.BlurEffectV.Parameters["pixelY"].SetValue(1.0f / _shadowRenderTarget.Height);

                Resources.BlurEffectH.Parameters["mult0"].SetValue(0.35f);
                Resources.BlurEffectH.Parameters["mult1"].SetValue(0.15f);
                Resources.BlurEffectV.Parameters["mult0"].SetValue(0.35f);
                Resources.BlurEffectV.Parameters["mult1"].SetValue(0.15f);

                // v blur
                Game1.Graphics.GraphicsDevice.SetRenderTarget(_shadowRenderTargetBlur);
                Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);
                spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.AnisotropicClamp, null, null, Resources.BlurEffectV, null);
                spriteBatch.Draw(_shadowRenderTarget, Vector2.Zero, Color.White);
                spriteBatch.End();

                // h blur
                Game1.Graphics.GraphicsDevice.SetRenderTarget(_shadowRenderTarget);
                Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);
                spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.AnisotropicClamp, null, null, Resources.BlurEffectH, null);
                spriteBatch.Draw(_shadowRenderTargetBlur, Vector2.Zero, Color.White);
                spriteBatch.End();

                MapManager.CurrentMap.Objects.ShadowTexture = _shadowRenderTarget;
            }

            ChangeRenderTarget();
            Game1.Graphics.GraphicsDevice.Clear(Color.Black);

            // draw the map
            MapManager.Draw(spriteBatch);

            if (UseShockEffect)
            {
                ChangeRenderTarget();

                var usedShader = MapManager.CurrentMap.UseLight ? Resources.ShockShader1 : Resources.ShockShader0;
                ObjectManager.SetSpriteShader(usedShader);

                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, usedShader.Effect);
                spriteBatch.Draw(_inactiveRenderTarget1, Vector2.Zero, Color.White);
                spriteBatch.End();
            }

            // @Move into the World class?
            if (MapManager.CurrentMap.UseLight && !UseShockEffect)
            {
                // draw the lights
                ChangeRenderTarget();
                MapManager.DrawLight(spriteBatch);

                // combine the light with the game
                ChangeRenderTarget();
                _lightRenderTarget = _inactiveRenderTarget1;

                Game1.Graphics.GraphicsDevice.Clear(Color.Black);
                Resources.LightShader.Parameters["sprLight"].SetValue(_lightRenderTarget);
                Resources.LightShader.Parameters["lightState"].SetValue(MapManager.CurrentMap.LightState);
                Resources.LightShader.Parameters["mode"].SetValue(0);
                Resources.LightShader.Parameters["width"].SetValue(_lightRenderTarget.Width);
                Resources.LightShader.Parameters["height"].SetValue(_lightRenderTarget.Height);

                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.AnisotropicClamp, null, null, Resources.LightShader);
                spriteBatch.Draw(_inactiveRenderTarget2, Vector2.Zero, Color.White);
                spriteBatch.End();
            }

            // update the game-systems
            foreach (var gameSystem in GameSystems)
                gameSystem.Value.Draw(spriteBatch);

            if (MapManager.CurrentMap.UseLight && !UseShockEffect && DrawPlayerOnTopPercentage > 0 && _lightRenderTarget != null)
            {
                Resources.LightShader.Parameters["sprLight"].SetValue(_lightRenderTarget);
                Resources.LightShader.Parameters["lightState"].SetValue(DrawPlayerOnTopPercentage);
                Resources.LightShader.Parameters["mode"].SetValue(1);
                Resources.LightShader.Parameters["width"].SetValue(_lightRenderTarget.Width);
                Resources.LightShader.Parameters["height"].SetValue(_lightRenderTarget.Height);

                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, Resources.LightShader, MapManager.Camera.TransformMatrix);
                MapManager.ObjLink.DrawTransition(spriteBatch);
                spriteBatch.End();
            }
            else if (DrawPlayerOnTopPercentage > 0)
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null, null, MapManager.Camera.TransformMatrix);
                MapManager.ObjLink.DrawTransition(spriteBatch);
                spriteBatch.End();
            }

            // draw the output of the light and the dark shader passes
            ChangeRenderTarget();
            Game1.Graphics.GraphicsDevice.SetRenderTarget(Game1.MainRenderTarget);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.AnisotropicWrap);

            spriteBatch.Draw(_inactiveRenderTarget1, new Rectangle(0, 0, Game1.Graphics.PreferredBackBufferWidth, Game1.Graphics.PreferredBackBufferHeight), Color.White);

            // debug stuff
            MapManager.Camera.Draw(spriteBatch);

            spriteBatch.End();
        }

        public void StartDialogPath(string dialogKey)
        {
            _dialogPathQueue.Enqueue(dialogKey);
        }

        public void AddFirstDialogPath(string dialogKey)
        {
            // @HACK
            var items = _dialogPathQueue.ToArray();
            _dialogPathQueue.Clear();

            _dialogPathQueue.Enqueue(dialogKey);
            foreach (var item in items)
                _dialogPathQueue.Enqueue(item);
        }

        // @TODO: this should probably be removed and replaced with StartDialogPath
        public void StartDialog(string dialogKey)
        {
            InGameOverlay.TextboxOverlay.StartDialog(Game1.LanguageManager.GetString(dialogKey, "error"));
        }

        /// <summary>
        /// @HACK: used by the map overlay to completly run a dialog path; this does only work for single dialogs
        /// the problem is that the current game dialog should be unaffected by the dialogs run by the map overlay
        /// </summary>
        /// <param name="dialogKey"></param>
        public void RunDialog(string dialogKey)
        {
            // look if a dialog path exists for the key
            DialogPath dialogPath = null;
            if (dialogKey != null && _dialogPaths.ContainsKey(dialogKey))
            {
                var paths = _dialogPaths[dialogKey];
                for (var i = 0; i < paths.Count; i++)
                {
                    if (SaveManager.GetString(paths[i].VariableKey) == null && paths[i].Condition == "0" ||
                        SaveManager.GetString(paths[i].VariableKey) == paths[i].Condition)
                    {
                        dialogPath = paths[i];
                        break;
                    }
                }
            }

            // try to start a new dialog box
            if (dialogPath == null && !Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
            {
                // directly start a dialog
                string stateString = null;
                if (dialogKey != null)
                    stateString = SaveManager.GetString(dialogKey);

                InGameOverlay.TextboxOverlay.StartDialog(
                    Game1.LanguageManager.GetString(dialogKey + (stateString != null ? "_" + stateString : ""), "error"));
            }

            while (dialogPath != null)
            {
                var breakLoop = true;
                var dialogPathState = 0;

                // execute the current dialog path
                if (dialogPath != null)
                {
                    while (dialogPath.Action.Count > dialogPathState &&
                           dialogPath.Action[dialogPathState].Execute())
                    {
                        dialogPathState++;

                        // init the next dialog action
                        if (dialogPath.Action.Count > dialogPathState)
                            dialogPath.Action[dialogPathState].Init();
                    }

                    // do not stop executing at a empty dialog path
                    if (dialogPath.Action.Count == 0)
                        breakLoop = false;

                    // finished current path?
                    if (dialogPath.Action.Count <= dialogPathState)
                    {
                        breakLoop = false;
                        dialogPath = null;
                    }
                }

                // exit the loop if there is nothing to do
                if (breakLoop)
                    break;
            }
        }

        public bool DialogIsRunning()
        {
            return _currentDialogPath != null || _dialogPathQueue.Count > 0;
        }

        public bool FinalDialogAction()
        {
            return false;
        }

        private void UpdateDialog()
        {
            while (_currentDialogPath != null || _dialogPathQueue != null)
            {
                var breakLoop = true;

                // start a new dialog path?
                if (_dialogPathQueue.Count > 0 && _currentDialogPath == null)
                {
                    _currentDialogPath = DequeueDialogPath();
                    _currentDialogPathState = 0;

                    if (_currentDialogPath != null && _currentDialogPath.Action.Count > _currentDialogPathState)
                        _currentDialogPath.Action[_currentDialogPathState].Init();
                }

                // execute the current dialog path
                if (_currentDialogPath != null)
                {
                    while (_currentDialogPath.Action.Count > _currentDialogPathState &&
                           _currentDialogPath.Action[_currentDialogPathState].Execute())
                    {
                        _currentDialogPathState++;

                        // init the next dialog action
                        if (_currentDialogPath.Action.Count > _currentDialogPathState)
                            _currentDialogPath.Action[_currentDialogPathState].Init();
                    }

                    // do not stop executing at a empty dialog path
                    if (_currentDialogPath.Action.Count == 0)
                        breakLoop = false;

                    // finished current path?
                    if (_currentDialogPath.Action.Count <= _currentDialogPathState)
                    {
                        breakLoop = false;
                        _currentDialogPath = null;
                    }
                }

                // exit the loop if there is nothing to do
                if (breakLoop)
                    break;
            }
        }

        private DialogPath DequeueDialogPath()
        {
            Game1.GameManager.InGameOverlay.TextboxOverlay.UpdateObjects = false;

            var dialogKey = _dialogPathQueue.Peek();

            // look if a dialog path exists for the key
            if (dialogKey != null && _dialogPaths.ContainsKey(dialogKey))
            {
                var paths = _dialogPaths[dialogKey];
                for (var i = 0; i < paths.Count; i++)
                {
                    if (SaveManager.GetString(paths[i].VariableKey) == null && paths[i].Condition == "0" ||
                        SaveManager.GetString(paths[i].VariableKey) == paths[i].Condition)
                    {
                        _dialogPathQueue.Dequeue();
                        return paths[i];
                    }
                }
            }

            // try to start a new dialog box
            if (!Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
            {
                _dialogPathQueue.Dequeue();

                // directly start a dialog
                string stateString = null;
                if (dialogKey != null)
                    stateString = SaveManager.GetString(dialogKey);

                InGameOverlay.TextboxOverlay.StartDialog(
                    Game1.LanguageManager.GetString(dialogKey + (stateString != null ? "_" + stateString : ""), "error"));
            }

            return null;
        }

        public void RenderShadows(SpriteBatch spriteBatch)
        {
            Game1.Graphics.GraphicsDevice.SetRenderTarget(_shadowRenderTarget);
            Game1.Graphics.GraphicsDevice.Clear(Color.Transparent);
            Game1.Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Game1.Graphics.GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicClamp;
            Game1.Graphics.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            Game1.Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            MapManager.CurrentMap.Objects.DrawShadow(spriteBatch);
        }

        public void ChangeRenderTarget()
        {
            var tempActiveRt = _activeRenderTarget;
            _activeRenderTarget = _inactiveRenderTarget2;
            _inactiveRenderTarget2 = _inactiveRenderTarget1;
            _inactiveRenderTarget1 = tempActiveRt;

            SetActiveRenderTarget();
        }

        public void SetActiveRenderTarget()
        {
            Game1.Graphics.GraphicsDevice.SetRenderTarget(_activeRenderTarget);
        }

        public RenderTarget2D GetLastRenderTarget()
        {
            return _inactiveRenderTarget1;
        }

        public void DrawTop(SpriteBatch spriteBatch)
        {
            // draw the inventory
            InGameOverlay.Draw(spriteBatch);
        }

        public void DrawRenderTarget(SpriteBatch spriteBatch)
        {
            // draw the rt stuff of the game ui
            InGameOverlay.DrawRenderTarget(spriteBatch);
        }

        public void SetGameScale(float scale)
        {
            _scaleMultiplier = MathF.Ceiling(scale) / scale;

            UpdateRenderTargets();
        }

        public void OnResize()
        {
            InGameOverlay.ResolutionChanged();

            Game1.RenderWidth = (int)(Game1.WindowWidth * _scaleMultiplier);
            Game1.RenderHeight = (int)(Game1.WindowHeight * _scaleMultiplier);

            MapManager.Camera.SetBounds(Game1.RenderWidth, Game1.RenderHeight);

            // center the player
            MapManager.Camera.ForceUpdate(MapManager.GetCameraTarget());

            UpdateRenderTargets();
        }

        public void OnResizeEnd()
        {
            InGameOverlay.UpdateRenderTarget();

            UpdateRenderTargets();
        }

        public void UpdateRenderTargets()
        {
            if ((CurrentRenderWidth == Game1.RenderWidth &&
                 CurrentRenderHeight == Game1.RenderHeight &&
                 CurrentRenderScale == MapManager.Camera.Scale) ||
                 Game1.RenderWidth == 0 || Game1.RenderHeight == 0)
                return;

            CurrentRenderWidth = Game1.RenderWidth;
            CurrentRenderHeight = Game1.RenderHeight;
            CurrentRenderScale = MapManager.Camera.Scale;

            _activeRenderTarget?.Dispose();
            _inactiveRenderTarget1?.Dispose();
            _inactiveRenderTarget2?.Dispose();
            _shadowRenderTarget?.Dispose();
            _shadowRenderTargetBlur?.Dispose();

            _activeRenderTarget = new RenderTarget2D(Game1.Graphics.GraphicsDevice, Game1.RenderWidth, Game1.RenderHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _inactiveRenderTarget1 = new RenderTarget2D(Game1.Graphics.GraphicsDevice, Game1.RenderWidth, Game1.RenderHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _inactiveRenderTarget2 = new RenderTarget2D(Game1.Graphics.GraphicsDevice, Game1.RenderWidth, Game1.RenderHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            // shadow render targets
            var shadowScale = MathHelper.Clamp(MapManager.Camera.Scale / 2, 1, 10);
            var shadowRtWidth = (int)(Game1.RenderWidth / shadowScale);
            var shadowRtHeight = (int)(Game1.RenderHeight / shadowScale);
            _shadowRenderTarget = new RenderTarget2D(Game1.Graphics.GraphicsDevice, shadowRtWidth, shadowRtHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _shadowRenderTargetBlur = new RenderTarget2D(Game1.Graphics.GraphicsDevice, shadowRtWidth, shadowRtHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            // temp render targets
            TempRT0?.Dispose();
            TempRT1?.Dispose();
            TempRT2?.Dispose();

            TempRT0 = new RenderTarget2D(Game1.Graphics.GraphicsDevice, BlurRenderTargetWidth, BlurRenderTargetHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            TempRT1 = new RenderTarget2D(Game1.Graphics.GraphicsDevice, BlurRenderTargetWidth, BlurRenderTargetHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            TempRT2 = new RenderTarget2D(Game1.Graphics.GraphicsDevice, SideBlurRenderTargetWidth, SideBlurRenderTargetHeight,
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        public void HealPlayer(int hearts)
        {
            CurrentHealth += hearts;
            if (CurrentHealth > MaxHearths * 4)
                CurrentHealth = MaxHearths * 4;
        }

        public void InflictDamage(int damage)
        {
            if (DebugMode)
                return;

            if (CloakType == CloakBlue)
                damage = (int)MathF.Ceiling(damage / 2f);
            if (GuardianAcornIsActive)
                damage = (int)MathF.Ceiling(damage / 2f);

            CurrentHealth -= damage;

            if (CurrentHealth < 0)
                CurrentHealth = 0;

            // reset count for the guardian acorn
            GuardianAcornCount = 0;

            if (GuardianAcornIsActive)
            {
                GuardianAcornDamageCount++;
                if (GuardianAcornDamageCount >= 3)
                    StopGuardianAcorn();
            }

            // piece of power
            if (PieceOfPowerIsActive)
            {
                PieceOfPowerDamageCount++;
                if (PieceOfPowerDamageCount >= 3)
                    StopPieceOfPower();
            }
        }

        public void InitGuardianAcorn()
        {
            if (PieceOfPowerIsActive)
                StopPieceOfPower();

            GuardianAcornIsActive = true;
            GuardianAcornDamageCount = 0;

            StartPieceOfPowerMusic();
        }

        public void StopGuardianAcorn()
        {
            GuardianAcornIsActive = false;
            Game1.GameManager.SetMusic(-1, 1, false);
        }

        public void InitPieceOfPower()
        {
            if (GuardianAcornIsActive)
                StopGuardianAcorn();

            PieceOfPowerIsActive = true;
            PieceOfPowerDamageCount = 0;

            StartPieceOfPowerMusic();
        }

        public void StartPieceOfPowerMusic()
        {
            // start playing music
            Game1.GameManager.SetMusic(72, 1);
        }

        public void StopPieceOfPower()
        {
            PieceOfPowerIsActive = false;
            Game1.GameManager.SetMusic(-1, 1, false);
        }

        public void ResetMusic()
        {
            for (var i = 0; i < MusicChannels; i++)
            {
                _musicArray[i] = -1;
                _musicCounter[i] = 0;
            }
        }

        public void UpdateMusic()
        {
            for (var i = 0; i < MusicChannels; i++)
            {
                if (_musicCounter[i] == 0)
                    continue;

                _musicCounter[i] -= Game1.DeltaTime;

                // finished playing the music?
                if (_musicCounter[i] <= 0)
                {
                    _musicCounter[i] = 0;
                    _musicArray[i] = -1;
                    PlayMusic();
                }
            }
        }

        public void StopMusic(bool reset = false)
        {
            if (reset)
                ResetMusic();

            Game1.GbsPlayer.Stop();
        }

        public void StopMusic(int time, int priority)
        {
            _musicCounter[priority] = time;
        }

        public void PlayMusic(bool startPlaying = true)
        {
            for (var i = MusicChannels - 1; i >= 0; i--)
            {
                if (_musicArray[i] >= 0)
                {
                    var songNumber = (byte)_musicArray[i];
                    if (Game1.GbsPlayer.CurrentTrack != songNumber)
                        Game1.GbsPlayer.StartTrack(songNumber);

                    if (startPlaying)
                        Game1.GbsPlayer.Play();

                    return;
                }
            }

            // no music is playing?
            Game1.GbsPlayer.Stop();
        }

        public void SetMusic(int songNr, int priority, bool startPlaying = true)
        {
            // @HACK: don't restart the overworld track if the version with the intro was already started;
            // make sure to not restart the music while showing the overworld in the final sequence
            if ((songNr == 4 && _musicArray[priority] == 48) || (priority != 2 && _musicArray[2] == 62))
                return;

            _musicArray[priority] = songNr;

            PlayMusic(startPlaying);
        }

        public int GetCurrentMusic()
        {
            for (var i = _musicArray.Length - 1; i >= 0; i--)
                if (_musicArray[i] >= 0)
                    return _musicArray[i];

            return -1;
        }

        public void UpdateSoundEffects()
        {
            var lowerVolume = false;

            // we use ToList to be able to remove entries in the foreach loop
            foreach (var soundEffect in CurrentSoundEffects.ToList())
            {
                if (CurrentSoundEffects[soundEffect.Key].LowerMusicVolume)
                    lowerVolume = true;

                // update the volume of the sound effects to match the current settings
                soundEffect.Value.Instance.Volume = CurrentSoundEffects[soundEffect.Key].Volume * GameSettings.EffectVolume / 100 * Values.SoundEffectVolumeMult;

                soundEffect.Value.Instance.IsLooped = false;

                if (soundEffect.Value.EndTime != 0 &&
                    soundEffect.Value.EndTime < Game1.TotalGameTime)
                    soundEffect.Value.Instance.Stop();

                // finished playing?
                if (soundEffect.Value.Instance.State == SoundState.Stopped)
                    CurrentSoundEffects.Remove(soundEffect.Key);
            }

            if (lowerVolume)
                Game1.GbsPlayer.SetVolumeMultiplier(0.35f);
        }

        public void PauseSoundEffects()
        {
            foreach (var soundEffect in CurrentSoundEffects)
                if (soundEffect.Value.Instance.State == SoundState.Playing)
                    soundEffect.Value.Instance.Pause();
        }

        public void ContinueSoundEffects()
        {
            foreach (var soundEffect in CurrentSoundEffects)
                if (soundEffect.Value.Instance.State == SoundState.Paused)
                    soundEffect.Value.Instance.Resume();
        }

        public void PlaySoundEffect(string name, bool restart, Vector2 position, float range = 256)
        {
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - position;
            var volume = 1 - playerDistance.Length() / range;

            if (volume > 0)
                PlaySoundEffect(name, restart, volume);
        }

        public void PlaySoundEffect(string name, bool restart = true, float volume = 1, float pitch = 0, bool lowerMusicVolume = false, float playtime = 0)
        {
            CurrentSoundEffects.TryGetValue(name, out var entry);

            // if the same sound is playing it will be stopped and replaced with the new instance
            if (restart && entry!= null && entry.Instance != null)
            {
                entry.Instance.Stop();
                CurrentSoundEffects.Remove(name);
            }
            if (!restart && entry != null && entry.Instance != null)
            {
                entry.Volume = volume;
                if (playtime != 0)
                    entry.EndTime = Game1.TotalGameTime + playtime;

                entry.Instance.Volume = volume * GameSettings.EffectVolume / 100f * Values.SoundEffectVolumeMult;
                entry.Instance.Pitch = pitch;
                
                return;
            }

            entry = new PlayingSoundEffect() { Volume = volume, LowerMusicVolume = lowerMusicVolume };
            entry.Instance = Resources.SoundEffects[name].CreateInstance();
            // the volume of the sound effects is higher than the music; so scale effect volume a little down
            entry.Instance.Volume = volume * GameSettings.EffectVolume / 100f * Values.SoundEffectVolumeMult;
            entry.Instance.Pitch = pitch;

            if (playtime != 0)
            {
                entry.Instance.IsLooped = true;
                entry.EndTime = Game1.TotalGameTime + playtime;
            }

            entry.Instance.Play();

            CurrentSoundEffects.Add(name, entry);
        }

        public void StopSoundEffect(string name)
        {
            if (CurrentSoundEffects.TryGetValue(name, out var entry))
                entry.Instance.Stop();
        }

        public bool IsPlaying(string name)
        {
            if (CurrentSoundEffects.TryGetValue(name, out var entry))
                return entry.Instance.State == SoundState.Playing;

            return false;
        }

        public void ShakeScreenContinue(int time, int maxX, int maxY, float shakeSpeedX, float shakeSpeedY)
        {
            var periodsX = (_shakeCountX / 100f * _shakeSpeedX) % (MathF.PI * 2);
            _shakeCountX = time;
            if (_shakeSpeedX > 0)
                _shakeCountX += periodsX / _shakeSpeedX * 100f;

            _shakeCountY = time;
            _maxOffsetX = maxX;
            _maxOffsetY = maxY;
            _shakeSpeedX = shakeSpeedX;
            _shakeSpeedY = shakeSpeedY;
        }

        public void ShakeScreen(int time, int maxX, int maxY, float shakeSpeedX, float shakeSpeedY, int startDirX = 1, int startDirY = 1)
        {
            _shakeCountX = time;
            _shakeCountY = time;
            _maxOffsetX = maxX;
            _maxOffsetY = maxY;
            _shakeSpeedX = shakeSpeedX;
            _shakeSpeedY = shakeSpeedY;

            if (_shakeSpeedX > 0)
            {
                var periodsX = MathF.Round((time / 100f * _shakeSpeedX) / MathF.PI);
                if ((startDirX == -1 && periodsX % 2 == 0) ||
                    (startDirX == 1 && periodsX % 2 == 1))
                    periodsX += 1;
                _shakeCountX = (periodsX * MathF.PI) / _shakeSpeedX * 100f;
            }

            if (_shakeSpeedY > 0)
            {
                var periodsY = MathF.Round((time / 100f * _shakeSpeedY) / MathF.PI);
                if ((startDirY == 1 && periodsY % 2 == 0) ||
                    (startDirY == -1 && periodsY % 2 == 1))
                    periodsY += 1;
                _shakeCountY = (periodsY * MathF.PI) / _shakeSpeedY * 100f;
            }
        }

        public void UpdateShake()
        {
            if (_shakeCountX > 0)
            {
                _shakeCountX -= Game1.DeltaTime;
                MapManager.Camera.ShakeOffsetX = (float)Math.Sin(_shakeCountX / 100f * _shakeSpeedX) * _maxOffsetX;
            }

            if (_shakeCountY > 0)
            {
                _shakeCountY -= Game1.DeltaTime;
                MapManager.Camera.ShakeOffsetY = (float)Math.Sin(_shakeCountY / 100f * _shakeSpeedY) * _maxOffsetY;
            }
        }

        public bool LoadMiniMap(string mapName)
        {
            // already loaded the levels?
            if (DungeonMaps.ContainsKey(mapName))
                return true;

            // load the mini map
            var fileName = Values.PathMinimapFolder + mapName + ".txt";
            var dungeonMap = SaveLoadMap.LoadMiniMap(fileName);

            if (dungeonMap == null)
                return false;

            DungeonMaps.Add(mapName, dungeonMap);
            return true;
        }

        public void SetDungeon(string dungeonName, int dungeonLevel)
        {
            // TODO_Opt preload all the minimaps

            var level = 0;
            while (true)
            {
                if (!LoadMiniMap(dungeonName + "_" + level))
                    break;

                LoadMiniMap(dungeonName + "_" + level + "_alt");

                level++;
            }

            MapManager.NextMap.DungeonMode = true;
            MapManager.NextMap.LocationName = dungeonName;
            MapManager.NextMap.LocationFullName = dungeonName + "_" + dungeonLevel;
        }

        public void DungeonUpdatePlayerPosition(Point position)
        {
            // updated map discovery state
            if (MapManager.CurrentMap.LocationFullName != null &&
                DungeonMaps.ContainsKey(MapManager.CurrentMap.LocationFullName) &&
                position.X >= 0 && position.Y >= 0 &&
                position.X < DungeonMaps[MapManager.CurrentMap.LocationFullName].Tiles.GetLength(0) &&
                position.Y < DungeonMaps[MapManager.CurrentMap.LocationFullName].Tiles.GetLength(1))
                DungeonMaps[MapManager.CurrentMap.LocationFullName].Tiles[position.X, position.Y].DiscoveryState = true;

            PlayerDungeonPosition = position;
        }

        public void SetMapPosition(Point position)
        {
            if (MapVisibility == null ||
                0 > position.X || position.X >= MapVisibility.GetLength(0) ||
                0 > position.Y || position.Y >= MapVisibility.GetLength(1))
                return;

            MapVisibility[position.X, position.Y] = true;
            PlayerMapPosition = position;
        }

        public GameItemCollected GetItem(string itemId)
        {
            for (var i = 0; i < Equipment.Length; i++)
            {
                if (Equipment[i] != null && Equipment[i].Name == itemId &&
                    (string.IsNullOrEmpty(Equipment[i].LocationBounding) ||
                     Equipment[i].LocationBounding == MapManager.CurrentMap.LocationName))
                    return Equipment[i];
            }

            for (var i = 0; i < CollectedItems.Count; i++)
            {
                // player has item
                if (CollectedItems[i].Name == itemId &&
                    (string.IsNullOrEmpty(CollectedItems[i].LocationBounding) ||
                     CollectedItems[i].LocationBounding == MapManager.CurrentMap.LocationName))
                    return CollectedItems[i];
            }

            return null;
        }

        public void CollectItem(GameItemCollected itemCollected, int equipmentSlot = -1)
        {
            if (itemCollected.LocationBounding == "")
                itemCollected.LocationBounding = null;

            var item = Game1.GameManager.ItemManager[itemCollected.Name];
            // the base item has the max count information
            var baseItem = Game1.GameManager.ItemManager[item.Name];

            // make sure to replace then name
            // this is used for items that have different variations like the normal powder or powderPD with dialog
            itemCollected.Name = item.Name;

            // add the arrow count to the bow and remove the arrows
            if (itemCollected.Name == "bow")
            {
                var arrow = Game1.GameManager.GetItem("arrow");
                if (arrow != null)
                {
                    itemCollected.Count += arrow.Count;
                    Game1.GameManager.RemoveItem("arrow", arrow.Count);
                }
            }
            // if we have the bow collected change the type to bow
            if (itemCollected.Name == "arrow")
            {
                var bow = Game1.GameManager.GetItem("bow");
                if (bow != null)
                {
                    itemCollected.Name = "bow";
                    item = Game1.GameManager.ItemManager[itemCollected.Name];
                    baseItem = Game1.GameManager.ItemManager[item.Name];
                }
            }

            if (itemCollected.Name == "cloakBlue")
                CloakType = CloakBlue;
            else if (itemCollected.Name == "cloakRed")
                CloakType = CloakRed;

            // unlock the ocarina songs
            if (itemCollected.Name == "ocarina_maria")
            {
                OcarinaSongs[0] = 1;

                if (SelectedOcarinaSong == -1)
                    SelectedOcarinaSong = 0;
            }
            if (itemCollected.Name == "ocarina_manbo")
            {
                OcarinaSongs[1] = 1;

                if (SelectedOcarinaSong == -1)
                    SelectedOcarinaSong = 1;
            }
            if (itemCollected.Name == "ocarina_frog")
            {
                OcarinaSongs[2] = 1;

                if (SelectedOcarinaSong == -1)
                    SelectedOcarinaSong = 2;
            }

            // magnifying lens collected
            if (itemCollected.Name == "trade13")
                HasMagnifyingLens = true;

            if (baseItem.Equipable)
            {
                var maxCount = baseItem.MaxCount;

                if (itemCollected.Name == "sword1")
                    SwordLevel = 1;
                else if (itemCollected.Name == "sword2")
                    SwordLevel = 2;

                if (itemCollected.Name == "shield" ||
                    itemCollected.Name == "mirrorShield")
                    ShieldLevel = item.Level;
                if (itemCollected.Name == "stonelifter" || itemCollected.Name == "stonelifter2")
                    StoneGrabberLevel = item.Level;

                // powder, bomb or arrow?
                // check if the inventory was upgraded or not
                if (item.Name == "powder" && SaveManager.GetString("upgradePowder") == "1")
                    maxCount += 20;
                if (item.Name == "bomb" && SaveManager.GetString("upgradeBomb") == "1")
                    maxCount += 30;
                if (item.Name == "bow" && SaveManager.GetString("upgradeBow") == "1")
                    maxCount += 30;

                // search if the player already owns the equipment
                for (var i = 0; i < Equipment.Length; i++)
                {
                    if (Equipment[i] != null && Equipment[i].Name == item.Name)
                    {
                        Equipment[i].Count += itemCollected.Count;

                        if (maxCount > 0 && Equipment[i].Count > maxCount)
                            Equipment[i].Count = maxCount;

                        return;
                    }
                }

                if (maxCount > 0 && itemCollected.Count > maxCount)
                    itemCollected.Count = maxCount;

                // requested equipment slot is empty?
                if (0 <= equipmentSlot && equipmentSlot < Equipment.Length && Equipment[equipmentSlot] == null)
                {
                    SetEquipment(equipmentSlot, itemCollected);
                    return;
                }

                // add item to the collected item list
                var start = equipmentSlot < 0 ? 4 : 0;
                for (var i = start; i < Equipment.Length; i++)
                {
                    if (Equipment[i] != null)
                        continue;

                    SetEquipment(i, itemCollected);
                    return;
                }
            }
            else
            {
                // search if the player already owns the item
                var found = false;
                for (var i = 0; i < CollectedItems.Count; i++)
                {
                    if (CollectedItems[i].Name == item.Name &&
                        CollectedItems[i].LocationBounding == itemCollected.LocationBounding)
                    {
                        CollectedItems[i].Count += itemCollected.Count;

                        if (baseItem.MaxCount > 0 && CollectedItems[i].Count > baseItem.MaxCount)
                            CollectedItems[i].Count = baseItem.MaxCount;

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // add new item
                    CollectedItems.Add(itemCollected);
                }

                if (item.Name == "heartMeter")
                {
                    var heart = Game1.GameManager.GetItem("heartMeter");

                    // expand hearts?
                    while (heart?.Count >= 4)
                    {
                        heart.Count -= 4;
                        Game1.GameManager.MaxHearths++;
                        Game1.GameManager.HealPlayer(99);
                        ItemDrawHelper.EnableHeartAnimationSound();
                    }
                }
                else if (item.Name == "flippers")
                    MapManager.ObjLink.HasFlippers = true;
            }
        }

        public int GetEquipmentSlot(string itemName)
        {
            for (var i = 0; i < Equipment.Length; i++)
            {
                if (Equipment[i] != null && Equipment[i].Name == itemName)
                    return i;
            }

            return 0;
        }

        public bool RemoveItem(string itemName, int count)
        {
            // equipment
            for (var i = 0; i < Equipment.Length; i++)
            {
                if (Equipment[i] == null || Equipment[i].Name != itemName ||
                    Game1.GameManager.ItemManager[Equipment[i].Name].Level == 0 && Equipment[i].Count < count)
                    continue;

                Equipment[i].Count -= count;

                // remove the item from the inventory if the player can only have 1 of it
                // bombs, powder, etc will stay in the inventory
                if (Equipment[i].Count <= 0 && Game1.GameManager.ItemManager[Equipment[i].Name].MaxCount == 1)
                    Equipment[i] = null;

                // remove the item?
                // not sure what to do here
                // in the original powder gets removed but bombs not; what happens to arrows I do not know
                if (itemName == "powder" && Equipment[i] != null && Equipment[i].Count == 0)
                    Equipment[i] = null;

                return true;
            }

            // items
            for (var i = 0; i < CollectedItems.Count; i++)
            {
                if (CollectedItems[i] == null || CollectedItems[i].Name != itemName ||
                    Game1.GameManager.ItemManager[CollectedItems[i].Name].Level == 0 && CollectedItems[i].Count < count ||
                    !string.IsNullOrEmpty(CollectedItems[i].LocationBounding) && CollectedItems[i].LocationBounding != MapManager.CurrentMap.LocationName)
                    continue;

                CollectedItems[i].Count -= count;

                // remove the item?
                if (Game1.GameManager.ItemManager[CollectedItems[i].Name].Level != 0 || CollectedItems[i].Count == 0)
                {
                    CollectedItems.RemoveAt(i);

                    if (itemName == "flippers")
                        MapManager.ObjLink.HasFlippers = false;
                }

                return true;
            }

            return false;
        }

        public void ChangeItem(int oldSlot, int newSlot)
        {
            var tempAcc = Equipment[oldSlot];

            SetEquipment(oldSlot, Equipment[newSlot]);
            SetEquipment(newSlot, tempAcc);
        }

        public void SetEquipment(int index, GameItemCollected item)
        {
            Equipment[index] = item;
            UpdateEquipment();
        }

        private void UpdateEquipment()
        {
            // check if link is carrying a shield
            MapManager.ObjLink.CarrySword = false;
            MapManager.ObjLink.CarryShield = false;

            for (var i = 0; i < Values.HandItemSlots; i++)
            {
                if (Equipment[i]?.Name == "sword1" || Equipment[i]?.Name == "sword2")
                    MapManager.ObjLink.CarrySword = true;
                else if (Equipment[i]?.Name == "shield" || Equipment[i]?.Name == "mirrorShield")
                    MapManager.ObjLink.CarryShield = true;
            }
        }

        public void StartNewGame(int slot, string slotName)
        {
            ResetStuff();

            SaveName = slotName;

            Equipment = new GameItemCollected[EquipmentSlots];

            UpdateEquipment();

            SaveManager.Reset();

            // set up values
            // debug fill the inventory
            if (SaveName == "DebugMode")
            {
                DebugMode = true;

                CollectItem(new GameItemCollected("sword1") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("shield") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("feather") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("stonelifter") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("pegasusBoots") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("shovel") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("flippers") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("magicRod") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("hookshot") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("boomerang") { Count = 1 }, 0);
                CollectItem(new GameItemCollected("powder") { Count = 999 }, 0);
                CollectItem(new GameItemCollected("bomb") { Count = 999 }, 0);
                CollectItem(new GameItemCollected("bow") { Count = 999 }, 0);
                CollectItem(new GameItemCollected("ocarina") { Count = 1 }, 0);
            }

            CollectedItems.Clear();

            DungeonMaps.Clear();

            ItemDrawHelper.Init();

            SwordLevel = 0;
            CloakType = CloakGreen;

            SelectedOcarinaSong = 0;
            OcarinaSongs[0] = 0;
            OcarinaSongs[1] = 0;
            OcarinaSongs[2] = 0;

            MaxHearths = 3;
            CurrentHealth = MaxHearths * 4;

            PlayerMapPosition = null;
            MapVisibility = new bool[16, 16];

            SaveSlot = slot;

            // randomize the directions of the egg
            Game1.GameManager.SaveManager.SetString("eggDirections", Game1.RandomNumber.Next(0, 4).ToString());

            // create empty map
            MapManager.CurrentMap = Map.Map.CreateEmptyMap();

            MapManager.ObjLink.Map = MapManager.CurrentMap;
            MapManager.ObjLink.MapTransitionStart = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.MapTransitionEnd = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.EntityPosition.Z = 0;
            MapManager.ObjLink.TransitionOutWalking = false;
            MapManager.ObjLink.TransitionInWalking = false;

            MapManager.ObjLink.InitGame();

            MapManager.Camera.ForceUpdate(MapManager.GetCameraTargetLink());

            // load the map
            MapManager.ObjLink.SetNextMapPosition(new Vector2(MapManager.ObjLink.PosX, MapManager.ObjLink.PosY));
            ((MapTransitionSystem)GameSystems[typeof(MapTransitionSystem)]).LoadMapFromFile("house1.map", true, true, Values.MapFirstTransitionColor, false);
            ((MapTransitionSystem)GameSystems[typeof(MapTransitionSystem)]).AdditionalBlackScreenDelay = Values.GameSaveBlackScreen;
        }

        public void LoadSaveFile(int slot)
        {
            ResetStuff();

            MapManager.ObjLink.InitGame();

            SaveGameSaveLoad.LoadSaveFile(this, slot);

            ItemDrawHelper.Init();

            UpdateEquipment();

            // create empty map
            MapManager.CurrentMap = Map.Map.CreateEmptyMap();
            MapManager.CurrentMap.Objects.SpawnObject(MapManager.ObjLink);

            MapManager.ObjLink.Map = MapManager.CurrentMap;
            MapManager.ObjLink.SetWalkingDirection(SaveDirection);

            MapManager.Camera.ForceUpdate(MapManager.GetCameraTargetLink());

            MapManager.ObjLink.MapTransitionStart = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.MapTransitionEnd = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.DirectionEntry = SaveDirection;
            MapManager.ObjLink.EntityPosition.Z = 0;
            MapManager.ObjLink.TransitionOutWalking = false;
            MapManager.ObjLink.TransitionInWalking = false;

            // load the map
            var transitionSystem = ((MapTransitionSystem)GameSystems[typeof(MapTransitionSystem)]);
            MapManager.ObjLink.SetNextMapPosition(new Vector2(SavePositionX, SavePositionY));
            transitionSystem.LoadMapFromFile(LoadedMap, true, true, Values.MapFirstTransitionColor, false);
            transitionSystem.AdditionalBlackScreenDelay = Values.GameSaveBlackScreen;
        }

        public void RespawnPlayer()
        {
            if (Game1.GameManager.SaveManager.HistoryEnabled)
            {
                Game1.GameManager.SaveManager.RevertHistory();
                Game1.GameManager.SaveManager.DisableHistory();
            }

            ResetStuff();

            // create empty map
            MapManager.CurrentMap = Map.Map.CreateEmptyMap();
            MapManager.CurrentMap.Objects.SpawnObject(MapManager.ObjLink);
            MapManager.ObjLink.Map = MapManager.CurrentMap;

            MapManager.Camera.ForceUpdate(MapManager.GetCameraTargetLink());

            // respawn the player
            MapManager.ObjLink.Respawn();

            ItemDrawHelper.Init();

            MapManager.ObjLink.MapTransitionStart = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.MapTransitionEnd = MapManager.ObjLink.EntityPosition.Position;

            MapManager.ObjLink.TransitionOutWalking = false;
            MapManager.ObjLink.TransitionInWalking = false;

            // respawn looking down
            MapManager.ObjLink.DirectionEntry = 3;
            MapManager.ObjLink.SetWalkingDirection(3);
            MapManager.ObjLink.SetNextMapPosition(MapManager.ObjLink.SavePosition);

            // load the map
            var transitionSystem = ((MapTransitionSystem)GameSystems[typeof(MapTransitionSystem)]);
            transitionSystem.LoadMapFromFile(MapManager.ObjLink.SaveMap, true, true, Values.MapFirstTransitionColor, false);
            transitionSystem.AdditionalBlackScreenDelay = Values.GameRespawnBlackScreen;
        }

        private void ResetStuff()
        {
            SaveGameSaveLoad.ClearSaveState();
            Game1.GameManager.SaveManager.DisableHistory();

            // this was done to support DialogActionCooldown working after loading a new save
            Game1.TotalGameTime = 0;
            Game1.TotalGameTimeLast = 0;
            Game1.FreezeTime = 0;

            _shakeCountX = 0;
            _shakeCountY = 0;
        }
    }
}
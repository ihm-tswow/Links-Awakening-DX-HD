using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
#if WINDOWS
using System.Windows.Forms;
#endif

namespace ProjectZ.InGame.GameSystems
{
    internal class MapTransitionSystem : GameSystem
    {
        public int AdditionalBlackScreenDelay;
        public bool StartDreamTransition;
        public bool StartTeleportTransition;
        public bool StartKnockoutTransition;

        public enum TransitionState
        {
            Idle,
            TransitionIn,
            TransitionBlank_0,
            TransitionBlank_1,
            TransitionOut,
            ColorMode
        }

        public TransitionState CurrentState = TransitionState.Idle;
        public const int ChangeMapTime = 350;
        public const int BlackScreenDelay = 75; // 125

        private readonly MapManager _gameMapManager;
        private readonly ObjTransition _transitionObject;

        private Thread _loadingThread;

        private string _nextMapName;
        private string _nextMapPosition;
        private bool _nextMapCenter;
        private bool _nextMapStartInMiddle;
        private Color _nextMapColor;
        private bool _nextColorMode;

        private float _changeMapCount;

        // will be reset after each transition
        private bool _centerCamera;
        private bool _finishedLoading;
        private bool _fullColorMode;
        private bool _knockoutTransition;
        private bool _transitionEnded;
        private bool _introTransition;

        private const int DreamTransitionTimeAddition = 3000;
        private const int TeleportTransitionTimeAddition = 2000 - ChangeMapTime;
        private int _wobbleTransitionTime;
        private bool _wobbleTransitionOut;
        private bool _wobbleTransitionIn;

        public MapTransitionSystem(MapManager gameMapManager)
        {
            _gameMapManager = gameMapManager;

            _transitionObject = new ObjTransition(_gameMapManager.CurrentMap);
        }

        public override void OnLoad()
        {
            _nextMapName = null;
        }

        public override void Update()
        {
            // start map change
            if (_nextMapName != null)
            {
                if (!string.IsNullOrEmpty(_nextMapPosition))
                    MapManager.ObjLink.SetNextMapPosition(_nextMapPosition);

                LoadMapFromFile(_nextMapName, _nextMapCenter, _nextMapStartInMiddle, _nextMapColor, _nextColorMode);
                _nextMapName = null;
            }

            if (_transitionEnded)
            {
                _transitionEnded = false;
                Game1.GameManager.SaveManager.SetString("transition_ended", "0");
            }

            if (CurrentState != TransitionState.Idle)
                Game1.GameManager.InGameOverlay.DisableOverlayToggle = true;
            else
                Game1.GbsPlayer.SetVolumeMultiplier(1);

            if (CurrentState == TransitionState.TransitionOut)
            {
                _changeMapCount += Game1.DeltaTime;

                var transitionState = _changeMapCount / ChangeMapTime;
                var percentage = MathHelper.Clamp((float)(Math.Sin(transitionState * 1.1) / Math.Sin(1.1)), 0, 1);
                MapManager.ObjLink.UpdateMapTransitionOut(percentage);

                if (!_wobbleTransitionOut && !_knockoutTransition)
                    Game1.GameManager.DrawPlayerOnTopPercentage = percentage;

                // slowly lower the volume of the music
                var newVolume = 1 - MathHelper.Clamp(transitionState, 0, 1);
                Game1.GbsPlayer.SetVolumeMultiplier(newVolume);

                if (_wobbleTransitionOut)
                {
                    // fade out to a white screen
                    var wobblePercentage = MathHelper.Clamp(_changeMapCount / ChangeMapTime, 0, 1);
                    _transitionObject.Brightness = wobblePercentage;
                    _transitionObject.WobblePercentage = (_wobbleTransitionTime + _changeMapCount) / (_wobbleTransitionTime + ChangeMapTime);
                }

                if (_changeMapCount >= ChangeMapTime)
                    CurrentState = TransitionState.TransitionBlank_0;
            }
            else if (CurrentState == TransitionState.TransitionBlank_0)
            {
                _changeMapCount += Game1.DeltaTime;

                MapManager.ObjLink.UpdateMapTransitionOut(1);

                // new map is loaded?
                if (_finishedLoading && _changeMapCount >= ChangeMapTime + BlackScreenDelay + AdditionalBlackScreenDelay)
                {
                    FinishLoading();
                    CurrentState = TransitionState.TransitionBlank_1;
                }
            }
            else if (CurrentState == TransitionState.TransitionBlank_1)
            {
                _changeMapCount += Game1.DeltaTime;

                MapManager.ObjLink.UpdateMapTransitionIn(0);

                if (_changeMapCount >= ChangeMapTime + BlackScreenDelay * 2 + AdditionalBlackScreenDelay)
                {
                    CurrentState = TransitionState.TransitionIn;

                    if (_wobbleTransitionIn)
                        _changeMapCount = _wobbleTransitionTime;
                    else
                    {
                        _transitionObject.WobbleTransition = false;
                        _changeMapCount = ChangeMapTime;
                    }
                }
            }
            else if (CurrentState == TransitionState.TransitionIn)
            {
                _changeMapCount -= Game1.DeltaTime;

                // update the position of the player to walk into the new room
                var percentage = MathHelper.Clamp(_changeMapCount / ChangeMapTime, 0, 1);
                MapManager.ObjLink.UpdateMapTransitionIn(1 - (float)(Math.Sin(percentage * 1.1) / Math.Sin(1.1)));

                // slowly increase the volume of the music; the music is only playing
                var newVolume = 1 - percentage;
                if (Game1.GbsPlayer.GetVolumeMultiplier() < 1)
                    Game1.GbsPlayer.SetVolumeMultiplier(newVolume);

                if (!_wobbleTransitionOut && !_knockoutTransition && !_introTransition)
                    Game1.GameManager.DrawPlayerOnTopPercentage = percentage;

                if (_wobbleTransitionOut)
                {
                    // fade out to a white screen
                    var wobblePercentage = 1 - MathHelper.Clamp((_wobbleTransitionTime - _changeMapCount) / ChangeMapTime, 0, 1);
                    _transitionObject.Brightness = wobblePercentage;
                    _transitionObject.WobblePercentage = _changeMapCount / _wobbleTransitionTime;
                }

                // light up the scene
                if (_changeMapCount <= 0)
                {
                    _transitionEnded = true;
                    Game1.GameManager.SaveManager.SetString("transition_ended", "1");

                    CurrentState = TransitionState.Idle;
                    EndTransition();
                }
            }

            if (_knockoutTransition)
            {
                var percentage = MathHelper.Clamp((_changeMapCount - 100) / (ChangeMapTime - 100), 0, 1);
                _transitionObject.TransitionColor = _nextMapColor * percentage;
                _transitionObject.Percentage = 1;

                Game1.GameManager.MapManager.UpdateCameraX = false;
                Game1.GameManager.MapManager.UpdateCameraY = false;
            }

            if (_fullColorMode)
            {
                _transitionObject.Percentage = 1;
                _transitionObject.TransitionColor = _nextMapColor * TransitionPercentage();
            }
            else if (!_knockoutTransition)
            {
                _transitionObject.Percentage = (float)Math.Sin(TransitionPercentage() * Math.PI / 2);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (CurrentState == TransitionState.Idle)
                return;

            _transitionObject.Draw(spriteBatch);
        }

        public void SetColorMode(Color color, float colorTransparency, bool playerOnTop = true)
        {
            if (colorTransparency > 0)
                CurrentState = TransitionState.ColorMode;
            else
                CurrentState = TransitionState.Idle;

            _changeMapCount = ChangeMapTime;

            _transitionObject.TransitionColor = color * colorTransparency;

            if (playerOnTop)
                Game1.GameManager.DrawPlayerOnTopPercentage = colorTransparency;
        }

        public bool IsTransitioningOut()
        {
            return CurrentState == TransitionState.TransitionOut;
        }

        public bool IsTransitioningIn()
        {
            return CurrentState == TransitionState.TransitionIn;
        }

        private void StartTransition()
        {
            // draw the player on top of everything
            MapManager.ObjLink.StartTransitioning();

            _introTransition = Game1.GameManager.SaveManager.GetString("played_intro", "0") != "1";
            // dont show the player for the intro sequence transition
            if (!_introTransition)
                Game1.GameManager.DrawPlayerOnTopPercentage = 1.0f;
        }

        private void EndTransition()
        {
            _knockoutTransition = false;

            MapManager.ObjLink.EndTransitioning();

            // start the new music
            Game1.GbsPlayer.SetVolumeMultiplier(1);
            Game1.GbsPlayer.Play();

            MapManager.Camera.SoftUpdate(Game1.GameManager.MapManager.GetCameraTarget());
        }

        public float TransitionPercentage()
        {
            return MathHelper.Clamp(_changeMapCount / ChangeMapTime, 0, 1);
        }

        public void AppendMapChange(string mapName, string position)
        {
            AppendMapChange(mapName, position, false, false, Values.MapTransitionColor, false);
        }

        public void AppendMapChange(string mapName, string position, bool centerCamera, bool startFromMiddle, Color transitionColor, bool colorMode)
        {
            _nextMapName = mapName;
            _nextMapPosition = position;
            _nextMapCenter = centerCamera;
            _nextMapStartInMiddle = startFromMiddle;
            _nextMapColor = transitionColor;
            _nextColorMode = colorMode;

            MapManager.ObjLink.OnAppendMapChange();
        }

        public void LoadMapFromFile(string mapFileName, bool centerCamera, bool startFromMiddle, Color transitionColor, bool colorMode)
        {
            // abort the old loading thread if it is still running
            // TODO: thread abort is not supported so it just waits till the loading is finished
            if (_loadingThread != null && _loadingThread.IsAlive)
                _loadingThread.Join();

            //Debug.Assert(CurrentState == TransitionState.Idle ||
            //             CurrentState == TransitionState.ColorMode, "Tried transition while not in idle");

            _finishedLoading = false;

            // only show the opening transition
            if (startFromMiddle)
            {
                _changeMapCount = ChangeMapTime;
                CurrentState = TransitionState.TransitionBlank_0;
            }
            else
            {
                _changeMapCount = 0;
                CurrentState = TransitionState.TransitionOut;
            }

            // center after loading
            _centerCamera = centerCamera;

            // add the transition object to the map
            _knockoutTransition = false;
            _fullColorMode = colorMode;
            _nextMapColor = transitionColor;

            _wobbleTransitionOut = false;
            _wobbleTransitionIn = false;

            _transitionObject.WobbleTransition = false;

            _transitionObject.TransitionColor = transitionColor;
            _transitionObject.Percentage = startFromMiddle ? 1 : 0;

            // start transition
            StartTransition();

            if (StartDreamTransition)
            {
                StartDreamTransition = false;
                DreamTransition();
            }
            if (StartTeleportTransition)
            {
                StartTeleportTransition = false;
                TeleportTransition();
            }

            if (StartKnockoutTransition)
            {
                StartKnockoutTransition = false;
                KnockoutTransition();
            }

            // start loading the new map in a thread
            _loadingThread = new Thread(o => ThreadLoading(mapFileName));
            _loadingThread.Start();
        }

        public void DreamTransition()
        {
            _transitionObject.WobbleTransition = true;
            _fullColorMode = true;
            _wobbleTransitionOut = true;

            Game1.GameManager.DrawPlayerOnTopPercentage = 0.0f;
            _nextMapColor = Color.White;

            // take longer
            _changeMapCount = -DreamTransitionTimeAddition;
            _wobbleTransitionTime = DreamTransitionTimeAddition;
            AdditionalBlackScreenDelay = 500;
        }

        public void TeleportTransition()
        {
            _transitionObject.WobbleTransition = true;
            _fullColorMode = true;
            _wobbleTransitionOut = true;
            _wobbleTransitionIn = true;

            Game1.GameManager.DrawPlayerOnTopPercentage = 0.0f;
            _nextMapColor = Color.White;

            // take longer
            _changeMapCount = -TeleportTransitionTimeAddition;
            _wobbleTransitionTime = TeleportTransitionTimeAddition;
            AdditionalBlackScreenDelay = 500;
        }

        public void KnockoutTransition()
        {
            _knockoutTransition = true;

            Game1.GameManager.DrawPlayerOnTopPercentage = 0.0f;
            _nextMapColor = Color.White;

            // take longer
            AdditionalBlackScreenDelay = 1000;
        }

        public void ThreadLoading(string mapFileName)
        {
            try
            {
                // @HACK: after loading the map the garbage collector will start and increase the frametime
                // this also leads to the door soundeffect cracking sometimes
                // if we wait a little bit the cracking gets reduced by a lot
                //Thread.Sleep(75);

                // load the map file
                SaveLoadMap.LoadMap(mapFileName, _gameMapManager.NextMap);
                // create the objects
                _gameMapManager.NextMap.Objects.LoadObjects();

                _finishedLoading = true;
            }
            catch (Exception exception)
            {
#if WINDOWS
                // show the error message instead of just crashing the game
                MessageBox.Show(exception.StackTrace, exception.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
                throw;
            }
        }

        public void FinishLoading()
        {
            AdditionalBlackScreenDelay = 0;

            // switch to the new map
            var oldMap = _gameMapManager.CurrentMap;
            _gameMapManager.CurrentMap = _gameMapManager.NextMap;
            _gameMapManager.NextMap = oldMap;

            var currentTrack = Game1.GbsPlayer.CurrentTrack;
            var nextTrack = -1;
            for (var i = 0; i < _gameMapManager.CurrentMap.MapMusic.Length; i++)
                if (_gameMapManager.CurrentMap.MapMusic[i] >= 0)
                    nextTrack = _gameMapManager.CurrentMap.MapMusic[i];

            if (currentTrack != nextTrack)
                Game1.GbsPlayer.Pause();
            
            Game1.GameManager.ResetMusic();

            // finish loading map
            _gameMapManager.FinishLoadingMap(_gameMapManager.CurrentMap);

            MapManager.ObjLink.UpdateMapTransitionIn(0);

            // set the new music
            for (var i = 0; i < _gameMapManager.CurrentMap.MapMusic.Length; i++)
                if (_gameMapManager.CurrentMap.MapMusic[i] >= 0)
                    Game1.GameManager.SetMusic(_gameMapManager.CurrentMap.MapMusic[i], i, false);

            // center the camera
            var goalPosition = Game1.GameManager.MapManager.GetCameraTarget();
            if (_centerCamera)
                MapManager.Camera.ForceUpdate(goalPosition);
            else
                MapManager.Camera.SoftUpdate(goalPosition);
        }
    }
}
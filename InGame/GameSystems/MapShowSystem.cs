using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System;
using System.Threading;

namespace ProjectZ.InGame.GameSystems
{
    class MapShowSystem : GameSystem
    {
        private Thread _loadingThread;

        public Color TransitionColor = Color.White;

        private Vector2[] _cameraTargets = new Vector2[] {
            new Vector2(248, 1344),
            new Vector2(120, 1488),
            new Vector2(416, 1984),
            new Vector2(256, 704),
            new Vector2(400, 1216) };

        private float _counter;
        private const float ChangeTargetTime = 5350;
        private const float FadeTime = 500;
        private const float FadeLock = 250;
        private int _targetIndex;

        private bool _isActive;
        private bool _finished;
        private bool _finishedLoading;
        private bool _init;

        public override void OnLoad()
        {
            _targetIndex = 0;
            _finished = false;
            _isActive = false;
        }

        public override void Update()
        {
            if (!_isActive)
                return;

            if (!_init)
            {
                if (_finishedLoading && _counter <= 0)
                {
                    _counter = ChangeTargetTime;
                    _finishedLoading = false;
                    _init = true;
                    FinishLoading();
                }
                else
                {
                    _counter -= Game1.DeltaTime;
                }

                return;
            }

            _counter -= Game1.DeltaTime;

            if (_finished && _counter < ChangeTargetTime - FadeTime - FadeLock)
            {
                _isActive = false;
                return;
            }

            if (_counter < 0)
            {
                _counter += ChangeTargetTime;

                _targetIndex++;
                if (_targetIndex < _cameraTargets.Length)
                {
                    Game1.GameManager.MapManager.CurrentMap.CameraTarget = _cameraTargets[_targetIndex];
                    MapManager.Camera.ForceUpdate(Game1.GameManager.MapManager.GetCameraTarget());
                }
                else
                {
                    _finished = true;
                    
                    // switch back to the ending map
                    var oldMap = Game1.GameManager.MapManager.CurrentMap;
                    Game1.GameManager.MapManager.CurrentMap = Game1.GameManager.MapManager.NextMap;
                    Game1.GameManager.MapManager.NextMap = oldMap;

                    Game1.GameManager.InGameOverlay.StartSequence("final");
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_isActive)
                return;

            spriteBatch.Begin();

            var fadePercentage = 0f;
            // fade out
            if (_counter < FadeTime + FadeLock)
                fadePercentage = 1 - MathF.Sin(MathHelper.Clamp((_counter - FadeLock) / FadeTime, 0, 1) * MathF.PI * 0.5f);
            // fade in
            else if (ChangeTargetTime - FadeTime - FadeLock < _counter)
                fadePercentage = MathF.Sin((1 - MathHelper.Clamp((ChangeTargetTime - FadeLock - _counter) / FadeTime, 0, 1)) * MathF.PI * 0.5f);

            spriteBatch.Draw(Resources.SprWhite, new Rectangle(0, 0, Game1.RenderWidth, Game1.RenderHeight), TransitionColor * fadePercentage);
            spriteBatch.End();
        }

        public void StartEnding()
        {
            if (_isActive)
                return;

            _isActive = true;
            _finishedLoading = false;
            _init = false;
            _counter = FadeLock + FadeTime;

            // used to spawn different npcs on the overworld
            Game1.GameManager.SaveManager.SetString("final_show", "1");
            Game1.GameManager.SaveManager.SetString("marin_state", "1");

            // start loading the new map in a thread
            _loadingThread = new Thread(o => ThreadLoading("overworld.map"));
            _loadingThread.Start();
        }

        private void ThreadLoading(string mapFileName)
        {
            // load the map file
            SaveLoadMap.LoadMap(mapFileName, Game1.GameManager.MapManager.NextMap);

            // create the objects
            Game1.GameManager.MapManager.NextMap.Objects.LoadObjects();

            _finishedLoading = true;
        }

        private void FinishLoading()
        {
            // switch to the new map
            var oldMap = Game1.GameManager.MapManager.CurrentMap;
            Game1.GameManager.MapManager.CurrentMap = Game1.GameManager.MapManager.NextMap;
            Game1.GameManager.MapManager.NextMap = oldMap;

            // center the camera
            Game1.GameManager.MapManager.CurrentMap.CameraTarget = _cameraTargets[_targetIndex];
            MapManager.Camera.ForceUpdate(Game1.GameManager.MapManager.GetCameraTarget());

            Game1.GameManager.StartDialogPath("final_show_map");
        }
    }
}

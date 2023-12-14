using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ProjectZ.InGame.Tests
{
    public class MapTest
    {
        private Dictionary<string, int> _keyList = new Dictionary<string, int>();

        private List<string> _doorSaveList = new List<string>();
        private List<string> _doorList = new List<string>();

        private List<string> _mapList = new List<string>();
        private const int StartIndex = 5;
        private int _currentMapIndex = StartIndex;

        private Vector2 _cameraPosition;

        private float _counter;
        private float ChangeTime = 25;

        private float DigTime = 500;
        private bool _hasDug;

        private float BombTime = 650;
        private bool _hasBombed;

        private float BallTime = 350;
        private bool _hasSpawnedBalls;

        private bool _isRunning;
        private bool _paused = true;

        public MapTest()
        {
            var mapPaths = Directory.GetFiles(Values.PathMapsFolder);

            for (var i = 0; i < mapPaths.Length; i++)
            {
                if (mapPaths[i].EndsWith(".map") && !mapPaths[i].Contains("test map"))
                {
                    _mapList.Add(mapPaths[i]);
                }
            }
        }

        private void Start()
        {
            _isRunning = true;
            _counter = ChangeTime;

            Game1.ScreenManager.ChangeScreen(Values.ScreenNameGame);

            LoadMap(_mapList[_currentMapIndex]);
        }

        public void Update()
        {
            if (InputHandler.KeyPressed(Keys.V))
                SpawnCurrentTester();
            if (InputHandler.KeyPressed(Keys.B))
                SpawnBalls();

            return;

            if (!_isRunning)
            {
                if (Game1.FinishedLoading)
                    Start();

                return;
            }

            if (InputHandler.KeyPressed(Keys.Space))
            {
                _paused = !_paused;
                InputHandler.ResetInputState();
            }
            if (InputHandler.KeyPressed(Keys.D1))
            {
                _paused = true;
                OffsetMap(-1);
            }
            if (InputHandler.KeyPressed(Keys.D2))
            {
                _paused = true;
                OffsetMap(1);
            }

            var direction = ControlHandler.GetMoveVector2();
            if (direction.Length() > 0)
            {
                _cameraPosition += direction * Game1.TimeMultiplier * 2.5f;
                Game1.GameManager.MapManager.CurrentMap.CameraTarget = _cameraPosition;
                MapManager.Camera.ForceUpdate(Game1.GameManager.MapManager.GetCameraTarget());
            }

            // close the textbox overlay
            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                Game1.GameManager.InGameOverlay.TextboxOverlay.Init();

            if (!_paused)
                _counter -= Game1.DeltaTime;


            //if (_counter < BombTime && !_hasBombed)
            //{
            //    Bomb();
            //    DestroyStones();
            //    _hasBombed = true;
            //}

            //if (_counter < DigTime && !_hasDug)
            //{
            //    Dig();
            //    _hasDug = true;
            //}

            //if (_counter < BallTime && !_hasSpawnedBalls)
            //{
            //    SpawnBallsField();
            //    _hasSpawnedBalls = true;
            //}

            // change map?
            if (_counter < 0)
            {
                _counter = ChangeTime;
                _hasDug = false;
                _hasBombed = false;
                _hasSpawnedBalls = false;

                if (!UpdateView())
                    OffsetMap(1);
            }
        }

        private void OffsetMap(int offset)
        {
            _currentMapIndex = (_currentMapIndex + offset) % _mapList.Count;
            if (_currentMapIndex < 0)
                _currentMapIndex += _mapList.Count;

            LoadMap(_mapList[_currentMapIndex]);
        }

        private bool UpdateView()
        {
            _cameraPosition.X += 160;
            if (_cameraPosition.X > Game1.GameManager.MapManager.CurrentMap.MapWidth * Values.TileSize)
            {
                _cameraPosition.X = 80;
                _cameraPosition.Y += 128;
            }

            if (_cameraPosition.Y - 64 > Game1.GameManager.MapManager.CurrentMap.MapHeight * Values.TileSize)
                return false;

            Game1.GameManager.MapManager.CurrentMap.CameraTarget = _cameraPosition;
            MapManager.Camera.ForceUpdate(Game1.GameManager.MapManager.GetCameraTarget());

            return true;
        }

        private void LoadMap(string path)
        {
            if (_currentMapIndex == StartIndex)
            {
                _keyList.Clear();
                _doorSaveList.Clear();
                _doorList.Clear();
            }

            var mapFileName = Path.GetFileName(path);

            // load the map file
            SaveLoadMap.LoadMap(mapFileName, Game1.GameManager.MapManager.NextMap);

            // create the objects
            Game1.GameManager.MapManager.NextMap.Objects.LoadObjects();

            var oldMap = Game1.GameManager.MapManager.CurrentMap;
            Game1.GameManager.MapManager.CurrentMap = Game1.GameManager.MapManager.NextMap;
            Game1.GameManager.MapManager.NextMap = oldMap;

            // center the camera
            _cameraPosition = new Vector2(
                80 + Game1.GameManager.MapManager.CurrentMap.MapOffsetX * Values.TileSize,
                64 + Game1.GameManager.MapManager.CurrentMap.MapOffsetY * Values.TileSize);
            Game1.GameManager.MapManager.CurrentMap.CameraTarget = _cameraPosition;
            MapManager.Camera.ForceUpdate(Game1.GameManager.MapManager.GetCameraTarget());

            //CheckMusic();

            GetDoorList();

            //CheckKeys();
        }

        private void GetDoorList()
        {
            var doors = new List<GameObject>();
            Game1.GameManager.MapManager.CurrentMap.Objects.GetObjectsOfType(doors, typeof(ObjDoor), 0, 0,
                Game1.GameManager.MapManager.CurrentMap.MapWidth * Values.TileSize,
                Game1.GameManager.MapManager.CurrentMap.MapHeight * Values.TileSize);
            foreach (var door in doors)
            {
                var doorObj = ((ObjDoor)door);
                if (doorObj._savePosition && doorObj._entryId != null)
                {
                    _doorSaveList.Add(Game1.GameManager.MapManager.CurrentMap.MapName + " : " + doorObj._entryId);
                }
                if (!doorObj._savePosition && doorObj._entryId != null)
                {
                    _doorList.Add(Game1.GameManager.MapManager.CurrentMap.MapName + " : " + doorObj._entryId);
                }
            }
        }

        /// <summary>
        /// Check if there are keys that are used in multiple cases
        /// </summary>
        private void CheckKeys()
        {
            var items = new List<GameObject>();
            Game1.GameManager.MapManager.CurrentMap.Objects.GetObjectsOfType(items, typeof(ObjItem), 0, 0,
                Game1.GameManager.MapManager.CurrentMap.MapWidth * Values.TileSize,
                Game1.GameManager.MapManager.CurrentMap.MapHeight * Values.TileSize);
            foreach (var item in items)
            {
                var saveKey = ((ObjItem)item).SaveKey;
                if (saveKey != null)
                {
                    if (_keyList.ContainsKey(saveKey))
                    {
                        Debug.Assert(false);
                        _keyList[saveKey]++;
                    }
                    else
                        _keyList.Add(saveKey, 1);
                }
            }

            var chests = new List<GameObject>();
            Game1.GameManager.MapManager.CurrentMap.Objects.GetObjectsOfType(chests, typeof(ObjChest), 0, 0,
                Game1.GameManager.MapManager.CurrentMap.MapWidth * Values.TileSize,
                Game1.GameManager.MapManager.CurrentMap.MapHeight * Values.TileSize);
            foreach (var chest in chests)
            {
                var saveKey = ((ObjChest)chest).ItemKey;
                if (saveKey != null)
                {
                    if (_keyList.ContainsKey(saveKey))
                    {
                        Debug.Assert(false);
                        _keyList[saveKey]++;
                    }
                    else
                        _keyList.Add(saveKey, 1);
                }
            }

            var barriers = new List<GameObject>();
            Game1.GameManager.MapManager.CurrentMap.Objects.GetObjectsOfType(barriers, typeof(ObjDestroyableBarrier), 0, 0,
                Game1.GameManager.MapManager.CurrentMap.MapWidth * Values.TileSize,
                Game1.GameManager.MapManager.CurrentMap.MapHeight * Values.TileSize);
            foreach (var barrier in barriers)
            {
                var saveKey = ((ObjDestroyableBarrier)barrier).SaveKey;
                if (saveKey != null)
                {
                    if (_keyList.ContainsKey(saveKey))
                    {
                        Debug.Assert(false);
                        _keyList[saveKey]++;
                    }
                    else
                        _keyList.Add(saveKey, 1);
                }
            }
        }

        private void CheckMusic()
        {
            if (Game1.GameManager.MapManager.CurrentMap.MapMusic[0] == -1)
                _paused = true;
        }

        private void SpawnBalls()
        {
            for (var i = 0; i < 100; i++)
            {
                var ball = new ObjTestObject(Game1.GameManager.MapManager.CurrentMap, (int)_cameraPosition.X, (int)_cameraPosition.Y);
                Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(ball);
            }
        }

        private void SpawnCurrentTester()
        {
            for (var y = -10; y < 10; y++)
                for (var x = -10; x < 10; x++)
                {
                    var posX = (int)(MapManager.ObjLink.EntityPosition.X / 16) * 16 + 8;
                    var posY = (int)(MapManager.ObjLink.EntityPosition.Y / 16) * 16 + 8;
                    var ball = new ObjWaterCurrentTester(Game1.GameManager.MapManager.CurrentMap, posX + x * 16, posY + y * 16);
                    Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(ball);
                }
        }

        private void SpawnBallsField()
        {
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    var ballPosition = new Vector2(
                        (int)(_cameraPosition.X - 80) + x * Values.TileSize + 8,
                        (int)(_cameraPosition.Y - 64) + y * Values.TileSize + 8);

                    Box box = Box.Empty;
                    if (!Game1.GameManager.MapManager.CurrentMap.Objects.Collision(
                        new Box(ballPosition.X - 2, ballPosition.Y - 2, 0, 4, 4, 8), Box.Empty, Values.CollisionTypes.Normal, 0, 0, ref box))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            var ball = new ObjTestObject(Game1.GameManager.MapManager.CurrentMap, (int)ballPosition.X, (int)ballPosition.Y);
                            Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(ball);
                        }
                    }
                }
            }
        }

        private void Bomb()
        {
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    var bombPosition = new Vector2(
                        (int)(_cameraPosition.X - 80) + x * Values.TileSize,
                        (int)(_cameraPosition.Y - 64) + y * Values.TileSize);

                    Game1.GameManager.MapManager.CurrentMap.Objects.Hit(MapManager.ObjLink, new Vector2(bombPosition.X + 8, bombPosition.Y + 8),
                        new Box(bombPosition.X, bombPosition.Y, 0, 16, 16, 16), HitType.Bomb, 2, false);
                }
            }
        }

        private void DestroyStones()
        {
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    var position = new Vector2(
                        (int)(_cameraPosition.X - 80) + x * Values.TileSize,
                        (int)(_cameraPosition.Y - 64) + y * Values.TileSize);

                    var recInteraction = new RectangleF(position.X + 4, position.Y + 4, 8, 8);

                    // find an object to carry
                    var grabbedObject = Game1.GameManager.MapManager.CurrentMap.Objects.GetCarryableObjects(recInteraction);
                    if (grabbedObject != null)
                    {
                        var carriableComponent = grabbedObject.Components[CarriableComponent.Index] as CarriableComponent;
                        if (carriableComponent != null && carriableComponent.Owner is ObjStone)
                        {
                            carriableComponent.StartGrabbing?.Invoke();
                            carriableComponent.Throw?.Invoke(new Vector2(0, 0));
                        }
                    }
                }
            }
        }

        private void Dig()
        {
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    var digPosition = new Point(
                        (int)(_cameraPosition.X - 80) / Values.TileSize + x,
                        (int)(_cameraPosition.Y - 64) / Values.TileSize + y);

                    if (Game1.GameManager.MapManager.CurrentMap.CanDig(digPosition))
                        Game1.GameManager.MapManager.CurrentMap.Dig(digPosition, new Vector2(digPosition.X, digPosition.Y + 8), 0);
                }
            }
        }
    }
}

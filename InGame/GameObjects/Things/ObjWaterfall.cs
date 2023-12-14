using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjWaterfall : GameObject
    {
        private const string SW1 = "water1";
        private const string SW2 = "water2";
        private const string SWF = "waterFall";
        private const string SPW = "stoneSpawner";

        private readonly string[,] _spawnObjects = {
            { null, null, null, null, null, null, null, SW2, null },
            { null, SWF, null, null, null, null, null, SW2, SW2 },
            { null, SWF, null, null, null, SWF, null, SW2, SW2 },
            { SW1, SW1, SW1, SW1, SW1, SW1, SW1, SW2, SW2 },
            { null, null, SWF, SWF, SWF, null, null, SW1, SW2 },
            { null, null, SWF, SWF, SWF, null, null, null, null },
            { null, null, SWF, SWF, SWF, null, null, null, null },
            { null, null, SWF, SWF, SWF, null, null, null, null },
            { null, SW1, SW1, SW1, SW1, SW1, null, null, null },
            { null, SW1, SW1, SW1, SW1, SW1, null, null, null },
            { null, null, SW1, SW1, SW1, SW1, null, null, null },
        };

        private readonly string[,] _stoneMap = {
            { null, null, null, null, null, null, null, null, null },
            { null, null, null, null, null, null, null, null, null },
            { null, null, null, null, null, null, null, null, null },
            { null, SPW, null, null, SPW, SPW, null, null, null },
            { null, null, null, null, null, null, null, SPW, null },
            { null, null, null, null, null, null, null, null, null },
            { null, null, null, null, null, null, null, null, null },
            { null, null, null, null, null, null, null, null, null },
            { null, null, SPW, null, SPW, null, null, null, null },
            { null, null, null, null, null, SPW, null, null, null },
            { null, null, null, null, null, null, null, null, null },
        };

        private readonly List<GameObject> _waterObjects = new List<GameObject>();

        private readonly int[,] _despawnTime =
        {
            {0, 0, 0, 0, 0, 0, 0, 5, 0},
            {0, 1, 0, 0, 0, 0, 0, 5, 5},
            {0, 2, 0, 0, 0, 1, 0, 4, 5},
            {3, 3, 3, 3, 3, 3, 3, 4, 4},
            {0, 0, 5, 5, 5, 0, 0, 4, 4},
            {0, 0, 6, 6, 6, 0, 0, 0, 0},
            {0, 0, 7, 7, 7, 0, 0, 0, 0},
            {0, 0, 8, 8, 8, 0, 0, 0, 0},
            {0, 9, 9, 9, 9, 9, 0, 0, 0},
            {0, 11, 11, 10, 10, 10, 0, 0, 0},
            {0, 0, 11, 11, 11, 11, 0, 0, 0},
        };

        private GameObject[,] _gameObjects;
        private readonly Point _position;

        private readonly string _strKey;

        private float _despawnCounter;
        private bool _isDespawning;
        private bool _wasUpdated;

        public ObjWaterfall() : base("waterfall") { }

        public ObjWaterfall(Map.Map map, int posX, int posY, string strKey) : base(map)
        {
            _strKey = strKey;
            _position = new Point(posX, posY);

            if (!string.IsNullOrEmpty(_strKey) &&
                Game1.GameManager.SaveManager.GetString(_strKey) == "1")
            {
                SpawnStones();
                IsDead = true;
                return;
            }

            SpawnObjects(new Vector2(posX, posY));

            // add key change listener
            if (!string.IsNullOrEmpty(_strKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            _wasUpdated = true;

            if (!_isDespawning)
                return;

            MapManager.ObjLink.FreezePlayer();

            _despawnCounter += Game1.DeltaTime;
            var timeStep = _despawnCounter / 585f;

            DespawnObjects(timeStep);

            if (timeStep > 12)
            {
                // stop the music
                Game1.GameManager.SetMusic(-1, 2);

                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void SpawnStones()
        {
            for (var y = 0; y < _stoneMap.GetLength(0); y++)
                for (var x = 0; x < _stoneMap.GetLength(1); x++)
                    if (_stoneMap[y, x] != null)
                    {
                        var objStones = new ObjStoneSpawner(Map, _position.X + x * 16, _position.Y + y * 16);
                        Map.Objects.SpawnObject(objStones);
                    }
        }

        private void SpawnObjects(Vector2 spawnPosition)
        {
            _gameObjects = new GameObject[_spawnObjects.GetLength(1), _spawnObjects.GetLength(0)];

            for (var y = 0; y < _spawnObjects.GetLength(0); y++)
            {
                for (var x = 0; x < _spawnObjects.GetLength(1); x++)
                {
                    var objName = _spawnObjects[y, x];
                    if (objName == null)
                        continue;

                    var _objParameter = MapData.GetParameter(objName, null);
                    if (_objParameter != null)
                    {
                        _objParameter[1] = (int)spawnPosition.X + x * 16;
                        _objParameter[2] = (int)spawnPosition.Y + y * 16;
                    }

                    var newObject = ObjectManager.GetGameObject(Map, objName, _objParameter);
                    Map.Objects.SpawnObject(newObject);

                    _gameObjects[x, y] = newObject;

                    // spawn deep water
                    if (objName == SW1)
                    {
                        var objWater = new ObjWaterDeep(Map, _position.X + x * 16, _position.Y + y * 16);
                        Map.Objects.SpawnObject(objWater);
                        _waterObjects.Add(objWater);
                    }
                    else if (objName == SW2)
                    {
                        var objWater = new ObjWater(Map, _position.X + x * 16, _position.Y + y * 16, -2);
                        Map.Objects.SpawnObject(objWater);
                        _waterObjects.Add(objWater);
                    }
                }
            }
        }

        private void DespawnObjects(float timeStep)
        {
            for (var y = 0; y < _gameObjects.GetLength(1); y++)
            {
                for (var x = 0; x < _gameObjects.GetLength(0); x++)
                {
                    if (_gameObjects[x, y] == null)
                        continue;

                    // cut the tile in half to have a nicer transition
                    if (_despawnTime[y, x] - 0.5f < timeStep)
                    {
                        var animatedTile = _gameObjects[x, y] as ObjAnimatedTile;
                        if (animatedTile != null && animatedTile.Sprite.SourceRectangle.Height * animatedTile.Sprite.Scale == 16)
                        {
                            animatedTile.Sprite.SourceRectangle.Y += 8;
                            animatedTile.Sprite.SourceRectangle.Height = 8;
                            animatedTile.EntityPosition.Offset(new Vector2(0, 8));
                        }
                    }

                    if (timeStep < _despawnTime[y, x])
                        continue;

                    Map.Objects.DeleteObjects.Add(_gameObjects[x, y]);
                    _gameObjects[x, y] = null;

                    // spawn stones
                    if (_stoneMap[y, x] != null)
                    {
                        var objStoneSpawn = new ObjStoneSpawner(Map,
                            _position.X + x * 16, _position.Y + y * 16);
                        Map.Objects.SpawnObject(objStoneSpawn);
                    }
                }
            }

            foreach (var objWater in _waterObjects)
            {
                // @HACK: should get reset by despawning the deep water obj
                var fieldX = (int)objWater.EntityPosition.X / 16;
                var fieldY = (int)objWater.EntityPosition.Y / 16;
                var currentState = Map.GetFieldState(fieldX, fieldY);
                if (objWater is ObjWater)
                    Map.SetFieldState(fieldX, fieldY, currentState & ~MapStates.FieldStates.Water);
                if (objWater is ObjWaterDeep)
                    Map.SetFieldState(fieldX, fieldY, currentState & ~MapStates.FieldStates.DeepWater);

                Map.Objects.DeleteObjects.Add(objWater);
            }

            _waterObjects.Clear();
        }

        private void KeyChanged()
        {
            if (_isDespawning || Game1.GameManager.SaveManager.GetString(_strKey) != "1")
                return;

            Game1.GameManager.SetMusic(75, 2);

            _isDespawning = true;

            // do not repeat the animation
            if (!_wasUpdated)
                _despawnCounter = 9999;
        }
    }
}
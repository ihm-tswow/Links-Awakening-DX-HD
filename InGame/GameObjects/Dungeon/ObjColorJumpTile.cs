using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjColorJumpTile : GameObject
    {
        private readonly List<GameObject> _collidingObjects = new List<GameObject>();
        private readonly DictAtlasEntry[] _sprites = new DictAtlasEntry[3];

        private readonly CSprite _sprite;
        private readonly ObjHole _objHole;

        private bool _restoreMode;
        private float _restoreCounter;

        private int _currentState;
        private readonly int _startState;
        
        private Rectangle _collisionRectangle;
        private Rectangle _fieldRectangle;

        public ObjColorJumpTile() : base("color_tile_0") { }

        public ObjColorJumpTile(Map.Map map, int posX, int posY, int state) : base(map)
        {
            Tags = Values.GameObjectTag.None;

            _sprites[0] = Resources.GetSprite("color_tile_0");
            _sprites[1] = Resources.GetSprite("color_tile_1");
            _sprites[2] = Resources.GetSprite("color_tile_2");

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _startState = Math.Clamp(state, 0, 2);
            _currentState = _startState;
            _collisionRectangle = new Rectangle(posX, posY, Values.TileSize, Values.TileSize);

            _fieldRectangle = map.GetField(posX, posY);

            _sprite = new CSprite(_sprites[_currentState], EntityPosition, Vector2.Zero);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            var drawComponent = new DrawCSpriteComponent(_sprite, Values.LayerBottom);
            AddComponent(DrawComponent.Index, drawComponent);

            _restoreCounter = Game1.RandomNumber.Next(500, 1500);

            // spawn hole; delete jump object
            _objHole = new ObjHole(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 16, 14, Rectangle.Empty, 0, 1, 0) { IsActive = false };
            Map.Objects.SpawnObject(_objHole);
        }

        private void Update()
        {
            // when the player leaves the room the tiles will get restored to there original state
            if (_currentState != _startState && !_fieldRectangle.Contains(MapManager.ObjLink.EntityPosition.Position))
            {
                _restoreMode = true;
            }

            if (_restoreMode)
            {
                _restoreCounter -= Game1.DeltaTime;

                if (_restoreCounter <= 0)
                {
                    _restoreCounter = Game1.RandomNumber.Next(500, 1500);
                    OffsetState(-1);

                    if (_currentState == _startState)
                        _restoreMode = false;
                }
            }

            if (_currentState == 3)
                return;

            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects,
                _collisionRectangle.X, _collisionRectangle.Y, _collisionRectangle.Width, _collisionRectangle.Height, BodyComponent.Mask);

            foreach (var collidingObject in _collidingObjects)
            {
                // is the player standing on the tile -> jump
                if (collidingObject is ObjLink link &&
                    _collisionRectangle.Contains(link._body.BodyBox.Box.Center) && link._body.IsGrounded)
                {
                    link.StartJump();
                    OffsetState(1);
                }
                // could be changed to work with all bodies; but things like bombs are not affected by the tile
                else if (collidingObject is EnemyBonePutter bonePutter && 
                         collidingObject.Components[BodyComponent.Index] is BodyComponent bodyComponent)
                {
                    if (bonePutter.StartJump() &&
                        _collisionRectangle.Contains(bodyComponent.BodyBox.Box.Center))
                    {
                        OffsetState(1);
                    }
                }
            }

        }

        private void OffsetState(int offset)
        {
            _currentState += offset;
            _currentState = MathHelper.Clamp(_currentState, _startState, 3);

            // set the sprite
            if (_currentState < 3)
                _sprite.SetSprite(_sprites[_currentState]);

            _sprite.IsVisible = _currentState != 3;

            // activate/deactivate the hole if the tile is gone
            _objHole.IsActive = _currentState == 3;
        }
    }
}
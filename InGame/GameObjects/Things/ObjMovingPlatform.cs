using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjMovingPlatform : GameObject
    {
        private readonly DrawSpriteComponent _spriteComponent;
        private readonly List<GameObject> _collidingObjects = new List<GameObject>();

        private readonly DictAtlasEntry _sprite;

        private readonly CBox _moveBox;
        private Box _lastBox;

        private readonly CBox _collisionBox;
        private Box _lastCollisionBox;

        private readonly Vector2 _startPosition;
        private readonly Vector2 _endPosition;

        private Vector2 _newPosition;

        private float _waitCounter;
        private float _state;
        private float _dir;
        private readonly int _time;

        private readonly int _mode;

        private bool _isStandingOnTop;
        private bool _wasStandingOnTop;
        private bool _isMoving;

        public ObjMovingPlatform() : base("moving_platform") { }

        public ObjMovingPlatform(Map.Map map, int posX, int posY, int offsetX, int offsetY, float state, int time, int mode) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 32, 16);

            _sprite = Resources.GetSprite("moving_platform");

            _startPosition = new Vector2(posX, posY);
            _endPosition = _startPosition + new Vector2(offsetX, offsetY);

            _state = (state / 2) * time;
            _dir = state > time ? -1 : 1;
            _time = time;

            _mode = mode;

            _moveBox = new CBox(EntityPosition, 0, -8, 0, 32, 16, 16);
            _collisionBox = new CBox(EntityPosition, 0, 0, 32, 16, 16);

            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(_collisionBox, Values.CollisionTypes.Normal | Values.CollisionTypes.MovingPlatform));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _spriteComponent = new DrawSpriteComponent(
                Resources.SprObjects, EntityPosition, _sprite.SourceRectangle, Vector2.Zero, Values.LayerBottom));
        }

        private void Update()
        {
            _lastBox = _moveBox.Box;
            _lastCollisionBox = _collisionBox.Box;

            if (_mode == 0)
                UpdateMove0();
            else if (_mode == 1)
                UpdateMode1();
            else if (_mode == 2)
                UpdateMode2();

            var bodyDirection = _newPosition - EntityPosition.Position;

            EntityPosition.Set(_newPosition);

            MoveBodies(bodyDirection);
        }

        // normal moving platform
        private void UpdateMove0()
        {
            // update the move or the wait counter
            if (_waitCounter <= 0)
            {
                _state += (Game1.DeltaTime - _waitCounter) * _dir;
                _waitCounter = 0;
            }
            else
                _waitCounter -= Game1.DeltaTime;

            // finished moving to the start/end
            if (_state < 0)
            {
                _dir = -_dir;
                _state = 0;
                _waitCounter = 175;
            }
            else if (_state > _time)
            {
                _dir = -_dir;
                _state = _time;
                _waitCounter = 175;
            }

            var percentage = (float)(-Math.Cos((_state / _time) * Math.PI) + 1) * 0.5f;
            _newPosition = Vector2.Lerp(_startPosition, _endPosition, percentage);
        }

        // move to target if the player is standing on top of the plaform
        private void UpdateMode1()
        {
            // is the player standing on the platform?
            _isStandingOnTop = (_state == _time || (MapManager.ObjLink._body.BodyBox.Box.Left >= _moveBox.Box.Left &&
                                                    MapManager.ObjLink._body.BodyBox.Box.Right < _moveBox.Box.Right)) &&
                               MapManager.ObjLink._body.IsGrounded &&
                               MapManager.ObjLink._body.BodyBox.Box.Intersects(_moveBox.Box);

            if (!_isMoving && _isStandingOnTop)
                _isMoving = true;

            if (_isStandingOnTop)
            {
                _waitCounter = 500;
                _dir = 1;

                if (!_wasStandingOnTop && _state < _time - 250)
                    Game1.GameManager.PlaySoundEffect("D378-17-11");
            }

            if (_isMoving)
            {
                if (_waitCounter <= 0 || _dir == 1)
                    _state += Game1.DeltaTime * _dir;
                else
                    // wait a little bit and not move up directly after the player left the platform
                    _waitCounter -= Game1.DeltaTime;
            }

            if (_state < 0)
            {
                _dir = -_dir;
                _state = 0;
                _isMoving = false;
            }
            else if (_state > _time)
            {
                _dir = -_dir;
                _state = _time;
            }

            var percentage = (float)(-Math.Cos((_state / _time) * Math.PI) + 1) * 0.5f;
            _newPosition = Vector2.Lerp(_startPosition, _endPosition, percentage);

            _wasStandingOnTop = _isStandingOnTop;
        }

        // only move if the player is standing on top and holding an object
        private void UpdateMode2()
        {
            // is the player standing on the platform?
            var intersection = MapManager.ObjLink._body.BodyBox.Box.Intersects(_moveBox.Box) &&
                               MapManager.ObjLink._body.IsGrounded;
            _isStandingOnTop = MapManager.ObjLink._body.BodyBox.Box.Left >= _moveBox.Box.Left &&
                               MapManager.ObjLink._body.BodyBox.Box.Right < _moveBox.Box.Right && intersection;

            // set/unset the face
            _spriteComponent.Sprite.SourceRectangle.Y = _sprite.SourceRectangle.Y + (intersection ? 16 : 0);

            // start moving if the player is standing on the platform and is carrying something
            if (!_isMoving && _isStandingOnTop && MapManager.ObjLink.CurrentState == ObjLink.State.Carrying)
            {
                if (_state < _time)
                    Game1.GameManager.PlaySoundEffect("D378-17-11");
                _isMoving = true;
            }

            if (_isMoving)
                _state += Game1.DeltaTime;

            // finished moving down?
            if (_state > _time)
            {
                _isMoving = false;
                _state = _time;
            }

            var percentage = (float)(-Math.Cos((_state / _time) * Math.PI) + 1) * 0.5f;
            _newPosition = Vector2.Lerp(_startPosition, _endPosition, percentage);
        }

        private void MoveBodies(Vector2 direction)
        {
            // check for colliding bodies and push them forward
            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects,
                (int)_lastBox.Left, (int)_lastBox.Back - 8, (int)_lastBox.Width, (int)_lastBox.Height, BodyComponent.Mask);

            foreach (var collidingObject in _collidingObjects)
            {
                var body = (BodyComponent)collidingObject.Components[BodyComponent.Index];

                if (body.BodyBox.Box.Front <= _lastCollisionBox.Back && body.BodyBox.Box.Intersects(_lastBox))
                {
                    var offset = Vector2.Zero;

                    // body standing on the platform
                    if (body.IsGrounded)
                    {
                        var add = Vector2.Zero;

                        // align the body with the platform so that the body is not wobbling around
                        if (Math.Abs(body.VelocityTarget.X) < 0.1f && Math.Abs(body.Velocity.X) < 0.1f)
                        {
                            var distance = (body.Position.X + direction.X) - EntityPosition.X;
                            var distanceNormal = (int)Math.Round(distance * MapManager.Camera.Scale, MidpointRounding.AwayFromZero) / MapManager.Camera.Scale;

                            var dir = distanceNormal - distance;
                            if (Math.Abs(dir) > 0.005)
                                add.X += dir;
                        }

                        offset = direction + add;

                        // put the body on top of the platform
                        if (direction.Y != 0)
                            offset.Y = _collisionBox.Box.Back - body.BodyBox.Box.Front;
                    }
                    // did the platform already move into the body?
                    else if (body.BodyBox.Box.Intersects(_collisionBox.Box))
                    {
                        // move the body up/down
                        if (direction.Y < 0)
                            offset.Y = _collisionBox.Box.Back - body.BodyBox.Box.Front - 0.05f;
                        else if (direction.Y > 0)
                            offset.Y = _collisionBox.Box.Back - body.BodyBox.Box.Front + 0.05f;
                    }

                    if (offset != Vector2.Zero)
                    {
                        SystemBody.MoveBody(body, offset, body.CollisionTypes, false, false, false);
                        body.Position.NotifyListeners();
                    }
                }
            }
        }
    }
}

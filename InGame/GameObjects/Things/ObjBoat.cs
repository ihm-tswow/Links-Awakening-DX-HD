using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.GameObjects.NPCs;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjBoat : GameObject
    {
        private readonly ObjFishermanBoat _objFisherman;

        private readonly List<GameObject> _collidingObjects = new List<GameObject>();

        private readonly CBox _moveBox;
        private Box _lastBox;

        private readonly CBox _collisionBox;
        private Box _lastCollisionBox;

        private readonly Vector2 _topPosition;
        private readonly Vector2 _bottomPosition;

        private Vector2 _currentPosition;
        private Vector2 _positionOffset;

        private Vector2 _newPosition;

        private float _velocity;
        private float _offsetTime;
        private bool _offset;

        private bool _isStandingOnTop;

        public ObjBoat() : base("boat") { }

        public ObjBoat(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 20, posY + 15, 0);
            EntitySize = new Rectangle(-20, -15, 40, 15);

            _topPosition = EntityPosition.Position;
            _bottomPosition = _topPosition + new Vector2(0, 5);

            _currentPosition = _topPosition;

            _collisionBox = new CBox(EntityPosition, -16, -14, 32, 14, 16);
            _moveBox = new CBox(EntityPosition,
                _collisionBox.OffsetX, _collisionBox.OffsetY - 8, _collisionBox.OffsetZ,
                _collisionBox.Box.Width, _collisionBox.Box.Height, _collisionBox.Box.Depth);

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(_collisionBox, Values.CollisionTypes.Normal) { DirectionFlag = 8 });
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawSpriteComponent("boat", EntityPosition, Values.LayerBottom));

            _objFisherman = new ObjFishermanBoat(map, posX + 1, posY - 16, null, "npc_fisherman", "npc_bridge", new Rectangle(0, 1, 14, 20));
            map.Objects.SpawnObject(_objFisherman);
        }

        private void OnKeyChange()
        {
            var spawnKey = "spawn_necklace";
            var spawnNecklace = Game1.GameManager.SaveManager.GetString(spawnKey);
            if (!string.IsNullOrEmpty(spawnNecklace) && spawnNecklace == "1")
            {
                var spawnPosition = new Vector2(EntityPosition.X - 48, EntityPosition.Y - 16);
                Game1.GameManager.SaveManager.RemoveString(spawnKey);
                var objNecklace = new ObjItem(Map, (int)spawnPosition.X, (int)spawnPosition.Y, null, null, "trade11", null);
                objNecklace.SpawnBoatSequence();
                Map.Objects.SpawnObject(objNecklace);

                // spawn splash effect
                var fallAnimation = new ObjAnimator(Map, (int)(spawnPosition.X + 8), (int)(spawnPosition.Y + 5),
                    Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
                Map.Objects.SpawnObject(fallAnimation);
            }
        }

        private void Update()
        {
            _lastBox = _moveBox.Box;
            _lastCollisionBox = _collisionBox.Box;

            UpdateMove();

            var moveDirection = _newPosition - EntityPosition.Position;
            EntityPosition.Set(_newPosition);

            MoveBodies(moveDirection);
        }

        // move to target if the player is standing on top of the plaform
        private void UpdateMove()
        {
            // is the player standing on the platform?
            var wasStandingOnTop = _isStandingOnTop;
            _isStandingOnTop = MapManager.ObjLink._body.IsGrounded &&
                               MapManager.ObjLink._body.BodyBox.Box.Intersects(_moveBox.Box);

            // jumped ontop of the boat?
            if (_isStandingOnTop && !wasStandingOnTop)
            {
                _velocity = 0.75f;
                _offsetTime = -100;
                _offset = false;
                _currentPosition.Y += _positionOffset.Y;
                _positionOffset = Vector2.Zero;
            }
            // jumped off the boat?
            if (!_isStandingOnTop && wasStandingOnTop)
            {
                _velocity = 0.0f;
                _offsetTime = -100;
                _offset = false;
                _currentPosition.Y += _positionOffset.Y;
                _positionOffset = Vector2.Zero;
            }

            var target = 0.25f;

            // slow down at the top/bottom
            if (_isStandingOnTop && _currentPosition.Y > _bottomPosition.Y - 2)
                target = 0.0125f;
            if (!_isStandingOnTop && _currentPosition.Y < _topPosition.Y + 1)
                target = 0.05f;

            _velocity = AnimationHelper.MoveToTarget(_velocity, target, 0.05f * Game1.TimeMultiplier);

            if (!_offset)
            {
                // move up or down
                if (_isStandingOnTop)
                    _currentPosition.Y = AnimationHelper.MoveToTarget(_currentPosition.Y, _bottomPosition.Y, _velocity * Game1.TimeMultiplier);
                else
                    _currentPosition.Y = AnimationHelper.MoveToTarget(_currentPosition.Y, _topPosition.Y, _velocity * Game1.TimeMultiplier);

                if (_currentPosition.Y == _topPosition.Y)
                {
                    _offset = true;
                    _currentPosition.Y += 1;
                }
                if (_currentPosition.Y == _bottomPosition.Y)
                {
                    _offset = true;
                    _currentPosition.Y -= 1;
                }
            }

            if (_offset)
            {
                _offsetTime += Game1.DeltaTime;
                var offsetRadiant = _offsetTime / 1000 * MathF.PI * 2;
                var goUp = !_isStandingOnTop;

                //if (Game1.GameManager.DialogIsRunning() && _objFisherman.Animator.CurrentAnimation.Id == "down")
                //    goUp = _objFisherman.Animator.CurrentAnimation.Id == "down";

                if (goUp)
                    offsetRadiant = offsetRadiant + MathF.PI;

                if (!Game1.GameManager.DialogIsRunning())
                    _objFisherman.Animator.Play(MathF.Sin(offsetRadiant) > 0 ? "idle" : "down");

                // 1sec up/down
                _positionOffset.Y = MathHelper.Clamp(MathF.Cos(offsetRadiant) * 1.1f, -1, 1);
            }
            else if (!Game1.GameManager.DialogIsRunning())
            {
                _objFisherman.Animator.Play(_isStandingOnTop ? "down" : "idle");
            }

            _newPosition = _currentPosition + _positionOffset;
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

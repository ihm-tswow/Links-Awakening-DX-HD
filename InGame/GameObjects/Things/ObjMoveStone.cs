using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjMoveStone : GameObject
    {
        private readonly List<GameObject> _collidingObjects = new List<GameObject>();

        private readonly AiComponent _aiComponent;
        private readonly BodyComponent _body;
        private readonly CBox _box;

        private Vector2 _startPosition;
        private Vector2 _goalPosition;

        private readonly string _strKey;
        private readonly string _strKeyDir;
        private readonly string _strResetKey;
        private readonly int _allowedDirections;
        private readonly int _moveTime = 450;

        private int _moveDirection;
        private bool _freezePlayer;
        private bool _isResetting;

        // type 1 sets the key directly on push and resets it on spawn
        // used for the gravestone
        private int _type;

        public ObjMoveStone(Map.Map map, int posX, int posY, int moveDirections, string strKey,
            string spriteId, Rectangle collisionRectangle, int layer, int type, bool freezePlayer, string resetKey) : base(map, spriteId)
        {
            EntityPosition = new CPosition(posX, posY + 16, 0);
            EntitySize = new Rectangle(0, -16, 16, 16);

            _allowedDirections = moveDirections;
            _strKey = strKey;
            _strKeyDir = strKey + "_dir";
            _type = type;
            _freezePlayer = freezePlayer;
            _strResetKey = resetKey;

            _body = new BodyComponent(EntityPosition, 3, -13, 10, 10, 8)
            {
                IgnoreHeight = true,
                IgnoreHoles = true,
            };

            // moves the stone
            var movingTrigger = new AiTriggerCountdown(_moveTime, MoveTick, MoveEnd);
            var movedState = new AiState { Init = InitMoved };

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", new AiState());
            _aiComponent.States.Add("moving", new AiState { Trigger = { movingTrigger } });
            _aiComponent.States.Add("moved", movedState);
            new AiFallState(_aiComponent, _body, null, null, 200);
            _aiComponent.ChangeState("idle");

            _box = new CBox(EntityPosition, collisionRectangle.X, collisionRectangle.Y,
                collisionRectangle.Width, collisionRectangle.Height, 16);

            var sprite = Resources.GetSprite(spriteId);

            AddComponent(AiComponent.Index, _aiComponent);
            if (moveDirections != 0)
                AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, new PushableComponent(_box, OnPush) { InertiaTime = 450 });
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(_box, Values.CollisionTypes.Normal | Values.CollisionTypes.Hookshot));
            AddComponent(DrawComponent.Index, new DrawSpriteComponent(spriteId, EntityPosition, new Vector2(0, -sprite.SourceRectangle.Height), layer));

            if (!string.IsNullOrEmpty(_strResetKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));

            // set the key
            if (_type == 1 && _strKey != null)
                Game1.GameManager.SaveManager.SetString(_strKey, "0");
            if (!string.IsNullOrEmpty(_strKeyDir))
                Game1.GameManager.SaveManager.SetString(_strKeyDir, "-1");
        }

        private void OnKeyChange()
        {
            // check if the block should be moved back to the start position
            var keyState = Game1.GameManager.SaveManager.GetString(_strResetKey);
            if (keyState == "1" && _aiComponent.CurrentStateId == "moved")
                ResetToStart();
        }

        private void ResetToStart()
        {
            _isResetting = true;
            _goalPosition = _startPosition;
            _startPosition = EntityPosition.Position;

            _moveDirection = (_moveDirection + 2) % 4;

            if (!string.IsNullOrEmpty(_strKeyDir))
                Game1.GameManager.SaveManager.SetString(_strKeyDir, "-1");

            ToMoving();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact ||
                _aiComponent.CurrentStateId != "idle")
                return false;

            if (_type == 1 && Game1.GameManager.StoneGrabberLevel <= 0)
                return false;

            _moveDirection = AnimationHelper.GetDirection(direction);

            if (_allowedDirections != -1 && (_allowedDirections & (0x01 << _moveDirection)) == 0)
                return false;

            // only move if there is nothing blocking the way
            var pushVector = AnimationHelper.DirectionOffset[_moveDirection];
            var collidingRectangle = Box.Empty;
            if (Map.Objects.Collision(new Box(
                    EntityPosition.X + pushVector.X * 16,
                    EntityPosition.Y + pushVector.Y * 16 - 16, 0, 16, 16, 16),
                Box.Empty, Values.CollisionTypes.Normal, 0, 0, ref collidingRectangle))
                return true;

            _startPosition = EntityPosition.Position;

            _goalPosition = new Vector2(
                _startPosition.X + pushVector.X * 16,
                _startPosition.Y + pushVector.Y * 16);

            ToMoving();

            if (!string.IsNullOrEmpty(_strResetKey))
                Game1.GameManager.SaveManager.SetString(_strResetKey, "0");

            // set the key
            if (_type == 1 && !string.IsNullOrEmpty(_strKey))
                Game1.GameManager.SaveManager.SetString(_strKey, "1");

            if (_type == 1 && !string.IsNullOrEmpty(_strKeyDir))
                Game1.GameManager.SaveManager.SetString(_strKeyDir, _moveDirection.ToString());

            return true;
        }

        private void ToMoving()
        {
            Game1.GameManager.PlaySoundEffect("D378-17-11");
            _aiComponent.ChangeState("moving");
        }

        private void MoveTick(double time)
        {
            // the movement is fast in the beginning and slows down at the end
            var amount = (float)Math.Sin((_moveTime - time) / _moveTime * (Math.PI / 2f));

            Move(amount);

            if (!_isResetting && _freezePlayer)
                MapManager.ObjLink.FreezePlayer();
        }

        private void MoveEnd()
        {
            // finished moving
            Move(1);

            // set the key
            if (_type == 0 && !string.IsNullOrEmpty(_strKey))
                Game1.GameManager.SaveManager.SetString(_strKey, "1");
            // set the direction key
            if (_type == 0 && !string.IsNullOrEmpty(_strKeyDir))
                Game1.GameManager.SaveManager.SetString(_strKeyDir, _moveDirection.ToString());

            if (_isResetting)
            {
                _isResetting = false;
                _aiComponent.ChangeState("idle");
            }
            else
                _aiComponent.ChangeState("moved");

            // can fall into holes after finishing the movement animation
            _body.IgnoreHoles = false;
        }

        private void InitMoved()
        {
            // fall into the water
            if (_body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
            {
                Game1.GameManager.PlaySoundEffect("D360-14-0E");

                // spawn splash effect
                var fallAnimation = new ObjAnimator(Map,
                    (int)(_body.Position.X + _body.OffsetX + _body.Width / 2.0f),
                    (int)(_body.Position.Y + _body.OffsetY + _body.Height - 2),
                    Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
                Map.Objects.SpawnObject(fallAnimation);

                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void Move(float amount)
        {
            var lastBox = _box.Box;

            EntityPosition.Set(Vector2.Lerp(_startPosition, _goalPosition, amount));

            // @HACK: this kind of stuff should be inside the movement system

            // check for colliding bodies and push them forward
            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects,
                (int)EntityPosition.Position.X, (int)EntityPosition.Position.Y - 16, 17, 17, BodyComponent.Mask);

            foreach (var collidingObject in _collidingObjects)
            {
                if (collidingObject is ObjMoveStone)
                    continue;

                var body = (BodyComponent)collidingObject.Components[BodyComponent.Index];

                if (body.BodyBox.Box.Intersects(_box.Box) && !body.BodyBox.Box.Intersects(lastBox))
                {
                    var offset = Vector2.Zero;
                    if (_moveDirection == 0)
                        offset.X = _box.Box.Left - body.BodyBox.Box.Right - 0.05f;
                    else if (_moveDirection == 2)
                        offset.X = _box.Box.Right - body.BodyBox.Box.Left + 0.05f;
                    else if (_moveDirection == 1)
                        offset.Y = _box.Box.Back - body.BodyBox.Box.Front - 0.05f;
                    else if (_moveDirection == 3)
                        offset.Y = _box.Box.Front - body.BodyBox.Box.Back + 0.05f;

                    SystemBody.MoveBody(body, offset, body.CollisionTypes, false, false, false);
                    body.Position.NotifyListeners();
                }
            }
        }
    }
}
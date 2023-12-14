using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyKarakoro : GameObject
    {
        private readonly Color[] _colors = { new Color(17, 172, 66), new Color(255, 8, 42), new Color(25, 132, 255) };

        private readonly List<GameObject> _holeList = new List<GameObject>();
        private readonly BoxCollisionComponent _boxCollision;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly CSprite _sprite;
        private readonly DamageFieldComponent _damageField;
        private readonly CarriableComponent _carriableComponent;
        private readonly AiDamageState _damageState;

        private ObjHole _hole;

        private readonly string _strKey;
        private readonly string _strAllSetKey;
        private const float WalkSpeed = 0.25f;
        private const float RotateSpeed = 0.85f;
        private const int ShakeTime = 900;
        private int _direction;
        private readonly int _colorIndex;
        private float _initShakeSpriteOffsetX;
        private bool _smallBody;
        private bool _throwDamage;

        private Vector2 _holeStartPosition;
        private Vector2 _holeTargetPosition;
        private const int HoleTime = 350;
        private bool _inHole;

        public EnemyKarakoro() : base("karakoro") { }

        public EnemyKarakoro(Map.Map map, int posX, int posY, int colorIndex, string strKey, string strAllSetKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 12, 0);
            EntitySize = new Rectangle(-12, -15, 24, 16);

            _colorIndex = MathHelper.Clamp(colorIndex, 0, 2);
            _strKey = strKey;
            _strAllSetKey = strAllSetKey;

            // the strAllSetKey is meant to be set if all karakoro are in there hole
            // if it is not set we reset each karakoro individually so the player has to start
            // over if he dies or leaves after settings only some karakoros but not all
            if (!string.IsNullOrEmpty(strKey) &&
                (string.IsNullOrEmpty(strAllSetKey) ||
                Game1.GameManager.SaveManager.GetString(strAllSetKey) != "1"))
            {
                Game1.GameManager.SaveManager.SetString(strKey, "0");
            }
            else
            {
                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/karakoro");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8)
            {
                MoveCollision = OnMoveCollision,
                HoleOnPull = OnHolePull,
                HoleAbsorb = () => OnHolePull(Vector2.Zero, 100),
                IgnoreHoles = true,
                AbsorbPercentage = 0.9f,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.NPCWall,
                AvoidTypes = Values.CollisionTypes.Hole,
                FieldRectangle = map.GetField(posX, posY, 8),
                Bounciness = 0.55f,
                Drag = 0.9f,
                DragAir = 1.0f
            };

            var stateWalk = new AiState { Init = InitWalk };
            stateWalk.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("idle"), 750, 1000));
            var stateRotate = new AiState { Init = InitRotate };
            stateRotate.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("idle"), 500, 750));
            var stateIdle = new AiState { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerRandomTime(EndIdle, 250, 500));
            var stateBall = new AiState(UpdateBall) { Init = InitBall };
            stateBall.Trigger.Add(new AiTriggerCountdown(3300, null, () => _aiComponent.ChangeState("shake")));
            var stateCarried = new AiState() { Init = InitCarried };
            var stateShake = new AiState { Init = InitShake };
            stateShake.Trigger.Add(new AiTriggerCountdown(ShakeTime, ShakeTick, ShakeEnd));
            var stateHoleJump = new AiState { Init = InitHoleJump };
            stateHoleJump.Trigger.Add(new AiTriggerCountdown(HoleTime, HoleJumpTick, HoleJumpEnd));
            var stateHole = new AiState();
            var stateWrongHole = new AiState();
            stateWrongHole.Trigger.Add(new AiTriggerCountdown(400, null, EndWrongHole));

            _aiComponent = new AiComponent();

            _aiComponent.States.Add("walk", stateWalk);
            _aiComponent.States.Add("rotate", stateRotate);
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("ball", stateBall);
            _aiComponent.States.Add("carried", stateCarried);
            _aiComponent.States.Add("shake", stateShake);
            _aiComponent.States.Add("holeJump", stateHoleJump);
            _aiComponent.States.Add("hole", stateHole);
            _aiComponent.States.Add("wrongHole", stateWrongHole);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 2) { HitMultiplierX = 2.5f, HitMultiplierY = 2.5f };

            _aiComponent.ChangeState(Game1.RandomNumber.Next(0, 2) == 0 ? "idle" : "walk");
            _aiComponent.ChangeState("walk");

            var damageBox = new CBox(EntityPosition, -8, -14, 0, 16, 14, 4);
            var hittableBox = new CBox(EntityPosition, -8, -14, 0, 16, 14, 8);
            var pushableBox = new CBox(EntityPosition, -8, -14, 0, 16, 14, 8);

            if (!string.IsNullOrEmpty(_strAllSetKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(CarriableComponent.Index, _carriableComponent = new CarriableComponent(
                new CRectangle(EntityPosition, new Rectangle(-8, -15, 16, 16)), CarryInit, CarryUpdate, CarryThrow)
            { IsActive = false });
            AddComponent(CollisionComponent.Index, _boxCollision = new BoxCollisionComponent(new CBox(EntityPosition, -8, -14, 16, 14, 8), Values.CollisionTypes.Enemy) { IsActive = false });
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, DrawSprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite) { Height = 1.0f, Rotation = 0.1f, ShadowWidth = 10, ShadowHeight = 5 });
        }

        private void OnKeyChange()
        {
            if (Game1.GameManager.SaveManager.GetString(_strAllSetKey, "0") == "1")
                Despawn();
        }

        private void Despawn()
        {
            _hole.IsActive = true;

            Map.Objects.SpawnObject(new ObjAnimator(Map,
                (int)EntityPosition.X - 8, (int)EntityPosition.Y - 16, Values.LayerPlayer, "Particles/spawn", "run", true));

            Map.Objects.DeleteObjects.Add(this);
        }

        private void UpdateBall()
        {
            if (_throwDamage)
            {
                var box = _body.BodyBox.Box;
                var hitCollision = Map.Objects.Hit(this, box.Center, box, HitType.ThrownObject, 2, false);
                if (hitCollision != 0)
                {
                    _body.Velocity.X = -_body.Velocity.X * 0.5f;
                    _body.Velocity.Y = -_body.Velocity.Y * 0.5f;
                }
            }
        }

        private void EndIdle()
        {
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            if (playerDistance.Length() < 38)
                _aiComponent.ChangeState("rotate");
            else
                _aiComponent.ChangeState("walk");
        }

        private void InitRotate()
        {
            var lastFrame = _animator.CurrentFrameIndex;

            if (_animator.CurrentAnimation.Id == "rotate")
            {
                _animator.Continue();
            }
            else
            {
                _animator.Play("rotate");

                // make sure to start the animation at the same frame as the current walk animation
                var directionFrame = _direction;
                if (directionFrame == 1)
                    directionFrame = 2;
                if (directionFrame == 2)
                    directionFrame = 1;

                _animator.SetFrame(directionFrame * 2 + lastFrame);
            }

            var direction = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (direction != Vector2.Zero)
                direction.Normalize();
            _body.VelocityTarget = direction * RotateSpeed;
        }

        private void InitCarried()
        {
            if (_aiComponent.LastStateId == "shake")
                _sprite.DrawOffset.X = _initShakeSpriteOffsetX;
        }

        private void InitBall()
        {
            _carriableComponent.IsActive = true;
            _damageField.IsActive = false;
            _body.IgnoreHoles = false;
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("ball");
        }

        private void InitWalk()
        {
            // walk into a random direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * WalkSpeed;

            _animator.Play("walk_" + _direction);
        }

        private void InitIdle()
        {
            _animator.Pause();
            _body.VelocityTarget = Vector2.Zero;

            // @HACK: the gets smaller when thrown;
            // if this would not be the case the enemy could be moved into a wall because he has a smaller collision box
            // so after the body was thrown we try to restore the original size
            if (_smallBody)
            {
                var box = new Box(EntityPosition.Position.X - 7, EntityPosition.Position.Y - 12, 0, 14, 12, 8);
                var cBox = Box.Empty;
                if (!Map.Objects.Collision(
                    box, Box.Empty, _body.CollisionTypes, _body.CollisionTypesIgnore, 0, _body.Level, ref cBox))
                {
                    _smallBody = false;
                    _body.OffsetX = -7;
                    _body.OffsetY = -12;
                    _body.Width = 14;
                    _body.Height = 12;
                }
            }
        }

        private void InitShake()
        {
            _initShakeSpriteOffsetX = _sprite.DrawOffset.X;
        }

        private void ShakeTick(double counter)
        {
            _sprite.DrawOffset.X = _initShakeSpriteOffsetX + (float)Math.Sin((ShakeTime - counter) / 1000 * (60 / 4f) * Math.PI) * 2;
        }

        private void ShakeEnd()
        {
            _carriableComponent.IsActive = false;
            _damageField.IsActive = true;
            _body.IgnoreHoles = true;
            _sprite.DrawOffset.X = _initShakeSpriteOffsetX;
            _aiComponent.ChangeState("walk");
        }

        private Vector3 CarryInit()
        {
            _smallBody = true;
            _body.OffsetX = -4;
            _body.OffsetY = -10;
            _body.Width = 8;
            _body.Height = 10;

            _aiComponent.ChangeState("carried");

            // the stone was picked up
            _body.IsActive = false;

            return EntityPosition.ToVector3();
        }

        private bool CarryUpdate(Vector3 newPosition)
        {
            if (!_body.FieldRectangle.Contains(new RectangleF(
                newPosition.X + _body.OffsetX, newPosition.Y + _body.OffsetY, _body.Width, _body.Height)))
                return false;

            EntityPosition.X = newPosition.X;
            EntityPosition.Y = newPosition.Y;
            EntityPosition.Z = newPosition.Z;
            EntityPosition.NotifyListeners();

            return true;
        }

        private void CarryThrow(Vector2 velocity)
        {
            _aiComponent.ChangeState("ball");

            _throwDamage = true;

            _body.IsActive = true;
            _body.IsGrounded = false;
            _body.JumpStartHeight = 0;

            var throwMultiplier = 0.75f;
            _body.Velocity.X = velocity.X * throwMultiplier;
            _body.Velocity.Y = velocity.Y * throwMultiplier;
            _body.Velocity.Z = 1.0f;
        }

        private void InitHoleJump()
        {
            // activate the collision so we can not walk into the ball in the hole
            _boxCollision.IsActive = true;
            _inHole = true;
            _carriableComponent.IsActive = false;
            _body.IsActive = false;
        }

        private void HoleJumpTick(double counter)
        {
            var lerpAmount = 1 - (float)(counter / HoleTime);
            var newPosition = Vector2.Lerp(_holeStartPosition, _holeTargetPosition, lerpAmount);
            EntityPosition.Set(newPosition);
            EntityPosition.Z = MathF.Sin(lerpAmount * MathF.PI) * 8;
        }

        private void HoleJumpEnd()
        {
            HoleJumpTick(0);

            // check if we are in the right hole
            if (_hole.Color == _colorIndex)
            {
                if (!string.IsNullOrEmpty(_strKey))
                    Game1.GameManager.SaveManager.SetString(_strKey, "1");

                Game1.GameManager.PlaySoundEffect("D378-04-04");
                _aiComponent.ChangeState("hole");
            }
            else
            {
                Game1.GameManager.PlaySoundEffect("D360-29-1D");
                _aiComponent.ChangeState("wrongHole");
            }

            EntityPosition.Set(_holeTargetPosition);
        }

        private void EndWrongHole()
        {
            // jump out of the hole
            _boxCollision.IsActive = false;
            _hole.IsActive = true;
            _inHole = false;
            _carriableComponent.IsActive = true;
            _body.IsActive = true;
            _body.Velocity.X = _body.FieldRectangle.Center.X < EntityPosition.X ? -1.25f : 1.25f;
            _body.Velocity.Z = 1.75f;
            _aiComponent.ChangeState("ball");
        }

        private void OnHolePull(Vector2 direction, float percentage)
        {
            if (!_inHole && percentage > 0.50f && _aiComponent.CurrentStateId == "ball")
            {
                // get the hole we are falling into
                var bodyBox = _body.BodyBox.Box;

                _holeList.Clear();
                Map.Objects.GetComponentList(
                    _holeList, (int)bodyBox.X, (int)bodyBox.Y, (int)bodyBox.Width, (int)bodyBox.Height, CollisionComponent.Mask);

                foreach (var gameObjectHole in _holeList)
                {
                    var collisionComponent = gameObjectHole.Components[CollisionComponent.Index] as CollisionComponent;
                    var collidingBox = Box.Empty;
                    if (collisionComponent == null ||
                        (collisionComponent.CollisionType & Values.CollisionTypes.Hole) == 0 ||
                        !collisionComponent.Collision(bodyBox, 0, 0, ref collidingBox))
                        continue;

                    if (gameObjectHole is ObjHole holeObject)
                    {
                        _hole = holeObject;
                        _hole.IsActive = false;

                        _holeStartPosition = EntityPosition.Position;
                        _holeTargetPosition = new Vector2(holeObject.Center.X, holeObject.Center.Y + 8);

                        _aiComponent.ChangeState("holeJump");

                        return;
                    }
                }
            }
        }

        private void DrawSprite(SpriteBatch spriteBatch)
        {
            _sprite.Draw(spriteBatch);

            // draw the colored part of the sprite
            var sourceX = _sprite.SourceRectangle.X;
            _sprite.SourceRectangle.X += (int)(28 / _sprite.Scale);

            _sprite.Color = _colors[_colorIndex];
            _sprite.Draw(spriteBatch);

            _sprite.SourceRectangle.X = sourceX;
            _sprite.Color = Color.White;
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState() || originObject == this)
                return Values.HitCollision.None;

            if (!_inHole)
            {
                Game1.GameManager.PlaySoundEffect("D360-03-03");

                _aiComponent.ChangeState("ball");
                _damageState.HitKnockBack(originObject, direction, type, pieceOfPower, false);

                return Values.HitCollision.Blocking;
            }

            if (type == HitType.Bow)
                return Values.HitCollision.Repelling;

            return Values.HitCollision.Particle | Values.HitCollision.Blocking;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            var pushStrength = 1f;
            if (!_inHole && type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * pushStrength, direction.Y * pushStrength, _body.Velocity.Z);

            return true;
        }

        private void OnMoveCollision(Values.BodyCollision direction)
        {
            if ((direction & Values.BodyCollision.Horizontal) != 0)
                _body.Velocity.X = -_body.Velocity.X * 0.5f;
            if ((direction & Values.BodyCollision.Vertical) != 0)
                _body.Velocity.Y = -_body.Velocity.Y * 0.5f;
            if ((direction & Values.BodyCollision.Floor) != 0)
            {
                // stop dealing damage after hitting the floor
                _throwDamage = false;

                if (_body.Velocity.Z == 0)
                    _body.Velocity *= 0.5f;
                else
                {
                    _body.Velocity.X *= 0.8f;
                    _body.Velocity.Y *= 0.8f;
                }
            }
        }
    }
}
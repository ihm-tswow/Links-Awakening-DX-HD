using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.Map;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossBlaino : GameObject
    {
        private readonly DictAtlasEntry _gloveSprite;

        private readonly MBossBlainoGlove _objGlove;

        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly CSprite _sprite;
        private readonly Animator _animator;
        private readonly AnimationComponent _animationComponent;

        private readonly string _saveKey;

        private const int HitTime1 = 400;
        private const int HitTime2 = 700;
        private const int HitTime3 = 125;

        private const int TimeSwing0 = 5000;
        private const int TimeSwing1 = 400;
        private const int TimeSwing2 = 300;

        private Vector2 _spawnPosition;

        private Vector2 _glovePosition;
        private Vector2 _gloveStartPosition;
        private Vector2 _gloveTargetPosition;

        private Vector2 _startPosition;
        private Vector2 _targetPosition;
        private Vector2 _lastPosition;

        private Vector2 _swingOrigin = new Vector2(0, -10);

        private float _swingStartRotation;
        private float _swingStartDistance;
        private float _lastSwingRotation;

        private int _direction = -1;
        private int _boxCount;
        private int _jumpFollowDelay;

        private bool _drawGlove;

        public MBossBlaino() : base("blaino") { }

        public MBossBlaino(Map.Map map, int posX, int posY, string saveKey, string resetDoor) : base(map)
        {
            if (!string.IsNullOrEmpty(saveKey) &&
                Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-16, -32, 32, 32);

            _saveKey = saveKey;

            _spawnPosition = EntityPosition.Position;

            _gloveSprite = Resources.GetSprite("blaino glove");

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/blaino");
            _animator.Play("jump");

            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8)
            {
                SimpleMovement = false,
                Drag = 0.65f,
                DragAir = 0.75f,
                Gravity = -0.15f,
                MoveCollision = OnMoveCollision,
                FieldRectangle = map.GetField(posX, posY, 16),
            };

            var stateWaiting = new AiState(UpdateWaiting);
            var stateJumping = new AiState(UpdateJumping) { Init = InitJump };
            var stateBox = new AiState(UpdateBox) { Init = InitBox };

            var stateHit0 = new AiState(UpdateHit0) { Init = InitHit };
            var stateHit1 = new AiState();
            stateHit1.Trigger.Add(new AiTriggerCountdown(HitTime1, TickHit1, () => TickHit1(0)));
            var stateHit2 = new AiState();
            stateHit2.Trigger.Add(new AiTriggerCountdown(HitTime2, TickHit2, () => TickHit2(0)));
            var stateHit3 = new AiState() { Init = InitHit3 };
            stateHit3.Trigger.Add(new AiTriggerCountdown(HitTime3, TickHit3, () => TickHit3(0)));
            var stateHitBlocked = new AiState();
            stateHitBlocked.Trigger.Add(new AiTriggerCountdown(250, null, () => _aiComponent.ChangeState("hit3")));

            var stateSwing0 = new AiState() { Init = InitSwing0 };
            stateSwing0.Trigger.Add(new AiTriggerCountdown(TimeSwing0, TickSwing0, () => TickSwing0(0)));
            var stateSwing1 = new AiState();
            stateSwing1.Trigger.Add(new AiTriggerCountdown(TimeSwing1, null, () => _aiComponent.ChangeState("swing2")));
            var stateSwing2 = new AiState() { Init = InitSwing2 };
            stateSwing2.Trigger.Add(new AiTriggerCountdown(TimeSwing2, TickSwing2, () => TickSwing2(0)));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("jumping", stateJumping);
            _aiComponent.States.Add("box", stateBox);
            _aiComponent.States.Add("hit0", stateHit0);
            _aiComponent.States.Add("hit1", stateHit1);
            _aiComponent.States.Add("hit2", stateHit2);
            _aiComponent.States.Add("hit3", stateHit3);
            _aiComponent.States.Add("hitBlocked", stateHitBlocked);
            _aiComponent.States.Add("swing0", stateSwing0);
            _aiComponent.States.Add("swing1", stateSwing1);
            _aiComponent.States.Add("swing2", stateSwing2);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 8, true, false);
            _damageState.AddBossDamageState(OnDeath);
            _damageState.ExplosionOffsetY = 8;
            _aiComponent.ChangeState("waiting");

            var hittableBox = new CBox(EntityPosition, -6, -16, 0, 12, 16, 8, true);
            var damageBox = new CBox(EntityPosition, -8, -14, 0, 16, 14, 8, true);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(PushableComponent.Index, new PushableComponent(damageBox, OnPush));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, Draw, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));

            _objGlove = new MBossBlainoGlove(map, this, EntityPosition.Position, resetDoor);
            Map.Objects.SpawnObject(_objGlove);
        }

        private void UpdateWaiting()
        {
            // jump around the start position
            if (_body.IsGrounded)
            {
                var targetDirection = _spawnPosition - EntityPosition.Position;
                if (targetDirection == Vector2.Zero)
                    targetDirection = new Vector2(-1, -0.25f);
                targetDirection.Normalize();

                _body.VelocityTarget = targetDirection * 0.25f;
                _body.Velocity.Z = 1;
            }

            // player entered the room?
            if (_body.FieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
            {
                // start boss music
                Game1.GameManager.SetMusic(79, 2);

                _aiComponent.ChangeState("jumping");
            }
        }

        private void InitJump()
        {
            _animator.Play("jump");
        }

        private void UpdateJumping()
        {
            GloveAnimationUpdate();

            // landed after a jump?
            if (!_body.IsGrounded)
                return;

            var playerPosition = MapManager.ObjLink.EntityPosition.Position;

            // jump infront of the player
            var verticalDistance = Math.Abs(EntityPosition.Y - MapManager.ObjLink.EntityPosition.Y) / 2;
            var sinDist = MathF.Sin((float)Game1.TotalGameTime / 500);
            var distanceToPlayer = 26 + sinDist * 2 + verticalDistance;
            var targetPosition = new Vector2(EntityPosition.X + _direction * distanceToPlayer, EntityPosition.Y);
            var targetDirection = playerPosition - targetPosition;
            var distance = targetDirection.Length();

            if (MapManager.ObjLink.IsStunned())
            {
                if (distance < 4)
                {
                    _aiComponent.ChangeState("swing0");
                    return;
                }
            }
            else if (distance < 32)
            {
                // only hit when we are clost to the player
                if (Game1.RandomNumber.Next(0, 3) == 0 && sinDist <= 0)
                {
                    if (Game1.RandomNumber.Next(0, 4) < 3)
                    {
                        ToBox(true);
                        return;
                    }
                    else
                    {
                        _aiComponent.ChangeState("hit0");
                        return;
                    }
                }
            }

            if (_jumpFollowDelay <= 0)
            {
                var speedMultiplier = MathHelper.Clamp(distance / 12, 0.25f, 1.0f);
                if (targetDirection != Vector2.Zero)
                    targetDirection.Normalize();
                _body.VelocityTarget = targetDirection * speedMultiplier;
            }
            else
            {
                _jumpFollowDelay--;
            }

            // jump
            _body.Velocity.Z = 1;

            var playerDirection = EntityPosition.Position - playerPosition;
            if (Math.Abs(playerDirection.X) > 8)
            {
                if (playerDirection.X < 0)
                    _direction = 1;
                else
                    _direction = -1;
            }

            _animationComponent.MirroredH = _direction == 1;
        }

        private void BodyMove(float percentage)
        {
            // adjust the start/target positions if the body has moved
            if (_lastPosition != Vector2.Zero)
            {
                var positionOffset = EntityPosition.Position - _lastPosition;
                _startPosition += positionOffset;
                _targetPosition += positionOffset;
            }

            var targetPosition = Vector2.Lerp(_startPosition, _targetPosition, percentage);

            // body movement does not work well because the steps are too big in lower framerates
            targetPosition.X = MathHelper.Clamp(targetPosition.X, _body.FieldRectangle.X + 7, _body.FieldRectangle.Right - 7);
            EntityPosition.Set(targetPosition);

            _lastPosition = targetPosition;
        }

        private void SetGlovePosition(Vector2 newPosition)
        {
            _glovePosition = newPosition;
            _objGlove.EntityPosition.Set(new Vector2(
                EntityPosition.X + _glovePosition.X * -_direction - (_direction == 1 ? 11 : 0),
                EntityPosition.Y - EntityPosition.Z + _glovePosition.Y));
        }

        private void InitSwing0()
        {
            _body.VelocityTarget = Vector2.Zero;
            _startPosition = EntityPosition.Position;
            _targetPosition = _startPosition + new Vector2(-_direction * 23, 0);
            _lastPosition = Vector2.Zero;

            _objGlove.IsActive = true;
            _objGlove.SetHitDirection(_direction);
            _drawGlove = true;

            _animator.Play("hit1");
        }

        private void TickSwing0(double counter)
        {
            var percentage = (float)(TimeSwing0 - counter) / 75 + MathF.PI / 2 - MathF.Asin(3 / 4f);

            var finishedSwing = false;
            if ((_lastSwingRotation % MathF.PI) > (percentage % MathF.PI) && percentage / MathF.PI >= 5)
            {
                percentage = MathF.PI;
                finishedSwing = true;
            }

            // moving back
            var movePercentage = MathHelper.Clamp((float)((TimeSwing0 - counter) / 750), 0, 1);
            BodyMove(MathF.Sin(movePercentage * MathF.PI / 2));

            // update the glove position
            var offset = new Vector2(2 - MathF.Cos(percentage) * 4 + MathF.Sin(percentage) * 2, -6 + MathF.Sin(percentage) * 8);
            SetGlovePosition(new Vector2(5, -14) + offset);

            if (counter == 0 || finishedSwing)
                _aiComponent.ChangeState("swing1");

            _lastSwingRotation = percentage;
        }

        private void InitSwing2()
        {
            _objGlove.SetKnockoutMode(true);

            _startPosition = EntityPosition.Position;
            _targetPosition = _startPosition + new Vector2(_direction * 36, 0);
            _lastPosition = Vector2.Zero;

            var originDirection = new Vector2(_glovePosition.X + 5.5f, _glovePosition.Y + 5.5f) - _swingOrigin;
            _swingStartDistance = originDirection.Length();
            _swingStartRotation = MathF.Atan2(originDirection.Y, originDirection.X);
        }

        private void TickSwing2(double counter)
        {
            var percentage = MathHelper.Clamp((float)((TimeSwing2 - counter) / (TimeSwing2 - 75)), 0, 1);
            var newRotation = MathHelper.Lerp(_swingStartRotation, _swingStartRotation + 3.75f, percentage);

            // moving forward
            BodyMove(MathF.Sin(percentage * MathF.PI / 2));

            SetGlovePosition(new Vector2(_swingOrigin.X - 5.5f, _swingOrigin.Y - 5.5f) + new Vector2(MathF.Cos(newRotation), MathF.Sin(newRotation)) * _swingStartDistance);

            // change animation frame
            if (newRotation > _swingStartRotation + 1.85f)
                _animator.Play("hit2");

            if (counter == 0)
            {
                _drawGlove = false;
                _objGlove.IsActive = false;
                _objGlove.SetKnockoutMode(false);
                _aiComponent.ChangeState("jumping");
            }
        }

        private void InitHit()
        {
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("hit0");
        }

        private void UpdateHit0()
        {
            if (!_animator.IsPlaying)
            {
                _startPosition = EntityPosition.Position;
                _targetPosition = _startPosition + new Vector2(-_direction * 6, 0);
                _lastPosition = Vector2.Zero;

                _objGlove.IsActive = true;
                _objGlove.SetHitDirection(_direction);
                _drawGlove = true;

                _animator.Play("hit1");
                _aiComponent.ChangeState("hit1");
            }
        }

        private void TickHit1(double time)
        {
            // move 6px back
            var percentage = 1 - (float)(time / HitTime1);

            BodyMove(percentage);

            // update the glove position
            SetGlovePosition(new Vector2(5, -14));

            if (time == 0)
            {
                _startPosition = EntityPosition.Position;
                _targetPosition = _startPosition + new Vector2(_direction * 40, 0);
                _lastPosition = Vector2.Zero;

                _objGlove.SetStunMode(true);

                TickHit2(HitTime2);
                _animator.Play("hit2");
                _aiComponent.ChangeState("hit2");
            }
        }

        private void TickHit2(double time)
        {
            var percentageGlove = MathHelper.Clamp((float)(HitTime2 - time - 25) / 125, 0, 1);
            var sPercentageGlove = 1 - MathF.Cos(percentageGlove * MathF.PI / 2);

            var percentage = MathHelper.Clamp((float)(HitTime2 - time - 75) / 125, 0, 1);
            var sPercentage = 1 - MathF.Cos(percentage * MathF.PI / 2);

            // move forward
            BodyMove(sPercentage);

            // update the glove position
            SetGlovePosition(Vector2.Lerp(new Vector2(-15, -11), new Vector2(-15 - 16, -11), sPercentageGlove));

            if (time == 0)
                _aiComponent.ChangeState("hit3");
        }

        private void InitHit3()
        {
            _gloveStartPosition = _glovePosition;
            _gloveTargetPosition = new Vector2(-15, -11);

            _objGlove.SetStunMode(false);
        }

        private void TickHit3(double time)
        {
            var percentage = 1 - (float)(time / HitTime3);

            // update the glove position
            SetGlovePosition(Vector2.Lerp(_gloveStartPosition, _gloveTargetPosition, percentage));

            if (time == 0)
            {
                _objGlove.IsActive = false;
                _drawGlove = false;
                _aiComponent.ChangeState("jumping");
            }
        }

        private void ToBox(bool isShort)
        {
            _body.VelocityTarget = Vector2.Zero;

            _aiComponent.ChangeState("box");

            _boxCount = Game1.RandomNumber.Next(1, 7) - 3;

            if (isShort)
                _animator.Play("box_short");
            else
                _animator.Play("box");
        }

        private void InitBox()
        {
            Game1.GameManager.PlaySoundEffect("D378-10-0A");
        }

        private void UpdateBox()
        {
            GloveAnimationUpdate();

            if (!_animator.IsPlaying)
            {
                // box again?
                if (_boxCount > 0)
                {
                    Game1.GameManager.PlaySoundEffect("D378-10-0A");
                    _animator.Play("prebox");
                }
                else
                    _aiComponent.ChangeState("jumping");

                _boxCount--;
            }
        }

        private void GloveAnimationUpdate()
        {
            if (_animator.CollisionRectangle != Rectangle.Empty)
            {
                _objGlove.IsActive = true;
                _objGlove.EntityPosition.Set(EntityPosition.Position +
                    new Vector2(_animator.CollisionRectangle.X * -_direction - (_direction == 1 ? 11 : 0), _animator.CollisionRectangle.Y));
            }
            else
            {
                _objGlove.IsActive = false;
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _sprite.Draw(spriteBatch);

            // draw the glove
            if (_drawGlove)
            {
                DrawHelper.DrawNormalized(spriteBatch, _gloveSprite,
                    new Vector2(EntityPosition.X + _glovePosition.X * -_direction - (_direction == 1 ? 11 : 0), EntityPosition.Y - EntityPosition.Z + _glovePosition.Y), Color.White);
            }
        }

        private void OnMoveCollision(Values.BodyCollision collision)
        {
            // make sure to not continuesly jump into the wall
            if ((collision & Values.BodyCollision.Horizontal) != 0 &&
                _aiComponent.CurrentStateId == "jumping")
            {
                _direction = -_direction;
            }
        }

        public bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            if (pushType == PushableComponent.PushType.Impact)
            {
                _jumpFollowDelay = 2;

                var mult = 2.25f;
                _body.Velocity = new Vector3(direction.X * mult, direction.Y * mult, _body.Velocity.Z);
                _body.VelocityTarget = direction * 0.125f;
            }

            return true;
        }

        public void GlovePush(Vector2 direction)
        {
            _objGlove.IsActive = false;
            _aiComponent.ChangeState("hitBlocked");

            _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);
        }

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState() ||
                _aiComponent.CurrentStateId == "damage" || _aiComponent.CurrentStateId == "dying")
                return Values.HitCollision.None;

            if (damageType == HitType.Bomb || damageType == HitType.Bow || damageType == HitType.MagicRod)
                return Values.HitCollision.Enemy;

            var hitDir = AnimationHelper.GetDirection(direction);
            if ((hitDir == 2 && _direction == -1) || (hitDir == 0 && _direction == 1))
                return Values.HitCollision.RepellingParticle;

            if (damageType == HitType.Hookshot)
                damage = 1;

            // different drag than needed for the jumps
            _body.DragAir = 0.75f;

            _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);

            if (_damageState.CurrentLives <= 0)
            {
                _animator.IsPlaying = false;
                _body.VelocityTarget = Vector2.Zero;
                Map.Objects.DeleteObjects.Add(_objGlove);
            }

            return Values.HitCollision.Enemy;
        }

        private void OnDeath()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            // stop boss music
            Game1.GameManager.SetMusic(-1, 2);

            Game1.GameManager.PlaySoundEffect("D360-27-1B");
            Map.Objects.SpawnObject(new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 8));

            Map.Objects.DeleteObjects.Add(this);
        }
    }
}
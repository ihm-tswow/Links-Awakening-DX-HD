using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    internal class MBossRollingBones : GameObject
    {
        private readonly MBossBone _bone;

        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly CSprite _sprite;
        private readonly Animator _animator;

        private readonly string _triggerKey;
        private readonly string _saveKey;

        private const float MoveSpeed = 1.2f;
        private const int Lives = 8;

        private Vector2 _moveDirection;

        private float _roomMiddle;
        private float _deathCount;
        private float _burstCounter;
        private int _jumpCount;

        public MBossRollingBones() : base("rolling bones") { }

        public MBossRollingBones(Map.Map map, int posX, int posY, string triggerKey, string saveKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-12, -30, 24, 32);

            _triggerKey = triggerKey;
            _saveKey = saveKey;

            if (!string.IsNullOrEmpty(_saveKey) &&
                Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/mbossOneBoss");
            _animator.Play("walk");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-10, -23));

            _body = new BodyComponent(EntityPosition, -9, -12, 18, 12, 8)
            {
                Drag = 0.65f,
                DragAir = 0.75f,
                Gravity = -0.15f,
                MoveCollision = OnMoveCollision,
                FieldRectangle = map.GetField(posX, posY)
            };
            _roomMiddle = _body.FieldRectangle.Center.Y + 4;

            var stateWaiting = new AiState();
            var stateInitJump = new AiState(UpdateInitJump);
            var stateIdle = new AiState(UpdateIdle);
            stateIdle.Trigger.Add(new AiTriggerCountdown(300, null, () => _aiComponent.ChangeState("jumping")));
            var stateJumping = new AiState(UpdateJumping) { Init = InitJump };
            var statePushing = new AiState();
            statePushing.Trigger.Add(new AiTriggerCountdown(500, null, ToPushed));
            var statePushed = new AiState(UpdatePushed);
            var stateBlink = new AiState();
            stateBlink.Trigger.Add(new AiTriggerCountdown(1000, UpdateBlink, () => _aiComponent.ChangeState("death")));
            var stateDeath = new AiState(UpdateDeath);
            stateDeath.Trigger.Add(new AiTriggerCountdown(2000, UpdateBlink, RemoveObject));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("initJump", stateInitJump);
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("jumping", stateJumping);
            _aiComponent.States.Add("pushing", statePushing);
            _aiComponent.States.Add("pushed", statePushed);
            _aiComponent.States.Add("blink", stateBlink);
            _aiComponent.States.Add("death", stateDeath);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, Lives, false, false) { OnDeath = OnDeath };
            _aiComponent.ChangeState("waiting");

            var damageCollider = new CBox(EntityPosition, -7, -11, 0, 14, 11, 8, true);
            var hittableBox = new CBox(EntityPosition, -8, -14, 0, 16, 14, 8, true);

            if (!string.IsNullOrEmpty(_triggerKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite) { ShadowWidth = 16, ShadowHeight = 6 });

            // check if we are on the left or the right side of the room
            int boneOffset = 0;
            if (posX > _body.FieldRectangle.X + Values.FieldWidth / 2)
            {
                _moveDirection.X = -MoveSpeed;
                _animator.Play("idle_0");
                boneOffset = 80;
            }
            else
            {
                _moveDirection.X = MoveSpeed;
                _animator.Play("idle_1");
                boneOffset = 32;
            }

            _bone = new MBossBone(map, posX, posY, boneOffset);
            map.Objects.SpawnObject(_bone);
        }

        private void OnMoveCollision(Values.BodyCollision collision)
        {
            if ((collision & Values.BodyCollision.Floor) != 0)
            {
                Game1.GameManager.PlaySoundEffect("D360-32-20");
            }
        }

        private void KeyChanged()
        {
            // activate the boss after entering the room
            if (_aiComponent.CurrentStateId == "waiting" && Game1.GameManager.SaveManager.GetString(_triggerKey) == "1")
            {
                // start boss music
                Game1.GameManager.SetMusic(79, 2);

                _aiComponent.ChangeState("initJump");
            }
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
            {
                _body.Velocity.X += direction.X * 0.35f;
                _body.Velocity.Y += direction.Y * 0.35f;
            }

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState() || _aiComponent.CurrentStateId == "death")
                return Values.HitCollision.None;

            if (damageType == HitType.Bomb || damageType == HitType.Bow || damageType == HitType.MagicRod)
                damage *= 2;

            // different drag than needed for the jumps
            _body.DragAir = 0.75f;

            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private void UpdateIdle()
        {
            _animator.Play("idle_" + (_moveDirection.X < 0 ? 0 : 1));
        }

        private void InitJump()
        {
            // jump to the top or to the bottom?
            if (EntityPosition.Y > _roomMiddle)
                _moveDirection.Y = -0.5f;
            else
                _moveDirection.Y = 0.5f;

            _body.DragAir = 1.0f;
            _body.Velocity = new Vector3(_moveDirection.X, _moveDirection.Y, 2);
            _body.IsGrounded = false;

            _animator.Play("jump_" + (_moveDirection.X < 0 ? 0 : 1));
        }

        private void UpdateInitJump()
        {
            if (_body.IsGrounded)
            {
                if (_jumpCount < 3)
                {
                    _jumpCount++;

                    if (EntityPosition.Y > _roomMiddle)
                        _moveDirection.Y = -0.5f;
                    else
                        _moveDirection.Y = 0.5f;

                    _body.DragAir = 1.0f;
                    _body.Velocity = new Vector3(_moveDirection.X * 0.375f, _moveDirection.Y * 0.375f, 1.0f);
                    _body.IsGrounded = false;
                }
                else
                {
                    _aiComponent.ChangeState("pushing");
                }
            }
        }

        private void UpdateJumping()
        {
            // landed after a jump?
            if (_body.IsGrounded)
            {
                if ((_body.LastVelocityCollision & Values.BodyCollision.Horizontal) != 0 &&
                    Math.Sign(_body.Velocity.X) == Math.Sign(_moveDirection.X))
                {
                    _aiComponent.ChangeState("pushing");
                    _moveDirection.X = -_moveDirection.X;
                    _body.Velocity.X = -_body.Velocity.X * 0.125f;
                    _animator.Play("idle_" + (_moveDirection.X < 0 ? 0 : 1));
                    return;
                }

                _aiComponent.ChangeState("idle");
            }
        }

        private void ToPushed()
        {
            // push the bar
            _bone.Push(_moveDirection.X < 0 ? 0 : 1);

            _animator.Play("push_" + (_moveDirection.X < 0 ? 0 : 1));
            _aiComponent.ChangeState("pushed");
        }

        private void UpdatePushed()
        {
            // finished pushing
            if (!_animator.IsPlaying)
                _aiComponent.ChangeState("idle");
        }

        private void UpdateBlink(double time)
        {
            _sprite.SpriteShader = time % 133 < 66 ? Resources.DamageSpriteShader0 : null;
        }

        private void UpdateDeath()
        {
            _burstCounter -= Game1.DeltaTime;
            if (_burstCounter < 0)
            {
                _burstCounter += 150;
                Game1.GameManager.PlaySoundEffect("D378-19-13");
            }

            _deathCount += Game1.DeltaTime;
            if (_deathCount > 100)
                _deathCount -= 100;
            else
                return;

            var posX = (int)EntityPosition.X + Game1.RandomNumber.Next(0, 32) - 8 - 16;
            var posY = (int)EntityPosition.Y - (int)EntityPosition.Z + Game1.RandomNumber.Next(0, 32) - 16 - 16;

            // spawn explosion effect
            Map.Objects.SpawnObject(new ObjAnimator(Map, posX, posY, Values.LayerBottom, "Particles/spawn", "run", true));
        }

        private void RemoveObject()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            // stop boss music
            Game1.GameManager.SetMusic(-1, 2);

            Game1.GameManager.PlaySoundEffect("D360-27-1B");
            Map.Objects.SpawnObject(new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 8));

            Map.Objects.DeleteObjects.Add(this);
            _bone.Delete();
        }

        private void OnDeath(bool pieceOfPower)
        {
            Game1.GameManager.PlaySoundEffect("D370-16-10");

            _aiComponent.ChangeState("blink");
            _damageState.IsActive = false;
        }
    }
}
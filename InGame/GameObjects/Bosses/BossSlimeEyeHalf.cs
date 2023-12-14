using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossSlimeEyeHalf : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly Animator _animator;
        private readonly PushableComponent _pushableComponent;
        private readonly BodyDrawShadowComponent _shadowComponent;
        private readonly CSprite _sprite;

        private Rectangle _fieldRectangle;

        private readonly string _saveKey;
        private readonly string _halfKey;

        private const int Lives = 4;
        private int _jumpHeight = 100;

        private bool _highJump;
        private bool _wasHit;
        private bool _doubleJump;

        public BossSlimeEyeHalf(Map.Map map, Vector2 position, string animationName, string saveKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, 0);
            EntitySize = new Rectangle(-16, -32, 32, 32);

            _saveKey = saveKey;
            _halfKey = _saveKey + "half";

            Game1.GameManager.SaveManager.SetString(_halfKey, "0");

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/nightmare eye");
            _animator.Play(animationName);

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _fieldRectangle = map.GetField((int)position.X, (int)position.Y, 16);

            _body = new BodyComponent(EntityPosition, -12, -24, 24, 22, 8)
            {
                Bounciness = 0.25f,
                Gravity = -0.15f,
                Drag = 0.85f,
                DragAir = 0.98f,
                IgnoreHeight = true
            };

            var hittableRectangle = new CBox(EntityPosition, -14, -24, 0, 28, 22, 8, true);
            var damageCollider = new CBox(EntityPosition, -14, -24, 28, 22, 8);

            var stateInit = new AiState();
            stateInit.Trigger.Add(new AiTriggerCountdown(50, null, InitJump)); // 166
            var stateWaiting = new AiState(UpdateWaiting);
            stateWaiting.Trigger.Add(new AiTriggerRandomTime(ToJumping, 750, 1250));
            var stateJumping = new AiState(UpdateJumping);
            var stateHighJumping = new AiState();
            stateHighJumping.Trigger.Add(new AiTriggerCountdown(300, UpdateHighJump, () => UpdateHighJump(0f)));
            var stateHighJumpingWait = new AiState();
            stateHighJumpingWait.Trigger.Add(new AiTriggerRandomTime(EndHighjump, 500, 1000));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("init", stateInit);
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("jumping", stateJumping);
            _aiComponent.States.Add("highJump", stateHighJumping);
            _aiComponent.States.Add("highJumpWaiting", stateHighJumpingWait);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, Lives, false) { BossHitSound = true };
            _damageState.AddBossDamageState(OnDeath);

            _aiComponent.ChangeState("init");

            _pushableComponent = new PushableComponent(_body.BodyBox, OnPush);
            AddComponent(PushableComponent.Index, _pushableComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableRectangle, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_body, _sprite)
            {
                ShadowWidth = 26,
                ShadowHeight = 8,
                OffsetY = -2,
                Height = Map.ShadowHeight * 1.25f
            });

            _damageState.SetDamageState();
        }

        private void InitJump()
        {
            _aiComponent.ChangeState("jumping");

            var direction = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            if (direction != Vector2.Zero)
                direction.Normalize();

            _body.Velocity = new Vector3(direction * 1.5f, 2.5f);
        }

        private void UpdateWaiting()
        {
            // start with the high jump?
            if (_wasHit && _body.Velocity.Length() < 0.25f)
                ToHighjump();

            UpdateAnimation();
        }

        private void ToJumping()
        {
            if (_wasHit)
                return;

            _aiComponent.ChangeState("jumping");

            // random direction
            var direction = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            float radius;

            // random direction
            var toPlayer = false;
            if (Game1.RandomNumber.Next(0, 4) < 3 && !_doubleJump)
            {
                radius = (Game1.RandomNumber.Next(0, 100) / 100f) * MathF.PI * 2;
            }
            else
            {
                toPlayer = true;
                _doubleJump = false;
                radius = MathF.Atan2(direction.Y, direction.X);
            }

            // 50% chance to jump twice towards the player
            _doubleJump = Game1.RandomNumber.Next(0, 2) == 0;

            var speed = (Game1.RandomNumber.Next(toPlayer ? 100 : 150, toPlayer ? 200 : 250) / 100f);
            var jumpDirection = new Vector2(MathF.Cos(radius), MathF.Sin(radius)) * speed;
            _body.Velocity = new Vector3(jumpDirection.X, jumpDirection.Y, 2);
        }

        private void UpdateJumping()
        {
            _sprite.Color = Color.White * MathF.Min((_jumpHeight - EntityPosition.Z) / 15f, 1);

            if (_body.IsGrounded)
            {
                _body.Velocity = Vector3.Zero;
                _aiComponent.ChangeState("waiting");
                _pushableComponent.IsActive = true;

                // came down from a high jump? -> shake the screen
                if (_highJump)
                {
                    _wasHit = false;
                    _highJump = false;

                    MapManager.ObjLink.GroundStun();

                    Game1.GameManager.PlaySoundEffect("D360-11-0B");
                    Game1.GameManager.ShakeScreen(250, 1, 2, 2.5f, 5.5f);
                }
            }

            UpdateAnimation();
        }

        private void ToHighjump()
        {
            _aiComponent.ChangeState("highJump");
            _body.Velocity = Vector3.Zero;
            _body.IgnoresZ = true;
            _highJump = true;
        }

        private void UpdateHighJump(double time)
        {
            EntityPosition.Z = (1 - MathF.Sin(((float)time / 300) * MathF.PI / 2)) * _jumpHeight;
            _sprite.Color = Color.White * MathF.Min((float)time / 50f, 1);
            _shadowComponent.Transparency = MathF.Min((float)time / 50f, 1);

            if (time == 0)
                _aiComponent.ChangeState("highJumpWaiting");

            UpdateAnimation();
        }

        private void EndHighjump()
        {
            _aiComponent.ChangeState("jumping");

            // clamp the position to land inside the room
            var newPosition = MapManager.ObjLink.EntityPosition.Position;
            newPosition.X = MathHelper.Clamp(newPosition.X,
                _fieldRectangle.X + _body.Width / 2, _fieldRectangle.Right - _body.Width / 2);
            newPosition.Y = MathHelper.Clamp(newPosition.Y + 8,
                _fieldRectangle.Y - _body.OffsetY,
                _fieldRectangle.Bottom - _body.OffsetY - _body.Height);

            EntityPosition.Set(newPosition);

            _body.Velocity.Z = 0;
            _body.IgnoresZ = false;

            _shadowComponent.Transparency = 1;
        }

        private void UpdateAnimation()
        {
            _animator.Play(_body.IsGrounded ? "half_floor" : "half_jump");
        }

        private void OnDeath()
        {
            if (!string.IsNullOrEmpty(_saveKey))
            {
                var key = Game1.GameManager.SaveManager.GetString(_halfKey);
                if (key != "1")
                    Game1.GameManager.SaveManager.SetString(_halfKey, "1");
                // killed both parts?
                else
                {
                    Game1.GameManager.PlaySoundEffect("D378-26-1A");
                    Game1.GameManager.SetMusic(-1, 2);
                    Game1.GameManager.SaveManager.SetString(_saveKey, "1");

                    // spawn big heart
                    Map.Objects.SpawnObject(new ObjItem(Map,
                        (int)EntityPosition.X - 8, (int)EntityPosition.Y - 24, "j", "d3_heartMeter", "heartMeterFull", null));
                }
            }

            Map.Objects.DeleteObjects.Add(this);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState())
                return Values.HitCollision.None;

            _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);

            _body.Velocity.X = direction.X * 5f;
            _body.Velocity.Y = direction.Y * 5f;

            _wasHit = true;

            return Values.HitCollision.Enemy;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type != PushableComponent.PushType.Impact)
                return false;

            _wasHit = true;

            _body.Velocity.X = direction.X * 1.5f;
            _body.Velocity.Y = direction.Y * 1.5f;

            return true;
        }
    }
}
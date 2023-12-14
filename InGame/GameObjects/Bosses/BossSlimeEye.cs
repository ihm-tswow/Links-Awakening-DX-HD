using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossSlimeEye : GameObject
    {
        private readonly EnemyGreenZol[] _zols = new EnemyGreenZol[2];

        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly Animator _animator;
        private readonly BodyDrawShadowComponent _shadowComponent;

        private readonly Vector2 _startPosition;

        private readonly string _enterKey;
        private readonly string _triggerKey;
        private readonly string _saveKey;

        private float _damageCounter;
        private float _lastStartDistance;
        private float _moveDistance;

        private int _fallHeight = 128;

        public BossSlimeEye() : base("slime_eye") { }

        public BossSlimeEye(Map.Map map, int posX, int posY, string enterKey, string triggerKey, string saveKey) : base(map)
        {
            if (!string.IsNullOrWhiteSpace(saveKey) && Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 16, posY + 16, _fallHeight);
            EntitySize = new Rectangle(-32, -32, 64, 32);

            _startPosition = EntityPosition.Position;

            _enterKey = enterKey;
            _triggerKey = triggerKey;
            _saveKey = saveKey;

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/nightmare eye");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            _sprite.Color = Color.Transparent;

            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            var fieldRectangle = map.GetField(posX, posY, 16);

            _body = new BodyComponent(EntityPosition, -21, -24, 42, 24, 8)
            {
                FieldRectangle = fieldRectangle,
                Bounciness = 0.25f,
                Drag = 0.85f,
                IgnoresZ = true,
                IsGrounded = false
            };

            var hittableRectangle = new CBox(EntityPosition, -8, -16, 16, 16, 8);
            var damageCollider = new CBox(EntityPosition, -21, -24, 42, 24, 8);

            var stateWaiting = new AiState();
            var stateSlimes = new AiState();
            stateSlimes.Trigger.Add(new AiTriggerRandomTime(TickSpawn, 1000, 1500));
            var stateFalling = new AiState(UpdateFalling);
            var stateSpawning = new AiState();
            var stateStopped = new AiState(UpdateStopped);
            stateStopped.Trigger.Add(new AiTriggerCountdown(1000, null, ToMoving));
            var stateMoving = new AiState(UpdateMoving);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("slimes", stateSlimes);
            _aiComponent.States.Add("falling", stateFalling);
            _aiComponent.States.Add("spawning", stateSpawning);
            _aiComponent.States.Add("stopped", stateStopped);
            _aiComponent.States.Add("moving", stateMoving);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 2, false);

            _aiComponent.ChangeState("waiting");

            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(_body.BodyBox, Values.CollisionTypes.Enemy));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableRectangle, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_body, _sprite)
            {
                ShadowWidth = 42,
                ShadowHeight = 12,
                OffsetY = -1,
                Height = Map.ShadowHeight * 1.25f,
                IsActive = false
            });
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
        }

        private void OnKeyChange()
        {
            if (!string.IsNullOrEmpty(_enterKey) &&
                Game1.GameManager.SaveManager.GetString(_enterKey) == "1" &&
                _aiComponent.CurrentStateId == "waiting")
            {
                Game1.GameManager.StartDialogPath("d3_boss");
                _aiComponent.ChangeState("slimes");
            }

            if (!string.IsNullOrEmpty(_triggerKey) &&
                Game1.GameManager.SaveManager.GetString(_triggerKey) == "1" &&
                _aiComponent.CurrentStateId == "slimes")
            {
                ToFalling();
            }
        }

        private void TickSpawn()
        {
            for (var i = 0; i < _zols.Length; i++)
            {
                if (_zols[i] == null || _zols[i].Map == null)
                {
                    var posX = Game1.RandomNumber.Next(0, 8);
                    var posY = Game1.RandomNumber.Next(0, 6);
                    // clamp the position to not land on a lamp
                    if (posY == 0 || posY == 5)
                        posX = Math.Clamp(posX, 1, 6);

                    var position = new Vector2(
                        _body.FieldRectangle.X + posX * 16,
                        _body.FieldRectangle.Y + posY * 16);

                    _zols[i] = new EnemyGreenZol(Map, (int)position.X, (int)position.Y, 100, true);
                    Map.Objects.SpawnObject(_zols[i]);

                    return;
                }
            }
        }

        private void ToFalling()
        {
            _aiComponent.ChangeState("falling");
            _body.IgnoresZ = false;
            _shadowComponent.IsActive = true;
        }

        private void UpdateFalling()
        {
            _sprite.Color = Color.White *
                            MathF.Min((_fallHeight - EntityPosition.Z) / 15f, 1);

            if (_body.IsGrounded)
            {
                Game1.GameManager.PlaySoundEffect("D378-12-0C");
                Game1.GameManager.ShakeScreen(500, 2, 4, 2.5f, 5.5f);

                MapManager.ObjLink.GroundStun();
                _aiComponent.ChangeState("stopped");
            }
        }

        private void UpdateStopped()
        {
            UpdateAnimation();
        }

        private void ToMoving()
        {
            _aiComponent.ChangeState("moving");

            var direction = _startPosition - EntityPosition.Position;
            var radius = MathF.Atan2(direction.Y, direction.X) + Game1.RandomNumber.Next(-25, 25) / 100f;
            _body.VelocityTarget = new Vector2(MathF.Cos(radius), MathF.Sin(radius)) * 0.1f;

            _moveDistance = Game1.RandomNumber.Next(0, 40) / 10f;
        }

        private void UpdateMoving()
        {
            // move
            var direction = _startPosition - EntityPosition.Position;
            var distance = direction.Length();

            if (_lastStartDistance < distance && distance > _moveDistance)
            {
                _body.VelocityTarget = Vector2.Zero;
                _aiComponent.ChangeState("stopped");
            }

            _lastStartDistance = distance;

            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            _damageCounter -= Game1.DeltaTime;
            if (_damageCounter < 0)
                _damageCounter = 0;
            if (_damageCounter > 6000)
                _damageCounter = 6000;

            var strAnimation = "idle";
            if (0 < _damageCounter && _damageCounter <= 1400)
                strAnimation = "split_0";
            else if (1400 < _damageCounter && _damageCounter <= 2800)
                strAnimation = "split_1";
            else if (2800 < _damageCounter && _damageCounter <= 4200)
                strAnimation = "split_2";
            else if (4200 < _damageCounter)
                strAnimation = "split_3";

            if (_animator.CurrentAnimation.Id != strAnimation)
                _animator.Play(strAnimation);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.DamageTrigger.CurrentTime > 0)
                return Values.HitCollision.None;

            Game1.GameManager.PlaySoundEffect("D370-07-07");

            _damageState.SetDamageState();

            // break apart
            if (_damageCounter > 4200 && damageType == HitType.PegasusBootsSword)
            {
                var slimeHalfLeft = new BossSlimeEyeHalf(Map, new Vector2(EntityPosition.X - 18, EntityPosition.Y), "left", _saveKey);
                var slimeHalfRight = new BossSlimeEyeHalf(Map, new Vector2(EntityPosition.X + 18, EntityPosition.Y), "right", _saveKey);

                Map.Objects.SpawnObject(slimeHalfLeft);
                Map.Objects.SpawnObject(slimeHalfRight);

                Map.Objects.DeleteObjects.Add(this);

                return Values.HitCollision.RepellingParticle;
            }

            _damageCounter += 1400;

            return Values.HitCollision.RepellingParticle;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            return true;
        }
    }
}
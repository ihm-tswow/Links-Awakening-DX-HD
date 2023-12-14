using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.GameObjects.MidBoss;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossEvilEagle : GameObject
    {
        private MBossGrimCreeper _grimCreeper;
        private MBossGrimCreeperFly _creeperFly0;
        private MBossGrimCreeperFly _creeperFly1;

        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly DamageFieldComponent _damageComponent;
        private readonly HittableComponent _hittableComponent;

        private readonly Vector2 _startPosition;

        private readonly string _saveKey;

        private Vector2 _slowStart;
        private float _slowCounter;
        private float _transparency = 0;

        private int _spawnIndex;
        private int _direction;

        private const float FlySpeed = 2;
        private const int Lives = 12;

        private Vector2 _wingStartPosition;
        private Vector2 _wingEndPosition;
        private float _wingCounter;
        private float _featherCounter;
        private const float WingTime = 700;

        private bool _featherAttack;
        private bool _playedIntro;
        private bool _introSound;

        public BossEvilEagle() : base("evil eagle") { }

        public BossEvilEagle(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-32, -32, 64, 64);

            _startPosition = EntityPosition.Position;

            _saveKey = saveKey;

            if (!string.IsNullOrWhiteSpace(_saveKey) && Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                // respawn the heart if the player died after he killed the boss without collecting the heart
                SpawnHeart();

                IsDead = true;
                return;
            }

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/evil eagle");
            _animator.Play("glide_-1");

            _sprite = new CSprite(EntityPosition) { Color = Color.Transparent };

            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -8, -16, 16, 16, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                Bounciness = 0.25f,
                Drag = 1.0f,
                DragAir = 1.0f,
                IgnoresZ = true,
                IsGrounded = false
            };

            var hittableRectangle = new CBox(EntityPosition, -8, -16, 16, 16, 8);
            var damageCollider = new CBox(EntityPosition, -8, -16, 16, 16, 8);

            var stateIdle = new AiState(UpdateIdle) { Init = InitIdle };
            var stateSpawnDelay = new AiState();
            stateSpawnDelay.Trigger.Add(new AiTriggerCountdown(750, null, () => _aiComponent.ChangeState("spawning")));
            var stateSpawning = new AiState(UpdateSpawning) { Init = InitSpawning };
            var statePreJump = new AiState();
            statePreJump.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("grimSaddle")));
            var stateGrimSaddle = new AiState(UpdateGrimSaddle) { Init = InitGrimSaddle };
            var stateSaddled = new AiState(UpdateSaddled);
            var stateAttack = new AiState(UpdateAttack) { Init = InitAttack };
            var stateDamaged = new AiState(UpdateDamaged);
            stateDamaged.Trigger.Add(new AiTriggerCountdown(500, null, FlyUp));
            var stateFlyUp = new AiState(UpdateFlyUp) { Init = InitFlyUp };
            var stateAttackEnter = new AiState(UpdateWingAttackEnter) { Init = InitWingAttackEnter };
            var stateFeatherAttack = new AiState(UpdateWingAttack) { Init = InitWingAttack };
            var statePreAttack = new AiState() { Init = InitPreAttack };
            statePreAttack.Trigger.Add(new AiTriggerCountdown(800, null, () => _aiComponent.ChangeState("grabAttack")));
            var stateGrab = new AiState(UpdateAttackGrab) { Init = InitGrab };
            var stateLeave = new AiState(UpdateLeave) { Init = InitLeave };
            var stateGone = new AiState();
            stateGone.Trigger.Add(new AiTriggerCountdown(100, null, ToAttack));

            _aiComponent = new AiComponent();
            _aiComponent.Trigger.Add(new AiTriggerUpdate(UpdateVisibility));

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("spawnDelay", stateSpawnDelay);
            _aiComponent.States.Add("spawning", stateSpawning);
            _aiComponent.States.Add("grimPreJump", statePreJump);
            _aiComponent.States.Add("grimSaddle", stateGrimSaddle);
            _aiComponent.States.Add("saddled", stateSaddled);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("damaged", stateDamaged);
            _aiComponent.States.Add("flyup", stateFlyUp);
            _aiComponent.States.Add("attackEnter", stateAttackEnter);
            _aiComponent.States.Add("featherAttack", stateFeatherAttack);
            _aiComponent.States.Add("preAttack", statePreAttack);
            _aiComponent.States.Add("grabAttack", stateGrab);
            _aiComponent.States.Add("leave", stateLeave);
            _aiComponent.States.Add("gone", stateGone);

            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, Lives, false, false, AiDamageState.BlinkTime * 2 * 10) { ExplosionOffsetY = 8 };
            _damageState.AddBossDamageState(OnDeath);

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DamageFieldComponent.Index, _damageComponent = new DamageFieldComponent(damageCollider, HitType.Enemy, 8));
            AddComponent(HittableComponent.Index, _hittableComponent = new HittableComponent(hittableRectangle, OnHit));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));

            InitStartSequence();
            _aiComponent.ChangeState("idle");

            // spawn position
            EntityPosition.Set(new Vector2(EntityPosition.X + 180, 24));
        }

        private void InitStartSequence()
        {
            _grimCreeper = new MBossGrimCreeper(Map, (int)EntityPosition.X - 32, (int)EntityPosition.Y + 32, null);
            _grimCreeper.StartNightmareSequnece();
            Map.Objects.SpawnObject(_grimCreeper);

            _creeperFly0 = new MBossGrimCreeperFly(Map, new Vector2(EntityPosition.X - 41, EntityPosition.Y + 32), new Vector2(-24, 12));
            _creeperFly0.StartSequenceMode();
            Map.Objects.SpawnObject(_creeperFly0);

            _creeperFly1 = new MBossGrimCreeperFly(Map, new Vector2(EntityPosition.X - 7, EntityPosition.Y + 32), new Vector2(-16, -8));
            _creeperFly1.StartSequenceMode();
            Map.Objects.SpawnObject(_creeperFly1);
        }

        private void UpdateVisibility()
        {
            var target = (_startPosition.X - 144 < EntityPosition.X && EntityPosition.X < _startPosition.X + 144 &&
                          EntityPosition.Y > -16 && EntityPosition.Y < 160) ? 1 : 0;
            _transparency = AnimationHelper.MoveToTarget(_transparency, target, 0.175f * Game1.TimeMultiplier);
            _sprite.Color = Color.White * _transparency;
        }

        private void InitIdle()
        {
            _body.Velocity = Vector3.Zero;
            _body.VelocityTarget = Vector2.Zero;
            _damageState.CurrentLives = Lives;
        }

        private void UpdateIdle()
        {
            if (MapManager.ObjLink.EntityPosition.Y > _startPosition.Y + 90 || !MapManager.ObjLink.IsClimbing())
                return;

            if (!_playedIntro)
            {
                Game1.GameManager.StartDialogPath("grim_creeper_3");
                _aiComponent.ChangeState("spawnDelay");
            }
            else
                ToAttack();
        }

        private void SpawnHeart()
        {
            Map.Objects.SpawnObject(new ObjItem(Map, (int)_startPosition.X - 8, (int)_startPosition.Y, null, _saveKey + "_heart", "heartMeterFull", null));
        }

        private void InitPreAttack()
        {
            _animator.SpeedMultiplier = 1.5f;

        }

        private void InitGrab()
        {
            _animator.Stop();
            _animator.Play("cflap_" + _direction);
            _animator.Stop();

            // adjust the distance
            var distance = MathF.Abs(MapManager.ObjLink.EntityPosition.X - EntityPosition.X);
            var mult = Math.Clamp(distance / 40 * 1.45f, 0.35f, 1.5f);
            var direction = new Vector2(_direction * mult, 1.45f);
            direction.Normalize();

            _body.VelocityTarget = direction * 4f;
        }

        private void UpdateAttackGrab()
        {
            if (EntityPosition.Y > _startPosition.Y + 180)
                ToAttack();
        }

        private void InitLeave()
        {
            //_body.VelocityTarget = new Vector2(0, -1);
        }

        private void UpdateLeave()
        {
            if (_direction < 0 && _body.Velocity.X > -3)
                _body.Velocity.X -= 0.15f * Game1.TimeMultiplier;
            if (_direction > 0 && _body.Velocity.X < 3)
                _body.Velocity.X += 0.15f * Game1.TimeMultiplier;

            if (_body.Velocity.Y > -2)
                _body.Velocity.Y -= 0.015f * Game1.TimeMultiplier;

            // start playing the glide animation
            if (_direction == -1 && EntityPosition.X < _startPosition.X ||
                _direction == 1 && EntityPosition.X > _startPosition.X)
            {
                _animator.Play("cglide_" + _direction);
            }
            if (_direction == -1 && EntityPosition.X < _startPosition.X - 160 ||
                _direction == 1 && EntityPosition.X > _startPosition.X + 160)
            {
                ToAttack();
            }
        }

        private void InitWingAttackEnter()
        {
            _direction = MapManager.ObjLink.EntityPosition.X < _startPosition.X ? -1 : 1;

            // only show the first frame
            _animator.Play("cflap_" + _direction);
            _animator.Stop();

            _body.Velocity = Vector3.Zero;
            _body.VelocityTarget = Vector2.Zero;

            _wingCounter = 0;
            _wingStartPosition = new Vector2(_startPosition.X - _direction * 100, _startPosition.Y - 24);
            _wingEndPosition = new Vector2(_startPosition.X - _direction * 28, _startPosition.Y + 8);

            EntityPosition.Set(_wingStartPosition);
        }

        private void UpdateWingAttackEnter()
        {
            _wingCounter += Game1.DeltaTime;
            var percentage = _wingCounter / WingTime;

            // start flapping the wings
            if (percentage > 0.80)
                _animator.Continue();

            if (percentage >= 1)
            {
                EntityPosition.Set(_wingEndPosition);

                // feather attack
                if (_featherAttack)
                    _aiComponent.ChangeState("featherAttack");
                else
                    _aiComponent.ChangeState("preAttack");

                _featherAttack = false;

                return;
            }

            var lerpState = MathF.Sin(percentage * MathF.PI / 2);
            var newPosition = Vector2.Lerp(_wingStartPosition, _wingEndPosition, lerpState);
            EntityPosition.Set(newPosition);
        }

        private void InitWingAttack()
        {
            _wingCounter = 0;
        }

        private void UpdateWingAttack()
        {
            _wingCounter += Game1.DeltaTime;

            // end state
            if (_wingCounter > 275 * 11)
            {
                _aiComponent.ChangeState("leave");
            }

            // move up and down
            var offset = MathF.Sin(_wingCounter / 1100 * MathF.PI * 2 + MathF.PI / 2) * 4 - 4;
            var newPosition = new Vector2(_wingEndPosition.X, _wingEndPosition.Y + offset);
            EntityPosition.Set(newPosition);

            // push the playyer
            if (MapManager.ObjLink.EntityPosition.Y < _startPosition.Y + 90)
                MapManager.ObjLink._body.AdditionalMovementVT.X = _direction * 1.1f;
            else
                MapManager.ObjLink._body.AdditionalMovementVT.X = 0;

            // shoot a feather
            _featherCounter += Game1.DeltaTime;
            if (_featherCounter > 275)
            {
                _featherCounter = 0;

                var startPosition = new Vector2(EntityPosition.X - _direction * 4, EntityPosition.Y + 10);
                var direction = MapManager.ObjLink.EntityPosition.Position - startPosition;
                if (direction != Vector2.Zero)
                    direction.Normalize();

                var radiants = MathF.Atan2(direction.Y, direction.X);
                // randomly offset the direction a little bit
                radiants += (Game1.RandomNumber.Next(0, 11) - 5) / 25f;
                var aimDirection = new Vector2(MathF.Cos(radiants), MathF.Sin(radiants));

                var eagleFeather = new BossEvilEagleFeather(Map, startPosition, aimDirection * 4);
                Map.Objects.SpawnObject(eagleFeather);

                Game1.GameManager.PlaySoundEffect("D378-50-32");
            }
        }

        private void UpdateDamaged()
        {
            if (!_damageState.IsInDamageState())
                _aiComponent.ChangeState("flyup");
        }

        private void FlyUp()
        {
            Game1.GameManager.PlaySoundEffect("D378-49-31");
            _animator.Play("cflap_" + _direction);
        }

        private void InitFlyUp()
        {
        }

        private void UpdateFlyUp()
        {
            if (_body.Velocity.Y > -2)
                _body.Velocity.Y -= 0.1f * Game1.TimeMultiplier;

            if (EntityPosition.Y < -48)
                ToAttack();
        }

        private void ToAttack()
        {
            // reset the boss
            if (MapManager.ObjLink.EntityPosition.Y > _startPosition.Y + 90)
            {
                _aiComponent.ChangeState("idle");
                return;
            }

            _body.VelocityTarget = Vector2.Zero;
            _damageComponent.IsActive = true;

            if (_damageState.CurrentLives == 4 || _damageState.CurrentLives == 5 || _featherAttack)
                _aiComponent.ChangeState("attackEnter");
            else
                _aiComponent.ChangeState("attack");
        }

        private void InitAttack()
        {
            _direction = Game1.RandomNumber.Next(0, 2) * 2 - 1;

            _body.Velocity = Vector3.Zero;
            _body.VelocityTarget.X = _direction * FlySpeed;

            var randomHeight = Game1.RandomNumber.Next(0, 28) * 2;

            EntityPosition.Set(new Vector2(_startPosition.X - _direction * 160, (int)MapManager.ObjLink.EntityPosition.Y - randomHeight));

            _animator.Play("cglide_" + _direction);
        }

        private void UpdateAttack()
        {
            if (EntityPosition.X < _startPosition.X - 180 || _startPosition.X + 180 < EntityPosition.X)
                _aiComponent.ChangeState("gone");
        }

        private void UpdateSaddled()
        {
            if (_body.Velocity.X > -3)
                _body.Velocity.X -= 0.15f * Game1.TimeMultiplier;
            if (_body.Velocity.Y > -2)
                _body.Velocity.Y -= 0.01f * Game1.TimeMultiplier;

            if (EntityPosition.X < _startPosition.X - 23)
            {
                _animator.Play("cglide_-1");
            }
            if (EntityPosition.X < _startPosition.X - 180)
            {
                // start attacking
                ToAttack();
            }
        }

        private void InitGrimSaddle()
        {
            _grimCreeper.StartSaddleJump();
        }

        private void UpdateGrimSaddle()
        {
            if (_grimCreeper.Map == null)
            {
                _aiComponent.ChangeState("saddled");
                _animator.Play("cflap");
                _animator.SpeedMultiplier = 0.5f;
            }
        }

        private void InitSpawning()
        {
            _playedIntro = true;
            _body.VelocityTarget.X = -FlySpeed;
            _spawnIndex = 0;
        }

        private void UpdateSpawning()
        {
            MapManager.ObjLink.FreezePlayer();

            if (_spawnIndex == 0)
            {
                if (EntityPosition.X < _startPosition.X - 180)
                {
                    _animator.Play("glide_1");
                    _body.VelocityTarget.X = FlySpeed;
                    EntityPosition.Y += 20;
                    _spawnIndex = 1;
                }
            }
            else if (_spawnIndex == 1)
            {
                if (!_introSound && EntityPosition.X > _startPosition.X - 130)
                {
                    _introSound = true;
                    Game1.GameManager.PlaySoundEffect("D378-34-22");
                }

                if (EntityPosition.X > _startPosition.X + 180)
                {
                    _introSound = false;
                    _animator.Play("glide_-1");
                    _body.VelocityTarget.X = -FlySpeed;
                    _spawnIndex = 2;
                    EntityPosition.Y += 20;
                }
            }
            else if (_spawnIndex == 2)
            {
                if (!_introSound && EntityPosition.X < _startPosition.X + 110)
                {
                    _introSound = true;
                    Game1.GameManager.PlaySoundEffect("D378-48-30");
                }

                if (EntityPosition.X < _startPosition.X + 40)
                {
                    Game1.GameManager.PlaySoundEffect("D360-48-30");
                    _animator.Play("flap");
                    _slowStart = EntityPosition.Position;
                    _body.VelocityTarget.X = 0;
                    _spawnIndex = 3;
                }
            }
            else if (_spawnIndex == 3)
            {
                _slowCounter += Game1.DeltaTime;
                if (_slowCounter > 500)
                {
                    _aiComponent.ChangeState("grimSaddle");
                    _slowCounter = 500;

                    _creeperFly0.ToLeave();
                    _creeperFly1.ToLeave();
                }

                var percentage = MathF.Sin(_slowCounter / 500 * MathF.PI / 2);
                var newPositionX = MathHelper.Lerp(_slowStart.X, _startPosition.X + 6, percentage);
                EntityPosition.Set(new Vector2(newPositionX, EntityPosition.Y));
            }
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState() || _aiComponent.CurrentStateId == "flyup")
                return Values.HitCollision.None;

            if (damageType == HitType.MagicRod || damageType == HitType.Boomerang)
                damage = 4;

            _damageComponent.IsActive = false;
            _damageState.SetDamageState(true);
            _damageState.CurrentLives -= damage;

            Game1.GameManager.PlaySoundEffect("D370-07-07");

            // dead?
            if (_damageState.CurrentLives <= 0)
            {
                _body.IsActive = false;
                _hittableComponent.IsActive = false;

                Game1.GameManager.StartDialogPath("grim_creeper_4");

                _damageState.OnDeathBoss(pieceOfPower);

                return Values.HitCollision.Enemy;
            }

            // next move will be the wing attack; not sure how this works in the original
            if (Game1.RandomNumber.Next(0, 2) == 0)
                _featherAttack = true;

            _aiComponent.ChangeState("damaged");
            _body.VelocityTarget = Vector2.Zero;

            return Values.HitCollision.Enemy;
        }

        private void OnDeath()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            Game1.GameManager.PlaySoundEffect("D378-26-1A");

            SpawnHeart();

            Map.Objects.DeleteObjects.Add(this);
        }
    }
}
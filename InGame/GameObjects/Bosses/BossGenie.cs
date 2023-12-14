using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    class BossGenie : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly DamageFieldComponent _damageField;
        private readonly AiDamageState _damageState;
        private readonly ShadowBodyDrawComponent _shadowComponent;

        private readonly BossGenieBottle _objBottle;

        private readonly Animator _smokeBottom;
        private readonly Animator _smokeTop;

        private readonly Animator _bodyAnimator;
        private readonly Animator _tailAnimator;

        private readonly CSprite _sprite;

        private readonly string _saveKey;

        private const float FollowSpeed = 1.5f;
        private const int AttackTime = 10000;
        private const int RotateTime = 2100;
        private const int Lives = 8;

        private Vector2 _spawnPosition;
        private readonly Vector2 _roomCenter;

        private BossGenieFireball[] _fireballs;
        private int _fireballCount;
        private int _fireballIndex;

        private bool _isVisible;

        private const int RotationOffsetY = 38;
        private float _currentRotation;
        private float _rotationDistance;

        private Vector2 _smokePosition0;
        private Vector2 _smokePosition1;
        private float _spawnCounter;
        private bool _drawSmoke;
        private bool _attackMode;

        public BossGenie(Map.Map map, string saveKey, Vector3 position, BossGenieBottle objBottle) : base(map)
        {
            _saveKey = saveKey;

            _objBottle = objBottle;

            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, position.Z);
            EntitySize = new Rectangle(-20, -80, 40, 80);

            _roomCenter = Map.GetRoomCenter(position.X, position.Y);

            _bodyAnimator = AnimatorSaveLoad.LoadAnimator("Nightmares/genie");
            _bodyAnimator.Play("idle");

            _tailAnimator = AnimatorSaveLoad.LoadAnimator("Nightmares/genie");
            _tailAnimator.Play("tail");

            _smokeTop = AnimatorSaveLoad.LoadAnimator("Nightmares/genie smoke");
            _smokeBottom = AnimatorSaveLoad.LoadAnimator("Nightmares/genie smoke");

            _sprite = new CSprite(EntityPosition);

            _body = new BodyComponent(EntityPosition, -5, -10, 10, 10, 8)
            {
                IgnoresZ = true,
                CollisionTypes = Values.CollisionTypes.None
            };

            var hittableBox = new CBox(EntityPosition, -15, -38, 0, 30, 26, 8, true);
            var damageCollider = new CBox(EntityPosition, -15, -38, 0, 30, 30, 8, true);

            var stateIdle = new AiState();
            var stateSpawn = new AiState(UpdateSpawn) { Init = InitSpawn };
            var stateSpawnDelay = new AiState();
            stateSpawnDelay.Trigger.Add(new AiTriggerCountdown(1000, null, SpawnDelayEnd));
            var stateDespawn = new AiState(UpdateDespawn) { Init = InitDespawn };
            var stateAttack = new AiState { Init = InitAttack };
            stateAttack.Trigger.Add(new AiTriggerCountdown(AttackTime, AttackTick, AttackEnd));
            var stateFollow = new AiState(UpdateFollow) { Init = InitFollow };
            var stateRotate = new AiState { Init = InitRotation };
            stateRotate.Trigger.Add(new AiTriggerCountdown(RotateTime, RotateTick, RotateEnd));

            _aiComponent = new AiComponent();

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("spawnDelay", stateSpawnDelay);
            _aiComponent.States.Add("despawn", stateDespawn);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("follow", stateFollow);
            _aiComponent.States.Add("rotate", stateRotate);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, Lives, true, false);
            _damageState.AddBossDamageState(OnDeath);
            _damageState.ExplosionOffsetY = -8;
            _damageState.BossHitSound = true;

            _aiComponent.ChangeState("idle");

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 4) { IsActive = false });
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new ShadowBodyDrawComponent(EntityPosition) { ShadowWidth = 18, ShadowHeight = 6 });
        }

        public void Spawn(Vector3 position)
        {
            EntityPosition.Set(position);

            _aiComponent.ChangeState("spawn");

            _spawnPosition = new Vector2(position.X, position.Y);
        }

        public void AttackSpawn(Vector3 position)
        {
            EntityPosition.Set(position);
            _aiComponent.ChangeState("spawn");

            // do not start at the beginning
            _smokeTop.SetFrame(3);
            _spawnCounter = 600;
            UpdateSpawn();
            _attackMode = true;
        }

        private void SpawnDelayEnd()
        {
            if (_attackMode)
            {
                Game1.GameManager.StartDialogPath("d2_boss_3");
                _aiComponent.ChangeState("follow");
            }
            else
            {
                Game1.GameManager.StartDialogPath("d2_boss_1");
                _aiComponent.ChangeState("attack");
            }
        }

        private void InitDespawn()
        {
            _damageField.IsActive = false;
            _smokePosition0 = new Vector2(EntityPosition.X, EntityPosition.Y - 54);
            _smokePosition1 = new Vector2(EntityPosition.X, EntityPosition.Y - 54);
            _smokeTop.Play("despawn");
            _spawnCounter = _smokeTop.GetAnimationTime();

            Game1.GameManager.PlaySoundEffect("D360-31-1F");

            _drawSmoke = true;
        }

        private void UpdateDespawn()
        {
            _spawnCounter -= Game1.DeltaTime;
            UpdateSmoke(_spawnCounter);

            if (!_smokeTop.IsPlaying)
                DespawnEnd();

            // despawn the genie
            if (_smokeTop.CurrentFrameIndex > 0)
                _isVisible = false;
        }

        private void DespawnEnd()
        {
            _drawSmoke = false;
            _shadowComponent.IsActive = false;
            _aiComponent.ChangeState("idle");
            _objBottle.StartFollowing();
        }

        private void InitSpawn()
        {
            _spawnCounter = 0;
            _smokePosition0 = new Vector2(EntityPosition.X, EntityPosition.Y - 29);
            _smokePosition1 = new Vector2(EntityPosition.X, EntityPosition.Y - 29);
            _smokeTop.Play("top");

            Game1.GameManager.PlaySoundEffect("D360-06-06");

            _shadowComponent.IsActive = true;
            _drawSmoke = true;
        }

        private void UpdateSpawn()
        {
            _spawnCounter += Game1.DeltaTime;
            UpdateSmoke(_spawnCounter);

            if (!_smokeTop.IsPlaying)
                SpawnEnd();

            // spawn the genie
            if (_smokeTop.CurrentFrameIndex == 6)
                _isVisible = true;
        }

        private void SpawnEnd()
        {
            _damageField.IsActive = true;
            _drawSmoke = false;
            _aiComponent.ChangeState("spawnDelay");
        }

        private void UpdateSmoke(float state)
        {
            _smokeBottom.Update();
            _smokeTop.Update();

            if (330 < _spawnCounter && _spawnCounter < 600)
            {
                _smokeBottom.Play("bottom");
                _smokePosition1.Y = EntityPosition.Y - 29 - ((_spawnCounter - 330) / 600f) * 25;
            }
            else
            {
                _smokeBottom.Stop();
            }

            var movePercentage = MathF.Sin(_spawnCounter / 600f * MathF.PI / 3) / MathF.Sin(MathF.PI / 3);

            // move up
            if (_spawnCounter < 600)
                _smokePosition0.Y = EntityPosition.Y - 29 - movePercentage * 25;
            else
                _smokePosition0.Y = EntityPosition.Y - 54;
        }

        private void InitFollow()
        {
            _bodyAnimator.Play("attack");
        }

        private void UpdateFollow()
        {
            // fly towards the player
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z - 8);
            if (playerDirection != Vector2.Zero)
            {
                playerDirection.Normalize();
                // have momentum and not directly change the direction
                var percentage = (float)Math.Pow(0.98, Game1.TimeMultiplier);
                _body.VelocityTarget = percentage * _body.VelocityTarget + (1 - percentage) * playerDirection * FollowSpeed;
            }
        }

        private void InitRotation()
        {
            _shadowComponent.IsActive = false;
            _damageField.IsActive = false;
            _body.VelocityTarget = Vector2.Zero;
            var centerOffset = new Vector2(EntityPosition.Position.X, EntityPosition.Position.Y - RotationOffsetY) - _roomCenter;
            _currentRotation = MathF.Atan2(centerOffset.Y, centerOffset.X);
            _rotationDistance = centerOffset.Length();
        }

        private void RotateTick(double counter)
        {
            // do not get to close the mirrored version
            if (_rotationDistance > 4)
                _rotationDistance -= Game1.TimeMultiplier * MathHelper.Clamp(_rotationDistance / 100, 0, 0.3f);

            // move the same distance each frame independent of how far we are away from the rotation origin
            var rotationSpeed = MathHelper.Clamp(_rotationDistance, 0, 6);
            _currentRotation += rotationSpeed / (_rotationDistance * MathF.PI) * Game1.TimeMultiplier;

            var newPosition = new Vector2(_roomCenter.X, _roomCenter.Y + RotationOffsetY) + new Vector2(MathF.Cos(_currentRotation), MathF.Sin(_currentRotation)) * _rotationDistance;
            EntityPosition.Set(newPosition);
        }

        private void RotateEnd()
        {
            _shadowComponent.IsActive = true;
            _damageField.IsActive = true;

            // throw fireball
            var fireball = new BossGenieFireball(Map, EntityPosition.ToVector3());
            Map.Objects.SpawnObject(fireball);

            // spawn the ball on the left or right side
            if (MapManager.ObjLink.EntityPosition.X < EntityPosition.X)
                _fireballIndex = 0;
            else
                _fireballIndex = 1;

            ThrowFireball(fireball);

            _aiComponent.ChangeState("follow");
        }

        private void InitAttack()
        {
            _fireballIndex = 0;
            _fireballCount = 8;
            _fireballs = new BossGenieFireball[_fireballCount];
            for (var i = 0; i < _fireballCount; i++)
            {
                // spawn behind the genie for the initial frame
                _fireballs[i] = new BossGenieFireball(Map, new Vector3(EntityPosition.X, EntityPosition.Y - 1, EntityPosition.Z + 12));
                Map.Objects.SpawnObject(_fireballs[i]);
            }
        }

        private void AttackTick(double counter)
        {
            var state = (float)((AttackTime - counter) / AttackTime);

            for (var i = _fireballIndex; i < _fireballCount; i++)
            {
                // 5 fireballs form a full circle
                var circleCount = 5;
                var fullTurns = 7;
                var radiant = (state * MathF.PI * 2 * fullTurns + i * 2 * (2 * MathF.PI) / 5);

                var newPosition = new Vector3(EntityPosition.X - MathF.Sin(radiant) * 12, EntityPosition.Y - 1, EntityPosition.Z + 30 - MathF.Cos(radiant) * 15);
                _fireballs[i].SetPosition(newPosition);

                // throw fireball at the time where the ball is behind the genie
                if (state > 2f / fullTurns + (_fireballIndex * (3 / (float)circleCount)) * (1 / (float)fullTurns))
                {
                    ThrowFireball();
                }
            }

            // move the genie left/right and up/down
            var moveState = state * MathF.PI * 2 * 2;
            var offsetX = MathF.Sin(moveState) * 27;
            var offsetY = MathF.Sin(moveState * 7) * 4;
            EntityPosition.Set(new Vector2(_spawnPosition.X + offsetX, _spawnPosition.Y + offsetY));
        }

        private void ThrowFireball()
        {
            ThrowFireball(_fireballs[_fireballIndex]);
            _bodyAnimator.Play(_fireballIndex % 2 == 0 ? "attack_0" : "attack_1");
            _fireballIndex++;
        }

        private void ThrowFireball(BossGenieFireball fireball)
        {
            fireball.EntityPosition.Set(new Vector3(
                EntityPosition.X + (_fireballIndex % 2 == 0 ? -15 : 15), EntityPosition.Y, EntityPosition.Z + 30));

            // throw the fireball in the direction of the player
            var playerDirection = MapManager.ObjLink.EntityPosition.ToVector3() -
                                  fireball.EntityPosition.ToVector3();
            if (playerDirection != Vector3.Zero)
            {
                playerDirection.Normalize();
                playerDirection *= 2.25f;
            }

            fireball.ThrowFireball(playerDirection);

            Game1.GameManager.PlaySoundEffect("D378-40-28");
        }

        private void AttackEnd()
        {
            _aiComponent.ChangeState("despawn");
        }

        private void Update()
        {
            _bodyAnimator.Update();
            _tailAnimator.Update();
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (_isVisible)
            {
                var color = Color.White;

                // change the draw effect
                if (_sprite.SpriteShader != null)
                {
                    spriteBatch.End();
                    ObjectManager.SpriteBatchBegin(spriteBatch, _sprite.SpriteShader);
                }

                if (_aiComponent.CurrentStateId == "rotate")
                {
                    color *= 0.5f;

                    var drawPosition = new Vector2(EntityPosition.Position.X, EntityPosition.Position.Y);
                    var centerOffset = drawPosition - new Vector2(_roomCenter.X, _roomCenter.Y + RotationOffsetY);
                    var newPosition = drawPosition - centerOffset * 2 + new Vector2(0, -EntityPosition.Z);

                    // draw the body
                    _bodyAnimator.Draw(spriteBatch, newPosition, color);
                    // draw the tail
                    _tailAnimator.Draw(spriteBatch, newPosition, color);
                }

                // draw the body
                _bodyAnimator.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z), color);
                // draw the tail
                _tailAnimator.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z), color);

                // change the draw effect
                if (_sprite.SpriteShader != null)
                {
                    spriteBatch.End();
                    ObjectManager.SpriteBatchBegin(spriteBatch, null);
                }
            }

            // draw the smoke while spawning/despawning
            if (_drawSmoke)
                DrawSpawn(spriteBatch);
        }

        private void DrawSpawn(SpriteBatch spriteBatch)
        {
            if (_smokeTop.IsPlaying)
                _smokeTop.Draw(spriteBatch, _smokePosition0, Color.White);
            if (_smokeBottom.IsPlaying)
                _smokeBottom.Draw(spriteBatch, _smokePosition1, Color.White);
        }

        private void OnDeath()
        {
            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            // stop boss music
            Game1.GameManager.SetMusic(-1, 2);

            var heartPosition = new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z);
            var centerDistance = heartPosition - _roomCenter;
            centerDistance.X = MathHelper.Clamp(centerDistance.X, -56, 56);
            centerDistance.Y = MathHelper.Clamp(centerDistance.Y, -24, 54);

            heartPosition = _roomCenter + centerDistance;

            // spawn big heart
            Map.Objects.SpawnObject(new ObjItem(Map,
                (int)heartPosition.X - 8, (int)heartPosition.Y - 16, "j", "d2_nHeart", "heartMeterFull", null));

            // remove the boss from the map
            Map.Objects.DeleteObjects.Add(this);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // can only get attacked in the follow state
            if (_aiComponent.CurrentStateId != "follow" ||
                _damageState.IsInDamageState() ||
                damageType == HitType.MagicPowder)
                return Values.HitCollision.None;

            _aiComponent.ChangeState("rotate");

            if (damageType == HitType.Bomb)
                damage *= 2;

            var damageReturn = _damageState.OnHit(MapManager.ObjLink, direction, HitType.ThrownObject, damage, pieceOfPower);

            // stop if we are dead
            if (_damageState.CurrentLives <= 0)
                _body.VelocityTarget = Vector2.Zero;

            return damageReturn;
        }
    }
}
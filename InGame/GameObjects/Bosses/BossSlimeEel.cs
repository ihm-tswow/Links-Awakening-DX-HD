using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
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
    class BossSlimeEel : GameObject
    {
        private readonly Animator _animator;
        private readonly AnimationComponent _animationComponent;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly CSprite _sprite;
        private readonly DamageFieldComponent _damageField;
        private readonly AiDamageState _aiDamageState;

        private readonly Animator _explosionAnimator;

        private readonly BossSlimeEelSpawn _eelSpawner;

        // parts are used to deal damage and have a hittable box
        private readonly BossSlimeEelTail[] _tailParts = new BossSlimeEelTail[5];

        private readonly DictAtlasEntry _spriteTail;
        private readonly DictAtlasEntry _spriteTailEnd;
        private readonly DictAtlasEntry _spriteHeart0;
        private readonly DictAtlasEntry _spriteHeart1;

        private readonly Vector2 _centerPosition;

        private Vector2 _startPosition;

        private Vector2 _hockshotPosition;
        private Vector2 _hookshotOffset;
        private float _pullSound;

        private double _retreatCount;
        private float _moveSpeed;
        private int _moveDirection = 1;

        private bool _attackOut;
        private bool _isFullyOut;

        private float _moveRotation;
        private float _rotationDir = 1;

        private const float MoveSpeed = 0.75f;

        private readonly float[] _tailDistance = { 10.0f, 8f, 9f };
        private readonly Vector2[] _savedPosition = new Vector2[30];
        private readonly Vector2[] _tailPositions = new Vector2[3];

        private string _saveKey;

        private bool _attackSound;

        private float _saveInterval = 33;
        private float _saveCounter;
        private int _saveIndex;

        public BossSlimeEel(Map.Map map, Vector2 centerPosition, BossSlimeEelSpawn eelSpawner, string saveKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            _centerPosition = centerPosition;
            _eelSpawner = eelSpawner;

            _saveKey = saveKey;

            EntityPosition = new CPosition(centerPosition.X, centerPosition.Y, 0);
            EntitySize = new Rectangle(-16, -16, 32, 32);

            _body = new BodyComponent(EntityPosition, -8, -8, 16, 16, 8)
            {
                MoveCollision = OnCollision,
                IgnoreHoles = true
            };

            _spriteTail = Resources.GetSprite("eel_tail_0");
            _spriteTailEnd = Resources.GetSprite("eel_tail_2");
            _spriteHeart0 = Resources.GetSprite("eel_heart_0");
            _spriteHeart1 = Resources.GetSprite("eel_heart_1");

            for (var i = 0; i < _tailParts.Length; i++)
            {
                if (i == 0)
                    _tailParts[i] = new BossSlimeEelTail(Map, EntityPosition.Position, 0, OnHitHeart);
                else
                    _tailParts[i] = new BossSlimeEelTail(Map, EntityPosition.Position, 0, null);

                _tailParts[i].SetActive(false);

                Map.Objects.SpawnObject(_tailParts[i]);
            }

            _explosionAnimator = AnimatorSaveLoad.LoadAnimator("Objects/explosion");

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/slime eel");

            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _aiComponent = new AiComponent();

            var stateSpawn = new AiState { Init = InitSpawn };
            var stateSpawnAttack = new AiState(UpdateSpawnAttack);
            var stateHidden = new AiState { Init = InitHidden };
            stateHidden.Trigger.Add(new AiTriggerCountdown(1000, null, EndHidden));
            var stateAttack = new AiState(UpdateAttack) { Init = InitAttack };
            var statePulled = new AiState(UpdatePulled) { Init = InitPulled };
            var stateRetreat = new AiState(UpdateRetreat) { Init = InitRetreat };
            var stateJumpingOut = new AiState(UpdateJumpingOut) { Init = InitJumpingOut };
            var statePulledOut = new AiState(UpdatePulledOut) { Init = InitPulledOut };
            statePulledOut.Trigger.Add(new AiTriggerRandomTime(ChangeDirection, 650, 1000));
            statePulledOut.Trigger.Add(new AiTriggerCountdown(1600, null, () => _aiComponent.ChangeState("blink")));
            var stateBlink = new AiState { Init = InitBlink };
            stateBlink.Trigger.Add(new AiTriggerCountdown(AiDamageState.CooldownTime, TickBlink, Explode));
            var stateExplode = new AiState(UpdateExplode) { Init = InitExplode };

            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("spawnAttack", stateSpawnAttack);
            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("pulled", statePulled);
            _aiComponent.States.Add("retreat", stateRetreat);
            _aiComponent.States.Add("jumpingOut", stateJumpingOut);
            _aiComponent.States.Add("pulledOut", statePulledOut);
            _aiComponent.States.Add("blink", stateBlink);
            _aiComponent.States.Add("explode", stateExplode);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, _sprite, 8, false, false)
            {
                HitMultiplierX = 0,
                HitMultiplierY = 0,
                ExplosionOffsetY = 16,
                BossHitSound = true
            };
            _aiDamageState.AddBossDamageState(OnDeath);

            _aiComponent.ChangeState("spawn");

            var damageCollider = new CBox(EntityPosition, -6, -6, 0, 12, 12, 8);
            var hittableBox = new CBox(EntityPosition, -6, -6, 0, 12, 12, 8);

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2) { IsActive = false });
        }

        public void SpawnAttack(int positionIndex)
        {
            SetAttackPosition(positionIndex);

            _attackSound = false;
            _sprite.IsVisible = true;
            _animator.Play("attack_spawn");
            _aiComponent.ChangeState("spawnAttack");
        }

        public void ToSpawned()
        {
            _aiComponent.ChangeState("hidden");
        }

        private void InitSpawn()
        {
            _sprite.IsVisible = false;
        }

        private void UpdateSpawnAttack()
        {
            if (!_attackSound && _animator.CurrentFrameIndex == 2)
            {
                _attackSound = true;
                Game1.GameManager.PlaySoundEffect("D370-22-16");
            }

            if (!_animator.IsPlaying)
            {
                _aiComponent.ChangeState("spawn");
            }
        }

        private void InitExplode()
        {
            Game1.GameManager.PlaySoundEffect("D378-12-0C");
            _explosionAnimator.Play("idle");

            _isFullyOut = false;
            _sprite.IsVisible = false;
        }

        private void UpdateExplode()
        {
            var collisionRect = _explosionAnimator.CollisionRectangle;
            if (collisionRect != Rectangle.Empty)
            {
                var collisionBox = new Box(
                    EntityPosition.X + collisionRect.X,
                    EntityPosition.Y + collisionRect.Y, 0,
                    collisionRect.Width, collisionRect.Height, 16);

                if (collisionBox.Intersects(MapManager.ObjLink._body.BodyBox.Box))
                    MapManager.ObjLink.HitPlayer(collisionBox, HitType.Bomb, 4);
            }

            _explosionAnimator.Update();
            if (!_explosionAnimator.IsPlaying)
                _aiComponent.ChangeState("hidden");
        }

        private void InitBlink()
        {
            _damageField.IsActive = false;
            _body.VelocityTarget = Vector2.Zero;
        }

        private void TickBlink(double counter)
        {
            _sprite.SpriteShader = (AiDamageState.CooldownTime - counter) % (AiDamageState.BlinkTime * 2) <
                                   AiDamageState.BlinkTime ? Resources.DamageSpriteShader0 : null;
        }

        private void Explode()
        {
            _aiComponent.ChangeState("explode");
        }

        private void InitJumpingOut()
        {
            Game1.GameManager.PlaySoundEffect("D370-22-16");

            _animator.Play("head_0");
            _body.VelocityTarget = _moveDirection * new Vector2(0, 1) * 1.5f;
            _moveRotation = _moveDirection != 1 ? MathF.PI : 0;
            _isFullyOut = true;

            for (var i = 0; i < _savedPosition.Length; i++)
                _savedPosition[i] = _startPosition;
        }

        private void UpdateJumpingOut()
        {
            var distance = Math.Abs(_startPosition.Y - EntityPosition.Y);
            var tailState = 1 - Math.Clamp(distance / 48, 0, 1);

            _eelSpawner.SetTailState(tailState);

            if (distance > 48)
                _aiComponent.ChangeState("pulledOut");

            UpdateTailPositions();
        }

        private void ChangeDirection()
        {
            _rotationDir = -_rotationDir;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            _moveRotation += MathF.PI;
        }

        private void InitPulledOut()
        {
        }

        private void UpdatePulledOut()
        {
            _moveRotation += _rotationDir * Game1.TimeMultiplier * 0.1f;

            var newVelocity = new Vector2(-MathF.Sin(_moveRotation), MathF.Cos(_moveRotation)) * MoveSpeed;
            _body.VelocityTarget = newVelocity;

            UpdateTailPositions();

            while (_moveRotation < MathF.PI * 2)
                _moveRotation += MathF.PI * 2;

            // update the animation
            var animationIndex = (int)((_moveRotation + Math.PI / 8) / (MathF.PI / 4)) % 8;

            if (animationIndex % 2 == 0)
            {
                if (animationIndex == 0 || animationIndex == 4)
                    _animator.Play("head_0");
                else
                    _animator.Play("head_2");
            }
            else
            {
                _animationComponent.MirroredH = animationIndex > 4;
                _animationComponent.MirroredV = animationIndex != 1 && animationIndex != 7;
                _animator.Play("head_1");
            }
        }

        private void OnDeath()
        {
            // spawn a heart
            var objItem = new ObjItem(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y - 8, "j", "d5_nHeart", "heartMeterFull", null);
            Map.Objects.SpawnObject(objItem);

            Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            // stop boss music
            Game1.GameManager.SetMusic(-1, 2);

            DespawnObjects();
        }

        private void DespawnObjects()
        {
            // delete the head
            Map.Objects.DeleteObjects.Add(this);

            // delete the tail
            for (var i = 0; i < _tailParts.Length; i++)
                Map.Objects.DeleteObjects.Add(_tailParts[i]);
        }

        private void UpdateTail()
        {
            var distance = Math.Abs(_startPosition.Y - EntityPosition.Y);
            var tailState = 1 - Math.Clamp(distance / 80, 0, 1);

            _eelSpawner.SetTailState(tailState);
        }

        private void InitRetreat()
        {
            _retreatCount = 0;
            _moveSpeed = 1 / 16f;
            _animator.Play("head_open");
        }

        private void UpdateRetreat()
        {
            _retreatCount += Game1.DeltaTime;

            var direction = _startPosition - EntityPosition.Position;

            if (_retreatCount > 1500)
            {
                _moveSpeed = AnimationHelper.MoveToTarget(_moveSpeed, 1, 0.05f * Game1.TimeMultiplier);
            }

            var moveDistance = _moveSpeed * Game1.TimeMultiplier;

            if (direction.Length() > moveDistance)
            {
                direction.Normalize();
                EntityPosition.Offset(direction * moveDistance);
            }
            else
            {
                _attackOut = false;
                EntityPosition.Set(_startPosition);
                _aiComponent.ChangeState("hidden");
            }

            UpdateTail();
        }

        private void InitPulled()
        {
            _damageField.IsActive = false;
            _attackOut = true;
            _pullSound = 0;

            _animator.Play("head_0");
        }

        private void UpdatePulled()
        {
            // finished pull
            if (!MapManager.ObjLink.Hookshot.IsMoving)
            {
                FinishPull();
            }

            UpdateTail();

            _pullSound += Game1.DeltaTime;
            if (_pullSound > 75)
            {
                _pullSound -= 75;
                Game1.GameManager.PlaySoundEffect("D360-41-29");
            }
        }

        private void FinishPull()
        {
            MapManager.ObjLink.Hookshot.HookshotPosition.PositionChangedDict.Remove(typeof(BossSlimeEel));

            _aiComponent.ChangeState("retreat");
        }

        private void InitAttack()
        {
            SetAttackPosition(Game1.RandomNumber.Next(0, 4));

            // activate damage
            for (var i = 0; i < _tailParts.Length; i++)
                _tailParts[i].SetActive(true);

            _damageField.IsActive = true;
            _sprite.IsVisible = true;
            _animator.Play("attack");
        }

        private void SetAttackPosition(int index)
        {
            _moveDirection = index < 2 ? 1 : -1;

            var spawnPosition = new Vector2(
                _centerPosition.X + (index % 2 == 0 ? -32 : 32),
                _centerPosition.Y + (index < 2 ? -56 : 56));

            _animationComponent.MirroredV = index >= 2;
            _animationComponent.MirroredH = index >= 2;

            EntityPosition.Set(spawnPosition);
            _startPosition = spawnPosition;
        }

        private void UpdateAttack()
        {
            if (!_attackSound && _animator.CurrentFrameIndex == 3)
            {
                _attackSound = true;
                Game1.GameManager.PlaySoundEffect("D370-22-16");
            }

            // finished attacking?
            if (!_animator.IsPlaying)
            {
                _aiComponent.ChangeState("hidden");
            }
        }

        private void InitHidden()
        {
            _sprite.IsVisible = false;
            _eelSpawner.SetTailIsMoving(true);
        }

        private void EndHidden()
        {
            _attackSound = false;
            _aiComponent.ChangeState("attack");
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // change the draw effect
            if (_sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, _sprite.SpriteShader);
            }

            // draw the tail
            if (_attackOut)
                for (var i = 0; i < 5; i++)
                {
                    var centerPosition = new Vector2(EntityPosition.X, EntityPosition.Y + (14 + i * 14) * -_moveDirection);
                    var position = new Vector2(centerPosition.X - 8, centerPosition.Y - 8);
                    _tailParts[i].EntityPosition.Set(centerPosition);

                    if (_moveDirection == 1 && position.Y + 16 < _startPosition.Y ||
                        _moveDirection == -1 && position.Y > _startPosition.Y)
                        continue;

                    var sprite = _spriteTail;
                    // draw the part with the heart
                    if (i == 0)
                        sprite = Game1.TotalGameTime % (32000 / 60f) < (16000 / 60f) ? _spriteHeart0 : _spriteHeart1;

                    DrawHelper.DrawNormalized(spriteBatch, sprite, position, Color.White);
                }

            // draw the tail while moving around outside
            if (_isFullyOut)
            {
                for (var i = _tailPositions.Length - 1; i >= 0; i--)
                {
                    var sprite = i == 2 ? _spriteTailEnd : _spriteTail;
                    var position = new Vector2(_tailPositions[i].X - 8, _tailPositions[i].Y - 8);
                    DrawHelper.DrawNormalized(spriteBatch, sprite, position, Color.White);
                }
            }

            // draw the head
            _sprite.Draw(spriteBatch);

            // change the draw effect
            if (_sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
            }

            // draw the explosion
            if (_explosionAnimator.IsPlaying)
                _explosionAnimator.Draw(spriteBatch, EntityPosition.Position, Color.White);
        }

        public Values.HitCollision OnHitHeart(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            // can not get attacked while not visible
            if (!_attackOut || (type & HitType.Sword) == 0)
                return Values.HitCollision.None;

            var wasAlive = _aiDamageState.CurrentLives > 0;
            var hitReturn = _aiDamageState.OnHit(originObject, direction, type, damage, false);

            if (_aiDamageState.CurrentLives <= 0 && wasAlive)
            {
                Game1.GameManager.StartDialogPath("slime_eel_1");
                _eelSpawner.ToDespawn();
            }

            return hitReturn;
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            // can get pulled out by the hookshot at animation frame 1, 2 and 3
            if (_aiComponent.CurrentStateId == "attack" && 0 < _animator.CurrentFrameIndex && _animator.CurrentFrameIndex < 4 && type == HitType.Hookshot)
            {
                _hockshotPosition = MapManager.ObjLink.Hookshot.HookshotPosition.Position;
                _hookshotOffset = EntityPosition.Position - _hockshotPosition;

                //TODO: not sure what the real condition for this is
                if (_aiDamageState.CurrentLives <= 4 && Game1.RandomNumber.Next(0, 10) < 5)
                    _aiComponent.ChangeState("jumpingOut");
                else
                {
                    MapManager.ObjLink.Hookshot.HookshotPosition.AddPositionListener(typeof(BossSlimeEel), OnPositionChange);
                    _aiComponent.ChangeState("pulled");
                }

                _eelSpawner.SetTailIsMoving(false);
                _eelSpawner.ChangeRotation();

                return Values.HitCollision.Blocking;
            }

            if (_aiComponent.CurrentStateId == "attack")
                return Values.HitCollision.RepellingParticle;

            if (!_isFullyOut && !_attackOut)
                return Values.HitCollision.None;

            return Values.HitCollision.RepellingParticle;
        }

        private void OnPositionChange(CPosition position)
        {
            // head gets pulled out of the hole
            var newPosition = position.Position + _hookshotOffset;
            EntityPosition.Set(newPosition);
        }

        private void UpdateTailPositions()
        {
            SavePosition();

            var indexCount = _saveIndex + (_saveCounter / _saveInterval);
            var timeDiff = _saveCounter + _saveInterval * 1000;

            for (var i = 0; i < _tailPositions.Length; i++)
            {
                indexCount -= _tailDistance[i];
                if (indexCount < 0)
                    indexCount += _savedPosition.Length;

                timeDiff -= _tailDistance[i] * _saveInterval;
                var index = (int)indexCount;

                _tailPositions[i] = Vector2.Lerp(
                    _savedPosition[index], _savedPosition[(index + 1) % _savedPosition.Length],
                    (timeDiff % _saveInterval) / _saveInterval);
            }
        }

        // TODO: make this better; does not work correctly
        private void SavePosition()
        {
            var position = EntityPosition.Position + _body.VelocityTarget * (Game1.DeltaTime / 16.6667f);
            _saveCounter += Game1.DeltaTime;
            var diff = _saveCounter % _saveInterval;

            var updateSteps = (int)(_saveCounter / _saveInterval);
            _saveIndex = (_saveIndex + updateSteps) % _savedPosition.Length;
            var index = _saveIndex;

            var currentDirection = _moveRotation;

            while (_saveCounter >= _saveInterval)
            {
                _saveCounter -= _saveInterval;

                index--;
                if (index < 0)
                    index = _savedPosition.Length - 1;

                var vecDir = new Vector2((float)Math.Sin(currentDirection), (float)Math.Cos(currentDirection));
                _savedPosition[index] = position - vecDir * (diff / 16.6667f);

                position = _savedPosition[index];
                diff = _saveInterval;
                currentDirection -= _moveDirection * 0.025f * (_saveInterval / 16.6667f);
            }
        }
    }
}
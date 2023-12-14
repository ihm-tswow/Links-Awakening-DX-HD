using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.GameObjects.Base.Components.AI
{
    class AiDamageState
    {
        public delegate void OnDeleteTemplate(bool pieceOfPower);
        public OnDeleteTemplate OnDeath;

        public delegate void OnLiveZero();
        public OnLiveZero OnLiveZeroed;

        public delegate void OnBurnDelegate();
        public OnBurnDelegate OnBurn;

        public int ExplosionOffsetY;
        public Point FlameOffset;

        public string SpawnItem;

        public float HitMultiplierX = 5;
        public float HitMultiplierY = 5;

        public bool IgnoreZeroDamage;
        public bool IsActive = true;
        public bool MoveBody = true;
        public bool UpdateLastStateFire;
        public bool DeathAnimation = true;
        public bool SpawnItems = true;

        private readonly GameObject _gameObject;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly CSprite _sprite;

        public SpriteShader DamageSpriteShader;
        private SpriteShader _normalShader;

        public AiTriggerCountdown DamageTrigger;
        private AiTriggerCountdown _deathCountdown;

        private bool _pieceOfPower;
        private float _bodyDrag;
        private float _bodyDragAir;

        private double _pieceOfPowerCounter;
        private int _pieceOfPowerDeathCount;

        public int CurrentLives;

        private bool _damageBlink;
        private bool _returnState;
        private readonly bool _hasBurnState;

        public const int BlinkTime = 66;
        public const int CooldownTime = BlinkTime * 6;
        private readonly int _cooldownTime;

        public int ExplostionWidth = 32;
        public int ExplostionHeight = 32;

        public bool HasDamageState;
        public bool BossHitSound;

        private float _deathCount = -1000;

        public AiDamageState(GameObject gameObject, BodyComponent body, AiComponent aiComponent, CSprite sprite, int lives, bool hasDamageState = true, bool hasBurnState = true, int cooldownTime = CooldownTime)
        {
            _gameObject = gameObject;
            _body = body;
            _aiComponent = aiComponent;
            _sprite = sprite;
            _normalShader = sprite.SpriteShader;

            CurrentLives = lives;

            HasDamageState = hasDamageState;
            _hasBurnState = hasBurnState;

            _cooldownTime = cooldownTime;

            // basic on death reaction
            OnDeath = BaseOnDeath;

            DamageSpriteShader = Resources.DamageSpriteShader0;

            _aiComponent.Trigger.Add(DamageTrigger = new AiTriggerCountdown(cooldownTime, DamageTick, FinishDamage));
            if (hasDamageState)
                _aiComponent.States.Add("damage", new AiState());

            var stateKnockBack = new AiState();
            stateKnockBack.Trigger.Add(new AiTriggerCountdown(cooldownTime, null, FinishKnockback));

            var statePieceOfPower = new AiState(UpdatePieceOfPower) { Init = InitPieceOfPower };
            var stateBurning = new AiState(UpdateBurning) { Init = InitBurning };
            var stateDamageDeath = new AiState { Init = () => OnDeath(false) };

            _aiComponent.Trigger.Add(_deathCountdown = new AiTriggerCountdown(cooldownTime, DeathTick, () => DeathTick(0)));

            _aiComponent.States.Add("knockBack", stateKnockBack);
            _aiComponent.States.Add("pieceOfPower", statePieceOfPower);
            if (hasBurnState)
                _aiComponent.States.Add("burning", stateBurning);
            _aiComponent.States.Add("damageDeath", stateDamageDeath);
        }

        public void AddBossDamageState(AiTriggerCountdown.TriggerEndFunction deathAnimationEnd)
        {
            OnDeath = OnDeathBoss;

            var stateDeath = new AiState(UpdateDeath);
            stateDeath.Trigger.Add(new AiTriggerCountdown(3000 / BlinkTime * BlinkTime, UpdateBlink, deathAnimationEnd));

            _aiComponent.States.Add("deathBoss", stateDeath);
        }

        public bool IsInDamageState()
        {
            return DamageTrigger.CurrentTime > 0 ||
                _aiComponent.CurrentStateId == "knockBack" ||
                _aiComponent.CurrentStateId == "burning" ||
                _aiComponent.CurrentStateId == "pieceOfPower";
        }

        public Values.HitCollision HitKnockBack(GameObject gameObject, Vector2 direction, HitType damageType, bool pieceOfPower, bool blink = true)
        {
            if (!IsActive || IsInDamageState())
                return Values.HitCollision.None;

            _aiComponent.ChangeState(pieceOfPower ? "pieceOfPower" : "knockBack");

            _damageBlink = blink;
            DamageTrigger.OnInit();

            if (pieceOfPower)
            {
                _body.Velocity.X = direction.X * 3;
                _body.Velocity.Y = direction.Y * 3;
            }
            else
            {
                _body.Velocity.X = direction.X * HitMultiplierX;
                _body.Velocity.Y = direction.Y * HitMultiplierY;
            }

            _returnState = true;

            return Values.HitCollision.Enemy;
        }

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!IsActive || IsInDamageState() || damageType == HitType.PegasusBootsPush)
                return Values.HitCollision.None;

            // directly delete the gameObject if the attack comes from a bowwow
            if (damageType == HitType.BowWow)
            {
                DeathAnimation = false;
                OnDeath(false);
                return Values.HitCollision.Enemy;
            }

            if (damage <= 0 && IgnoreZeroDamage || DamageTrigger.CurrentTime > 0)
                return Values.HitCollision.Enemy;

            CurrentLives -= damage;

            // burn on powder impact
            if ((damageType == HitType.MagicPowder || damageType == HitType.MagicRod) && _hasBurnState)
            {
                if (_aiComponent.CurrentStateId != "burning")
                {
                    _aiComponent.ChangeState("burning");
                    var speedMultiply = (damageType == HitType.MagicPowder ? 0.125f : 0.5f);

                    if (MoveBody)
                    {
                        _body.Velocity.X = direction.X * HitMultiplierX * speedMultiply;
                        _body.Velocity.Y = direction.Y * HitMultiplierY * speedMultiply;
                    }

                    Game1.GameManager.PlaySoundEffect("D378-18-12");

                    return Values.HitCollision.Enemy;
                }
            }

            if (_aiComponent.CurrentStateId == "burning")
                return Values.HitCollision.None;

            // play sound effect
            if (!BossHitSound)
            {
                if (pieceOfPower)
                    Game1.GameManager.PlaySoundEffect("D370-17-11");

                Game1.GameManager.PlaySoundEffect("D360-03-03");
            }
            else
            {
                if (CurrentLives <= 0)
                    Game1.GameManager.PlaySoundEffect("D378-19-13");
                else
                    Game1.GameManager.PlaySoundEffect("D370-07-07");
            }

            if (pieceOfPower)
                _aiComponent.ChangeState("pieceOfPower");
            else
            {
                if (HasDamageState)
                {
                    _returnState = true;
                    _aiComponent.ChangeState("damage");
                }
            }

            DamageTrigger.OnInit();

            _damageBlink = damage > 0;

            if (MoveBody)
            {
                if (pieceOfPower)
                {
                    _body.Velocity.X = direction.X * 3;
                    _body.Velocity.Y = direction.Y * 3;
                }
                else
                {
                    _body.Velocity.X = direction.X * HitMultiplierX;
                    _body.Velocity.Y = direction.Y * HitMultiplierY;
                }
            }

            // trigger death event?
            if (CurrentLives <= 0)
            {
                OnLiveZeroed?.Invoke();
                _deathCountdown.OnInit();
            }

            return Values.HitCollision.Enemy;
        }

        public void SetDamageState(bool blink = true)
        {
            _damageBlink = blink;
            DamageTrigger.OnInit();
        }

        private void InitPieceOfPower()
        {
            _pieceOfPower = true;

            _bodyDrag = _body.Drag;
            _bodyDragAir = _body.DragAir;

            _body.Drag = 1.0f;
            _body.DragAir = 1.0f;

            _pieceOfPowerDeathCount = 0;
        }

        private void UpdatePieceOfPower()
        {
            if (!HasDamageState)
                _aiComponent.States[_aiComponent.LastStateId].Update?.Invoke();

            // draw a trail
            if (_pieceOfPowerCounter <= 0)
            {
                _pieceOfPowerCounter = 80;
                var animation = new ObjAnimator(_gameObject.Map, 0, 0, 0, 0, Values.LayerPlayer, "Particles/pieceOfPowerTrail", "run", true);
                animation.EntityPosition.Set(_body.Position.Position +
                                             new Vector2(_body.OffsetX + _body.Width / 2f, _body.OffsetY + _body.Height / 2f));
                animation.EntityPosition.Z = _body.Position.Z;
                Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(animation);
                _pieceOfPowerDeathCount++;
            }
            _pieceOfPowerCounter -= Game1.DeltaTime;

            var collision = false;

            if ((_body.LastVelocityCollision & Values.BodyCollision.Horizontal) != 0)
                _body.Velocity.X = 0;
            if ((_body.LastVelocityCollision & Values.BodyCollision.Vertical) != 0)
                _body.Velocity.Y = 0;

            // glide on the wall depending on the angle the body moved towards the wall
            if (((_body.LastVelocityCollision & Values.BodyCollision.Horizontal) != 0 && MathF.Abs(_body.Velocity.X) > MathF.Abs(_body.Velocity.Y)) ||
                ((_body.LastVelocityCollision & Values.BodyCollision.Vertical) != 0 && MathF.Abs(_body.Velocity.Y) > MathF.Abs(_body.Velocity.X)))
            {
                collision = true;
            }

            // last collision
            if ((collision && _pieceOfPowerDeathCount > 1) || _pieceOfPowerDeathCount > 5)
            {
                _pieceOfPower = false;
                _body.Drag = _bodyDrag;
                _body.DragAir = _bodyDragAir;

                if (CurrentLives <= 0)
                {
                    _body.Velocity.X = 0;
                    _body.Velocity.Y = 0;
                    OnDeath(true);
                }
                else
                {
                    _aiComponent.ChangeState(_aiComponent.LastStateId, true);
                }
            }
        }

        private void InitBurning()
        {
            OnBurn?.Invoke();

            _body.VelocityTarget = Vector2.Zero;

            // spawn explosion effect
            var burnAnimator = new ObjAnimator(_gameObject.Map, 0, 0, 0, 0, Values.LayerTop, "Particles/flame", "idle", false);
            burnAnimator.EntityPosition.Set(_gameObject.EntityPosition.Position);

            // move the animation with the game object
            burnAnimator.EntityPosition.SetParent(_gameObject.EntityPosition,
                new Vector2((int)(_body.OffsetX + _body.Width / 2) + FlameOffset.X,
                            (int)(_body.OffsetY + _body.Height) - 8 + FlameOffset.Y));

            // remove the burning sprite if the ai state changes (e.g. by falling down a hole)
            burnAnimator.Animator.OnFrameChange = () =>
            {
                // @HACK
                burnAnimator.AnimationComponent.UpdateSprite();
                if (_aiComponent.Owner.Map == null || _aiComponent.CurrentStateId != "burning")
                    burnAnimator.Map.Objects.DeleteObjects.Add(burnAnimator);
            };
            burnAnimator.Animator.OnAnimationFinished = () =>
            {
                FinishBurning();
                burnAnimator.Map.Objects.DeleteObjects.Add(burnAnimator);
            };

            Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(burnAnimator);
        }

        private void UpdateBurning()
        {
            if (UpdateLastStateFire)
                _aiComponent.States[_aiComponent.LastStateId].Update?.Invoke();
        }

        private void FinishBurning()
        {
            OnDeath(false);
        }

        private void DamageTick(double time)
        {
            if (_damageBlink)
                _sprite.SpriteShader = (_cooldownTime - time) % (BlinkTime * 2) < BlinkTime ? DamageSpriteShader : _normalShader;
        }

        private void FinishDamage()
        {
            _sprite.SpriteShader = _normalShader;

            if (CurrentLives > 0 &&
                _aiComponent.CurrentStateId != "pieceOfPower" &&
                _aiComponent.LastStateId != "pieceOfPower" &&
                _aiComponent.LastStateId != "knockBack")
            {
                // go back to the previous state without calling the init methods
                if (HasDamageState && _returnState)
                {
                    _returnState = false;
                    _aiComponent.ChangeState(_aiComponent.LastStateId, true);
                }
            }
        }

        private void FinishKnockback()
        {
            _sprite.SpriteShader = _normalShader;

            // go back to the previous state without calling the init methods
            _aiComponent.ChangeState(_aiComponent.LastStateId, true);
        }

        private void DeathTick(double time)
        {
            // die when the time is over or the velocity of the body is low enough
            if (time <= 0 || (time < _cooldownTime - 175 && _body.Velocity.Length() < 0.5f && HitMultiplierX > 0 && HitMultiplierY > 0))
            {
                if (_pieceOfPower)
                {
                    _body.Drag = _bodyDrag;
                    _body.DragAir = _bodyDragAir;
                }

                _deathCountdown.Stop();
                OnDeath(false);
            }
        }

        private void UpdateBlink(double time)
        {
            var blinkTime = BlinkTime;
            _sprite.SpriteShader = time % (blinkTime * 2) >= blinkTime ? DamageSpriteShader : _normalShader;
        }

        private void UpdateDeath()
        {
            _deathCount += Game1.DeltaTime;
            if (_deathCount < 100)
                return;
            _deathCount -= 100;

            Game1.GameManager.PlaySoundEffect("D378-19-13");

            var posX = (int)_gameObject.EntityPosition.X - ExplostionWidth / 2 + Game1.RandomNumber.Next(0, ExplostionWidth) - 8;
            var posY = (int)_gameObject.EntityPosition.Y - (int)_gameObject.EntityPosition.Z + ExplosionOffsetY - ExplostionHeight + Game1.RandomNumber.Next(0, ExplostionHeight) - 8;

            // spawn explosion effect
            _gameObject.Map.Objects.SpawnObject(new ObjAnimator(_gameObject.Map, posX, posY, Values.LayerTop, "Particles/spawn", "run", true));
        }

        public void OnDeathBoss(bool pieceOfPower)
        {
            Game1.GameManager.PlaySoundEffect("D370-16-10");

            IsActive = false;

            // start the death animation
            _aiComponent.ChangeState("deathBoss");
        }

        public void BaseOnDeath(bool pieceOfPower)
        {
            if (_gameObject.Map == null)
                return;

            _gameObject.Map.Objects.DeleteObjects.Add(_gameObject);

            // play sound effect
            if (pieceOfPower)
                Game1.GameManager.PlaySoundEffect("D370-18-12");

            Game1.GameManager.PlaySoundEffect("D378-19-13");

            // spawn explosion effect
            var bodyCenter = _body.BodyBox.Box.Center;
            bodyCenter.Y += ExplosionOffsetY;
            if (DeathAnimation)
                if (!pieceOfPower)
                {
                    Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(
                        new ObjAnimator(_gameObject.Map, (int)bodyCenter.X - 12, (int)(bodyCenter.Y - _body.Position.Z - 12 - 5),
                        Values.LayerTop, "Particles/explosion0", "run", true));
                }
                else
                {
                    var animation = new ObjAnimator(_gameObject.Map, 0, 0, Values.LayerTop, "Particles/pieceOfPowerExplosion", "run", true);
                    animation.EntityPosition.Set(new Vector2(bodyCenter.X, bodyCenter.Y - _body.Position.Z));
                    Game1.GameManager.MapManager.CurrentMap.Objects.SpawnObject(animation);
                }

            if (!SpawnItems)
                return;

            Game1.GameManager.GuardianAcornCount++;
            Game1.GameManager.PieceOfPowerCount++;

            // TODO_End reevaluate
            // spawn heart or ruby
            string strObject = SpawnItem;
            if (strObject == null)
            {
                var random = Game1.RandomNumber.Next(0, 100);
                if (random < 33)
                    strObject = "ruby";
                else if (random < 40)
                    strObject = "heart";
            }

            if (Game1.GameManager.GuardianAcornCount >= 12)
            {
                Game1.GameManager.GuardianAcornCount -= 12;

                var objItem = new ObjItem(_gameObject.Map, 0, 0, "j", null, "guardianAcorn", null, true);
                objItem.EntityPosition.Set(new Vector3(bodyCenter.X, bodyCenter.Y, _body.Position.Z));
                _gameObject.Map.Objects.SpawnObject(objItem);
            }
            // 40 to 45 enemies?
            // @TODO: remove
            else if (Game1.GameManager.PieceOfPowerCount >= 45)
            {
                Game1.GameManager.PieceOfPowerCount -= 45;

                var objItem = new ObjItem(_gameObject.Map, 0, 0, "j", null, "pieceOfPower", null, true);
                objItem.EntityPosition.Set(new Vector3(bodyCenter.X, bodyCenter.Y, _body.Position.Z));
                _gameObject.Map.Objects.SpawnObject(objItem);
            }
            else if (strObject != null)
            {
                // spawn a heart or a ruby
                var objItem = new ObjItem(_gameObject.Map, 0, 0, "j", null, strObject, null, true);
                objItem.EntityPosition.Set(new Vector3(bodyCenter.X, bodyCenter.Y, _body.Position.Z));
                _gameObject.Map.Objects.SpawnObject(objItem);
            }
        }
    }
}

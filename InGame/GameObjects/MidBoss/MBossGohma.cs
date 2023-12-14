using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.MidBoss
{
    class MBossGohma : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _aiDamageState;
        private readonly AnimationComponent _animationComponent;
        private readonly AiTriggerCountdown _attackAbortTrigger;
        private readonly CSprite _sprite;
        private readonly DamageFieldComponent _damageField;

        private const int ShakeTime = 1500;
        private const int BodyWidth = 28;

        private Vector2 _attackStartPosition;
        private Vector2 _attackTargetPosition;

        private const float AttackSpeed = 1.5f;
        private const float AttackReturnSpeed = 1f;

        private const float WalkSpeed = 1.0f;
        private const float RunSpeed = 1.5f;

        // 0: both parts are alive
        // 1: one of them is dead
        // 2: both parts are dead
        private int _bossState;

        private string _saveKey;
        private bool _isOnTop;

        public MBossGohma(Map.Map map, int posX, int posY, string saveKey, bool onTop) : base(map, "gohma")
        {
            EntityPosition = new CPosition(posX + 16, posY + 16, 0);
            EntitySize = new Rectangle(-16, -16, 32, 16);

            _saveKey = saveKey;
            _isOnTop = onTop;

            // there is no door and this is strange because in the original you can kill only one of them and just reenter the room
            if (!string.IsNullOrEmpty(_saveKey))
            {
                // check if the boss was already killed
                var bossState = Game1.GameManager.SaveManager.GetInt(_saveKey, 0);
                if (bossState == 2)
                {
                    IsDead = true;
                    return;
                }
                else
                {
                    Game1.GameManager.SaveManager.SetInt(_saveKey, 0);
                }

                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            }

            _animator = AnimatorSaveLoad.LoadAnimator("MidBoss/gohma");

            _sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(0, -16));

            _body = new BodyComponent(EntityPosition, -BodyWidth / 2, -14, BodyWidth, 14, 8)
            {
                IgnoreHoles = true,
                MoveCollision = OnCollision,
                FieldRectangle = Map.GetField(posX, posY, 16)
            };

            _aiComponent = new AiComponent();

            var stateIdle = new AiState(UpdateIdle);
            var stateWalk = new AiState { Init = InitWalk };
            stateWalk.Trigger.Add(new AiTriggerRandomTime(ChangeState, 1500, 3000));
            var stateRun = new AiState { Init = InitRun };
            stateRun.Trigger.Add(new AiTriggerRandomTime(ChangeState, 1500, 2500));
            var stateShake = new AiState { Init = InitShake };
            stateShake.Trigger.Add(new AiTriggerCountdown(ShakeTime, ShakeTick, ShakeEnd));
            var stateAttack = new AiState(UpdateAttack) { Init = InitAttack };
            // this trigger is used to abort the attack with a little delay so to not directly return
            stateAttack.Trigger.Add(_attackAbortTrigger = new AiTriggerCountdown(65, null, () => _aiComponent.ChangeState("attackReturn"), false));
            var stateAttackReturn = new AiState(UpdateAttackRevert) { Init = InitAttackReturn };
            var stateWait = new AiState();

            var stateEye0 = new AiState();
            stateEye0.Trigger.Add(new AiTriggerCountdown(1000, null, ToEye1));
            var stateEye1 = new AiState();
            stateEye1.Trigger.Add(new AiTriggerCountdown(400, null, ToEye2));
            var stateEye2 = new AiState();
            stateEye2.Trigger.Add(new AiTriggerCountdown(350, null, ToEye3));
            var stateEye3 = new AiState();
            stateEye3.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("walk")));

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("walk", stateWalk);
            _aiComponent.States.Add("run", stateRun);
            _aiComponent.States.Add("attackShake", stateShake);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("attackReturn", stateAttackReturn);
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("eye0", stateEye0);
            _aiComponent.States.Add("eye1", stateEye1);
            _aiComponent.States.Add("eye2", stateEye2);
            _aiComponent.States.Add("eye3", stateEye3);

            _aiDamageState = new AiDamageState(this, _body, _aiComponent, _sprite, 12, false, false)
            {
                BossHitSound = true,
                HitMultiplierX = 0,
                HitMultiplierY = 0,
                ExplosionOffsetY = 8
            };
            _aiDamageState.AddBossDamageState(OnDeath);

            _aiComponent.ChangeState("idle");

            var damageCollider = new CBox(EntityPosition, -14, -14, 0, 28, 14, 8);
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 4));
            // RepelMultiplier needs to be high so that the player does not end in the boss
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush) { RepelMultiplier = 1.5f });
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));
        }

        private void OnKeyChange()
        {
            _bossState = Game1.GameManager.SaveManager.GetInt(_saveKey, 0);
        }

        private void ChangeState()
        {
            _animator.SpeedMultiplier = 1f;

            // 25% chance to start walking
            var changeState = Game1.RandomNumber.Next(0, 4) < 3 &&
                              MapManager.ObjLink.EntityPosition.Position.Y < EntityPosition.Position.Y + 40;

            if (changeState)
            {
                // player is standing above the boss
                if (Game1.RandomNumber.Next(0, 2) == 0)
                    _aiComponent.ChangeState("attackShake");
                else
                    ToEye0();
            }
            else
            {
                _aiComponent.ChangeState("walk");
            }

            // player left the room?
            if (!_body.FieldRectangle.Intersects(MapManager.ObjLink.BodyRectangle))
            {
                Game1.GameManager.SetMusic(-1, 2);
                _aiComponent.ChangeState("idle");
            }
        }

        private void UpdateIdle()
        {
            // start walking
            if (_body.FieldRectangle.Intersects(MapManager.ObjLink.BodyRectangle))
            {
                Game1.GameManager.SetMusic(79, 2);
                _aiComponent.ChangeState("walk");
            }
        }

        private void ToEye0()
        {
            // stop walking
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("stand");

            _aiComponent.ChangeState("eye0");
        }

        private void ToEye1()
        {
            _animator.Play("eye");

            _aiComponent.ChangeState("eye1");
        }

        private void ToEye2()
        {
            // spawn a fireball
            Map.Objects.SpawnObject(new EnemyFireball(Map, (int)EntityPosition.X, (int)EntityPosition.Y - 8, 1.25f));

            _aiComponent.ChangeState("eye2");
        }

        private void ToEye3()
        {
            _animator.Play("stand");

            _aiComponent.ChangeState("eye3");
        }

        private void InitWalk()
        {
            var direction = -1 + Game1.RandomNumber.Next(0, 2) * 2;
            _body.VelocityTarget = new Vector2(direction, 0) * WalkSpeed;
            _animator.Play("walk");
        }

        private void InitRun()
        {
            var direction = -1 + Game1.RandomNumber.Next(0, 2) * 2;
            _body.VelocityTarget = new Vector2(direction, 0) * RunSpeed;
            _animator.Play("walk");
            _animator.SpeedMultiplier = 1.5f;
        }

        private void InitShake()
        {
            _body.VelocityTarget = Vector2.Zero;
        }

        private void ShakeTick(double counter)
        {
            // 5 frames to go left/right
            _animationComponent.SpriteOffset.X = MathF.Sin(MathF.PI * ((ShakeTime - (float)counter) / 1000 * (60 / 5f)));
            _animationComponent.UpdateSprite();
        }

        private void ShakeEnd()
        {
            _animationComponent.SpriteOffset.X = 0;

            // attack or start running depending on if the player is standing above the boss
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection.Y < 0)
                _aiComponent.ChangeState("run");
            else
                _aiComponent.ChangeState("attack");
        }

        private void InitAttack()
        {
            _attackStartPosition = EntityPosition.Position;
            // 45 if the top is the last one alive
            _attackTargetPosition = EntityPosition.Position + new Vector2(0, _bossState == 1 ? 45 : 25);

            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            var offset = 44;
            // make sure to not leave the room
            if (playerDirection.X < -22 && _body.FieldRectangle.Left <= EntityPosition.Position.X - BodyWidth / 2 - offset)
                _attackTargetPosition.X -= offset;
            if (playerDirection.X > 22 && EntityPosition.Position.X + BodyWidth / 2 + offset <= _body.FieldRectangle.Right)
                _attackTargetPosition.X += offset;
        }

        private void UpdateAttack()
        {
            var targetDirection = _attackTargetPosition - EntityPosition.Position;
            var offset = AttackSpeed * Game1.TimeMultiplier;

            if (targetDirection.Length() <= offset)
            {
                EntityPosition.Set(_attackTargetPosition);
                _aiComponent.ChangeState("attackReturn");
                _attackStartPosition.X = _attackTargetPosition.X;
            }
            else
            {
                // move towards the target position
                targetDirection.Normalize();
                EntityPosition.Move(targetDirection * AttackSpeed);
            }
        }

        private void InitAttackReturn()
        {
            Game1.GameManager.PlaySoundEffect("D370-22-16");
        }

        private void UpdateAttackRevert()
        {
            var targetDirection = _attackStartPosition - EntityPosition.Position;
            var offset = AttackReturnSpeed * Game1.TimeMultiplier;

            if (targetDirection.Length() <= offset)
            {
                EntityPosition.Set(_attackStartPosition);
                _aiComponent.ChangeState("walk");
            }
            else
            {
                // move towards the target position
                targetDirection.Normalize();
                EntityPosition.Move(targetDirection * AttackReturnSpeed);
            }
        }

        private void OnDeath()
        {
            // spawn a heart
            var objItem = new ObjItem(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y - 16, "j", null, "heart", null);
            Map.Objects.SpawnObject(objItem);

            _damageField.IsActive = false;

            Game1.GameManager.SaveManager.SetInt(_saveKey, _bossState + 1);

            if (_bossState == 1)
            {
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");
                // stop boss music
                Game1.GameManager.SetMusic(-1, 2);
            }

            Map.Objects.DeleteObjects.Add(this);
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            // change the direction if we collide with a wall
            _body.VelocityTarget.X = -_body.VelocityTarget.X;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            // abort the attack
            if (_aiComponent.CurrentStateId == "attack" && !_attackAbortTrigger.IsRunning())
            {
                _attackAbortTrigger.OnInit();
                _attackAbortTrigger.Start();
            }

            return true;
        }

        public Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // can only hit the boss with the hookshot or an arrow
            if ((damageType & (HitType.Hookshot | HitType.Bow | HitType.MagicRod | HitType.Boomerang)) == 0 ||
                (_aiComponent.CurrentStateId != "eye1" && _aiComponent.CurrentStateId != "eye2") ||
                _aiDamageState.IsInDamageState())
            {
                return Values.HitCollision.RepellingParticle;
            }

            if (damageType == HitType.Bow)
                damage *= 2;
            if (damageType == HitType.MagicRod)
                damage *= 2;
            if (damageType == HitType.Boomerang)
                damage = 4;

            return _aiDamageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }
    }
}

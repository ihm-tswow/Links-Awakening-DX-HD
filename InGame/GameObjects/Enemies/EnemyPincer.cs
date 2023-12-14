using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyPincer : GameObject
    {
        private readonly CSprite _sprite;
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly DamageFieldComponent _damageField;
        private readonly BodyComponent _body;
        private readonly AiStunnedState _stunnedState;

        private readonly AiDamageState _damageState;
        private readonly Rectangle _tailRectangle = new Rectangle(184, 124, 8, 8);

        private readonly Vector2 _spawnPosition;
        private Vector2 _direction;
        private Vector2 _attackOffset;
        private Vector2 _retractStartPosition;

        private float _attackCounter;
        private int _dirIndex;

        public EnemyPincer() : base("pincer") { }

        public EnemyPincer(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-32, -32, 64, 64);

            _spawnPosition = new Vector2(EntityPosition.X, EntityPosition.Y);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/pincer");
            _animator.Play("eyes");

            _sprite = new CSprite(EntityPosition) { IsVisible = false };
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -8));

            _body = new BodyComponent(EntityPosition, -6, -6, 12, 12, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                Drag = 0.75f,
                IgnoreHoles = true
            };

            var stateWaiting = new AiState(UpdateWaiting);
            var stateSpawning = new AiState(null);
            stateSpawning.Trigger.Add(new AiTriggerCountdown(1000, null, ToAttack));
            var stateAttacking = new AiState(UpdateAttack);
            var stateAttackWait = new AiState(null);
            stateAttackWait.Trigger.Add(new AiTriggerCountdown(1000, null, ToRetract));
            var stateRetract = new AiState(UpdateRetract);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("spawning", stateSpawning);
            _aiComponent.States.Add("attacking", stateAttacking);
            _aiComponent.States.Add("attackWait", stateAttackWait);
            _aiComponent.States.Add("retract", stateRetract);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 2, false) { HitMultiplierX = 1.5f, HitMultiplierY = 1.5f };
            _stunnedState = new AiStunnedState(_aiComponent, animationComponent, 3300, 900);

            _aiComponent.ChangeState("waiting");

            var damageBox = new CBox(EntityPosition, -5, -5, 0, 10, 10, 4);
            var hittableBox = new CBox(EntityPosition, -7, -7, 14, 14, 8);

            AddComponent(PushableComponent.Index, new PushableComponent(hittableBox, OnPush));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 2) { IsActive = false });
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact &&
                (_aiComponent.CurrentStateId == "attacking" ||
                 _aiComponent.CurrentStateId == "attackWait" ||
                 _aiComponent.CurrentStateId == "retract" ||
                _stunnedState.IsStunned()))
            {
                if (_aiComponent.CurrentStateId == "attacking")
                    _aiComponent.ChangeState("attackWait");

                var mult = 1.5f;
                _body.Velocity = new Vector3(direction.X * mult, direction.Y * mult, 0);
                return true;
            }

            return false;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (damageType == HitType.MagicPowder)
            {
                _stunnedState.StartStun();
                return Values.HitCollision.Enemy;
            }

            // can only attack while the enemy is attacking
            if (_aiComponent.CurrentStateId != "attacking" &&
                _aiComponent.CurrentStateId != "attackWait" &&
                _aiComponent.CurrentStateId != "retract" &&
                !_stunnedState.IsStunned())
                return Values.HitCollision.None;

            if (_aiComponent.CurrentStateId == "attacking")
                _aiComponent.ChangeState("attackWait");

            _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);

            // make sure to not fly away like for other enemies
            if (pieceOfPower)
                _body.Drag = 0.75f;

            return Values.HitCollision.Enemy;
        }

        private void ToWaiting()
        {
            _aiComponent.ChangeState("waiting");
            _sprite.IsVisible = false;
            _damageField.IsActive = false;
        }

        private void UpdateWaiting()
        {
            _direction = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.Position.X, EntityPosition.Position.Y - 4);
            if (_direction.Length() < 42)
            {
                _aiComponent.ChangeState("spawning");

                EntityPosition.Set(_spawnPosition);

                _sprite.IsVisible = true;
                _animator.Play("eyes");
            }
        }

        private void ToAttack()
        {
            _damageField.IsActive = true;
            _aiComponent.ChangeState("attacking");
            GetAttackDirection();
        }

        private void UpdateAttack()
        {
            _attackCounter += (Game1.TimeMultiplier * 2) / 35.0f;
            if (_attackCounter > 1)
                _attackCounter = 1;

            _attackOffset = _direction * _attackCounter * 35.0f;
            EntityPosition.Set(_spawnPosition + _attackOffset);

            if (_attackCounter >= 1)
                _aiComponent.ChangeState("attackWait");
        }

        private void ToRetract()
        {
            _aiComponent.ChangeState("retract");
            _retractStartPosition = EntityPosition.Position - _spawnPosition;
            _attackCounter = 1;
        }

        private void UpdateRetract()
        {
            _attackCounter -= (Game1.TimeMultiplier * 1.25f) / 35.0f;
            if (_attackCounter < 0)
                _attackCounter = 0;

            _attackOffset = Vector2.Lerp(_retractStartPosition, Vector2.Zero, 1 - _attackCounter);
            EntityPosition.Set(_spawnPosition + _attackOffset);

            if (_attackCounter <= 0)
                ToWaiting();
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the body
            if (_sprite.IsVisible && _aiComponent.CurrentStateId != "spawning")
                for (var i = 0; i < 3; i++)
                {
                    var position =
                        _spawnPosition + (EntityPosition.Position - _spawnPosition) * (0.15f + (i / 2f) * 0.5f) - new Vector2(4, 4);

                    spriteBatch.Draw(Resources.SprEnemies, position, _tailRectangle, Color.White);
                }

            // draw the head
            _sprite.Draw(spriteBatch);
        }

        private void GetAttackDirection()
        {
            _direction = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            if (_direction != Vector2.Zero)
                _direction.Normalize();

            var degree = MathHelper.ToDegrees((float)Math.Atan2(-_direction.Y, -_direction.X)) + 360;

            _dirIndex = (int)((degree + 22.5f) / 45) % 8;

            _animator.Play(_dirIndex.ToString());
        }
    }
}
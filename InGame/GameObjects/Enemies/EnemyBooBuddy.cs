using System;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyBooBuddy : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AnimationComponent _animatorComponent;
        private readonly AiDamageState _aiDamageState;
        private readonly DamageFieldComponent _damageField;
        private readonly DrawShadowCSpriteComponent _shadowDrawComponent;

        private RectangleF _rangeBox;
        private string _ligthKey;

        private Vector2 _targetPositionOffset;
        private float _speed;
        private static float _positionRadiant = 0.76f;

        private float _directionChangeCounter;
        private Vector2 _targetVelocity;

        private float _cooldownCounter;
        private bool _cooldownPosition;

        private int _fadeTime = 250;

        public EnemyBooBuddy() : base("boo buddy") { }

        public EnemyBooBuddy(Map.Map map, int posX, int posY, string lightKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 24, 0);
            EntitySize = new Rectangle(-8, -21, 16, 21);

            _ligthKey = lightKey;

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/boo buddy");
            _animator.Play("attack");

            var sprite = new CSprite(EntityPosition);
            _animatorComponent = new AnimationComponent(_animator, sprite, new Vector2(0, -5));

            _rangeBox = map.GetField(posX, posY, 8);

            _body = new BodyComponent(EntityPosition, -7, -20, 14, 14, 8)
            {
                IgnoresZ = true,
                CollisionTypes = Values.CollisionTypes.None,
            };

            // randomize the speed and the target position so that the ghosts don't align with each other
            _positionRadiant += (float)Math.PI;
            _targetPositionOffset = new Vector2((float)Math.Sin(_positionRadiant) * 6, (float)Math.Cos(_positionRadiant) * 6 + 4);
            _speed = 0.55f + Game1.RandomNumber.Next(0, 101) / 1000f;

            _aiComponent = new AiComponent();

            var stateAttacking = new AiState(UpdateAttacking) { Init = InitAttacking };
            var stateCooldown = new AiState(UpdateCooldown) { Init = InitCooldown };
            var stateFleeing = new AiState(UpdateFleeing) { Init = InitFleeing };
            var stateFading = new AiState();
            stateFading.Trigger.Add(new AiTriggerCountdown(_fadeTime, UpdateFading, FinishedFading));

            _aiComponent.States.Add("attacking", stateAttacking);
            _aiComponent.States.Add("cooldown", stateCooldown);
            _aiComponent.States.Add("fleeing", stateFleeing);
            _aiComponent.States.Add("fading", stateFading);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, sprite, 4, true, false);

            var damageCollider = new CBox(EntityPosition, -7, -20, 0, 14, 14, 8);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, _animatorComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowDrawComponent = new DrawShadowCSpriteComponent(sprite));

            if (!string.IsNullOrEmpty(_ligthKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));

            _aiComponent.ChangeState("attacking");
        }

        private void KeyChanged()
        {
            var lightOn = Game1.GameManager.SaveManager.GetString(_ligthKey) == "1";

            if (lightOn)
                _aiComponent.ChangeState("fleeing");
            else if (_aiComponent.CurrentStateId == "fleeing")
                _aiComponent.ChangeState("attacking");
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (damageType == HitType.Bow)
                return _aiDamageState.OnHit(gameObject, direction, damageType, 1, pieceOfPower);

            if (_aiComponent.CurrentStateId == "fleeing" || damageType == HitType.MagicRod)
                return _aiDamageState.OnHit(gameObject, direction, damageType, 4, pieceOfPower);

            // was hit in the attack state -> change int cooldown mode
            if (_aiComponent.CurrentStateId == "attacking")
            {
                _aiComponent.ChangeState("cooldown");
                return Values.HitCollision.Enemy;
            }

            return Values.HitCollision.None;
        }

        private void InitAttacking()
        {
            _animatorComponent.Sprite.Color = Color.White;
            _shadowDrawComponent.IsActive = true;

            _animator.Play("attack");
        }

        private void UpdateAttacking()
        {
            if (_rangeBox.Contains(MapManager.ObjLink.BodyRectangle))
            {
                // move towards the player
                var playerDirection = MapManager.ObjLink.EntityPosition.Position + _targetPositionOffset - EntityPosition.Position;
                if (playerDirection != Vector2.Zero)
                    playerDirection.Normalize();

                _targetVelocity = playerDirection * _speed;
            }
            else
            {
                _directionChangeCounter -= Game1.DeltaTime;

                // change direction
                if (_directionChangeCounter <= 0)
                {
                    _directionChangeCounter = Game1.RandomNumber.Next(750, 1000);

                    // move towards the center of the room with a random offset
                    var center = _rangeBox.Center + new Vector2(8, 12);
                    var centerOffset = center - EntityPosition.Position;

                    // depending on how far away the enemy is to the center of the room the direction can diverge more or less
                    var offsetLength = (int)centerOffset.Length();
                    var randomOffset = Game1.RandomNumber.Next(0, Math.Max(1, 50 - offsetLength)) / 50f * 2f * Math.PI;

                    var radius = Math.Atan2(centerOffset.Y, centerOffset.X) + randomOffset;

                    _targetVelocity = new Vector2((float)Math.Cos(radius), (float)Math.Sin(radius)) * _speed * 0.5f;
                }
            }

            var percentage = (float)Math.Pow(0.9, Game1.TimeMultiplier);
            _body.VelocityTarget = percentage * _body.VelocityTarget + (1 - percentage) * _targetVelocity;
            _animatorComponent.MirroredH = _targetVelocity.X < 0;
        }

        private void InitCooldown()
        {
            // disable the damage field
            _damageField.IsActive = false;

            _animator.Play("hit");
            _cooldownPosition = false;
            _cooldownCounter = 0;
            _body.VelocityTarget = Vector2.Zero;
        }

        private void UpdateCooldown()
        {
            _cooldownCounter += Game1.DeltaTime;

            // blink
            var isTransparent = _cooldownCounter % 133 < 66;
            _animatorComponent.Sprite.Color = isTransparent ? Color.Transparent : Color.White;
            _shadowDrawComponent.IsActive = !isTransparent;

            // change location
            if (!_cooldownPosition && _cooldownCounter > 500)
            {
                _cooldownPosition = true;

                // the new position is the mirror position from the center
                var center = _rangeBox.Center + new Vector2(8, 8);
                var centerOffset = EntityPosition.Position - center;
                EntityPosition.Set(center - centerOffset);
            }

            // finished cooldown
            if (_cooldownCounter > 1000)
            {
                _aiComponent.ChangeState("attacking");

                // reactivate the damage field
                _damageField.IsActive = true;
            }
        }

        private void InitFleeing()
        {
            _animatorComponent.Sprite.Color = Color.White;
            _shadowDrawComponent.IsActive = true;

            _animator.Play("flee");
        }

        private void UpdateFleeing()
        {
            // move away from the player
            var playerDirection = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();

            var percentage = (float)Math.Pow(0.9, Game1.TimeMultiplier);
            _body.VelocityTarget = percentage * _body.VelocityTarget + (1 - percentage) * playerDirection * 0.25f;
            _animatorComponent.MirroredH = playerDirection.X < 0;

            // remove enemy if he is too far away
            var center = _rangeBox.Center + new Vector2(0, 12);
            var centerOffset = EntityPosition.Position - center;
            if (Math.Abs(centerOffset.X) > 80 || Math.Abs(centerOffset.Y) > 72)
                _aiComponent.ChangeState("fading");
        }

        private void UpdateFading(double time)
        {
            // fade away
            _animatorComponent.Sprite.Color = Color.White * (float)(time / _fadeTime);
        }

        private void FinishedFading()
        {
            Map.Objects.DeleteObjects.Add(this);
        }
    }
}
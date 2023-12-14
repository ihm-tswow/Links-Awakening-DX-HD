using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Dungeon;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyGhini : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly DamageFieldComponent _damageField;
        private readonly CSprite _sprite;

        private readonly Rectangle _triggerField;
        private readonly Vector2 _centerPosition;

        private Vector2 _velocity;

        private double _direction;

        private float _flyHeight = 14;
        private float _rotationDirection;
        private float _dirChangeCount;
        private float _transparency;

        private bool _mainGhini;

        public EnemyGhini() : base("ghini") { }

        public EnemyGhini(Map.Map map, int posX, int posY, bool mainGhini, bool spawnAnimation) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, spawnAnimation ? 0 : _flyHeight);
            EntitySize = new Rectangle(-8, -32, 16, 32);

            _mainGhini = mainGhini;

            _triggerField = map.GetField(posX, posY);
            _centerPosition = new Vector2(_triggerField.Center.X, _triggerField.Center.Y + 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/ghini");
            _animator.Play("fly_1");

            _sprite = new CSprite(EntityPosition) { Color = spawnAnimation ? Color.Transparent : Color.White };
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -6, -12, 12, 12, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                IgnoreHoles = true,
                IgnoresZ = true,
            };

            var stateInit = new AiState();
            stateInit.Trigger.Add(new AiTriggerCountdown(64, null, () => _aiComponent.ChangeState("spawning")));
            var stateSpawning = new AiState(UpdateSpawning);
            var stateFlying = new AiState(UpdateFlying);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("init", stateInit);
            _aiComponent.States.Add("spawning", stateSpawning);
            _aiComponent.States.Add("flying", stateFlying);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 8, true, false) { IsActive = !spawnAnimation };
            _damageState.OnDeath = OnDeath;

            _aiComponent.ChangeState(spawnAnimation ? "init" : "flying");

            var damageCollider = new CBox(EntityPosition, -6, -14, 0, 12, 14, 8, true);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 4) { IsActive = !spawnAnimation });
            AddComponent(HittableComponent.Index, new HittableComponent(damageCollider, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new ShadowBodyDrawComponent(EntityPosition));
        }

        private void UpdateSpawning()
        {
            _transparency = AnimationHelper.MoveToTarget(_transparency, 1, Game1.TimeMultiplier * 0.15f);
            _sprite.Color = Color.White * _transparency;

            EntityPosition.Z += Game1.TimeMultiplier * 0.25f;

            if (EntityPosition.Z >= _flyHeight)
            {
                EntityPosition.Z = _flyHeight;
                _aiComponent.ChangeState("flying");
                _damageState.IsActive = true;
                _damageField.IsActive = true;
            }
        }

        private void UpdateFlying()
        {
            _dirChangeCount -= Game1.DeltaTime;

            // change the direction
            if (_dirChangeCount <= 0)
            {
                // the farther away the enemy is from the origin the more likely it becomes that he will move towards the center position
                var directionToStart = _centerPosition - EntityPosition.Position;
                var radiusToCenter = Math.Atan2(directionToStart.Y, directionToStart.X);

                var maxDistanceX = 85.0f;
                var maxDistanceY = 55.0f;
                var distanceMultiplier = Math.Clamp(
                    Math.Min(
                        (maxDistanceX - Math.Abs(directionToStart.X)) / maxDistanceX,
                        (maxDistanceY - Math.Abs(directionToStart.Y)) / maxDistanceY), 0, 1);

                _direction = radiusToCenter + (Math.PI - Game1.RandomNumber.Next(0, 628) / 100f) * distanceMultiplier;

                // new direction + new rotation speed
                _dirChangeCount = Game1.RandomNumber.Next(750, 1500) * (distanceMultiplier * 0.5f + 0.5f);
                _rotationDirection = Game1.RandomNumber.Next(-100, 100) / 1000f * distanceMultiplier;
            }

            _velocity *= (float)Math.Pow(0.95f, Game1.TimeMultiplier);

            _velocity += new Vector2((float)Math.Cos(_direction), (float)Math.Sin(_direction)) * 0.035f * Game1.TimeMultiplier;
            _direction += _rotationDirection * Game1.TimeMultiplier;

            // clamp the speed
            if (_velocity.Length() > 1.75f)
            {
                _velocity.Normalize();
                _velocity *= 1.75f;
            }

            _body.VelocityTarget = _velocity;

            _animator.Play("fly_" + (_body.VelocityTarget.X < 0 ? -1 : 1));
        }

        private void OnDeath(bool pieceOfPower)
        {
            if (_mainGhini)
                KillOtherGhinies();

            if (Game1.RandomNumber.Next(0, 100) < 75)
            {
                _damageState.SpawnItems = false;
                // spawns a fairy
                Map.Objects.SpawnObject(new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y, (int)EntityPosition.Z));
            }

            _damageState.BaseOnDeath(pieceOfPower);
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (type == HitType.MagicPowder)
                return Values.HitCollision.None;

            if (type == HitType.Bomb || type == HitType.Bow || type == HitType.MagicRod)
                damage *= 2;

            return _damageState.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        private void KillOtherGhinies()
        {
            var enemyList = new List<GameObject>();

            Map.Objects.GetGameObjectsWithTag(enemyList, Values.GameObjectTag.Enemy,
                _triggerField.X, _triggerField.Y, _triggerField.Width, _triggerField.Height);

            foreach (var enemy in enemyList)
            {
                if (enemy != this && enemy.IsActive && (enemy.GetType() == typeof(EnemyGhini) ||
                                                        enemy.GetType() == typeof(EnemyGhiniGiant)))
                {
                    var aiComponent = (AiComponent)enemy.Components[AiComponent.Index];
                    aiComponent?.ChangeState("damageDeath");
                }
            }
        }
    }
}
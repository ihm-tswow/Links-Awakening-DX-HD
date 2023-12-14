using System;
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
    internal class EnemyGhiniGiant : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly DamageFieldComponent _damageField;
        private readonly CSprite _sprite;

        private Rectangle _fieldRectangle;
        private Vector2 _velocity;
        private Vector2 _vecDirection;

        private double _direction;

        private float _rotationDirection;
        private float _dirChangeCount;
        private float _transparency;

        private int _flyHeight = 7;

        public EnemyGhiniGiant() : base("giant ghini") { }

        public EnemyGhiniGiant(Map.Map map, int posX, int posY, bool spawnAnimation) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16 + 7, spawnAnimation ? 0 : _flyHeight);
            EntitySize = new Rectangle(-16, -(30 + _flyHeight), 32, 30 + _flyHeight);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/ghiniGiant");
            _animator.Play("fly_1");

            _sprite = new CSprite(EntityPosition) { Color = spawnAnimation ? Color.Transparent : Color.White };
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-16, -30));

            _fieldRectangle = map.GetField(posX, posY, 16);

            _body = new BodyComponent(EntityPosition, -12, -30, 24, 30, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                IgnoreHoles = true,
                IgnoresZ = true,
            };

            var stateSpawning = new AiState(UpdateSpawning);
            var stateFlying = new AiState(UpdateFlying);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("spawning", stateSpawning);
            _aiComponent.States.Add("flying", stateFlying);

            _aiComponent.ChangeState(spawnAnimation ? "spawning" : "flying");

            var damageBox = new CBox(EntityPosition, -12, -28, 0, 24, 26, 8, true);
            var hittableBox = new CBox(EntityPosition, -13, -29, 0, 26, 28, 8, true);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 8, true, false) { OnDeath = OnDeath, IsActive = !spawnAnimation };

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 4) { IsActive = !spawnAnimation });
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new ShadowBodyDrawComponent(EntityPosition) { ShadowWidth = 24, ShadowHeight = 6 });
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
                var newDirection = Game1.RandomNumber.Next(0, 628) / 100f;
                _vecDirection = new Vector2((float)Math.Cos(newDirection), (float)Math.Sin(newDirection));
                _direction = newDirection;

                // new direction + new rotation speed
                _dirChangeCount = Game1.RandomNumber.Next(600, 1200);
                _rotationDirection = Game1.RandomNumber.Next(-100, 100) / 1000f;
            }

            _velocity *= (float)Math.Pow(0.95f, Game1.TimeMultiplier);

            _velocity += new Vector2((float)Math.Cos(_direction), (float)Math.Sin(_direction)) * 0.025f * Game1.TimeMultiplier;
            _direction += _rotationDirection * Game1.TimeMultiplier;

            _velocity += _vecDirection * 0.025f * Game1.TimeMultiplier;

            if ((EntityPosition.X < _fieldRectangle.X && _vecDirection.X < 0) ||
                (EntityPosition.X > _fieldRectangle.X + _fieldRectangle.Width && _vecDirection.X > 0))
            {
                _vecDirection.X = -Math.Sign(_vecDirection.X);
                _vecDirection.Y = 0;
                _dirChangeCount += 500;
                _direction = 1;
            }

            if ((EntityPosition.Y < _fieldRectangle.Y && _vecDirection.Y < 0) ||
                (EntityPosition.Y > _fieldRectangle.Y + _fieldRectangle.Height && _vecDirection.Y > 0))
            {
                _vecDirection.X = 0;
                _vecDirection.Y = -Math.Sign(_vecDirection.Y);
                _dirChangeCount += 500;
            }

            _body.VelocityTarget = _velocity;

            _animator.Play("fly_" + (_body.VelocityTarget.X < 0 ? -1 : 1));
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (type == HitType.MagicPowder)
                return Values.HitCollision.None;

            if (type == HitType.Bomb || type == HitType.Bow || type == HitType.MagicRod)
                damage *= 2;

            return _damageState.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        private void OnDeath(bool pieceOfPower)
        {
            if (Game1.RandomNumber.Next(0, 100) < 75)
            {
                _damageState.SpawnItems = false;
                // spawns a fairy
                Map.Objects.SpawnObject(new ObjDungeonFairy(Map, (int)EntityPosition.X, (int)EntityPosition.Y, (int)EntityPosition.Z));
            }

            _damageState.BaseOnDeath(pieceOfPower);
        }
    }
}
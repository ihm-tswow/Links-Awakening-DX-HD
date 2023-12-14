using System;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyCrow : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly AiTriggerTimer _followTimer;
        private readonly CBox _damageCollider;
        private readonly CBox _hittableBoxFly;
        private readonly HittableComponent _hittableComponent;

        private readonly Box _activationBox;

        private double _dirRadius;
        private int _dirIndex;
        private bool _goldLeaf;

        private const string _leafSaveKey = "ow_goldLeafCrow";

        public EnemyCrow() : base("crow") { }

        public EnemyCrow(Map.Map map, int posX, int posY, bool goldLeaf) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 12, 0);

            _goldLeaf = goldLeaf;

            if (_goldLeaf)
                EntitySize = new Rectangle(-8, -32, 16, 48);
            else
                EntitySize = new Rectangle(-8, -32, 16, 36);

            // abort spawn if the player already has the leaf
            if (_goldLeaf && Game1.GameManager.SaveManager.GetString(_leafSaveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _activationBox = new Box(posX - 10, posY - 32, 0, 36, 80, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/crow");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-7, -16));

            _body = new BodyComponent(EntityPosition, -6, -14, 12, 14, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                IgnoreHoles = true,
                IgnoresZ = true
            };

            var stateWaiting = new AiState(UpdateWaiting);
            stateWaiting.Trigger.Add(new AiTriggerRandomTime(UpdateLookDirection, 250, 750));
            var stateStart = new AiState(UpdateStart) { Init = InitStart };
            var stateFlying = new AiState(UpdateFlying);
            stateFlying.Trigger.Add(_followTimer = new AiTriggerTimer(1000));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("start", stateStart);
            _aiComponent.States.Add("flying", stateFlying);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 2, true, false);

            if (_goldLeaf)
                _damageState.OnDeath = OnDeath;

            _damageState.IsActive = false;

            _aiComponent.ChangeState("waiting");

            _damageCollider = new CBox(EntityPosition, -6, -14, 0, 12, 14, 4, true);
            var hittableBox = new CBox(EntityPosition, -8, -32, 0, 16, _goldLeaf ? 48 : 36, 8);
            _hittableBoxFly = new CBox(EntityPosition, -6, -15, 0, 12, 14, 8, true);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(_damageCollider, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, _hittableComponent = new HittableComponent(hittableBox, OnHit));
            AddComponent(PushableComponent.Index, new PushableComponent(_damageCollider, OnPush));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerTop));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        private void OnDeath(bool pieceofpower)
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();
            playerDirection *= 2.25f;

            // spawn the golden leaf jumping towards the player
            var objLeaf = new ObjItem(Map, 0, 0, null, _leafSaveKey, "goldLeaf", null, true);
            objLeaf.EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z));
            objLeaf.SetVelocity(new Vector3(playerDirection.X, playerDirection.Y, 1.5f));
            objLeaf.Collectable = false;
            Map.Objects.SpawnObject(objLeaf);

            _damageState.BaseOnDeath(pieceofpower);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (damageType == HitType.MagicPowder)
                return Values.HitCollision.None;

            if (damageType == HitType.Bow || damageType == HitType.MagicRod)
                damage /= 2;

            // start attacking?
            if (_aiComponent.CurrentStateId == "waiting" && (damageType == HitType.Bomb || damageType == HitType.ThrownObject))
            {
                _aiComponent.ChangeState("start");
                return Values.HitCollision.None;
            }

            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private void UpdateLookDirection()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection.Length() > 96)
                return;

            _dirIndex = MapManager.ObjLink.PosX < EntityPosition.X - _dirIndex * 4 ? -1 : 1;
            _animator.Play("idle_" + _dirIndex);
        }

        private void UpdateWaiting()
        {
            // activate the crow
            if (!_goldLeaf && MapManager.ObjLink._body.BodyBox.Box.Intersects(_activationBox))
                _aiComponent.ChangeState("start");
        }

        private void InitStart()
        {
            _hittableComponent.HittableBox = _hittableBoxFly;
        }

        private void UpdateStart()
        {
            _animator.Play("fly_" + _dirIndex);

            EntityPosition.Set(new Vector3(
                EntityPosition.X,
                EntityPosition.Y,
                EntityPosition.Z + 0.5f * Game1.TimeMultiplier));

            if (EntityPosition.Z >= 15)
            {
                EntityPosition.Z = 15;
                _aiComponent.ChangeState("flying");
                _damageState.IsActive = true;
                _dirRadius = Math.Atan2(MapManager.ObjLink.PosY - EntityPosition.Y, MapManager.ObjLink.PosX - EntityPosition.X);
            }
        }

        private void UpdateFlying()
        {
            var direction = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z);
            var directionRadius = Math.Atan2(direction.Y, direction.X);

            if (direction.Length() < 80)
            {
                var followSpeed = 0.02f;
                if (directionRadius < _dirRadius - followSpeed || _followTimer.State)
                    _dirRadius -= followSpeed * Game1.TimeMultiplier;
                else if (directionRadius > _dirRadius + followSpeed)
                    _dirRadius += followSpeed * Game1.TimeMultiplier;
            }

            var velocity = new Vector2((float)Math.Cos(_dirRadius), (float)Math.Sin(_dirRadius));
            _body.VelocityTarget = velocity * 1.25f;

            _dirIndex = velocity.X < 0 ? -1 : 1;
            _animator.Play("fly_" + _dirIndex);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_aiComponent.CurrentStateId == "waiting")
                return false;

            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction * 1.75f, _body.Velocity.Z);

            return true;
        }
    }
}
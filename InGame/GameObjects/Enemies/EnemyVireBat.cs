using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using System;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyVireBat : GameObject
    {
        private readonly BodyComponent _body;
        private readonly CSprite _sprite;
        private double _liveTime = 1250;

        private const float AttackSpeed = 2.0f;
        
        private bool _isAttackable;

        public EnemyVireBat(Map.Map map, Vector3 position, Vector2 direction) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(position.X, position.Y, position.Z);
            EntitySize = new Rectangle(-8, -48, 16, 48);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/vire bat");
            animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, _sprite, Vector2.Zero);

            var _aiComponent = new AiComponent();

            var stateMove = new AiState();
            stateMove.Trigger.Add(new AiTriggerCountdown(500, null, () => _aiComponent.ChangeState("wait")));
            var stateWait = new AiState() { Init = InitWait };
            stateWait.Trigger.Add(new AiTriggerCountdown(500, null, () => _aiComponent.ChangeState("attack")));
            var stateAttack = new AiState(UpdateAttack) { Init = InitAttack };

            _aiComponent.States.Add("move", stateMove);
            _aiComponent.States.Add("wait", stateWait);
            // it would probably look better if we move down while attacking
            _aiComponent.States.Add("attack", stateAttack);

            _aiComponent.ChangeState("move");

            _body = new BodyComponent(EntityPosition, -5, -12, 10, 12, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                VelocityTarget = direction,
                CollisionTypes = Values.CollisionTypes.None
            };

            var damageCollider = new CBox(EntityPosition, -5, -6, 0, 10, 6, 8, true);
            var hittableBox = new CBox(EntityPosition, -4, -12, 0, 8, 12, 8, true);

            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, new PushableComponent(damageCollider, OnPush));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(_sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
        }

        private void InitAttack()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position -
                new Vector2(EntityPosition.X, EntityPosition.Y - EntityPosition.Z);
            if (playerDirection != Vector2.Zero)
            {
                playerDirection.Normalize();
                _body.VelocityTarget = playerDirection * AttackSpeed;
            }
        }

        private void UpdateAttack()
        {
            _liveTime -= Game1.DeltaTime;

            // fade out
            if (_liveTime <= 100)
                _sprite.Color = Color.White * ((float)_liveTime / 100f);

            if (_liveTime < 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private void InitWait()
        {
            _isAttackable = true;
            _body.VelocityTarget = Vector2.Zero;
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (!_isAttackable)
                return Values.HitCollision.None;

            OnDeath();

            return Values.HitCollision.Enemy;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private void OnDeath()
        {
            var splashAnimator = new ObjAnimator(Map, 0, 0, 0, 0, Values.LayerTop, "Particles/spawn", "run", true);
            splashAnimator.EntityPosition.Set(new Vector2(EntityPosition.X - 8, EntityPosition.Y - EntityPosition.Z - 16));
            Map.Objects.SpawnObject(splashAnimator);

            Game1.GameManager.PlaySoundEffect("D378-19-13");

            Map.Objects.DeleteObjects.Add(this);
        }
    }
}
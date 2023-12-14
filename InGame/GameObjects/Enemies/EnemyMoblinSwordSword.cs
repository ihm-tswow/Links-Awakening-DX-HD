using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyMoblinSwordSword : GameObject
    {
        public readonly Animator Animator;
        public readonly CSprite Sprite;

        private readonly EnemyMoblinSword _owner;
        private readonly CBox _collisionBox;

        private double _lastHitTime;

        public EnemyMoblinSwordSword(Map.Map map, EnemyMoblinSword owner) : base(map)
        {
            _owner = owner;
            _owner.EntityPosition.AddPositionListener(typeof(EnemyMoblinSwordSword), PositionChange);

            EntityPosition = new CPosition(owner.EntityPosition.X, owner.EntityPosition.Y - 1, owner.EntityPosition.Z);
            EntitySize = new Rectangle(-22, -8 - 24, 44, 48);

            Animator = AnimatorSaveLoad.LoadAnimator("Enemies/moblin sword sword");

            Sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(Animator, Sprite, new Vector2(-8, -15));

            _collisionBox = new CBox(0, 0, 0, 0, 0, 4);
            UpdateCollisionBox();

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(_collisionBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(_collisionBox, OnHit));
            AddComponent(PushableComponent.Index, new PushableComponent(_collisionBox, OnPush) { RepelParticle = true });
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(Sprite, Values.LayerPlayer));
        }

        private void PositionChange(CPosition position)
        {
            EntityPosition.Set(new Vector2(position.X, position.Y - position.Z - 1));
        }

        private void Update()
        {
            UpdateCollisionBox();
        }

        private void UpdateCollisionBox()
        {
            _collisionBox.Box.X = EntityPosition.X - 8 + Animator.CollisionRectangle.X;
            _collisionBox.Box.Y = EntityPosition.Y - 15 + Animator.CollisionRectangle.Y;
            _collisionBox.Box.Width = Animator.CollisionRectangle.Width;
            _collisionBox.Box.Height = Animator.CollisionRectangle.Height;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
            {
                _owner.Body.Velocity.X = direction.X * 1.5f;
                _owner.Body.Velocity.Y = direction.Y * 1.5f;
            }

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (damageType == HitType.MagicRod || damageType == HitType.MagicPowder || damageType == HitType.Bow || damageType == HitType.Hookshot || damageType == HitType.Boomerang ||
                (_lastHitTime != 0 && Game1.TotalGameTime - _lastHitTime < 250))
                return Values.HitCollision.None;

            _lastHitTime = Game1.TotalGameTime;

            _owner.Body.Velocity.X = direction.X * 1.5f;
            _owner.Body.Velocity.Y = direction.Y * 1.5f;

            return Values.HitCollision.RepellingParticle;
        }
    }
}
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyGopongaFlowerGiant : GameObject
    {
        private readonly Animator _animator;
        private readonly AiDamageState _aiDamageState;

        public EnemyGopongaFlowerGiant() : base("giant goponga flower") { }

        public EnemyGopongaFlowerGiant(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 16, posY + 16, 0);
            EntitySize = new Rectangle(-16, -16, 32, 32);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/goponga flower giant");
            _animator.OnAnimationFinished = AnimationFinished;
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-16, -16));

            var body = new BodyComponent(EntityPosition, -14, -12, 28, 28, 8) { IgnoresZ = true };

            var collisionBox = new CBox(EntityPosition, -15, -13, 30, 28, 8);
            var hittableBox = new CBox(EntityPosition, -15, -13, 30, 28, 8);
            var damageBox = new CBox(EntityPosition, -16, -14, 32, 30, 8);

            var aiComponent = new AiComponent();
            aiComponent.States.Add("idle", new AiState(() => { }));
            _aiDamageState = new AiDamageState(this, body, aiComponent, sprite, 4)
            {
                HitMultiplierX = 0,
                HitMultiplierY = 0,
                FlameOffset = new Point(0, -8)
            };
            aiComponent.ChangeState("idle");

            AddComponent(AiComponent.Index, aiComponent);
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Enemy));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 4));
            AddComponent(BodyComponent.Index, body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(body, sprite, Values.LayerPlayer) { WaterOutline = false });
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            // only bowwow can attack
            if (type != HitType.BowWow && type != HitType.Hookshot && type != HitType.MagicRod && type != HitType.Boomerang)
                return Values.HitCollision.Blocking;

            if (type != HitType.BowWow && (type == HitType.MagicRod || damage >= _aiDamageState.CurrentLives))
            {
                _aiDamageState.HitMultiplierX = 4;
                _aiDamageState.HitMultiplierY = 4;
            }

            return _aiDamageState.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        private void AnimationFinished()
        {
            // start attacking the player?
            if (_animator.CurrentAnimation.Id == "idle")
            {
                var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
                if (playerDistance.Length() < 128)
                {
                    _animator.Play("pre_attack");

                    // shoot fireball
                    Map.Objects.SpawnObject(new EnemyFireball(Map, (int)EntityPosition.X, (int)EntityPosition.Y, 0.8f));

                    return;
                }

                // continue with the idle animation and don't start an attack
                _animator.Play("idle");
            }
        }
    }
}
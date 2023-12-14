using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    internal class AnglerFishStone : GameObject
    {
        private readonly BodyComponent _body;
        private readonly CPosition _position;

        public AnglerFishStone(Map.Map map, int posX, int posY) : base(map)
        {
            _position = new CPosition(posX, posY, 0);

            var animator = AnimatorSaveLoad.LoadAnimator("nightmares/anger fish stone");
            animator.Play("run");

            _body = new BodyComponent(_position, -8, -14, 16, 14, 8)
            {
                CollisionTypes = Values.CollisionTypes.None
            };

            var sprite = new CSprite(_position);
            var animationComponent = new AnimationComponent(animator, sprite, new Vector2(-8, -14));

            var damageBox = new CBox(_position, -6, -12, 12, 12, 8);
            var hittableBox = new CBox(_position, -8, -14, 16, 14, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 6));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
        }

        private void Update()
        {
            // despawn the stone
            if (_position.Y + 6 > Map.MapHeight * 16)
                Map.Objects.DeleteObjects.Add(this);
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            return Values.HitCollision.RepellingParticle;
        }
    }
}
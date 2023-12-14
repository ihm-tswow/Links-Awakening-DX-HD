using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyMegaThwomp : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly BodyComponent _body;

        public EnemyMegaThwomp() : base("mega thwomp") { }

        public EnemyMegaThwomp(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, -1, 32, 32);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/mega thwomp");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(0, 0));

            _body = new BodyComponent(EntityPosition, 0, -1, 32, 32, 8)
            {
                MoveCollision = OnCollision,
                Gravity2D = 0.15f,
                IsActive = false
            };

            var stateIdle = new AiState();
            // short delay before starting to fall down
            var statePreFalling = new AiState();
            statePreFalling.Trigger.Add(new AiTriggerCountdown(500, null, ToFalling));
            var stateFalling = new AiState();
            var stateFallen = new AiState();

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("preFalling", statePreFalling);
            _aiComponent.States.Add("falling", stateFalling);
            _aiComponent.States.Add("fallen", stateFallen);
            _aiComponent.ChangeState("idle");

            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
        }

        private void ToPreFalling()
        {
            _aiComponent.ChangeState("preFalling");
            _animator.Play("hit");
        }

        private void ToFalling()
        {
            _aiComponent.ChangeState("falling");
            _body.IsActive = true;
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (type == HitType.PegasusBootsPush && _aiComponent.CurrentStateId == "idle")
                ToPreFalling();

            return Values.HitCollision.None;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if ((collision & Values.BodyCollision.Bottom) != 0)
            {
                _body.IsActive = false;
                Game1.GameManager.ShakeScreen(750, 1, 2, 2.5f, 6.5f);
                Game1.GameManager.PlaySoundEffect("D378-12-0C");
            }
        }
    }
}
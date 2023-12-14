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
    internal class EnemySeaUrchin : GameObject
    {
        private readonly BodyComponent _body;

        private readonly float _moveSpeed = 0.25f;
        private readonly int _collisionDamage = 2;

        private Vector2 _lastPosition;

        private float _soundCounter;

        public EnemySeaUrchin() : base("sea urchin") { }

        public EnemySeaUrchin(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _body = new BodyComponent(EntityPosition, -8, -14, 16, 14, 8)
            {
                Bounciness = 0.25f,
                Drag = 0.85f,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Player
            };

            var sprite = new CSprite(EntityPosition);
            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/sea urchin");
            animator.Play("idle");

            // randomize the start frame
            animator.SetFrame(Game1.RandomNumber.Next(0, animator.CurrentAnimation.Frames.Length));

            var animatorComponent = new AnimationComponent(animator, sprite, new Vector2(-8, -16));

            var aiComponent = new AiComponent();
            aiComponent.States.Add("idle", new AiState());
            var damageState = new AiDamageState(this, _body, aiComponent, sprite, 1) { OnBurn = () => animator.Pause() };
            aiComponent.ChangeState("idle");

            var hittableBox = new CBox(EntityPosition, -8, -16, 0, 16, 16, 8, true);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(AiComponent.Index, aiComponent);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Enemy));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush) { CooldownTime = 0 });
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite) { Height = 1.0f, Rotation = 0.1f });
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type != PushableComponent.PushType.Continues)
                return false;

            // push the enemy away if the player is holding a shield in the push direction
            if (MapManager.ObjLink.CurrentState == ObjLink.State.Blocking &&
                AnimationHelper.GetDirection(direction) == MapManager.ObjLink.Direction)
            {
                _body.Velocity = new Vector3(direction.X, direction.Y, 0) * _moveSpeed;

                // play sound effect
                if (_lastPosition != EntityPosition.Position)
                {
                    _soundCounter -= Game1.DeltaTime;
                    if (_soundCounter < 0)
                    {
                        Game1.GameManager.PlaySoundEffect("D360-62-3E", false);
                        _soundCounter += 75;
                    }
                }

                _lastPosition = EntityPosition.Position;

                return true;
            }

            MapManager.ObjLink.HitPlayer(-direction, HitType.Enemy, _collisionDamage, true);
            return false;
        }
    }
}
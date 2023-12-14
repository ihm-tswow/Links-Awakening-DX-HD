using System;
using Microsoft.Xna.Framework;
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
    internal class EnemyBomber : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;

        private Vector2 _startPosition;

        private float _flyHeight = 14;

        public EnemyBomber() : base("bomber") { }

        public EnemyBomber(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, _flyHeight);
            EntitySize = new Rectangle(-12, -32, 24, 32);

            _startPosition = EntityPosition.Position;

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/bomber");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-12, -16));

            _body = new BodyComponent(EntityPosition, -8, -12, 16, 12, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                DragAir = 0.975f,
                Gravity = -0.175f,
                IgnoreHoles = true,
                IgnoresZ = true,
            };

            var stateWaiting = new AiState() { Init = InitWaiting };
            stateWaiting.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("moving"), 500, 1000));
            var stateMoving = new AiState() { Init = InitMoving };
            stateMoving.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("waiting"), 500, 1000));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("moving", stateMoving);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 2) { OnBurn = OnBurn };

            _aiComponent.ChangeState("waiting");

            var hittableBox = new CBox(EntityPosition, -7, -12, 0, 14, 12, 8, true);
            var damageBox = new CBox(EntityPosition, -7, -12, 0, 14, 12, 4, true);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite) { ShadowWidth = 12, ShadowHeight = 4 });
        }

        private void OnBurn()
        {
            _animator.Pause();
            _body.IgnoresZ = false;
            _body.DragAir = 0.9f;
            _body.Bounciness = 0.5f;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // can only be attacked with the sword while holding it
            if ((damageType & HitType.Sword) != 0 && (damageType & HitType.SwordHold) == 0 && (damageType & HitType.SwordSpin) == 0)
            {
                _body.Velocity.X = direction.X * 5;
                _body.Velocity.Y = direction.Y * 5;

                return Values.HitCollision.None;
            }

            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private void InitWaiting()
        {
            _body.VelocityTarget = Vector2.Zero;

            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var distance = playerDistance.Length();

            // bomb
            if (distance < 80 && Game1.RandomNumber.Next(0, 4) != 4)
            {
                Vector2 throwDirection;

                if (distance < 64)
                {
                    // throw towards the player
                    if (playerDistance != Vector2.Zero)
                        playerDistance.Normalize();
                    throwDirection = playerDistance * (distance / 64) * 1.0f;
                }
                else
                {
                    // throw into a random direction
                    var randomRadius = Game1.RandomNumber.Next(0, 620) / 100;
                    throwDirection = new Vector2((float)Math.Sin(randomRadius), (float)Math.Cos(randomRadius)) * 0.75f;
                }

                // spawn a bomb
                var bomb = new ObjBomb(Map, 0, 0, false, true);
                bomb.EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y, 20));
                bomb.Body.Velocity = new Vector3(throwDirection, 0);
                bomb.Body.CollisionTypes = Values.CollisionTypes.None;
                bomb.Body.Gravity = -0.1f;
                bomb.Body.DragAir = 1.0f;
                bomb.Body.Bounciness = 0.5f;
                Map.Objects.SpawnObject(bomb);
            }
        }

        private void InitMoving()
        {
            // the farther away the enemy is from the origin the more likely it becomes that he will move towards the start position
            var directionToStart = _startPosition - EntityPosition.Position;
            var radiusToStart = Math.Atan2(directionToStart.Y, directionToStart.X);

            var maxDistance = 80.0f;
            var randomDir = radiusToStart + (Math.PI - Game1.RandomNumber.Next(0, 628) / 100f) *
                Math.Clamp(((maxDistance - directionToStart.Length()) / maxDistance), 0, 1);

            _body.VelocityTarget = new Vector2((float)Math.Cos(randomDir), (float)Math.Sin(randomDir)) * 0.5f;
        }
    }
}
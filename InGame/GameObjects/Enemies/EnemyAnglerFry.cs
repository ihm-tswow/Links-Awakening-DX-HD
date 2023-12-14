using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyAnglerFry : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiDamageState _damageState;
        private readonly AiComponent _aiComponent;
        private readonly CSprite _sprite;

        private const float MovementSpeed = 0.5f;
        private const int SpawnTime = 100;

        private float _swimCounter;

        public EnemyAnglerFry() : base("anglerFry") { }

        public EnemyAnglerFry(Map.Map map, int posX, int posY, int dir) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/angler fry");
            animator.Play("move_" + dir);

            _sprite = new CSprite(EntityPosition);
            _sprite.Color = Color.Transparent;
            var animationComponent = new AnimationComponent(animator, _sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -8, -16, 16, 16, 8)
            {
                CollisionTypes = Values.CollisionTypes.None,
                DragAir = 0.8f,
                DragWater = 0.8f,
                IgnoresZ = true,
                SplashEffect = false
            };

            var triggerCount = new AiTriggerCountdown(SpawnTime, TickSpawn, () => TickSpawn(1));
            var stateMoving = new AiState(UpdateMoving);
            var stateDespawning = new AiState();
            stateDespawning.Trigger.Add(new AiTriggerCountdown(SpawnTime, TickDespawn, () => TickDespawn(1)));

            _aiComponent = new AiComponent();
            _aiComponent.Trigger.Add(triggerCount);
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("despawning", stateDespawning);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 1)
            { SpawnItems = false, HitMultiplierX = 6, HitMultiplierY = 6 };
            _aiComponent.ChangeState("moving");

            var hittableBox = new CBox(EntityPosition, -8, -14, 0, 16, 14, 8);
            var damageBox = new CBox(EntityPosition, -7, -12, 0, 14, 12, 4);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));

            triggerCount.OnInit();
            _body.VelocityTarget.X = dir * MovementSpeed;
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            if (_damageState.OnHit(originObject, direction, type, damage, pieceOfPower) != Values.HitCollision.None)
            {
                _body.UpdateFieldState = false;
                _body.VelocityTarget = Vector2.Zero;
            }

            return Values.HitCollision.Repelling;
        }

        private void TickSpawn(double time)
        {
            _sprite.Color = Color.White * (float)((SpawnTime - time) / SpawnTime);
        }

        private void UpdateMoving()
        {
            // start despawning
            if (EntityPosition.X < -8 || Map.MapWidth * 16 + 8 < EntityPosition.X)
                _aiComponent.ChangeState("despawning");

            _swimCounter += Game1.DeltaTime;
            _body.VelocityTarget.Y = MathF.Sin(_swimCounter / 300f) * 0.25f;
        }

        private void TickDespawn(double time)
        {
            _sprite.Color = Color.White * (float)(time / SpawnTime);
            if (time <= 0)
                Map.Objects.DeleteObjects.Add(this);
        }
    }
}
using System;
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
    internal class EnemyKeese : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;

        private readonly float _turnSpeed;

        private float _flyState;
        private int _dir;

        public EnemyKeese() : base("keese") { }

        public EnemyKeese(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            _turnSpeed = Game1.RandomNumber.Next(20, 30) / 1000f;
            _flyState = (float)Math.PI / 2 * Game1.RandomNumber.Next(0, 4);
            _dir = Game1.RandomNumber.Next(0, 2) * 2 - 1;

            EntityPosition = new CPosition(posX + 8, posY + 24, 0);
            EntitySize = new Rectangle(-8, -24, 16, 24);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/keese");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animatorComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -21));

            var fieldRectangle = map.GetField(posX, posY, 8);

            _body = new BodyComponent(EntityPosition, -6, -20, 12, 8, 8)
            {
                IgnoresZ = true,
                IgnoreHoles = true,
                CollisionTypes = Values.CollisionTypes.None,
                MoveCollision = OnCollision,
                FieldRectangle = fieldRectangle
            };

            _aiComponent = new AiComponent();

            var stateIdle = new AiState(UpdateIdle);
            var stateCooldown = new AiState();
            stateCooldown.Trigger.Add(new AiTriggerRandomTime(StartIdle, 1000, 2000));
            var stateFlying = new AiState(UpdateFlying);
            stateFlying.Trigger.Add(new AiTriggerRandomTime(StartCooldown, 1000, 2000));

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("cooldown", stateCooldown);
            _aiComponent.States.Add("flying", stateFlying);
            new AiFallState(_aiComponent, _body, null, null, 0);
            var damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1) { OnBurn = OnBurn };

            _aiComponent.ChangeState("cooldown");

            var damageCollider = new CBox(EntityPosition, -5, -20, 0, 10, 8, 4);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, damageState.OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer) { WaterOutline = false });
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void OnBurn()
        {
            _body.IgnoresZ = false;
            _body.IgnoreHoles = false;
        }

        private void StartIdle()
        {
            _aiComponent.ChangeState("idle");
        }

        private void UpdateIdle()
        {
            var distVec = EntityPosition.Position - new Vector2(MapManager.ObjLink.PosX, MapManager.ObjLink.PosY + 16);

            // start flying if the player is near the keese
            if (distVec.Length() < 60)
                StartFlying();
        }

        private void StartFlying()
        {
            _aiComponent.ChangeState("flying");
            _dir = Game1.RandomNumber.Next(0, 2) * 2 - 1;
        }

        private void UpdateFlying()
        {
            _animator.Play("fly");

            _flyState += _dir * _turnSpeed * Game1.TimeMultiplier;
            var vecDirection = new Vector2((float)Math.Sin(_flyState), (float)Math.Cos(_flyState));

            _body.VelocityTarget = vecDirection * 0.75f;
        }

        private void StartCooldown()
        {
            _aiComponent.ChangeState("cooldown");
            _animator.Play("idle");
            _body.VelocityTarget = Vector2.Zero;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            _flyState += (float)Math.PI;
        }
    }
}
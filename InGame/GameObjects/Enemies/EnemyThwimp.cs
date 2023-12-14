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
    internal class EnemyThwimp : GameObject
    {
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly BodyComponent _body;

        private const float ResturnSpeed = 0.5f;

        public EnemyThwimp() : base("thwimp") { }

        public EnemyThwimp(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Damage;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -64, 16, 114);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/thwimp");
            _animator.Play("idle");

            _body = new BodyComponent(EntityPosition, -7, -16, 14, 16, 8)
            {
                MoveCollision = OnCollision,
                Drag = 0.8f,
                IgnoresZ = true,
                Gravity2D = 0.165f
            };

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            var stateIdle = new AiState(UpdateIdle);
            var stateFall = new AiState { Init = InitFall };
            var stateWait = new AiState { Init = InitWait };
            stateWait.Trigger.Add(new AiTriggerCountdown(700, null, () => _aiComponent.ChangeState("return")));
            var stateReturn = new AiState { Init = InitReturn };
            var stateReturned = new AiState { Init = InitReturned };
            stateReturned.Trigger.Add(new AiTriggerCountdown(550, null, () => _aiComponent.ChangeState("idle")));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("fall", stateFall);
            _aiComponent.States.Add("wait", stateWait);
            _aiComponent.States.Add("return", stateReturn);
            _aiComponent.States.Add("returned", stateReturned);
            _aiComponent.ChangeState("idle");

            var damageBox = new CBox(EntityPosition, -5, -10, 0, 10, 8, 4);
            var hittableBox = new CBox(EntityPosition, -5, -10, 0, 10, 8, 8);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
        }

        private void UpdateIdle()
        {
            // trigger trap
            var distance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            var distanceH = Math.Abs(distance.X);

            var angry = distanceH < 40;
            _animator.Play(angry ? "angry" : "idle");

            if (distanceH < 22)
            {
                _aiComponent.ChangeState("fall");
            }
        }

        private void InitFall()
        {
            // start falling down
            _body.IgnoresZ = false;
            _animator.Play("angry");

            Game1.GameManager.PlaySoundEffect("D360-08-08");
        }

        private void InitWait()
        {
            _animator.Play("idle");
         
            Game1.GameManager.PlaySoundEffect("D360-09-09");
            Game1.GameManager.ShakeScreen(75, 0, 1, 0, 500);
        }

        private void InitReturn()
        {
            _body.IgnoresZ = true;
            _body.VelocityTarget.Y = -ResturnSpeed;
        }

        private void InitReturned()
        {
            _body.VelocityTarget.Y = 0;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if ((collision & Values.BodyCollision.Bottom) != 0 && _aiComponent.CurrentStateId == "fall")
                _aiComponent.ChangeState("wait");
            else if ((collision & Values.BodyCollision.Top) != 0 && _aiComponent.CurrentStateId == "return")
                _aiComponent.ChangeState("returned");
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            return Values.HitCollision.RepellingParticle;
        }
    }
}
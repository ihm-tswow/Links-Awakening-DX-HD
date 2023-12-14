using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.GameObjects.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyBuzzBlob : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly AiStunnedState _sunnedState;

        private readonly float _moveSpeed = 0.33f;
        private const int ShockTime = 550;
        private bool _isCukeman;

        public EnemyBuzzBlob() : base("buzz blob") { }

        public EnemyBuzzBlob(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-10, -16, 20, 20);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/buzzblob");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-6, -16));

            var fieldRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -4, -10, 8, 10, 8)
            {
                AvoidTypes = Values.CollisionTypes.Hole |
                             Values.CollisionTypes.NPCWall,
                FieldRectangle = fieldRectangle
            };

            var stateWalking = new AiState() { Init = InitWalking };
            stateWalking.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walking"), 500, 1000));
            var stateShocking = new AiState(UpdateShocking);
            stateShocking.Trigger.Add(new AiTriggerCountdown(ShockTime, null, () => _aiComponent.ChangeState("postShock")));
            var statePostShock = new AiState() { Init = InitPostShock };
            statePostShock.Trigger.Add(new AiTriggerCountdown(350, null, () => _aiComponent.ChangeState("walking")));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("walking", stateWalking);
            _aiComponent.States.Add("shocking", stateShocking);
            _aiComponent.States.Add("postShock", statePostShock);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 4, false) { OnDeath = OnDeath, OnBurn = () => _animator.Pause() };
            _sunnedState = new AiStunnedState(_aiComponent, animationComponent, 3300, 900) { ShakeOffset = 1, SilentStateChange = false, ReturnState = "walking" };
            new AiFallState(_aiComponent, _body, OnHolePull, OnHoleDeath, 400);

            _aiComponent.ChangeState("walking");

            var interactionBox = new CBox(EntityPosition, -10, -16, 20, 20, 8);
            var hittableBox = new CBox(EntityPosition, -6, -14, 12, 14, 8);
            var damageBox = new CBox(EntityPosition, -5, -12, 0, 10, 12, 4);
            var pushableBox = new CBox(EntityPosition, -4, -11, 0, 8, 11, 4);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(InteractComponent.Index, new InteractComponent(interactionBox, OnInteract));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void OnDeath(bool pieceOfPower)
        {
            Game1.GameManager.UseShockEffect = false;

            _damageState.BaseOnDeath(pieceOfPower);
        }

        private void UpdateShocking()
        {
            MapManager.ObjLink.CanWalk = false;
        }

        private void InitPostShock()
        {
            Game1.GameManager.UseShockEffect = false;
        }

        private void InitWalking()
        {
            _animator.Play(_isCukeman ? "cukeman" : "walk");

            // new random direction
            var directionIndex = Game1.RandomNumber.Next(0, 8);
            var radius = directionIndex / 4.0 * Math.PI;
            _body.VelocityTarget = new Vector2((float)Math.Sin(radius), (float)Math.Cos(radius)) * _moveSpeed;
        }

        private void OnHolePull()
        {
            _animator.Play("walk");
            _animator.SpeedMultiplier = 2.0f;
        }

        private void OnHoleDeath()
        {
            Game1.GameManager.UseShockEffect = false;
        }

        private bool OnInteract()
        {
            if (!_isCukeman)
                return false;

            Game1.GameManager.StartDialogPath("cukeman");

            return true;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z) * 2f;

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState())
                return Values.HitCollision.None;

            if ((damageType & HitType.Hookshot) != 0)
            {
                _animator.Pause();
                _sunnedState.StartStun();

                _body.Velocity.X = direction.X * 5;
                _body.Velocity.Y = direction.Y * 5;
                _body.VelocityTarget = Vector2.Zero;

                return Values.HitCollision.Enemy;
            }
            else if (damageType == HitType.MagicPowder)
            {
                _isCukeman = true;
                _animator.Play("cukeman");
                Game1.GameManager.PlaySoundEffect("D360-03-03");

                // spawn explosion effect
                ObjAnimator animator;
                Map.Objects.SpawnObject(animator = new ObjAnimator(Map, 0, 0, Values.LayerBottom, "Particles/spawn", "run", true));
                animator.EntityPosition.Set(new Vector2(EntityPosition.X - 8, EntityPosition.Y - 16));

                return Values.HitCollision.Enemy;
            }
            else if (!_sunnedState.IsStunned() && ((damageType & HitType.Sword) != 0 || damageType == HitType.PegasusBootsSword))
            {
                StartShock();

                return Values.HitCollision.Enemy;
            }

            if ((damageType & HitType.Sword) == 0 && damageType != HitType.PegasusBootsSword && damageType != HitType.SwordShot)
                damage *= 2;

            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private void StartShock()
        {
            if (_aiComponent.CurrentStateId == "shocking")
                return;

            MapManager.ObjLink.ShockPlayer(ShockTime);
            Game1.GameManager.PlaySoundEffect("D378-28-1C");

            _body.VelocityTarget = Vector2.Zero;
            _aiComponent.ChangeState("shocking");
            _animator.Play("shock");
        }
    }
}
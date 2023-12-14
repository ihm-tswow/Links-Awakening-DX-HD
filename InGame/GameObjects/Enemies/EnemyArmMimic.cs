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
    internal class EnemyArmMimic : GameObject
    {
        private readonly BodyComponent _body;
        private readonly Animator _animator;
        private readonly AiDamageState _aiDamageState;
        private readonly AiTriggerTimer _repelTimer;
        private readonly AiStunnedState _aiStunnedState;

        private Vector2 _lastPosition;
        private int _direction;
        private bool _wasColliding;

        public EnemyArmMimic() : base("armMimic") { }

        public EnemyArmMimic(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/arm mimic");

            var sprite = new CSprite(EntityPosition);
            var animatorComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -5, -10, 10, 10, 8)
            {
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                IsSlider = true,
                MaxSlideDistance = 4.0f
            };

            var stateUpdate = new AiState(Update);

            var aiComponent = new AiComponent();
            aiComponent.Trigger.Add(_repelTimer = new AiTriggerTimer(500));

            aiComponent.States.Add("idle", stateUpdate);
            new AiFallState(aiComponent, _body, null, null, 300);
            _aiDamageState = new AiDamageState(this, _body, aiComponent, sprite, 2);
            _aiStunnedState = new AiStunnedState(aiComponent, animatorComponent, 3300, 900);

            aiComponent.ChangeState("idle");

            var hittableBox = new CBox(EntityPosition, -6, -15, 2, 12, 15, 8);
            var damageBox = new CBox(EntityPosition, -6, -12, 2, 12, 12, 4);
            var pushableBox = new CBox(EntityPosition, -5, -14, 2, 10, 14, 4);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(hittableBox, HitType.Enemy, 12));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(AiComponent.Index, aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animatorComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        private void Update()
        {
            var moved = false;
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            // move when near the player
            if (playerDistance.Length() < 80)
            {
                if (_wasColliding)
                {
                    var direction = -MapManager.ObjLink.LastMoveVector;
                    var diff = (MapManager.ObjLink.EntityPosition.Position - _lastPosition) / Game1.TimeMultiplier;

                    // this will stop the enemy if the player is walking into an obstacle
                    direction = new Vector2(
                        Math.Min(Math.Abs(direction.X), Math.Abs(diff.X)) * Math.Sign(direction.X),
                        Math.Min(Math.Abs(direction.Y), Math.Abs(diff.Y)) * Math.Sign(direction.Y));

                    _body.VelocityTarget = direction * 0.75f;

                    if (direction.Length() > 0.01f)
                    {
                        moved = true;
                        _direction = AnimationHelper.GetDirection(direction);

                        if (_animator.CurrentAnimation.Id != "walk_" + _direction)
                            _animator.Play("walk_" + _direction);
                        else
                            _animator.Continue();
                    }
                }

                _wasColliding = true;
                _lastPosition = MapManager.ObjLink.EntityPosition.Position;
            }
            else
            {
                _wasColliding = false;
                _body.VelocityTarget = Vector2.Zero;
            }

            if (!moved)
                _animator.Pause();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!_repelTimer.State)
                return Values.HitCollision.None;
            _repelTimer.Reset();

            // stun state
            if (damageType == HitType.Hookshot || damageType == HitType.Boomerang)
            {
                _body.VelocityTarget = Vector2.Zero;
                _body.Velocity.X += direction.X * 4.0f;
                _body.Velocity.Y += direction.Y * 4.0f;

                _aiStunnedState.StartStun();
                _animator.Pause();

                return Values.HitCollision.Enemy;
            }

            // damaged not from the front; piece of power or while using pegasus boots
            if (damageType != HitType.PegasusBootsSword && damageType != HitType.SwordShot && (damageType & HitType.SwordSpin) == 0 && !pieceOfPower)
                damage = 0;

            return _aiDamageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }
    }
}
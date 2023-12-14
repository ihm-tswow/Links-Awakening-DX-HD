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
    internal class EnemyMaskMimic : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AnimationComponent _animatorComponent;
        private readonly AiDamageState _aiDamageState;
        private readonly AiStunnedState _aiStunnedState;

        private readonly Rectangle _fieldRectangle;

        private Vector2 _lastPosition;
        private int _direction;
        private bool _wasColliding;

        public EnemyMaskMimic() : base("mask mimic") { }

        public EnemyMaskMimic(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/mask mimic");
            _animator.Play("walk");

            var sprite = new CSprite(EntityPosition);
            _animatorComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            _fieldRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8)
            {
                Gravity = -0.075f,
                DragAir = 1.0f,
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                FieldRectangle = _fieldRectangle,
                IsSlider = true,
                MaxSlideDistance = 4.0f
            };

            _aiComponent = new AiComponent();

            var stateUpdate = new AiState(Update);

            _aiComponent.States.Add("idle", stateUpdate);
            _aiStunnedState = new AiStunnedState(_aiComponent, _animatorComponent, 3300, 900);
            new AiFallState(_aiComponent, _body, null, null, 300);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, sprite, 2);
            _aiComponent.ChangeState("idle");

            var damageBox = new CBox(EntityPosition, -7, -15, 2, 14, 15, 4);
            var hittableBox = new CBox(EntityPosition, -7, -15, 2, 14, 15, 8);
            var pushableBox = new CBox(EntityPosition, -7, -14, 2, 14, 14, 8);

            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, _animatorComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));
        }

        private void Update()
        {
            var moved = false;
            if (_fieldRectangle.Contains(MapManager.ObjLink.EntityPosition.Position))
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

                        // deadzone to not have a fixed point where the direction gets changed
                        if (Math.Abs(direction.X) * ((_direction % 2 == 0) ? 1.1f : 1f) >
                            Math.Abs(direction.Y) * ((_direction % 2 != 0) ? 1.1f : 1f))
                            _direction = direction.X < 0 ? 0 : 2;
                        else
                            _direction = direction.Y < 0 ? 1 : 3;

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
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (damageType == HitType.MagicPowder)
                return Values.HitCollision.None;

            if (damageType == HitType.Bow)
                damage = 1;

            if (damageType == HitType.Hookshot || damageType == HitType.Boomerang)
            {
                _aiStunnedState.StartStun();

                _body.VelocityTarget = Vector2.Zero;

                _body.Velocity.X = direction.X * 5;
                _body.Velocity.Y = direction.Y * 5;

                return Values.HitCollision.Enemy;
            }

            // can be hit if the damage source is coming from the back
            var dir = AnimationHelper.GetDirection(direction);
            if (dir == _direction ||
                damageType == HitType.Bomb ||
                damageType == HitType.Bow ||
                damageType == HitType.MagicRod)
            {
                return _aiDamageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
            }

            return Values.HitCollision.RepellingParticle | Values.HitCollision.Repelling1;
        }
    }
}
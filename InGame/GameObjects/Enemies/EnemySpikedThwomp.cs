using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.Systems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class EnemySpikedThwomp : GameObject
    {
        private readonly List<GameObject> _collidingObjects = new List<GameObject>();

        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly BodyComponent _body;

        private readonly CBox _collisionBox;

        private readonly Vector2 _startPosition;

        private Box _lastCollisionBox;

        public EnemySpikedThwomp() : base("spiked thwomp") { }

        public EnemySpikedThwomp(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 16, posY + 16, 0);
            EntitySize = new Rectangle(-16, -16, 32, 32);

            _startPosition = EntityPosition.Position;

            _body = new BodyComponent(EntityPosition, -16, -16, 32, 31, 8)
            {
                Gravity2D = 0.175f,
                IgnoresZ = true,
                CollisionTypes =
                    Values.CollisionTypes.Normal | Values.CollisionTypes.NPCWall,
                MoveCollision = OnCollision
            };

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/spiked thwomp");
            _animator.Play("attack");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-16, -16));

            var stateIdle = new AiState(UpdateIdle);
            var stateAttack = new AiState();
            var stateAttackCooldown = new AiState();
            stateAttackCooldown.Trigger.Add(new AiTriggerCountdown(1000, null, ToGoingUp)); // 800 closer to the actual value
            var stateGoingUp = new AiState(UpdateGoingUp);
            var stateUpWaiting = new AiState();
            stateUpWaiting.Trigger.Add(new AiTriggerCountdown(600, null, ToIdle));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("attack", stateAttack);
            _aiComponent.States.Add("attackCooldown", stateAttackCooldown);
            _aiComponent.States.Add("goingUp", stateGoingUp);
            _aiComponent.States.Add("upWaiting", stateUpWaiting);

            _aiComponent.ChangeState("idle");

            var damageBox = new CBox(EntityPosition, -14, -16, 28, 32, 4);
            var hittableBox = new CBox(EntityPosition, -14, -16, 28, 32, 8);
            _collisionBox = new CBox(EntityPosition, -16, -16, 32, 4, 8);

            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(_collisionBox, Values.CollisionTypes.Enemy));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            return Values.HitCollision.RepellingParticle;
        }

        private void ToIdle()
        {
            _aiComponent.ChangeState("idle");
        }

        private void UpdateIdle()
        {
            // look at the player
            var lookDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;

            var radiant = MathF.Atan2(lookDirection.X, -lookDirection.Y);

            var clampedValue = Math.Clamp((Math.Abs(radiant) - 0.75f) / (MathF.PI - 1.25f), 0, 2);
            var animationDir = (int)(clampedValue * 3) + 1;

            if (animationDir <= 3)
                _animator.Play((radiant < 0 ? "l" : "r") + animationDir);
            else
                _animator.Play("down");

            // start attacking?
            if (Math.Abs(lookDirection.X) < 22)
            {
                ToAttacking();
            }
        }

        private void ToAttacking()
        {
            _aiComponent.ChangeState("attack");
            _animator.Play("attack");
            _body.IgnoresZ = false;
            Game1.GameManager.PlaySoundEffect("D360-08-08");
        }

        private void ToGoingUp()
        {
            _aiComponent.ChangeState("goingUp");
            _body.IgnoresZ = true;
            _body.Velocity.Y = 0;
        }

        private void UpdateGoingUp()
        {
            _lastCollisionBox = _collisionBox.Box;

            EntityPosition.Move(new Vector2(0, -0.5f));
            if (EntityPosition.Y < _startPosition.Y)
            {
                EntityPosition.Set(new Vector2(EntityPosition.X, _startPosition.Y));
                _aiComponent.ChangeState("upWaiting");
            }

            MoveBodies();
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if ((collision & Values.BodyCollision.Bottom) != 0 &&
                _aiComponent.CurrentStateId == "attack")
            {
                HitGround();
            }
        }

        private void HitGround()
        {
            // shake the screen
            Game1.GameManager.ShakeScreen(750, 0, 2, 2f, 5.5f);

            Game1.GameManager.PlaySoundEffect("D378-12-0C");

            _aiComponent.ChangeState("attackCooldown");
        }

        private void MoveBodies()
        {
            // check for colliding bodies and push them forward
            _collidingObjects.Clear();
            Map.Objects.GetComponentList(_collidingObjects,
                (int)_lastCollisionBox.Left, (int)_lastCollisionBox.Back - 8,
                (int)_lastCollisionBox.Width, (int)_lastCollisionBox.Height, BodyComponent.Mask);

            foreach (var collidingObject in _collidingObjects)
            {
                var body = (BodyComponent)collidingObject.Components[BodyComponent.Index];

                // the enemy move into the body that was on top of him
                if (body.BodyBox.Box.Front <= _lastCollisionBox.Back &&
                    body.BodyBox.Box.Front >= _collisionBox.Box.Back &&
                    body.BodyBox.Box.Intersects(_collisionBox.Box))
                {
                    // move the body up
                    var offset = new Vector2(0, _collisionBox.Box.Back - body.BodyBox.Box.Front - 0.001f);
                    SystemBody.MoveBody(body, offset, body.CollisionTypes, false, false, false);
                    body.Position.NotifyListeners();
                }
            }
        }
    }
}

using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjBird : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly AiTriggerSwitch _changeDirectionSwitch;
        private readonly DamageFieldComponent _damageField;
        private readonly Animator _animator;
        private readonly CSprite _sprite;

        private int _direction;

        private float _attackCounter;
        private float _attackingCounter;
        private float _attackTransparency;
        private bool _attackMode;
        private int _hitCounter;

        public ObjBird() : base("bird") { }

        public ObjBird(Map.Map map, int posX, int posY) : base(map)
        {
            var rectangle = new Rectangle(0, 0, 14, 8);
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -32, 16, 32);

            _body = new BodyComponent(EntityPosition, -6, -8, 12, 8, 8)
            {
                MoveCollision = OnCollision,
                Bounciness = 0.25f,
                Drag = 0.9f,
                Gravity = -0.1f,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Player |
                    Values.CollisionTypes.NPCWall
            };

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/bird");
            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -15));

            var stateIdle = new AiState() { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walking"), 500, 1500));
            var stateWalking = new AiState(UpdateWalking) { Init = InitWalk };
            stateWalking.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("idle"), 750, 1500));
            stateWalking.Trigger.Add(_changeDirectionSwitch = new AiTriggerSwitch(250));
            var stateFleeIdle = new AiState(UpdateFleeState) { Init = InitIdle };
            stateFleeIdle.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("fleeingWalking"), 500, 1500));
            var stateFleeWalking = new AiState(UpdateFleeState) { Init = InitWalk };
            stateFleeWalking.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("fleeingIdle"), 750, 1500));
            var stateFleeing = new AiState(UpdateFleeing);
            var stateAttack = new AiState(UpdateAttack);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("walking", stateWalking);
            _aiComponent.States.Add("fleeingIdle", stateFleeIdle);
            _aiComponent.States.Add("fleeingWalking", stateFleeWalking);
            _aiComponent.States.Add("fleeing", stateFleeing);
            _aiComponent.States.Add("attack", stateAttack);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 2);
            _aiComponent.ChangeState(Game1.RandomNumber.Next(0, 10) < 5 ? "idle" : "walking");

            var box = new CBox(EntityPosition, -6, -12, 0, 12, 12, 8, true);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(box, HitType.Enemy, 2) { IsActive = false });
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
        }

        public void InitAttackMode()
        {
            _aiComponent.ChangeState("attack");

            // spawn around the player
            var radiant = Game1.RandomNumber.Next(0, 628) / 100f;
            var offset = new Vector2(MathF.Sin(radiant), MathF.Cos(radiant));

            var spawnPosition = MapManager.ObjLink.EntityPosition.Position + new Vector2(0, 24) + offset * 80;
            EntityPosition.Set(spawnPosition);
            EntityPosition.Z = 16;

            _damageField.IsActive = true;

            var direction = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.Position.X, EntityPosition.Position.Y - 16);
            if (direction != Vector2.Zero)
                direction.Normalize();
            _body.VelocityTarget = direction * 1.5f;

            _body.IgnoresZ = true;
            _body.CollisionTypes = Values.CollisionTypes.None;

            _direction = _body.VelocityTarget.X < 0 ? 0 : 1;
            _animator.Play("walk_" + _direction);
            _animator.SpeedMultiplier = 2;
        }

        private void UpdateAttack()
        {
            _attackingCounter += Game1.DeltaTime;

            var target = _attackingCounter < 2000 ? 1 : 0;
            _attackTransparency = AnimationHelper.MoveToTarget(_attackTransparency, target, 0.1f * Game1.TimeMultiplier);
            _sprite.Color = Color.White * _attackTransparency;

            Game1.GameManager.PlaySoundEffect("D378-45-2D", false);

            if (_attackTransparency == 0)
                Map.Objects.DeleteObjects.Add(this);
        }

        private void SpawnBirds()
        {
            if (!_attackMode)
                return;

            var playerDir = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDir.Length() > 120)
            {
                _attackMode = false;

                // change back into the normal mode
                if (_aiComponent.CurrentStateId != "idle" &&
                    _aiComponent.CurrentStateId != "walking")
                    _aiComponent.ChangeState("idle");
            }

            Game1.GameManager.PlaySoundEffect("D370-19-13", false);

            _attackCounter -= Game1.DeltaTime;
            if (_attackCounter < 0)
            {
                _attackCounter += Game1.RandomNumber.Next(300, 550);

                var objBird = new ObjBird(Map, (int)EntityPosition.X, (int)EntityPosition.Y);
                objBird.InitAttackMode();
                Map.Objects.SpawnObject(objBird);
            }
        }

        private void InitIdle()
        {
            // stop and wait
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("idle_" + _direction);
        }

        private void InitWalk()
        {
            // change the direction
            var rotation = Game1.RandomNumber.Next(0, 628) / 100f;
            _body.VelocityTarget = new Vector2(
                                       (float)Math.Sin(rotation),
                                       (float)Math.Cos(rotation)) * Game1.RandomNumber.Next(25, 40) / 100f;

            UpdateAnimation();
        }

        private void UpdateWalking()
        {
            // jump while walking
            if (_body.IsGrounded)
                _body.Velocity.Z = 0.65f;
        }

        private void UpdateFleeState()
        {
            SpawnBirds();

            // start fleeing from the player
            var distance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            if (distance.Length() < 32)
                _aiComponent.ChangeState("fleeing");
        }

        private void UpdateFleeing()
        {
            SpawnBirds();

            // flee from the player
            var playerDir = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;

            // stop fleeing
            if (playerDir.Length() > 48)
            {
                _aiComponent.ChangeState("fleeingIdle");
                return;
            }

            if (playerDir != Vector2.Zero)
                playerDir.Normalize();
            _body.VelocityTarget = playerDir;

            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            _direction = _body.VelocityTarget.X < 0 ? 0 : 1;
            _animator.Play("walk_" + _direction);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_damageState.IsInDamageState())
                return Values.HitCollision.None;

            _damageState.SetDamageState();
            
            // start spawning other birds
            _hitCounter++;
            if (_hitCounter > 35)
                _attackMode = true;

            Game1.GameManager.PlaySoundEffect("D360-03-03");
            Game1.GameManager.PlaySoundEffect("D370-19-13");
            _aiComponent.ChangeState("fleeing");

            Game1.GameManager.StartDialogPath("bird_hit");

            _body.Velocity.X = direction.X * 3.5f;
            _body.Velocity.Y = direction.Y * 3.5f;

            return Values.HitCollision.Blocking;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Continues)
            {
                // push the bird away
                _body.Velocity = new Vector3(direction.X, direction.Y, 0) * 0.65f;

                // try to walk away from the pusher
                if (_aiComponent.CurrentStateId != "idle")
                    return true;

                _aiComponent.ChangeState("walking");

                var offsetAngle = MathHelper.ToRadians(Game1.RandomNumber.Next(45, 85) * (_direction * 2 - 1));
                var newDirection = new Vector2(
                                       direction.X * (float)Math.Cos(offsetAngle) -
                                       direction.Y * (float)Math.Sin(offsetAngle),
                                       direction.X * (float)Math.Sin(offsetAngle) +
                                       direction.Y * (float)Math.Cos(offsetAngle)) * 0.5f;
                _body.VelocityTarget = newDirection;

                UpdateAnimation();
            }
            else if (type == PushableComponent.PushType.Impact)
            {
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);
            }

            return true;
        }

        private void OnCollision(Values.BodyCollision moveCollision)
        {
            if ((moveCollision & Values.BodyCollision.Floor) != 0)
                return;

            // can only change the direction every so often
            if (!_changeDirectionSwitch.State)
                return;
            _changeDirectionSwitch.Reset();

            // rotate after wall collision
            if ((moveCollision & Values.BodyCollision.Horizontal) != 0)
                _body.VelocityTarget.X = -_body.VelocityTarget.X * 0.5f;
            else if ((moveCollision & Values.BodyCollision.Vertical) != 0)
                _body.VelocityTarget.Y = -_body.VelocityTarget.Y * 0.5f;

            UpdateAnimation();
        }
    }
}
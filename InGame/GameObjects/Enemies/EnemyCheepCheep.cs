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
    internal class EnemyCheepCheep : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly Animator _animator;

        private readonly CBox _damageBox;
        private readonly CBox _headJumpBox;

        private Vector2 _jumpStart;
        private const int JumpTime = 1500;

        private readonly float _movementSpeed;
        private int _dir;
        private readonly bool _canJump;

        public EnemyCheepCheep() : base("cheep cheep") { }

        public EnemyCheepCheep(Map.Map map, int posX, int posY, int dir, bool canJump) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _dir = dir;
            _canJump = canJump;

            _movementSpeed = canJump ? 0.75f : 0.5f;

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/cheep cheep");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -8, -12, 16, 10, 8)
            {
                MoveCollision = OnCollision,
                AvoidTypes = Values.CollisionTypes.NPCWall,
                CollisionTypes =
                    Values.CollisionTypes.Normal,
                FieldRectangle = map.GetField(posX, posY),
                DragAir = 0.8f,
                Gravity2DWater = 0f
            };

            var stateMoving = new AiState();
            if (_canJump)
            {
                stateMoving.Trigger.Add(new AiTriggerRandomTime(ToMoving, 350, 1250));
                stateMoving.Trigger.Add(new AiTriggerRandomTime(ToJump, 750, 1250));
            }

            var statePause = new AiState();
            statePause.Trigger.Add(new AiTriggerCountdown(700, null, ToMoving));
            var stateJumping = new AiState();
            stateJumping.Trigger.Add(new AiTriggerCountdown(JumpTime, UpdateJump, EndJump));
            var stateDead = new AiState(UpdateDeath);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("pause", statePause);
            _aiComponent.States.Add("jumping", stateJumping);
            _aiComponent.States.Add("dead", stateDead);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1)
            {
                HitMultiplierX = 3.0f,
                HitMultiplierY = 2.0f,
                FlameOffset = new Point(0, 2),
                OnBurn = () => _animator.Pause()
            };

            ToMoving();

            var hittableBox = new CBox(EntityPosition, -8, -14, 0, 16, 12, 8);
            _damageBox = new CBox(EntityPosition, -6, -14, 0, 12, 12, 4);
            _headJumpBox = new CBox(EntityPosition, -6, -16, 0, 12, 6, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(_damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            _body.IgnoresZ = false;

            return _damageState.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        public override void Init()
        {
            // look into the direction of the player
            if (_dir % 2 != 0 && MapManager.ObjLink.NextMapPositionStart != null)
                _animator.Play("idle_" + (EntityPosition.X > MapManager.ObjLink.NextMapPositionStart.Value.X ? 0 : 2));
        }

        private void ToMoving()
        {
            _aiComponent.ChangeState("moving");

            _body.VelocityTarget = AnimationHelper.DirectionOffset[_dir] * _movementSpeed;

            // update the animation if the fish is going to the left or right
            if (_dir % 2 == 0)
                _animator.Play("idle_" + _dir);

            _dir = (_dir + 2) % 4;
        }

        private void ToPausing()
        {
            _aiComponent.ChangeState("pause");
            _body.VelocityTarget = Vector2.Zero;
        }

        private void ToJump()
        {
            _body.IgnoresZ = true;
            _body.VelocityTarget = Vector2.Zero;

            _aiComponent.ChangeState("jumping");
            Game1.GameManager.PlaySoundEffect("D360-36-24");

            // scale down the damage box
            _damageBox.OffsetY += 8;
            _damageBox.Box.Height -= 8;

            _jumpStart = EntityPosition.Position;

            // splash animation
            var position = new Point((int)_jumpStart.X, (int)_jumpStart.Y);
            var splashAnimator = new ObjAnimator(_body.Owner.Map, position.X, position.Y - 14, 0, 3, 1, "Particles/fishingSplash", "idle", true);
            Map.Objects.SpawnObject(splashAnimator);
        }

        private void UpdateJump(double time)
        {
            var newPosition = _jumpStart - new Vector2(0, 64) * MathF.Sin((float)(time / JumpTime) * MathF.PI);
            EntityPosition.Set(newPosition);

            // player jumped on the fish?
            if ((MapManager.ObjLink._body.Velocity.Y > 0 && !MapManager.ObjLink._body.IsGrounded || time > JumpTime / 2) &&
                _headJumpBox.Box.Bottom >= MapManager.ObjLink._body.BodyBox.Box.Bottom &&
                _headJumpBox.Box.Intersects(MapManager.ObjLink._body.BodyBox.Box))
            {
                Game1.GameManager.PlaySoundEffect("D370-14-0E");

                MapManager.ObjLink._body.Velocity.Y -= 1f;
                _aiComponent.ChangeState("dead");
                _animator.Play("dead_" + _dir);
                _body.VelocityTarget = Vector2.Zero;
                _body.IgnoresZ = false;
                _body.Gravity2DWater = 0.05f;
                _body.CollisionTypes = Values.CollisionTypes.None;
            }
        }

        private void EndJump()
        {
            // scale up the damage box
            _damageBox.OffsetY -= 8;
            _damageBox.Box.Height += 8;

            _body.IgnoresZ = false;
            UpdateJump(JumpTime);
            ToMoving();
        }

        private void UpdateDeath()
        {
            if ((_body.CurrentFieldState & MapStates.FieldStates.DeepWater) != 0)
                _body.CollisionTypes = Values.CollisionTypes.Normal;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            // kill the fish when it reaches the ground
            if (_aiComponent.CurrentStateId == "dead" &&
                (direction & Values.BodyCollision.Vertical) != 0)
            {
                _damageState.OnHit(MapManager.ObjLink, new Vector2(0, 1), HitType.Sword1, 1, false);
                return;
            }

            if ((direction & Values.BodyCollision.Vertical) != 0)
                _body.Velocity.Y = 0;

            if (_aiComponent.CurrentStateId != "moving")
                return;

            if (_canJump)
                ToMoving();
            else
                ToPausing();
        }
    }
}
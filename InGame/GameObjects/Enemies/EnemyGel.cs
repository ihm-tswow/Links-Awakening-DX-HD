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
    internal class EnemyGel : GameObject
    {
        private readonly Animator _animator;
        private readonly AiComponent _ai;
        private readonly BodyComponent _body;
        private readonly AnimationComponent _animatorComponent;
        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly CSprite _sprite;
        private readonly AiTriggerSwitch _grabCooldown;

        private int _grabX;
        private int _grabY;
        private int _dir;
        private int _timerOffset;

        public EnemyGel() : base("gel") { }

        public EnemyGel(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-4, -12, 7, 17);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/gel");
            _animator.Play("0");

            _sprite = new CSprite(EntityPosition);
            _animatorComponent = new AnimationComponent(_animator, _sprite, new Vector2(-4, -7));

            _timerOffset = Game1.RandomNumber.Next(0, 1000);

            var fieldRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -4, -7, 7, 7, 8)
            {
                Gravity = -0.2f,
                AvoidTypes = Values.CollisionTypes.Hole |
                             Values.CollisionTypes.NPCWall,
                FieldRectangle = fieldRectangle
            };

            _grabX = Game1.RandomNumber.Next(90, 110);
            _grabY = Game1.RandomNumber.Next(25, 40);

            _ai = new AiComponent();

            var stateIdle = new AiState(UpdateIdle);
            stateIdle.Trigger.Add(new AiTriggerCountdown(200, null, EndIdle));
            var stateWalking = new AiState(UpdateWalking);
            stateWalking.Trigger.Add(new AiTriggerRandomTime(EndWalking, 100, 150));
            var stateShaking = new AiState(UpdateShaking);
            stateShaking.Trigger.Add(new AiTriggerCountdown(1000, null, EndShaking));
            var stateJumping = new AiState(UpdateJumping);
            var stateGrabbing = new AiState(UpdateGrabbing);
            stateGrabbing.Trigger.Add(new AiTriggerRandomTime(EndGrabbing, 1500, 2500));

            var stateGrabbingRelease = new AiState(UpdateJumping);

            _ai.States.Add("idle", stateIdle);
            _ai.States.Add("walking", stateWalking);
            _ai.States.Add("shaking", stateShaking);
            _ai.States.Add("jumping", stateJumping);
            _ai.States.Add("grabbing", stateGrabbing);
            _ai.States.Add("grabbingRelease", stateGrabbingRelease);
            new AiFallState(_ai, _body, null, null, 100);
            new AiDeepWaterState(_body);
            var damageState = new AiDamageState(this, _body, _ai, _sprite, 1);

            _ai.Trigger.Add(_grabCooldown = new AiTriggerSwitch(2000));

            _ai.ChangeState("idle");

            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, damageState.OnHit));
            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(new CRectangle(EntityPosition, new Rectangle(-4, -7, 7, 12)), OnPlayerCollision));
            AddComponent(AiComponent.Index, _ai);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, _animatorComponent);
            AddComponent(DrawComponent.Index, _bodyDrawComponent = new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
        }

        public void InitSpawn()
        {
            _body.Velocity.Z = 2;
        }

        private void UpdateIdle()
        {
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play(_dir.ToString());
        }

        private void EndIdle()
        {
            // only start walking if the player is in the range of the enemy
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDistance.Length() > 50)
            {
                _ai.ChangeState("idle");
                return;
            }

            _ai.ChangeState("walking");
        }

        private void UpdateWalking()
        {
            var vecDirection = new Vector2(
                MapManager.ObjLink.PosX - EntityPosition.X,
                MapManager.ObjLink.PosY - EntityPosition.Y);
            if (vecDirection != Vector2.Zero)
                vecDirection.Normalize();
            _dir = vecDirection.X < 0 ? -1 : 1;

            _animator.Play((-_dir).ToString());

            _body.VelocityTarget = vecDirection * 0.5f;
        }

        private void EndWalking()
        {
            // start shaking
            if (Game1.RandomNumber.Next(0, 10) == 0)
                _ai.ChangeState("shaking");
            else
                _ai.ChangeState("idle");
        }

        private void UpdateShaking()
        {
            _body.VelocityTarget = Vector2.Zero;
            _animatorComponent.SpriteOffset.X = -4 + (float)Math.Sin((Game1.TotalGameTime + _timerOffset) / 25f);
            _animatorComponent.UpdateSprite();
        }

        private void EndShaking()
        {
            // start jumping
            _ai.ChangeState("jumping");

            _animatorComponent.SpriteOffset.X = -4;

            var vecDirection = new Vector2(
                MapManager.ObjLink.PosX - EntityPosition.X,
                MapManager.ObjLink.PosY - EntityPosition.Y);
            if (vecDirection != Vector2.Zero)
                vecDirection.Normalize();

            _body.VelocityTarget = vecDirection * 1.25f;
            _body.Velocity.Z = 1.25f;
        }

        private void UpdateJumping()
        {
            if (_body.IsGrounded)
            {
                _body.VelocityTarget = Vector2.Zero;
                _ai.ChangeState("idle");
            }
        }

        private void UpdateGrabbing()
        {
            EntityPosition.Set(MapManager.ObjLink.EntityPosition);
            MapManager.ObjLink.SlowDown(0.5f);
            MapManager.ObjLink.DisableItems = true;

            _bodyDrawComponent.Layer = Values.LayerTop;
            _animator.Play(_dir.ToString());

            _animatorComponent.SpriteOffset.X = -4 + (float)Math.Sin((Game1.TotalGameTime + _timerOffset) / _grabX) * 3.5f;
            _animatorComponent.SpriteOffset.Y = -7 - 2 + (float)Math.Sin((Game1.TotalGameTime + _timerOffset) / _grabY) * 1.5f;
            _animatorComponent.UpdateSprite();
        }

        private void EndGrabbing()
        {
            var angle = Game1.RandomNumber.Next(-100, 100) / 200f * (float)Math.PI;
            var vecDirection = new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));
            _body.VelocityTarget = vecDirection;
            _body.Velocity.Z = 1.5f;

            _animatorComponent.SpriteOffset.X = -4;
            _animatorComponent.SpriteOffset.Y = -7;

            _bodyDrawComponent.Layer = Values.LayerPlayer;
            _ai.ChangeState("grabbingRelease");
            _grabCooldown.Reset();
        }

        private void OnPlayerCollision(GameObject gameObject)
        {
            if (_grabCooldown.State &&
                _ai.CurrentStateId != "grabbing" &&
                _ai.CurrentStateId != "grabbingRelease" &&
                _ai.CurrentStateId != "burning")
                _ai.ChangeState("grabbing");
        }
    }
}
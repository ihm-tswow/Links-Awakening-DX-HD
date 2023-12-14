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
    internal class EnemyRedZol : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _damageState;
        private readonly AnimationComponent _animationComponent;

        private readonly EnemyGel _gel0;
        private readonly EnemyGel _gel1;

        private float _jumpAcceleration = 1.5f;

        private bool _spawnSmallZols = true;

        public EnemyRedZol() : base("red zol") { }

        public EnemyRedZol(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 13, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/red zol");
            _animator.Play("walk_1");

            var sprite = new CSprite(EntityPosition);
            _animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-6, -16));

            _body = new BodyComponent(EntityPosition, -6, -10, 12, 10, 8)
            {
                MoveCollision = OnCollision,
                // Values.CollisionTypes.Hole can not ignore holes because they need to walk into one in dungeon 4
                AvoidTypes = Values.CollisionTypes.NPCWall |
                             Values.CollisionTypes.DeepWater,
                FieldRectangle = map.GetField(posX, posY),
                Gravity = -0.15f,
                Bounciness = 0.25f,
                Drag = 0.85f
            };

            var stateWaiting = new AiState { Init = InitWaiting };
            stateWaiting.Trigger.Add(new AiTriggerRandomTime(EndWaiting, 200, 200));
            var stateWalking = new AiState(StateWalking) { Init = InitWalking };
            stateWalking.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("waiting"), 132, 132));
            var stateShaking = new AiState();
            stateShaking.Trigger.Add(new AiTriggerCountdown(1000, TickShake, ShakeEnd));
            var stateJumping = new AiState { Init = InitJumping };

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("waiting", stateWaiting);
            _aiComponent.States.Add("walking", stateWalking);
            _aiComponent.States.Add("shaking", stateShaking);
            _aiComponent.States.Add("jumping", stateJumping);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1)
            {
                OnDeath = OnDeath
            };
            new AiFallState(_aiComponent, _body, null, null, 100);
            new AiDeepWaterState(_body);

            _aiComponent.ChangeState("waiting");

            var damageBox = new CBox(EntityPosition, -6, -11, 0, 12, 11, 4);
            var hittableBox = new CBox(EntityPosition, -6, -11, 12, 11, 8);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite));

            // spawn two gels inactive (needed for the enemy trigger)
            _gel0 = new EnemyGel(Map, posX, posY) { IsActive = false };
            Map.Objects.SpawnObject(_gel0);

            _gel1 = new EnemyGel(Map, posX, posY) { IsActive = false };
            Map.Objects.SpawnObject(_gel1);
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            // spawn small zols if the damage is not over 1
            if (damage > 1)
            {
                ((HittableComponent)Components[HittableComponent.Index]).IsActive = false;
                _spawnSmallZols = false;
            }
            else
            {
                _damageState.SpawnItems = false;
                _damageState.DeathAnimation = false;
            }

            return _damageState.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        private void InitWaiting()
        {
            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("idle");
        }

        private void EndWaiting()
        {
            var distance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            if (distance.Length() > 80 || !_body.FieldRectangle.Intersects(MapManager.ObjLink.BodyRectangle))
                return;

            if (Game1.RandomNumber.Next(0, 10) == 0)
                _aiComponent.ChangeState("shaking");
            else
                _aiComponent.ChangeState("walking");
        }

        private void InitWalking()
        {
            _animator.Play("walk");
        }

        private void StateWalking()
        {
            // walk to the player
            MoveToPlayer(0.4f);
        }

        private void TickShake(double time)
        {
            _animationComponent.SpriteOffset.X = -6 + (float)Math.Sin(time / 25f);
            _animationComponent.UpdateSprite();
        }

        private void ShakeEnd()
        {
            _animationComponent.SpriteOffset.X = -6;
            _animationComponent.UpdateSprite();

            _aiComponent.ChangeState("jumping");
        }

        private void InitJumping()
        {
            _animator.Play("walk");

            _body.Velocity.Z = _jumpAcceleration;

            // move to the player
            MoveToPlayer(1.25f);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

            return true;
        }

        private void OnCollision(Values.BodyCollision type)
        {
            // hit the floor after a jump
            if ((type & Values.BodyCollision.Floor) != 0)
                _aiComponent.ChangeState("waiting");
        }

        private void MoveToPlayer(float speed)
        {
            var vecDirection = new Vector2(
                MapManager.ObjLink.PosX - EntityPosition.X,
                MapManager.ObjLink.PosY - EntityPosition.Y);

            if (vecDirection == Vector2.Zero)
                return;

            vecDirection.Normalize();
            _body.VelocityTarget = vecDirection * speed;
        }

        private void OnDeath(bool pieceOfPower)
        {
            _damageState.BaseOnDeath(pieceOfPower);

            if (!_spawnSmallZols)
            {
                Map.Objects.DeleteObjects.Add(_gel0);
                Map.Objects.DeleteObjects.Add(_gel1);
                return;
            }

            // positions are set so that the gels are inside of the body to not collide with stuff
            _gel0.EntityPosition.Set(new Vector2(EntityPosition.X - 1.9f - Game1.RandomNumber.Next(0, 2), EntityPosition.Y - Game1.RandomNumber.Next(0, 2)));
            _gel0.IsActive = true;
            _gel0.InitSpawn();
            _gel1.EntityPosition.Set(new Vector2(EntityPosition.X + 2.9f + Game1.RandomNumber.Next(0, 2), EntityPosition.Y - Game1.RandomNumber.Next(0, 2)));
            _gel1.IsActive = true;
            _gel1.InitSpawn();
        }
    }
}
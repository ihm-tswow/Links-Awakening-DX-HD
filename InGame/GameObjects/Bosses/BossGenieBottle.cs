using System;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Bosses
{
    class BossGenieBottle : GameObject
    {
        private readonly BossGenie _objGenie;

        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly DamageFieldComponent _damageField;
        private readonly AiTriggerSwitch _aiDamageSwitch;
        private readonly BoxCollisionComponent _collisionComponent;
        private readonly AnimationComponent _animationComponent;
        private readonly CarriableComponent _carriableComponent;
        private readonly AiDamageState _damageState;

        private readonly Vector2 _spawnTargetPosition;

        private const float SpawnMoveSpeed = 0.5f;
        private const float FollowSpeed = 0.75f;
        private const int Lives = 3;

        private bool _showedStunnedMessage;

        public BossGenieBottle() : base("genie") { }

        public BossGenieBottle(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -32, 16, 32);

            _spawnTargetPosition = new Vector2(EntityPosition.X, EntityPosition.Y + 24);

            if (!string.IsNullOrWhiteSpace(saveKey) && Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                // respawn the heart if the player died after he killed the boss without collecting the heart
                SpawnHeart();

                IsDead = true;
                return;
            }

            // add the genie to the map
            _objGenie = new BossGenie(map, saveKey, new Vector3(EntityPosition.X, EntityPosition.Y, 0), this);
            map.Objects.SpawnObject(_objGenie);

            _animator = AnimatorSaveLoad.LoadAnimator("Nightmares/genie bottle");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);

            _animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -5, -10, 10, 10, 8)
            {
                CollisionTypes = Values.CollisionTypes.Normal,
                Bounciness = 0.0f,
                Gravity = -0.15f,
                Drag = 0.65f,
                DragAir = 1.0f,
                MoveCollision = OnCollision,
                FieldRectangle = Map.GetField(posX, posY, 16)
            };

            var hittableBox = new CBox(EntityPosition, -8, -16, 0, 16, 16, 8, true);
            var pushableBox = new CBox(EntityPosition, -8, -16, 0, 16, 16, 8, true);
            var damageCollider = new CBox(EntityPosition, -7, -14, 14, 14, 8);
            var carryRectangle = new CRectangle(EntityPosition, new Rectangle(-8, -16, 16, 16));

            var stateIdle = new AiState(UpdateIdle);
            var stateTriggered = new AiState();
            stateTriggered.Trigger.Add(new AiTriggerCountdown(500, null, () => _aiComponent.ChangeState("spawn")));
            var stateSpawn = new AiState(UpdateSpawn) { Init = InitSpawn };
            var stateSpawnDelay = new AiState { Init = InitSpawnDelay };
            stateSpawnDelay.Trigger.Add(new AiTriggerCountdown(3300, null, EndSpawnDelay));
            var stateReturnDelay = new AiState();
            stateReturnDelay.Trigger.Add(new AiTriggerCountdown(800, null, EndSpawnDelay));
            var stateSpawned = new AiState();
            var stateFollow = new AiState(UpdateFollow) { Init = InitFollow };
            var stateStunned = new AiState(UpdateStunned);
            stateStunned.Trigger.Add(new AiTriggerCountdown(2500, null, () => _aiComponent.ChangeState("shaking")));
            var stateShaking = new AiState();
            stateShaking.Trigger.Add(new AiTriggerCountdown(600, TickShake, ShakeEnd));
            var stateGrabbed = new AiState();
            var stateReturn = new AiState(UpdateReturn) { Init = InitReturn };
            var stateThrown = new AiState(UpdateThrown);

            _aiComponent = new AiComponent();
            _aiComponent.Trigger.Add(_aiDamageSwitch = new AiTriggerSwitch(500));

            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("triggered", stateTriggered);
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("spawnDelay", stateSpawnDelay);
            _aiComponent.States.Add("returnDelay", stateReturnDelay);
            _aiComponent.States.Add("spawned", stateSpawned);
            _aiComponent.States.Add("follow", stateFollow);
            _aiComponent.States.Add("stunned", stateStunned);
            _aiComponent.States.Add("shaking", stateShaking);
            _aiComponent.States.Add("grabbed", stateGrabbed);
            _aiComponent.States.Add("return", stateReturn);
            _aiComponent.States.Add("thrown", stateThrown);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, Lives) { MoveBody = false, OnDeath = OnDeath, BossHitSound = true };

            _aiComponent.ChangeState("idle");

            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2) { IsActive = false });
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(CarriableComponent.Index, _carriableComponent = new CarriableComponent(carryRectangle, CarryInit, CarryUpdate, CarryThrow) { IsActive = false });
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, _collisionComponent = new BoxCollisionComponent(pushableBox, Values.CollisionTypes.Enemy) { IsActive = false });
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite) { ShadowWidth = 12, ShadowHeight = 6 });
        }

        private void UpdateIdle()
        {
            // player entered the room?
            if (_body.FieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
            {
                Game1.GameManager.StartDialogPath("d2_boss");
                _aiComponent.ChangeState("triggered");
            }
        }

        private void UpdateStunned()
        {
            // show the message one time
            if (_body.IsGrounded && !_showedStunnedMessage)
            {
                _showedStunnedMessage = true;
                Game1.GameManager.StartDialogPath("d2_boss_2");
            }
        }

        private void OnDeath(bool pieceOfPower)
        {
            // spawn the genie
            _objGenie.AttackSpawn(new Vector3(EntityPosition.X, EntityPosition.Y + 38, 27));

            // remove the bottle from the map
            Map.Objects.DeleteObjects.Add(this);

            Game1.GameManager.PlaySoundEffect("D378-41-29");
        }

        public void StartFollowing()
        {
            _aiComponent.ChangeState("follow");
        }

        private void EndSpawnDelay()
        {
            // spawn the genie
            _objGenie.Spawn(new Vector3(EntityPosition.X, EntityPosition.Y + 1, 27));
            _aiComponent.ChangeState("spawned");
        }

        private void InitSpawnDelay()
        {
            _collisionComponent.IsActive = true;
        }

        private void InitReturn()
        {
            _animator.Play("wobble");
            // make sure to not get blocked by the lamps
            _body.CollisionTypes = Values.CollisionTypes.None;
        }

        private void UpdateReturn()
        {
            var direction = _spawnTargetPosition - EntityPosition.Position;
            if (!MoveTowards(direction))
            {
                _body.CollisionTypes = Values.CollisionTypes.Normal;
                _aiComponent.ChangeState("returnDelay");
            }
        }

        private Vector3 CarryInit()
        {
            _aiComponent.ChangeState("grabbed");

            _carriableComponent.IsActive = false;
            _body.IsActive = false;

            return new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z);
        }

        private bool CarryUpdate(Vector3 newPosition)
        {
            EntityPosition.Set(newPosition);
            return true;
        }

        private void CarryThrow(Vector2 velocity)
        {
            _aiComponent.ChangeState("thrown");
            _body.Bounciness = 0.65f;
            _body.DragAir = 0.99f;
            _body.IsGrounded = false;
            _body.JumpStartHeight = 0;
            _body.IsActive = true;
            _body.Velocity = new Vector3(velocity.X, velocity.Y, 0) * 0.85f;
        }

        private void UpdateThrown()
        {
            // stopped without hitting a wall?
            if (_body.Velocity.Length() < 0.1f)
            {
                _aiComponent.ChangeState("return");
            }
        }

        private void InitSpawn()
        {
            _animator.Play("wobble");
        }

        private void UpdateSpawn()
        {
            var direction = _spawnTargetPosition - EntityPosition.Position;

            if (!MoveTowards(direction))
                _aiComponent.ChangeState("spawnDelay");
        }

        private bool MoveTowards(Vector2 direction)
        {
            // jump
            if (_body.IsGrounded)
            {
                _body.Velocity.Z = 1.25f;
                Game1.GameManager.PlaySoundEffect("D360-32-20");
            }

            // move towards the target position
            if (direction.Length() > SpawnMoveSpeed * Game1.TimeMultiplier)
            {
                direction.Normalize();
                _body.VelocityTarget = direction * SpawnMoveSpeed;
            }
            else
            {
                EntityPosition.Set(_spawnTargetPosition);
                _body.VelocityTarget = Vector2.Zero;
                return false;
            }

            return true;
        }

        private void InitFollow()
        {
            _collisionComponent.IsActive = false;
            _damageField.IsActive = true;
            _animator.Play("wobble");
        }

        private void UpdateFollow()
        {
            if (!_body.IsGrounded || _body.Velocity.Z > 0)
                return;

            _body.Velocity.X = 0;
            _body.Velocity.Y = 0;

            // jump
            _body.Velocity.Z = 1.25f;

            Game1.GameManager.PlaySoundEffect("D360-32-20");

            // follow the player
            var direction = MapManager.ObjLink.EntityPosition.Position - new Vector2(EntityPosition.X, EntityPosition.Y - 4);
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _body.VelocityTarget = direction * FollowSpeed;
            }
        }

        private void ToStun(Vector2 velocity)
        {
            _carriableComponent.IsActive = true;
            _damageField.IsActive = false;
            _body.Bounciness = 0f;
            _body.VelocityTarget = Vector2.Zero;
            _body.Velocity = new Vector3(velocity.X, velocity.Y, 2);
            _collisionComponent.IsActive = true;
            _animator.Play("idle");
            _aiComponent.ChangeState("stunned");
        }

        private void TickShake(double time)
        {
            _animationComponent.SpriteOffset.X = (float)Math.Sin(time / 25f);
            _animationComponent.UpdateSprite();
        }

        private void ShakeEnd()
        {
            _animationComponent.SpriteOffset.X = 0;
            _animationComponent.UpdateSprite();
            _carriableComponent.IsActive = false;
            _aiComponent.ChangeState("follow");
        }

        private void SpawnHeart()
        {
            // spawn big heart
            Map.Objects.SpawnObject(new ObjItem(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y, "", "d2_nHeart", "heartMeterFull", null));
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if (_aiComponent.CurrentStateId == "thrown" &&
                (collision & (Values.BodyCollision.Horizontal | Values.BodyCollision.Vertical)) != 0)
            {
                _aiComponent.ChangeState("return");
                _damageState.OnHit(MapManager.ObjLink, Vector2.Zero, HitType.ThrownObject, 1, false);

                _body.Velocity.X = -_body.Velocity.X * 0.2f;
                _body.Velocity.Y = -_body.Velocity.Y * 0.2f;
                _body.Bounciness = 0.5f;
            }
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_aiComponent.CurrentStateId == "follow")
            {
                _aiDamageSwitch.Reset();
                ToStun(direction * 0.75f);
                return Values.HitCollision.RepellingParticle;
            }

            if (_aiDamageSwitch.State)
                return Values.HitCollision.RepellingParticle;

            return Values.HitCollision.None;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type != PushableComponent.PushType.Impact)
                return false;

            if (_aiComponent.CurrentStateId == "follow")
            {
                _body.Velocity = new Vector3(direction.X, direction.Y, 1);
                _body.Bounciness = 0.0f;
            }

            return true;
        }
    }
}
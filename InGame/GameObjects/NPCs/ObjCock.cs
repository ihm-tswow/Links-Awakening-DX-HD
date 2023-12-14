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

namespace ProjectZ.InGame.GameObjects.NPCs
{
    public class ObjCock : GameObjectFollower
    {
        private ObjCockParticle _objParticle;

        private readonly BodyDrawComponent _drawComponent;
        private readonly BodyDrawShadowComponent _shadowCompnent;
        private readonly CarriableComponent _carriableCompnent;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly CSprite _sprite;

        private readonly string _saveKey;

        private const int CarryHeight = 14;

        private int _blinkTime;
        private int _direction;

        private bool _isThrown;
        private bool _slowReturn;
        private bool _freezePlayer;
        private bool _isActive = true;

        private const int FollowDistance = 18;

        public ObjCock() : base("cock") { }

        public ObjCock(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _saveKey = saveKey;
            // skeleton was already awakend?
            if (_saveKey != null && Game1.GameManager.SaveManager.GetString(_saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            // TODO_CHECK: must align with the player body
            _body = new BodyComponent(EntityPosition, -4, -10, 8, 10, 8)
            {
                Bounciness = 0f,
                Gravity = -0.075f,
                Drag = 0.85f,
                IsSlider = true,
                CollisionTypes = Values.CollisionTypes.None,
            };

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/cock");
            _animator.Play("stand_3");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, Vector2.Zero);

            // blink for ~1000ms
            _blinkTime = (1000 / AiDamageState.BlinkTime) * AiDamageState.BlinkTime;

            var stateSkeleton = new AiState();
            var stateParticle = new AiState(UpdateParticle) { Init = InitParticle };
            var stateBlinking = new AiState();
            stateBlinking.Trigger.Add(new AiTriggerCountdown(_blinkTime, TickBlink, EndBlink));
            var statePreSpawn = new AiState();
            statePreSpawn.Trigger.Add(new AiTriggerCountdown(1100, null, ToSpawn));
            var stateSpawn = new AiState();
            stateSpawn.Trigger.Add(new AiTriggerCountdown(750, null, StartFollowing));
            // buffer state to not be one frame into a jump while showing the textbox
            var statePreFollowing = new AiState();
            statePreFollowing.Trigger.Add(new AiTriggerCountdown(100, null, EndPreFollowing));
            var stateFollowing = new AiState(UpdateWalking) { Init = InitWalk };
            var stateThrown = new AiState(UpdateThrown);
            var statePickedUp = new AiState(UpdatePickedUp);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("skeleton", stateSkeleton);
            _aiComponent.States.Add("particle", stateParticle);
            _aiComponent.States.Add("blinking", stateBlinking);
            _aiComponent.States.Add("preSpawn", statePreSpawn);
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("preFollowing", statePreFollowing);
            _aiComponent.States.Add("following", stateFollowing);
            _aiComponent.States.Add("thrown", stateThrown);
            _aiComponent.States.Add("pickedUp", statePickedUp);

            AddComponent(CarriableComponent.Index, _carriableCompnent = new CarriableComponent(
                new CRectangle(EntityPosition, new Rectangle(-6, -14, 12, 14)), CarryInit, CarryUpdate, CarryThrow)
            { CarryHeight = CarryHeight });
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(OcarinaListenerComponent.Index, new OcarinaListenerComponent(OnSongPlayed));
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(new CBox(EntityPosition, -8, -16, 16, 16, 8), Values.CollisionTypes.Normal));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _drawComponent = new BodyDrawComponent(_body, _sprite, Values.LayerBottom));
            AddComponent(DrawShadowComponent.Index, _shadowCompnent = new BodyDrawShadowComponent(_body, _sprite) { IsActive = false });

            // no saveKey => spawned by the player in the following state
            if (_saveKey == null)
            {
                ToActiveState();
                _aiComponent.ChangeState("following");
            }
            else
            {
                _animator.Play("skeleton");
                _aiComponent.ChangeState("skeleton");
            }
        }

        public override void SetPosition(Vector2 position)
        {
            EntityPosition.Set(position);
        }

        private void SetActive(bool isActive)
        {
            _isActive = isActive;
            _drawComponent.IsActive = isActive;
            _shadowCompnent.IsActive = isActive;
            _carriableCompnent.IsActive = isActive;
        }

        private void OnSongPlayed(int songIndex)
        {
            if (songIndex == 2 && _aiComponent.CurrentStateId == "skeleton")
                _aiComponent.ChangeState("particle");
        }

        private void Update()
        {
            // do not follow the player into dungeons
            if (Map.DungeonMode && _isActive)
                SetActive(false);
            if (!Map.DungeonMode && !_isActive)
                SetActive(true);

            if (_freezePlayer)
                MapManager.ObjLink.FreezePlayer();
        }

        private void ToActiveState()
        {
            ((DrawComponent)Components[DrawComponent.Index]).Layer = Values.LayerPlayer;
            ((BodyDrawShadowComponent)Components[DrawShadowComponent.Index]).IsActive = true;
            RemoveComponent(CollisionComponent.Index);
        }

        private void InitParticle()
        {
            _freezePlayer = true;

            Game1.GameManager.SetMusic(84, 2);

            // spawn the particle
            _objParticle = new ObjCockParticle(Map, new Vector2(EntityPosition.X, EntityPosition.Y - 8));
            Map.Objects.SpawnObject(_objParticle);
        }

        private void UpdateParticle()
        {
            // start blinking when the particle hits the skeleton
            if (!_objParticle.IsRunning())
                _aiComponent.ChangeState("blinking");
        }

        private void TickBlink(double time)
        {
            _sprite.SpriteShader = ((_blinkTime - time) % (AiDamageState.BlinkTime * 2) < AiDamageState.BlinkTime) ? Resources.DamageSpriteShader0 : null;
        }

        private void EndBlink()
        {
            _sprite.SpriteShader = null;
            _aiComponent.ChangeState("preSpawn");
        }

        private void ToSpawn()
        {
            // explosion
            _animator.Play("spawn");
            ToActiveState();

            Game1.GameManager.PlaySoundEffect("D378-12-0C");
            Game1.GameManager.SetMusic(-1, 2);

            // spawn explosion effect
            var objAnimation = new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y - 8, Values.LayerTop, "Particles/explosionBomb", "run", true);
            Map.Objects.SpawnObject(objAnimation);

            _aiComponent.ChangeState("spawn");
        }

        private void StartFollowing()
        {
            Game1.GameManager.PlaySoundEffect("D368-16-10");

            // add the rooster as a follower
            var itemRooster = new GameItemCollected("rooster") { Count = 1 };
            MapManager.ObjLink.PickUpItem(itemRooster, false);

            Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            _aiComponent.ChangeState("preFollowing");
        }

        private void EndPreFollowing()
        {
            _freezePlayer = false;
            _animator.Play("stand_3");
            _aiComponent.ChangeState("following");
        }

        private void InitWalk()
        {
            SetThrowState(false);
        }

        private void UpdateWalking()
        {
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            var distance = playerDirection.Length();
            var playerSpeed = MapManager.ObjLink.LastMoveVector.Length();

            // slowly transition to the full speed
            var movementSpeed = MathHelper.Clamp((distance - FollowDistance) / 4, -2, 2);
            if (Math.Abs(distance - FollowDistance) > FollowDistance + 4)
                movementSpeed = MathHelper.Clamp(distance / (FollowDistance + 4), -2, 2);
            // slowly walk back to the player after been thrown
            if (_slowReturn)
                movementSpeed = MathHelper.Clamp(movementSpeed, playerSpeed, 1);

            if (movementSpeed > 0 && !_isThrown)
            {
                if (playerDirection != Vector2.Zero)
                    playerDirection.Normalize();

                _body.Velocity.X = playerDirection.X * movementSpeed;
                _body.Velocity.Y = playerDirection.Y * movementSpeed;

                _direction = AnimationHelper.GetDirection(playerDirection);
                _animator.Play("stand_" + _direction);
            }

            // stop slow return when we reached the player or the player is moving faster away than we are moving
            if (!_isThrown && (distance <= FollowDistance || playerSpeed > 1))
                _slowReturn = false;

            // fly over deep water
            if ((_body.CurrentFieldState & MapStates.FieldStates.DeepWater) != 0)
            {
                _body.IsGrounded = false;
                _body.IgnoresZ = true;
                var targetPosZ = 7.5f + MathF.Sin(((float)Game1.TotalGameTime / 1000) * MathF.PI * 2) * 1.5f;
                EntityPosition.Z = AnimationHelper.MoveToTarget(EntityPosition.Z, targetPosZ, 1 * Game1.TimeMultiplier);
            }
            else
            {
                _body.IgnoresZ = false;
            }

            // jump
            if (_body.IsGrounded)
            {
                var jumpHeight = MathHelper.Clamp(distance / 18, 1, 2);
                // while returning from a throw do not jump high
                if (_slowReturn)
                    jumpHeight = 1;

                _body.Velocity.Z = jumpHeight;
            }
        }

        public void TargetVelocity(Vector2 targetVelocity, float maxSpeed, int direction)
        {
            // move towards the target velocity
            var target = _body.VelocityTarget + targetVelocity * 0.05f * Game1.TimeMultiplier;
            if (target.Length() > maxSpeed)
            {
                target.Normalize();
                target *= maxSpeed;
            }

            _body.VelocityTarget = target;

            _direction = direction;
            _animator.Play("stand_" + _direction);
        }

        private void UpdatePickedUp()
        {
            if (!MapManager.ObjLink.IsFlying())
                MapManager.ObjLink.StartFlying(this);

            Game1.GameManager.PlaySoundEffect("D378-45-2D", false);

            // move up
            var targetPosZ = 36 + MathF.Sin(((float)Game1.TotalGameTime / 450) * MathF.PI * 2) * 1.5f;
            EntityPosition.Z = AnimationHelper.MoveToTarget(EntityPosition.Z, targetPosZ, 0.5f * Game1.TimeMultiplier);

            // lift the player up
            if (EntityPosition.Z > CarryHeight)
                MapManager.ObjLink.EntityPosition.Z = EntityPosition.Z - CarryHeight;
        }

        private void UpdateThrown()
        {
            if (_body.IsGrounded)
            {
                _aiComponent.ChangeState("following");
                _body.Velocity.X = 0;
                _body.Velocity.Y = 0;
            }
        }

        private void SetThrowState(bool thrown)
        {
            _isThrown = thrown;
            _body.DragAir = thrown ? 0.975f : 0.85f;
        }

        private Vector3 CarryInit()
        {
            _body.IgnoresZ = true;
            _body.Velocity = Vector3.Zero;
            _body.VelocityTarget = Vector2.Zero;
            _body.CollisionTypes = MapManager.ObjLink._body.CollisionTypes;

            _animator.SpeedMultiplier = 2.0f;
            _aiComponent.ChangeState("pickedUp");
            EntityPosition.AddPositionListener(typeof(ObjCock), OnPositionChange);

            return new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z);
        }

        private bool CarryUpdate(Vector3 position)
        {
            EntityPosition.Set(new Vector3(position.X, position.Y, position.Z));
            return true;
        }

        private void CarryThrow(Vector2 direction)
        {
            _body.Velocity = new Vector3(direction.X, direction.Y, 0);

            MapManager.ObjLink.StopFlying();
        }

        public void StopFlying()
        {
            _body.IgnoresZ = false;
            _body.IsGrounded = false;
            _body.VelocityTarget = Vector2.Zero;
            _body.CollisionTypes = Values.CollisionTypes.None;

            _slowReturn = true;
            SetThrowState(true);
            _animator.SpeedMultiplier = 1.0f;
            _aiComponent.ChangeState("thrown");
            EntityPosition.RemovePositionListener(typeof(ObjCock));
        }

        private void OnPositionChange(CPosition newPosition)
        {
            if (MapManager.ObjLink.IsFlying())
                MapManager.ObjLink.SetPosition(new Vector2(newPosition.X, newPosition.Y));
        }
    }
}
using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyLeever : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly DamageFieldComponent _damageField;
        private readonly AiDamageState _damageState;
        private readonly Animator _animator;
        private readonly CSprite _sprite;

        private readonly Rectangle _fieldPosition;

        private const float MoveSpeed = 0.5f;

        public EnemyLeever() : base("leever") { }

        public EnemyLeever(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-50, -50 - 8, 100, 100);

            _fieldPosition = map.GetField(posX, posY);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/leever");
            _animator.Play("move");

            _sprite = new CSprite(EntityPosition);
            _sprite.IsVisible = false;

            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8)
            {
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Enemy |
                    Values.CollisionTypes.Player,
                AvoidTypes =
                    Values.CollisionTypes.Hole |
                    Values.CollisionTypes.NPCWall,
                Bounciness = 0.25f,
                Drag = 0.85f,
            };

            var stateInit = new AiState();
            stateInit.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("hidden"), 750, 1500));
            var stateHidden = new AiState();
            var stateSpawning = new AiState(UpdateSpawning);
            var stateMoving = new AiState(UpdateMoving);
            stateMoving.Trigger.Add(new AiTriggerRandomTime(ToLeaving, 2000, 3000));
            var stateLeaving = new AiState(UpdateLeaving);
            var stateWaiting = new AiState();
            stateWaiting.Trigger.Add(new AiTriggerRandomTime(Spawn, 1000, 2000));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("init", stateInit);
            _aiComponent.States.Add("hidden", stateHidden);
            _aiComponent.States.Add("spawning", stateSpawning);
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("leaving", stateLeaving);
            _aiComponent.States.Add("waiting", stateWaiting);
            _damageState = new AiDamageState(this, _body, _aiComponent, _sprite, 2) { OnBurn = OnBurn };

            _aiComponent.ChangeState("init");

            var damageBox = new CBox(EntityPosition, -8, -13, 0, 16, 14, 4);
            var hittableBox = new CBox(EntityPosition, -7, -14, 0, 14, 14, 8);
            var spawnRectangle = new Rectangle(posX + 8 + EntitySize.X, posY + 16 + EntitySize.Y, EntitySize.Width, EntitySize.Height);

            AddComponent(ObjectCollisionComponent.Index, new ObjectCollisionComponent(spawnRectangle, OnEnterSpawnArea));
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, _damageState.OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(DrawComponent.Index, _bodyDrawComponent = new BodyDrawComponent(_body, _sprite, Values.LayerPlayer) { IsActive = false });
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));

            Deactivate();
        }

        private void OnBurn()
        {
            _animator.Pause();
        }

        private void OnEnterSpawnArea(GameObject gameObject)
        {
            // spawn if the player enters the spawnRectangle
            if (_aiComponent.CurrentStateId == "hidden")
                Spawn();
        }

        private void Activate()
        {
            _damageField.IsActive = true;
            _body.IsActive = true;
        }

        private void Deactivate()
        {
            _damageState.IsActive = false;
            _damageField.IsActive = false;
            _body.IsActive = false;
        }

        private void Spawn()
        {
            // find new position
            var newPosition = new Vector2(
                _fieldPosition.X + Game1.RandomNumber.Next(0, 10) * 16,
                _fieldPosition.Y + Game1.RandomNumber.Next(0, 8) * 16);

            // make sure to not spawn directly at the player
            var playerDistance = MapManager.ObjLink.EntityPosition.Position - new Vector2(newPosition.X + 8, newPosition.Y + 16);
            if (playerDistance.Length() < 24)
                return;

            // respawn if the position is free
            var collidingRectangle = Box.Empty;
            var fieldState = Map.GetFieldState(newPosition);
            if ((fieldState & (MapStates.FieldStates.Water | MapStates.FieldStates.DeepWater)) == 0 &&
                !Map.Objects.Collision(new Box(newPosition.X, newPosition.Y, 0, 16, 16, 16), Box.Empty,
                Values.CollisionTypes.Normal | Values.CollisionTypes.NPCWall | Values.CollisionTypes.Enemy | Values.CollisionTypes.Player, 0, 0, ref collidingRectangle))
            {
                EntityPosition.Set(newPosition + new Vector2(8, 16));
                ToSpawning();
            }
        }

        private void ToSpawning()
        {
            _aiComponent.ChangeState("spawning");

            _bodyDrawComponent.IsActive = true;
            _sprite.IsVisible = true;

            _animator.Play("spawn");
        }

        private void UpdateSpawning()
        {
            if (_animator.CurrentFrameIndex > 0)
                _damageState.IsActive = true;

            if (!_animator.IsPlaying)
                ToMoving();
        }

        private void ToMoving()
        {
            _aiComponent.ChangeState("moving");

            Activate();

            _animator.Play("move");
        }

        private void UpdateMoving()
        {
            // move in the direction of the player
            var direction = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            direction.Normalize();
            _body.VelocityTarget = direction * MoveSpeed;
        }

        private void ToLeaving()
        {
            _aiComponent.ChangeState("leaving");

            Deactivate();

            _body.VelocityTarget = Vector2.Zero;
            _animator.Play("leave");
        }

        private void UpdateLeaving()
        {
            if (_animator.CurrentFrameIndex > 1)
                _damageState.IsActive = false;

            if (!_animator.IsPlaying)
                ToWaiting();
        }

        private void ToWaiting()
        {
            _aiComponent.ChangeState("waiting");

            _bodyDrawComponent.IsActive = false;
            _sprite.IsVisible = false;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (!_body.IsActive)
                return false;

            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

            return true;
        }
    }
}
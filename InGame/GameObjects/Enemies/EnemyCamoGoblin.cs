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
    internal class EnemyCamoGoblin : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly DrawShadowCSpriteComponent _shadowComponent;
        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly DamageFieldComponent _damageField;
        private readonly AiDamageState _damageState;
        private readonly Animator _animator;
        private readonly AnimationComponent _animationComponent;

        private readonly float _movementSpeed;
        private readonly string _strColor;

        public EnemyCamoGoblin() : base("camo goblin") { }

        // color: 0 = red, 1 = green, 2 = blue
        public EnemyCamoGoblin(Map.Map map, int posX, int posY, int color) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -24, 16, 24);

            if (color == 0)
            {
                _strColor = "red";
                _movementSpeed = 1;
            }
            else if (color == 1)
            {
                _strColor = "green";
                _movementSpeed = 0.75f;
            }
            else
            {
                _strColor = "blue";
                _movementSpeed = 1; // TODO: look at real speed
            }

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/camo goblin");

            var sprite = new CSprite(EntityPosition);

            _animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);

            _body = new BodyComponent(EntityPosition, -7, -14, 14, 14, 8)
            {
                HoleOnPull = OnHolePull,
                CollisionTypes = Values.CollisionTypes.Normal,
                AvoidTypes = Values.CollisionTypes.Hole,
                FieldRectangle = map.GetField(posX, posY, 16),
                Bounciness = 0.25f,
                Drag = 0.85f,
            };

            var stateIdle = new AiState(UpdateIdle) { Init = InitIdle };
            var stateMove = new AiState { Init = InitMove };
            stateMove.Trigger.Add(new AiTriggerRandomTime(EndMove, 500, 800));
            var stateSpawn = new AiState(UpdateSpawn) { Init = InitSpawn };
            var stateWobble = new AiState(UpdateWobble) { Init = InitWobble };
            var stateDespawn = new AiState(UpdateDespawn) { Init = InitDespawn };
            var stateHolePull = new AiState();
            stateHolePull.Trigger.Add(new AiTriggerCountdown(1000, null, () => _aiComponent.ChangeState("idle")));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("move", stateMove);
            _aiComponent.States.Add("spawn", stateSpawn);
            _aiComponent.States.Add("wobble", stateWobble);
            _aiComponent.States.Add("despawn", stateDespawn);
            _aiComponent.States.Add("holePull", stateHolePull);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 1) { OnBurn = () => _animator.Pause() };
            new AiFallState(_aiComponent, _body, null, null, 0);

            var damageBox = new CBox(EntityPosition, -6, -20, 0, 12, 20, 4);
            var hittableBox = new CBox(EntityPosition, -6, -20, 0, 12, 20, 8);
            var pushableBox = new CBox(EntityPosition, -6, -20, 0, 12, 20, 8);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageBox, HitType.Enemy, 2) { IsActive = false });
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, _animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DrawComponent.Index, _bodyDrawComponent = new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new DrawShadowCSpriteComponent(sprite));

            _aiComponent.ChangeState("idle");
        }

        private void InitIdle()
        {
            _shadowComponent.IsActive = false;
            _bodyDrawComponent.Layer = Values.LayerBottom;
            _animator.Play("eyes_" + _strColor);
        }

        private void UpdateIdle()
        {
            var distance = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (distance.Length() < 36 && _body.FieldRectangle.Contains(MapManager.ObjLink.BodyRectangle))
                _aiComponent.ChangeState("move");
        }

        private void InitMove()
        {
            // move towards the player
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection != Vector2.Zero)
            {
                playerDirection.Normalize();
                _body.VelocityTarget = playerDirection * _movementSpeed;
            }
        }

        private void EndMove()
        {
            _aiComponent.ChangeState("spawn");
        }

        private void InitSpawn()
        {
            _shadowComponent.IsActive = true;
            _bodyDrawComponent.Layer = Values.LayerPlayer;
            _animator.Play("spawn_" + _strColor);
            _animationComponent.MirroredH = EntityPosition.X > MapManager.ObjLink.EntityPosition.X;
            _body.VelocityTarget = Vector2.Zero;
        }

        private void UpdateSpawn()
        {
            if (!_animator.IsPlaying)
            {
                _aiComponent.ChangeState("wobble");
            }
        }

        private void InitWobble()
        {
            _damageField.IsActive = true;
            _animator.Play("wobble_" + _strColor);
        }

        private void UpdateWobble()
        {
            // finished wobbling?
            if (!_animator.IsPlaying)
            {
                _damageField.IsActive = false;
                _aiComponent.ChangeState("despawn");
            }
        }

        private void InitDespawn()
        {
            _animator.Play("despawn_" + _strColor);
        }

        private void UpdateDespawn()
        {
            if (!_animator.IsPlaying)
            {
                _aiComponent.ChangeState("idle");
            }
        }

        private void OnHolePull(Vector2 direction, float percentage)
        {
            _aiComponent.ChangeState("holePull");
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            // can only be hit in the wobble state
            if (_aiComponent.CurrentStateId != "spawn" &&
                _aiComponent.CurrentStateId != "wobble")
                return Values.HitCollision.None;

            return _damageState.OnHit(originObject, direction, type, damage, pieceOfPower);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            // can only be pushed in the wobble state
            if (_aiComponent.CurrentStateId != "spawn" &&
                _aiComponent.CurrentStateId != "wobble")
                return false;

            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

            return true;
        }
    }
}
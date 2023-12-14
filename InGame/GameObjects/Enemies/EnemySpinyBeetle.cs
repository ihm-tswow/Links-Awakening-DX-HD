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
    internal class EnemySpinyBeetle : GameObject
    {
        public override bool IsActive
        {
            set
            {
                base.IsActive = value;
                _carriedObject.IsActive = value;
            }
        }

        private readonly GameObject _carriedObject;
        private readonly CarriableComponent _carriableComponent;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly CSprite _sprite;
        private readonly AiDamageState _aiDamageState;
        private readonly AiTriggerTimer _hiddenTimer;
        private readonly DamageFieldComponent _damageField;

        private Rectangle _fieldRectangle;

        // 0: grass
        // 1: stone
        // 2: skull
        private readonly int _type;

        private bool _bushDestroyed;

        public EnemySpinyBeetle() : base("spiny beetle") { }

        public EnemySpinyBeetle(Map.Map map, int posX, int posY, int type) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 7, 0);
            EntitySize = new Rectangle(-8, -7, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/spiny beetle");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -4));

            _fieldRectangle = map.GetField(posX, posY);

            _body = new BodyComponent(EntityPosition, -7, -2, 14, 10, 8)
            {
                MoveCollision = OnCollision,
                HoleAbsorb = OnHoleAbsorb,
                Drag = 0.8f,
                AvoidTypes = Values.CollisionTypes.Hole |
                             Values.CollisionTypes.NPCWall,
                FieldRectangle = _fieldRectangle
            };

            // spawn a bush carried by the beetle
            if (type == 0)
                _carriedObject = new ObjBush(map, posX, posY, null, "bush_0", true, true, false, Values.LayerPlayer, null) { RespawnGras = false };
            else if (type == 1)
                _carriedObject = new ObjStone(map, posX, posY, "stone_0", null, null, null, false, false);
            else
                _carriedObject = new ObjStone(map, posX, posY, "skull", null, null, null, false, false);

            _type = type;

            // deactivate physics
            var body = (BodyComponent)_carriedObject.Components[BodyComponent.Index];
            if (body != null)
            {
                body.IsActive = false;
            }

            _carriableComponent = (CarriableComponent)_carriedObject.Components[CarriableComponent.Index];

            var stateInit = new AiState();
            stateInit.Trigger.Add(new AiTriggerCountdown(1500, null, () => _aiComponent.ChangeState("hiding")));
            var stateHiding = new AiState(UpdateHiding);
            stateHiding.Trigger.Add(_hiddenTimer = new AiTriggerTimer(500));
            var stateMoving = new AiState();
            stateMoving.Trigger.Add(new AiTriggerRandomTime(ToHide, 550, 850));
            var stateRunning = new AiState();
            stateRunning.Trigger.Add(new AiTriggerRandomTime(ChangeDirection, 550, 850));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("init", stateInit);
            _aiComponent.States.Add("hiding", stateHiding);
            _aiComponent.States.Add("moving", stateMoving);
            _aiComponent.States.Add("running", stateRunning);
            new AiFallState(_aiComponent, _body);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, _sprite, 1);

            _aiComponent.ChangeState("moving");

            var damageCollider = new CBox(EntityPosition, -7, -4, 0, 14, 10, 4);
            var hittableRectangle = new CBox(EntityPosition, -6, -4, 12, 10, 8);

            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableRectangle, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, _sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));

            EntityPosition.AddPositionListener(typeof(EnemySpinyBeetle), UpdateObjPosition);
            map.Objects.SpawnObject(_carriedObject);
            UpdateObjPosition(EntityPosition);

            ToHide();

            _aiComponent.ChangeState("init");
        }

        private void UpdateObjPosition(CPosition newPosition)
        {
            if (_aiComponent.CurrentStateId != "hiding" && _aiComponent.CurrentStateId != "moving")
                return;

            var offset = _aiComponent.CurrentStateId == "hiding" ? 0 : 4;
            var offsetY = _type == 0 ? 1 : 6;
            _carriedObject.EntityPosition.Set(new CPosition(newPosition.X, newPosition.Y + offsetY, newPosition.Z + offset));
        }

        private int PlayerDirection()
        {
            var distance = MapManager.ObjLink.EntityPosition.Position - (EntityPosition.Position + new Vector2(0, 9));

            if (_fieldRectangle.Contains(MapManager.ObjLink.PosX, MapManager.ObjLink.PosY))
            {
                // horizontally
                if (Math.Abs(distance.Y) < 8 && distance.Length() < 64)
                    return Math.Sign(distance.X) < 0 ? 0 : 2;
                // down
                if (Math.Abs(distance.X) < 8 && distance.Y > 0 && distance.Y < 32)
                    return 3;
            }

            return -1;
        }

        private void ToHide()
        {
            if (_aiComponent.CurrentStateId != "moving" || (PlayerDirection() >= 0 && _body.LastVelocityCollision == 0))
                return;

            _damageField.IsActive = false;
            _body.VelocityTarget = Vector2.Zero;
            _sprite.IsVisible = false;
            _aiComponent.ChangeState("hiding");

            UpdateObjPosition(EntityPosition);
        }

        private void UpdateHiding()
        {
            var playerDirection = PlayerDirection();
            if (playerDirection >= 0 && _hiddenTimer.State)
            {
                ToWalk();
                _body.VelocityTarget = AnimationHelper.DirectionOffset[playerDirection];
            }

            // object was destroyed or picked up?
            if (_carriedObject.IsDead || (_carriableComponent != null && _carriableComponent.IsPickedUp))
            {
                ToRunning();
                _body.VelocityTarget = Vector2.Zero;
            }
        }

        private void Show()
        {
            _sprite.IsVisible = true;
            _damageField.IsActive = true;
        }

        private void ToWalk()
        {
            Show();
            UpdateObjPosition(EntityPosition);
            _aiComponent.ChangeState("moving");
        }

        private void ToRunning()
        {
            Show();
            ChangeDirection();
            _aiComponent.ChangeState("running");
        }

        private void ChangeDirection()
        {
            var randomDir = Game1.RandomNumber.Next(0, 100);
            var directionRadius = (float)(Math.PI * 2 * (randomDir / 100.0f));
            _body.VelocityTarget = new Vector2((float)Math.Cos(directionRadius), (float)Math.Sin(directionRadius));
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.5f, direction.Y * 1.5f, _body.Velocity.Z);

            return true;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (!_bushDestroyed && _type == 0)
            {
                _bushDestroyed = true;

                if (!_carriedObject.IsDead)
                    ((ObjBush)_carriedObject).DestroyBush(direction);

                if (damageType == HitType.Bomb || damageType == HitType.Bow || damageType == HitType.Hookshot)
                    return Values.HitCollision.Blocking;
            }

            // gets repelled by the stone/skull
            if (_type > 0 && _aiComponent.CurrentStateId != "running")
            {
                _body.Velocity = new Vector3(direction.X * 0.25f, direction.Y * 0.25f, _body.Velocity.Z);
                return Values.HitCollision.RepellingParticle;
            }

            _sprite.IsVisible = true;

            return _aiDamageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            // collided with a wall?
            if ((direction & (Values.BodyCollision.Horizontal | Values.BodyCollision.Vertical)) != 0)
            {
                ToHide();
            }
        }

        private void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 2.0f;
        }
    }
}
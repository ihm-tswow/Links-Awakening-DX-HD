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
    internal class EnemyLikeLike : GameObject
    {
        private readonly Animator _animator;
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly CBox _collisionBox;
        private readonly AiTriggerTimer _collisionTimer;

        private float _moveSpeed = 0.5f;
        private int _direction;
        private bool _hasPlayerTrapped;
        private bool _stoleShield;

        public EnemyLikeLike() : base("like like") { }

        public EnemyLikeLike(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntityPosition.AddPositionListener(typeof(EnemyLikeLike), UpdatePosition);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/likelike");
            _animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.Enemy,
                AvoidTypes = Values.CollisionTypes.Hole |
                             Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                Bounciness = 0.25f,
                Drag = 0.85f,
                AbsorbPercentage = 0.8f
            };

            var stateMove = new AiState(UpdateMoving);
            stateMove.Trigger.Add(new AiTriggerRandomTime(ChangeDirection, 350, 650));
            stateMove.Trigger.Add(_collisionTimer = new AiTriggerTimer(1750));
            var stateTrap = new AiState(UpdateTrap);

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("moving", stateMove);
            _aiComponent.States.Add("trap", stateTrap);
            new AiFallState(_aiComponent, _body, OnHoleAbsorb);
            _damageState = new AiDamageState(this, _body, _aiComponent, sprite, 2);
            _damageState.OnDeath = OnDeath;
            ToMoving();

            // start randomly idle or walking facing a random direction
            _direction = Game1.RandomNumber.Next(0, 4);

            var boxHittable = new CBox(EntityPosition, -7, -14, 14, 14, 8);
            _collisionBox = new CBox(EntityPosition, -5, -10, 10, 8, 2);

            AddComponent(HittableComponent.Index, new HittableComponent(boxHittable, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }

        private void UpdatePosition(CPosition newPosition)
        {
            if (_hasPlayerTrapped)
                MapManager.ObjLink.SetPosition(newPosition.Position);
        }

        private void ToMoving()
        {
            ChangeDirection();
            _animator.SpeedMultiplier = 1.0f;
            _aiComponent.ChangeState("moving");
            _hasPlayerTrapped = false;
        }

        private void UpdateMoving()
        {
            // collided with the player?
            if (_collisionTimer.State && !MapManager.ObjLink.IsTrapped() &&
                _collisionBox.Box.Intersects(MapManager.ObjLink._body.BodyBox.Box))
                ToTrap();
        }

        private void ToTrap()
        {
            MapManager.ObjLink.TrapPlayer();
            MapManager.ObjLink.SetPosition(EntityPosition.Position);

            if (!_stoleShield)
            {
                _stoleShield = MapManager.ObjLink.StealShield();
                if (_stoleShield)
                    _damageState.SpawnItem = "shieldBack";
            }

            _animator.SpeedMultiplier = 2.0f;
            _aiComponent.ChangeState("trap");
            _body.VelocityTarget = Vector2.Zero;
            _hasPlayerTrapped = true;
        }

        private void UpdateTrap()
        {
            if (!MapManager.ObjLink.IsTrapped())
                ToMoving();
        }

        private void ChangeDirection()
        {
            // random new direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * _moveSpeed;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_hasPlayerTrapped && (damageType & HitType.Sword) != 0)
                return Values.HitCollision.None;

            if (_hasPlayerTrapped && (damageType == HitType.Boomerang || damageType == HitType.Bow ||
                                      damageType == HitType.Hookshot || damageType == HitType.MagicRod))
            {
                _hasPlayerTrapped = false;
                MapManager.ObjLink.FreeTrappedPlayer();
                direction = MapManager.ObjLink.ForwardVector;
            }

            return _damageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private void OnDeath(bool pieceOfPower)
        {
            _damageState.BaseOnDeath(pieceOfPower);

            // free the player
            if (_hasPlayerTrapped)
                MapManager.ObjLink.FreeTrappedPlayer();
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction * 1.5f, _body.Velocity.Z);

            return true;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (_aiComponent.CurrentStateId != "moving")
                return;

            if ((direction & (Values.BodyCollision.Horizontal | Values.BodyCollision.Vertical)) != 0)
            {
                _direction = (_direction + 2) % 4;
                _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * _moveSpeed;
            }
        }

        private void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 3f;
        }
    }
}
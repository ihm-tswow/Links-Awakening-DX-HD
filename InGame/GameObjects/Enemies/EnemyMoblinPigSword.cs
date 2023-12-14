using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyMoblinPigSword : GameObject
    {
        private readonly EnemyMoblinPigSwordSword _sword;

        public BodyComponent Body;

        private readonly CSprite _sprite;
        private readonly Animator _animator;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _damageState;
        private readonly BodyDrawComponent _drawComponent;

        private Rectangle _fieldRectangle;

        private const float MoveSpeed = 0.5f;
        private const float AttackMoveSpeed = 0.55f;
        private const int AttackRange = 80;

        private int _direction;

        private bool _isActive = true;
        public override bool IsActive
        {
            set
            {
                _isActive = value;
                _sword.IsActive = value;
            }
            get => _isActive;
        }

        public EnemyMoblinPigSword() : base("moblinPigSword") { }

        public EnemyMoblinPigSword(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/moblinPig");
            _animator.Play("walk_1");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -16));

            _fieldRectangle = map.GetField(posX, posY);

            Body = new BodyComponent(EntityPosition, -6, -10, 12, 10, 8)
            {
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.Enemy,
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                FieldRectangle = _fieldRectangle,
                Bounciness = 0.25f,
                AbsorbPercentage = 0.9f,
                Drag = 0.85f
            };

            var stateIdle = new AiState { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerRandomTime(EndIdle, 300, 500));
            var stateWalk = new AiState { Init = InitWalking };
            stateWalk.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("idle"), 550, 850));
            var stateAttack = new AiState(UpdateAttack);

            _aiComponent = new AiComponent();
            _aiComponent.Trigger.Add(new AiTriggerUpdate(UpdateDamageTick));
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("walking", stateWalk);
            _aiComponent.States.Add("attack", stateAttack);
            new AiFallState(_aiComponent, Body, OnHoleAbsorb, OnAbsorbDeath);
            _damageState = new AiDamageState(this, Body, _aiComponent, _sprite, 2)
            {
                OnDeath = OnDeath,
                OnBurn = OnBurn
            };

            var damageBox = new CBox(EntityPosition, -8, -12, 0, 16, 12, 4);
            var hittableBox = new CBox(EntityPosition, -4, -14, 8, 12, 8);
            var pushableBox = new CBox(EntityPosition, -7, -11, 0, 14, 11, 4);

            _drawComponent = new BodyDrawComponent(Body, _sprite, Values.LayerPlayer);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, _damageState.OnHit));
            AddComponent(BodyComponent.Index, Body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));

            _sword = new EnemyMoblinPigSwordSword(Map, this);
        }

        public override void Init()
        {
            // add the sword to the map
            Map.Objects.SpawnObject(_sword);

            //var playerDirection = MapManager.ObjLink.NextMapPositionEnd.Value - EntityPosition.Position;
            //if (playerDirection.Length() < AttackRange)
            //{
            //    _aiComponent.ChangeState("attack");
            //    // make sure to update the animation to look at the player when he enters the goblin cave
            //    UpdateDirection(playerDirection);
            //}
            //else
            {
                // start randomly idle or walking facing a random direction
                _direction = Game1.RandomNumber.Next(0, 4);
                _aiComponent.ChangeState(Game1.RandomNumber.Next(0, 2) == 0 ? "walking" : "idle");
            }
        }

        private void OnBurn()
        {
            _animator.Pause();
            _sword.Animator.Pause();
        }

        private void UpdateDamageTick()
        {
            _sword.Sprite.SpriteShader = _sprite.SpriteShader;
        }

        private void OnDeath(bool pieceOfPower)
        {
            _damageState.BaseOnDeath(pieceOfPower);

            Map.Objects.DeleteObjects.Add(_sword);
        }

        private void InitIdle()
        {
            Body.VelocityTarget = Vector2.Zero;

            _animator.Play("stand_" + _direction);
            _sword.Animator.Play("stand_" + _direction);
        }

        private void EndIdle()
        {
            var distance = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;

            if (_fieldRectangle.Contains(MapManager.ObjLink.PosX, MapManager.ObjLink.PosY) && distance.Length() < AttackRange)
                _aiComponent.ChangeState("attack");
            else
                _aiComponent.ChangeState("walking");
        }

        private void InitWalking()
        {
            ChangeDirection();
        }

        private void ChangeDirection()
        {
            _animator.SpeedMultiplier = 0.5f;
            _sword.Animator.SpeedMultiplier = 0.5f;

            // random new direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _animator.Play("walk_" + _direction);
            _sword.Animator.Play("walk_" + _direction);
            Body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * MoveSpeed;
        }

        private void UpdateAttack()
        {
            var playerDirection = (MapManager.ObjLink.EntityPosition.Position + AnimationHelper.DirectionOffset[_direction] * 3) - EntityPosition.Position;

            if (!_fieldRectangle.Contains(MapManager.ObjLink.PosX, MapManager.ObjLink.PosY) ||
                playerDirection.Length() > AttackRange)
            {
                _aiComponent.ChangeState("idle");
                return;
            }

            if (playerDirection != Vector2.Zero)
                playerDirection.Normalize();

            Body.VelocityTarget = playerDirection * AttackMoveSpeed;

            UpdateDirection(playerDirection);

            _animator.SpeedMultiplier = 1f;
            _sword.Animator.SpeedMultiplier = 1f;
        }

        private void UpdateDirection(Vector2 direction)
        {
            _direction = AnimationHelper.GetDirection(direction);

            _animator.Play("walk_" + _direction);
            _sword.Animator.Play("walk_" + _direction);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                Body.Velocity = new Vector3(direction.X, direction.Y, Body.Velocity.Z);

            return true;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (_aiComponent.CurrentStateId != "walking")
                return;

            // stop walking
            _aiComponent.ChangeState("idle");
        }

        private void OnHoleAbsorb()
        {
            _animator.Play("walk_" + _direction);
            _sword.Animator.Play("walk_" + _direction);

            _animator.SpeedMultiplier = 3f;
            _sword.Animator.SpeedMultiplier = 3f;
        }

        private void OnAbsorbDeath()
        {
            Map.Objects.DeleteObjects.Add(_sword);
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (_direction == 1)
                ((DrawComponent)_sword.Components[DrawComponent.Index]).Draw(spriteBatch);

            _drawComponent.Draw(spriteBatch);

            if (_direction != 1)
                ((DrawComponent)_sword.Components[DrawComponent.Index]).Draw(spriteBatch);
        }
    }
}
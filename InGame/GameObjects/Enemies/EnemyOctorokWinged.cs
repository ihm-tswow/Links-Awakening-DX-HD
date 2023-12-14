using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyOctorokWinged : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly Animator _animator;
        private readonly AiDamageState _aiDamageState;
        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly AiTriggerSwitch _damageSwitch;

        private readonly Rectangle _wingRectangle = new Rectangle(160, 67, 8, 18);

        private readonly Vector2[] _shotOffset =
        {
            new Vector2(-8, -1),new Vector2(0, -6),
            new Vector2(8, -1),new Vector2(0, 11)
        };

        private readonly Rectangle _fieldRectangle;

        private float _walkSpeed = 0.5f;
        private int _direction;
        private float _flyCounter;

        public EnemyOctorokWinged() : base("winged octorok") { }

        public EnemyOctorokWinged(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 15, 0);
            EntitySize = new Rectangle(-8, -15 - 16, 16, 32);

            _animator = AnimatorSaveLoad.LoadAnimator("Enemies/octorok");
            _fieldRectangle = map.GetField(posX, posY);

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-8, -15));

            _body = new BodyComponent(EntityPosition, -7, -12, 14, 12, 8)
            {
                MoveCollision = OnCollision,
                AbsorbPercentage = 0.9f,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Enemy,
                AvoidTypes = Values.CollisionTypes.Hole |
                             Values.CollisionTypes.NPCWall,
                FieldRectangle = _fieldRectangle,
                Bounciness = 0.25f,
                Drag = 0.85f,
                Gravity = -0.04f,
            };

            var stateIdle = new AiState { Init = InitIdle };
            stateIdle.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("walking"), 250, 500));
            var stateWalking = new AiState { Init = InitWalking };
            stateWalking.Trigger.Add(new AiTriggerRandomTime(() => _aiComponent.ChangeState("idle"), 750, 1000));
            var stateFlying = new AiState(UpdateFlying) { Init = InitFlying };

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("walking", stateWalking);
            _aiComponent.States.Add("flying", stateFlying);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, sprite, 1) { OnBurn = () => _animator.Pause() };
            _aiComponent.Trigger.Add(_damageSwitch = new AiTriggerSwitch(350));
            new AiFallState(_aiComponent, _body, OnHoleAbsorb);

            // random start position/state
            _direction = Game1.RandomNumber.Next(0, 4);
            _animator.Play("walk_" + _direction);
            _aiComponent.ChangeState(Game1.RandomNumber.Next(0, 2) == 0 ? "idle" : "walking");

            var damageCollider = new CBox(EntityPosition, -8, -13, 4, 16, 13, 4);
            var hittableBox = new CBox(EntityPosition, -7, -15, 0, 14, 15, 8, true);
            var pushableBox = new CBox(EntityPosition, -7, -13, 0, 14, 13, 4, true);

            _bodyDrawComponent = new BodyDrawComponent(_body, sprite, Values.LayerPlayer);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageCollider, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(pushableBox, OnPush));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, sprite) { Height = 1.0f, Rotation = 0.1f, ShadowWidth = 10, ShadowHeight = 5 });
        }

        private void InitIdle()
        {
            _animator.Play("stand_" + _direction);
            _body.VelocityTarget = new Vector2(0, 0);

            // shoot if the player is in the range and in the right direction
            var playerDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            if (playerDirection.Length() < 80)
            {
                if (playerDirection != Vector2.Zero)
                    playerDirection.Normalize();
                var direction = AnimationHelper.GetDirection(playerDirection);

                if (direction == _direction)
                {
                    // shoot
                    var shot = new EnemyOctorokShot(Map,
                        EntityPosition.X + _shotOffset[_direction].X,
                        EntityPosition.Y + _shotOffset[_direction].Y,
                        AnimationHelper.DirectionOffset[_direction] * 2f);
                    Map.Objects.SpawnObject(shot);
                }
            }
        }

        private void InitWalking()
        {
            // random new direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _animator.Play("walk_" + _direction);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * _walkSpeed;
        }

        //private void UpdateWalking()
        //{
        //    _aiComponent.ChangeState("flying");

        //    _body.VelocityTarget = Vector2.Zero;
        //}

        private void InitFlying()
        {
            // fly towards the player
            var vecDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            vecDirection.Normalize();

            _body.VelocityTarget = vecDirection;
            _body.Velocity.Z = 1.25f;
            _body.AvoidTypes = Values.CollisionTypes.NPCWall;
            _body.FieldRectangle = RectangleF.Empty;
        }

        private void UpdateFlying()
        {
            _flyCounter += Game1.DeltaTime;

            // face the player
            var vecDirection = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            _direction = AnimationHelper.GetDirection(vecDirection);
            _animator.Play("walk_" + _direction);

            if (_body.IsGrounded && _body.Velocity.Z <= 0)
            {
                _flyCounter = 0;
                _aiComponent.ChangeState("idle");
                _damageSwitch.Reset();
                _body.AvoidTypes = Values.CollisionTypes.Hole |
                                   Values.CollisionTypes.NPCWall;
                _body.FieldRectangle = _fieldRectangle;
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the wings
            spriteBatch.Draw(Resources.SprEnemies, new Vector2(EntityPosition.X - _wingRectangle.Width - 5, EntityPosition.Y - 8 - EntityPosition.Z),
                _wingRectangle, Color.White, 0, new Vector2(0, 9), Vector2.One,
                (int)_flyCounter % 132 < 66 ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
            spriteBatch.Draw(Resources.SprEnemies, new Vector2(EntityPosition.X + 5, EntityPosition.Y - 8 - EntityPosition.Z),
                _wingRectangle, Color.White, 0, new Vector2(0, 9), Vector2.One,
                ((int)_flyCounter % 132 < 66 ? SpriteEffects.FlipVertically : SpriteEffects.None) | SpriteEffects.FlipHorizontally, 0);

            // draw the body
            _bodyDrawComponent.Draw(spriteBatch);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if ((damageType & HitType.Sword) != 0 && !_body.IsGrounded)
                return Values.HitCollision.None;

            // start flying over the player if the octorok is facing him
            var playerDirection = AnimationHelper.GetDirection(
                MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position);

            if (_direction != (playerDirection + 2) % 4 && _damageSwitch.State &&
                (_aiComponent.CurrentStateId == "walking" || _aiComponent.CurrentStateId == "idle") &&
                damageType != HitType.PegasusBootsSword &&
                damageType != HitType.Bow &&
                damageType != HitType.Hookshot &&
                damageType != HitType.MagicRod &&
                damageType != HitType.MagicPowder &&
                damageType != HitType.Boomerang)
            {
                _aiComponent.ChangeState("flying");
                return Values.HitCollision.None;
            }

            return _aiDamageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _body.Velocity.Z);

            return true;
        }

        private void OnCollision(Values.BodyCollision direction)
        {
            if (_aiComponent.CurrentStateId == "walking")
                _aiComponent.ChangeState("idle");
        }

        private void OnHoleAbsorb()
        {
            _animator.SpeedMultiplier = 3f;
            _animator.Play("walk_" + _direction);
        }
    }
}
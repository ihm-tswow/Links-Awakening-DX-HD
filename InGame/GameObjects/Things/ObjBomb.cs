using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjBomb : GameObject
    {
        public BodyComponent Body;
        public bool DamageEnemies;

        private readonly Animator _animator;
        private readonly BodyDrawShadowComponent _bodyShadow;
        private readonly CarriableComponent _carriableComponent;
        private readonly BodyDrawComponent _drawComponent;

        private readonly bool _playerBomb;
        private readonly bool _floorExplode;

        public const int BlinkTime = 1000 / 60 * 4;

        private const int ExplosionTime = 1500;

        private double _bombCounter;
        private double _explostionTime;
        private double _lastHitTime;
        private double _deepWaterCounter;

        private bool _exploded;
        private bool _arrowMode;

        public ObjBomb(Map.Map map, float posX, float posY, bool playerBomb, bool floorExplode, int explosionTime = ExplosionTime) : base(map)
        {
            if (!map.Is2dMap)
                EntityPosition = new CPosition(posX, posY, 5);
            else
                EntityPosition = new CPosition(posX, posY - 5, 0);

            EntitySize = new Rectangle(-8, -16, 16, 20);

            Body = new BodyComponent(EntityPosition, -4, -8, 8, 8, 4)
            {
                Bounciness = 0.5f,
                Bounciness2D = 0.5f,
                Drag = 0.85f,
                DragAir = 1.0f,
                DragWater = 0.985f,
                Gravity = -0.15f,
                HoleAbsorb = FallDeath,
                MoveCollision = OnCollision,
                IgnoreInsideCollision = false,
            };

            if (map.Is2dMap)
            {
                Body.OffsetY = -1;
                Body.Height = 1;
            }

            _playerBomb = playerBomb;
            // explode on collision with the floor
            _floorExplode = floorExplode;

            _animator = AnimatorSaveLoad.LoadAnimator("Objects/bomb");
            _animator.OnAnimationFinished = FinishedAnimation;
            _animator.Play("idle");

            _explostionTime = explosionTime;
            _bombCounter = _explostionTime;

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, Vector2.Zero);
            _drawComponent = new BodyDrawComponent(Body, sprite, Values.LayerPlayer);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(HittableComponent.Index, new HittableComponent(new CBox(EntityPosition, -4, -10, 8, 10, 8), OnHit));
            AddComponent(BodyComponent.Index, Body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);

            // can not push away the bombs from enemies; would probably be fun
            if (playerBomb)
                AddComponent(PushableComponent.Index, new PushableComponent(Body.BodyBox, OnPush) { RepelMultiplier = 0.5f });

            AddComponent(CarriableComponent.Index, _carriableComponent = new CarriableComponent(
                new CRectangle(EntityPosition, new Rectangle(-4, -8, 8, 8)), CarryInit, CarryUpdate, CarryThrow));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, _bodyShadow = new BodyDrawShadowComponent(Body, sprite));
        }

        private void Update()
        {
            if (_exploded)
            {
                // use the collision data from the animation to deal damage
                if (!_playerBomb)
                {
                    var collisionRect = _animator.CollisionRectangle;
                    if (collisionRect != Rectangle.Empty)
                    {
                        var collisionBox = new Box(
                            EntityPosition.X + collisionRect.X,
                            EntityPosition.Y + collisionRect.Y, 0,
                            collisionRect.Width, collisionRect.Height, 16);

                        if (collisionBox.Intersects(MapManager.ObjLink._body.BodyBox.Box))
                            MapManager.ObjLink.HitPlayer(collisionBox, HitType.Bomb, 4);
                    }
                }

                // remove bomb if the animation is finished
                if (!_animator.IsPlaying)
                    Map.Objects.DeleteObjects.Add(this);
            }
            else
            {
                // blink
                if (_bombCounter < 500)
                {
                    if (_bombCounter % (BlinkTime * 2) < BlinkTime)
                        _animator.Play("blink");
                    else
                        _animator.Play("idle");
                }

                _bombCounter -= Game1.DeltaTime;
                if (_bombCounter <= 0)
                    Explode();
            }

            // fall into the water
            if (!Map.Is2dMap && Body.IsGrounded && Body.CurrentFieldState.HasFlag(MapStates.FieldStates.DeepWater))
            {
                _deepWaterCounter -= Game1.DeltaTime;

                if (_deepWaterCounter <= 0)
                {
                    // spawn splash effect
                    var fallAnimation = new ObjAnimator(Map,
                        (int)(Body.Position.X + Body.OffsetX + Body.Width / 2.0f),
                        (int)(Body.Position.Y + Body.OffsetY + Body.Height / 2.0f),
                        Values.LayerPlayer, "Particles/fishingSplash", "idle", true);
                    Map.Objects.SpawnObject(fallAnimation);

                    Map.Objects.DeleteObjects.Add(this);
                }
            }
            else if (Body.IsGrounded)
            {
                _deepWaterCounter = 75;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _drawComponent.Draw(spriteBatch);
        }

        private void FallDeath()
        {
            // play sound effect
            Game1.GameManager.PlaySoundEffect("D360-24-18");

            var fallAnimation = new ObjAnimator(Map, 0, 0, Values.LayerBottom, "Particles/fall", "idle", true);
            fallAnimation.EntityPosition.Set(new Vector2(
                Body.Position.X + Body.OffsetX + Body.Width / 2.0f - 5,
                Body.Position.Y + Body.OffsetY + Body.Height / 2.0f - 5));
            Map.Objects.SpawnObject(fallAnimation);

            Map.Objects.DeleteObjects.Add(this);
        }

        private Vector3 CarryInit()
        {
            _animator.Play("idle");

            // the bomb was picked up
            Body.IsActive = false;

            return EntityPosition.ToVector3();
        }

        private bool CarryUpdate(Vector3 newPosition)
        {
            _bombCounter = ExplosionTime;

            EntityPosition.X = newPosition.X;

            if (!Map.Is2dMap)
            {
                EntityPosition.Y = newPosition.Y;
                EntityPosition.Z = newPosition.Z;
            }
            else
            {
                EntityPosition.Y = newPosition.Y - newPosition.Z;
                EntityPosition.Z = 0;
            }

            EntityPosition.NotifyListeners();
            return true;
        }

        private void CarryThrow(Vector2 velocity)
        {
            Body.Drag = 0.75f;
            Body.DragAir = 1.0f;
            Body.IsGrounded = false;
            Body.IsActive = true;
            if (_playerBomb)
                Body.Level = MapStates.GetLevel(MapManager.ObjLink._body.CurrentFieldState);

            // do not throw the bomb up when the player lets it fall down (e.g. by walking into a door)
            if (velocity == Vector2.Zero)
                Body.Velocity = Vector3.Zero;
            else
                Body.Velocity = new Vector3(velocity.X * 0.45f, velocity.Y * 0.45f, 1.25f);

            if (Map.Is2dMap)
                Body.Velocity.Y = -0.75f;

            Body.CollisionTypesIgnore = Values.CollisionTypes.ThrowWeaponIgnore;

            _carriableComponent.IsActive = false;
        }

        public void Explode()
        {
            _exploded = true;
            Body.Velocity = Vector3.Zero;
            Body.IsActive = false;
            _bodyShadow.IsActive = false;
            _carriableComponent.IsActive = false;

            // deals damage to the player or to the enemies
            if (_playerBomb || DamageEnemies)
                Map.Objects.Hit(this, new Vector2(EntityPosition.X, EntityPosition.Y),
                    new Box(EntityPosition.X - 20, EntityPosition.Y - 20 - 5, 0, 40, 40, 16), HitType.Bomb, 2, false);

            Game1.GameManager.PlaySoundEffect("D378-12-0C");

            _animator.Play("explode");
            _animator.SetFrame(1);

            // shake the screen
            Game1.GameManager.ShakeScreen(150, 8, 2, 5, 2.5f);
        }

        private void FinishedAnimation()
        {
            // explode after the idle animation is finished
            if (_animator.CurrentAnimation.Id == "idle")
                Explode();
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if (Map.Is2dMap)
            {
                if ((collision & Values.BodyCollision.Horizontal) != 0)
                {
                    Body.Velocity.X = -Body.Velocity.X * 0.25f;
                    Game1.GameManager.PlaySoundEffect("D360-09-09");
                }
                if ((collision & Values.BodyCollision.Bottom) != 0 && Body.Velocity.Y < -0.075f)
                {
                    Body.DragAir *= 0.975f;
                    _carriableComponent.IsActive = true;
                    Game1.GameManager.PlaySoundEffect("D360-09-09");
                }
            }

            if ((collision & Values.BodyCollision.Floor) != 0)
            {
                if (Body.Velocity.Z > 0.5f)
                    Game1.GameManager.PlaySoundEffect("D360-09-09");

                //Body.Level = 0;
                Body.Drag *= 0.8f;
                _carriableComponent.IsActive = true;

                if (_floorExplode && Body.Velocity.Z <= 0)
                    Explode();
            }
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (_exploded)
                return false;

            // push the bomb away
            if (type == PushableComponent.PushType.Impact)
            {
                Body.Drag = 0.85f;
                Body.Velocity = new Vector3(direction.X * 1.5f, direction.Y * 1.5f, Body.Velocity.Z);
                return true;
            }

            return false;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // got picked up by an arrow?
            if (_playerBomb && _bombCounter + 175 > _explostionTime && gameObject is ObjArrow objArrow)
            {
                _arrowMode = true;
                Body.IgnoresZ = true;
                Body.IgnoreHoles = true;
                Body.Velocity = Vector3.Zero;
                EntityPosition.Z = 0;
                objArrow.InitBombMode(this);
            }

            if (_arrowMode)
                return Values.HitCollision.None;

            if (_exploded || (_lastHitTime != 0 && Game1.TotalGameTime - _lastHitTime < 250) || damageType == HitType.Bow)
                return Values.HitCollision.None;

            _lastHitTime = Game1.TotalGameTime;

            Body.Drag = 0.85f;
            Body.DragAir = 0.85f;
            Body.Velocity.X += direction.X * 4;
            Body.Velocity.Y += direction.Y * 4;

            if (Map.Is2dMap)
            {
                Body.DragAir = 0.925f;
                Body.Velocity.Y = direction.Y * 2f;
            }

            return Values.HitCollision.Blocking;
        }
    }
}
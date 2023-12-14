using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemySpark : GameObject
    {
        private readonly BodyComponent _body;

        private readonly Color _lightColor = new Color(255, 255, 255);

        private Vector2 _lastPosition;
        private string _destructionKey;

        private double _directionChangeTime;
        private float _lightState;
        private float _lightCount;
        private int _moveDir;
        private bool _goingClockwise;
        private bool _wasTouchingWall;
        private bool _gettingDestroyed;
        private bool _init;

        public EnemySpark() : base("spark") { }

        public EnemySpark(Map.Map map, int posX, int posY, int direction, bool clockwise, string destructionKey) : base(map)
        {
            // maybe create a new tag for enemies that should be ignored by the enemy trigger
            Tags = Values.GameObjectTag.Damage;

            EntityPosition = new CPosition(posX + 8, posY + 8, 0);
            EntitySize = new Rectangle(-32, -32, 64, 64);

            _lastPosition = EntityPosition.Position;

            _moveDir = direction;
            _goingClockwise = clockwise;
            _destructionKey = destructionKey;

            var animator = AnimatorSaveLoad.LoadAnimator("Enemies/spark");
            animator.Play("idle");

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(animator, sprite, new Vector2(-8, -8));

            _body = new BodyComponent(EntityPosition, -4, -4, 8, 8, 8)
            {
                FieldRectangle = map.GetField(posX, posY),
                MoveCollision = OnCollision,
                IgnoreHeight = true,
                IgnoresZ = true,
                CollisionTypes =
                    Values.CollisionTypes.Normal |
                    Values.CollisionTypes.Hole |
                    Values.CollisionTypes.NPCWall
            };

            var damageBox = new CBox(EntityPosition, -8, -8, 0, 16, 16, 4);
            var hittableBox = new CBox(EntityPosition, -6, -6, 12, 12, 8);

            if (!string.IsNullOrEmpty(destructionKey))
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChanged));

            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(_body, sprite, Values.LayerPlayer) { WaterOutline = false });
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));
        }

        public override void Init()
        {
            _init = true;
            base.Init();
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (damageType == HitType.Boomerang)
            {
                Game1.GameManager.PlaySoundEffect("D360-03-03");
                Destroy();
                return Values.HitCollision.Enemy;
            }

            return Values.HitCollision.Blocking;
        }

        private void OnKeyChanged()
        {
            if (_gettingDestroyed || !_init)
                return;

            var keyState = Game1.GameManager.SaveManager.GetString(_destructionKey);
            if (keyState == "1")
                Destroy();
        }

        private void Destroy()
        {
            _gettingDestroyed = true;

            // remove the enemy
            Map.Objects.DeleteObjects.Add(this);

            // spawn explosion effect that ends in a fairy spawning
            var animationExplosion = new ObjAnimator(Map, (int)EntityPosition.X - 8, (int)EntityPosition.Y - 8, Values.LayerTop, "Particles/spawn", "run", true);
            animationExplosion.Animator.OnAnimationFinished = () =>
            {
                // remove the explosion animation
                animationExplosion.Map.Objects.DeleteObjects.Add(animationExplosion);
                // spawn fairy
                animationExplosion.Map.Objects.SpawnObject(new ObjDungeonFairy(animationExplosion.Map, (int)EntityPosition.X, (int)EntityPosition.Y + 4, 0));
            };

            Map.Objects.SpawnObject(animationExplosion);
        }

        private void OnCollision(Values.BodyCollision collider)
        {
            if ((collider & Values.BodyCollision.Vertical) != 0 && AnimationHelper.DirectionOffset[_moveDir].Y != 0 ||
               (collider & Values.BodyCollision.Horizontal) != 0 && AnimationHelper.DirectionOffset[_moveDir].X != 0)
                _moveDir = (_moveDir + (_goingClockwise ? 3 : 1)) % 4;
        }

        private void Update()
        {
            _lightCount += Game1.DeltaTime;
            _lightState = (int)(Math.Sin(_lightCount / 10f) + 1.5);

            // if the body is not sliding on the wall anymore change the direction
            var positionChange = (EntityPosition.Position - _lastPosition) * AnimationHelper.DirectionOffset[(_moveDir + 1) % 4];
            if (positionChange.Length() > 0.0f && (_directionChangeTime + 250 <= Game1.TotalGameTime || _wasTouchingWall))
            {
                _directionChangeTime = Game1.TotalGameTime;
                _moveDir = (_moveDir + (_goingClockwise ? 1 : 3)) % 4;
            }

            _wasTouchingWall = positionChange.Length() == 0.0f;

            var moveVelocity = new Vector2(
                AnimationHelper.DirectionOffset[_moveDir].X + AnimationHelper.DirectionOffset[(_moveDir + (_goingClockwise ? 1 : 3)) % 4].X * 0.25f,
                AnimationHelper.DirectionOffset[_moveDir].Y + AnimationHelper.DirectionOffset[(_moveDir + (_goingClockwise ? 1 : 3)) % 4].Y * 0.25f);
            _body.VelocityTarget = moveVelocity;

            _lastPosition = EntityPosition.Position;
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawLight(spriteBatch, new Rectangle((int)EntityPosition.X - 32, (int)EntityPosition.Y - 32, 64, 64),
                _lightColor * (0.125f + _lightState * 0.25f));
        }
    }
}
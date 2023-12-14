using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    internal class EnemyMiniMoldorm : GameObject
    {
        private readonly AiComponent _aiComp;
        private readonly BodyComponent _bodyComp;
        private readonly BodyDrawComponent _bodyDrawComp;
        private readonly CSprite _sprite;
        private readonly DictAtlasEntry _spriteHead0;
        private readonly DictAtlasEntry _spriteHead1;
        private readonly DictAtlasEntry _spritePart0;
        private readonly DictAtlasEntry _spritePart1;

        private Vector2 _tailOnePosition;
        private Vector2 _tailTwoPosition;

        private float _directionChangeMultiplier;
        private float _direction;
        private float _changeDirCount;
        private int _dir = 1;
        private const int SpriteOffsetY = 7;

        public EnemyMiniMoldorm(Map.Map map, int posX, int posY) : base(map, "miniMoldormHead0")
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 8 + SpriteOffsetY, 0);
            EntitySize = new Rectangle(-20, -20 - SpriteOffsetY, 40, 40);
            _tailOnePosition = EntityPosition.Position;
            _tailTwoPosition = EntityPosition.Position;

            _spriteHead0 = Resources.GetSprite("miniMoldormHead0");
            _spriteHead1 = Resources.GetSprite("miniMoldormHead1");
            _spritePart0 = Resources.GetSprite("miniMoldormPart0");
            _spritePart1 = Resources.GetSprite("miniMoldormPart1");

            _sprite = new CSprite("miniMoldormHead0", EntityPosition, new Vector2(0, -SpriteOffsetY)) { Center = new Vector2(8, 8) };

            _bodyComp = new BodyComponent(EntityPosition, -5, -5 - SpriteOffsetY, 10, 10, 8)
            {
                MoveCollision = OnCollision,
                HoleAbsorb = OnHoleAbsorb,
                AbsorbPercentage = 1f,
                Gravity = -0.1f,
                DragAir = 1.0f,
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY)
            };

            _aiComp = new AiComponent();

            var stateWalking = new AiState(Update);
            _aiComp.States.Add("walking", stateWalking);
            var damageState = new AiDamageState(this, _bodyComp, _aiComp, _sprite, 2, false)
            {
                FlameOffset = new Point(0, 10 - SpriteOffsetY),
                UpdateLastStateFire = true
            };

            _aiComp.ChangeState("walking");

            var damageBox = new CBox(EntityPosition, -6, -6 - SpriteOffsetY, 12, 12, 4);
            var hittableBox = new CBox(EntityPosition, -6, -6 - SpriteOffsetY, 12, 12, 8);

            AddComponent(AiComponent.Index, _aiComp);
            AddComponent(BodyComponent.Index, _bodyComp);
            AddComponent(PushableComponent.Index, new PushableComponent(_bodyComp.BodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, damageState.OnHit));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            _bodyDrawComp = new BodyDrawComponent(_bodyComp, _sprite, Values.LayerPlayer);
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
        }

        private void UpdateHeadSprite(Vector2 direction)
        {
            // calculate the sprite used
            //var direction = MapManager.ObjLink.EntityPosition.Position - EntityPosition.Position;
            //direction.Normalize();
            //_direction = -(float)Math.Atan2(direction.Y, direction.X) + (float)Math.PI;

            var modRotation = (MathF.Abs(_direction)) % (MathF.PI / 2);
            var sprite = MathF.PI / 8 < modRotation && modRotation < MathF.PI / 2 - MathF.PI / 8;
            _sprite.SourceRectangle = sprite ? _spriteHead1.ScaledRectangle : _spriteHead0.ScaledRectangle;

            // rotation of the sprite
            var dir = AnimationHelper.GetDirection(direction, MathF.PI * (9 / 8f));
            _sprite.Rotation = dir * (float)Math.PI / 2;
        }

        private void Update()
        {
            _changeDirCount -= Game1.DeltaTime;

            if (_changeDirCount < 0)
                ChangeDirection();

            _direction += _dir * 0.04f * Game1.TimeMultiplier;

            if (_direction < 0)
                _direction += (float)(Math.PI * 2);

            // move
            var vecDirection = new Vector2((float)Math.Sin(_direction), (float)Math.Cos(_direction));
            _bodyComp.VelocityTarget = vecDirection * 0.75f;

            if (_aiComp.CurrentStateId == "burning")
                _bodyComp.VelocityTarget = Vector2.Zero;

            _directionChangeMultiplier = AnimationHelper.MoveToTarget(_directionChangeMultiplier, 1, 0.025f * Game1.TimeMultiplier);

            UpdateHeadSprite(vecDirection);

            UpdateTailPositions();
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // change the draw effect
            if (_sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, _sprite.SpriteShader);
            }

            // draw the tail
            var partTwoRectangle = _spritePart1.ScaledRectangle;
            var posTwo = _tailTwoPosition - new Vector2(partTwoRectangle.Width / 2f, partTwoRectangle.Height / 2f);
            spriteBatch.Draw(Resources.SprEnemies, posTwo, partTwoRectangle, Color.White);

            var partOneRectangle = _spritePart0.ScaledRectangle;
            var posOne = _tailOnePosition - new Vector2(partOneRectangle.Width / 2f, partOneRectangle.Height / 2f);
            spriteBatch.Draw(Resources.SprEnemies, posOne, partOneRectangle, Color.White);

            // draw the head
            _bodyDrawComp.Draw(spriteBatch);

            // change the draw effect
            if (_sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
            }
        }

        private void UpdateTailPositions()
        {
            // first
            var goalPosition = new Vector2(EntityPosition.X, EntityPosition.Y - SpriteOffsetY) -
                               new Vector2((float)Math.Sin(_direction), (float)Math.Cos(_direction)) * 1.0f;
            var direction = _tailOnePosition - goalPosition;
            var clampLength = MathHelper.Clamp(direction.Length(), 0, 5.5f);

            if (direction != Vector2.Zero)
                direction.Normalize();
            _tailOnePosition = goalPosition + direction * clampLength;

            // second
            direction = _tailTwoPosition - _tailOnePosition;
            clampLength = MathHelper.Clamp(direction.Length(), 0, 6);

            if (direction != Vector2.Zero)
                direction.Normalize();
            _tailTwoPosition = _tailOnePosition + direction * clampLength;
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            if (Game1.RandomNumber.Next(0, 2) == 0)
                _dir = -_dir;

            if ((collision & Values.BodyCollision.Horizontal) != 0)
                _direction = (float)Math.Atan2(-_bodyComp.VelocityTarget.X * _directionChangeMultiplier, _bodyComp.VelocityTarget.Y);
            else if ((collision & Values.BodyCollision.Vertical) != 0)
                _direction = (float)Math.Atan2(_bodyComp.VelocityTarget.X, -_bodyComp.VelocityTarget.Y * _directionChangeMultiplier);

            _directionChangeMultiplier *= 0.5f;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _bodyComp.Velocity = new Vector3(direction.X * 1.75f, direction.Y * 1.75f, _bodyComp.Velocity.Z);

            return true;
        }

        private void ChangeDirection()
        {
            _changeDirCount = Game1.RandomNumber.Next(2000, 4500);
            _dir = -_dir;
        }

        private void OnHoleAbsorb()
        {
            // absorb the tail
            _tailOnePosition = Vector2.Lerp(_tailOnePosition, new Vector2(EntityPosition.X, EntityPosition.Y - SpriteOffsetY), 0.15f * Game1.TimeMultiplier);
            _tailTwoPosition = Vector2.Lerp(_tailTwoPosition, _tailOnePosition, 0.15f * Game1.TimeMultiplier);

            if ((new Vector2(EntityPosition.X, EntityPosition.Y - SpriteOffsetY) - _tailTwoPosition).Length() > 2)
                return;

            Map.Objects.DeleteObjects.Add(this);

            var fallAnimation = new ObjAnimator(Map, (int)EntityPosition.X - 5, (int)EntityPosition.Y - 5 - SpriteOffsetY, Values.LayerBottom, "Particles/fall", "idle", true);
            Map.Objects.SpawnObject(fallAnimation);
        }
    }
}
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.InGame.GameObjects.Enemies
{
    internal class EnemyPokey : GameObject
    {
        private readonly BodyComponent _body;
        private readonly AiComponent _aiComponent;
        private readonly AiDamageState _aiDamageState;
        private readonly CSprite _sprite;

        private readonly DictAtlasEntry _spriteHead;
        private readonly DictAtlasEntry _spriteBody;

        private float _moveSpeed = 1 / 3f;
        private int _direction;
        private int _state;

        public EnemyPokey() : base("pokey") { }

        public EnemyPokey(Map.Map map, int posX, int posY) : base(map)
        {
            Tags = Values.GameObjectTag.Enemy;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-10, -48, 20, 48);

            _spriteHead = Resources.GetSprite("pokey");
            _spriteBody = Resources.GetSprite("pokey body");

            _sprite = new CSprite("pokey body", EntityPosition);
            _body = new BodyComponent(EntityPosition, -7, -14, 14, 14, 8)
            {
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.Enemy,
                AvoidTypes = Values.CollisionTypes.Hole | Values.CollisionTypes.NPCWall,
                FieldRectangle = map.GetField(posX, posY),
                Gravity = -0.15f,
                Bounciness = 0.35f,
                Drag = 0.8f,
                DragAir = 0.8f,
                MaxJumpHeight = 4f,
                IgnoreHeight = true
            };

            var stateMoving = new AiState { Init = InitWalking };
            stateMoving.Trigger.Add(new AiTriggerRandomTime(ChangeDirection, 550, 850));

            _aiComponent = new AiComponent();
            _aiComponent.States.Add("moving", stateMoving);
            new AiFallState(_aiComponent, _body, null);
            _aiDamageState = new AiDamageState(this, _body, _aiComponent, _sprite, 4);

            _aiComponent.ChangeState("moving");

            var damageBox = new CBox(EntityPosition, -7, -14, 0, 14, 14, 16);
            var hittableBox = new CBox(EntityPosition, -7, -14, 14, 14, 24);

            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(PushableComponent.Index, new PushableComponent(_body.BodyBox, OnPush));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite) { ShadowWidth = 10, ShadowHeight = 5 });
        }

        private void InitWalking()
        {
            ChangeDirection();
        }

        private void ChangeDirection()
        {
            // random new direction
            _direction = Game1.RandomNumber.Next(0, 4);
            _body.VelocityTarget = AnimationHelper.DirectionOffset[_direction] * _moveSpeed;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_aiDamageState.IsInDamageState())
                return Values.HitCollision.None;

            if (damageType == HitType.Bomb || damageType == HitType.Bow)
                damage /= 2;

            if ((damageType & HitType.Sword2) != 0 ||
                damageType == HitType.Hookshot ||
                damageType == HitType.MagicPowder ||
                damageType == HitType.MagicRod ||
                pieceOfPower)
                damage *= 2;

            var hitType = _aiDamageState.OnHit(gameObject, direction, damageType, damage, pieceOfPower);

            if (_aiDamageState.CurrentLives > 0)
            {
                _state += 1;
                if (_state <= 2)
                {
                    EntityPosition.Z = 14;
                    _body.Velocity.Z = -0.5f;

                    var bodyPart = new EnemyPokeyPart(Map, EntityPosition.X, EntityPosition.Y, direction * 2f, _body.Velocity);
                    Map.Objects.SpawnObject(bodyPart);
                }
            }

            if (_state == 2)
                _sprite.SetSprite(_spriteHead);

            return hitType;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                _body.Velocity = new Vector3(direction.X, direction.Y, _body.Velocity.Z);

            return true;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // change the draw effect
            if (_sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, _sprite.SpriteShader);
            }

            // draw the body
            var posY = EntityPosition.Y - EntityPosition.Z;
            if (_state == 0)
            {
                DrawHelper.DrawNormalized(spriteBatch, _spriteBody, new Vector2(EntityPosition.X, posY), _sprite.Color);
                posY -= 12;
            }

            var offsetX = 0.0f;
            if (_state <= 1)
            {
                // dont wobble at the floor
                if (_state == 0)
                    offsetX = (float)Math.Sin(Game1.TotalGameTime * 0.0125);

                DrawHelper.DrawNormalized(spriteBatch, _spriteBody, new Vector2(EntityPosition.X + offsetX, posY), _sprite.Color);
                posY -= 12;
            }

            // draw the head
            offsetX = -(float)Math.Sin(Game1.TotalGameTime * 0.0125) * (_state == 0 ? 2 : 1);
            DrawHelper.DrawNormalized(spriteBatch, _spriteHead, new Vector2(EntityPosition.X + offsetX, posY), _sprite.Color);

            // make sure to also move the shadow
            if (_state >= 2)
                _sprite.DrawOffset.X = offsetX;

            // change the draw effect
            // this would not be very efficient if a lot of sprite used effects
            if (_sprite.SpriteShader != null)
            {
                spriteBatch.End();
                ObjectManager.SpriteBatchBegin(spriteBatch, null);
            }
        }
    }
}
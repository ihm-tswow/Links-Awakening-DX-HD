using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.Components.AI;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjBall : GameObject
    {
        private readonly DamageFieldComponent _damageField;
        private readonly AiComponent _aiComponent;
        private readonly AiTriggerSwitch _repelSwitch;
        private readonly DrawCSpriteComponent _drawComponent;
        private readonly BodyDrawShadowComponent _shadowComponent;
        private readonly BodyComponent _body;
        private readonly CBox _damageBox;

        private readonly Vector2 _spawnPosition;

        private readonly string _saveStringPosX;
        private readonly string _saveStringPosY;

        private bool _hitEnemies;
        private bool _absorbed;
        private bool _hasMoved;

        public ObjBall() : base("ball") { }

        public ObjBall(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -32, 16, 32);

            _saveStringPosX = saveKey + "_posX";
            _saveStringPosY = saveKey + "_posY";

            _spawnPosition = EntityPosition.Position;

            // load the position of the ball if it was already picked up
            LoadPosition();

            // this is the same size as the player so that it can not get thrown into the wall
            _body = new BodyComponent(EntityPosition, -4, -10, 8, 10, 14)
            {
                CollisionTypes = Values.CollisionTypes.Normal | Values.CollisionTypes.NPCWall,
                CollisionTypesIgnore = Values.CollisionTypes.ThrowIgnore,
                MoveCollision = Collision,
                DragAir = 1.0f,
                Gravity = -0.125f,
                Bounciness = 0.6f,
                HoleAbsorb = OnHoleAbsorb,
                //MaxJumpHeight = 3, // make sure that we can not throw the ball over a barrier
            };

            var cSprite = new CSprite("ball", EntityPosition, new Vector2(-7, -15));

            var stateIdle = new AiState(UpdateIdle);
            var stateAbsorb = new AiState();
            stateAbsorb.Trigger.Add(new AiTriggerCountdown(100, null, EndAbsorb));
            var stateWaiting = new AiState();
            stateWaiting.Trigger.Add(new AiTriggerCountdown(650, null, EndWait));

            _aiComponent = new AiComponent();
            _aiComponent.Trigger.Add(_repelSwitch = new AiTriggerSwitch(250));
            _aiComponent.States.Add("idle", stateIdle);
            _aiComponent.States.Add("absorb", stateAbsorb);
            _aiComponent.States.Add("wait", stateWaiting);
            _aiComponent.ChangeState("idle");

            var bodyBox = new CBox(EntityPosition, -7, -12, 14, 12, 14);
            _damageBox = new CBox(EntityPosition, -7, -14, 0, 14, 14, 14, true);

            AddComponent(BodyComponent.Index, _body);
            AddComponent(CarriableComponent.Index, new CarriableComponent(
                new CRectangle(EntityPosition, new Rectangle(-7, -14, 14, 14)), CarryInit, CarryUpdate, CarryThrow));
            AddComponent(PushableComponent.Index, new PushableComponent(bodyBox, OnPush));
            AddComponent(HittableComponent.Index, new HittableComponent(bodyBox, OnHit));
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(_damageBox, HitType.ThrownObject, 2) { IsActive = false });
            AddComponent(AiComponent.Index, _aiComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _drawComponent = new DrawCSpriteComponent(cSprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, _shadowComponent = new BodyDrawShadowComponent(_body, cSprite));
        }

        private void UpdateIdle()
        {
            // save the position when the ball stops moving
            if (_body.IsGrounded && _body.Velocity == Vector3.Zero && _hasMoved)
            {
                // not sure where else this could be done; ideally there would be a despawn function an object could use
                // but this should work fine
                SavePosition();
            }

            _hasMoved = _body.Velocity != Vector3.Zero;
        }

        private void OnHoleAbsorb()
        {
            if (_absorbed)
                return;

            _absorbed = true;
            _aiComponent.ChangeState("absorb");
        }

        private void EndAbsorb()
        {
            // play sound effect
            Game1.GameManager.PlaySoundEffect("D360-24-18");

            var fallAnimation = new ObjAnimator(Map, 0, 0, Values.LayerBottom, "Particles/fall", "idle", true);
            fallAnimation.EntityPosition.Set(new Vector2(
                _body.Position.X + _body.OffsetX + _body.Width / 2.0f - 5,
                _body.Position.Y + _body.OffsetY + _body.Height / 2.0f - 5));
            Map.Objects.SpawnObject(fallAnimation);

            ToWait();

            // reset the ball to the initial position
            _body.Velocity.Z = -0.5f;
            EntityPosition.Set(new Vector3(_spawnPosition.X, _spawnPosition.Y, 32));

            SavePosition();
        }

        private void ToWait()
        {
            _aiComponent.ChangeState("wait");
            _drawComponent.IsActive = false;
            _shadowComponent.IsActive = false;
            _body.IsActive = false;
        }

        private void EndWait()
        {
            _absorbed = false;
            _drawComponent.IsActive = true;
            _shadowComponent.IsActive = true;
            _body.IsActive = true;
        }

        private void Update()
        {
            if (_hitEnemies)
            {
                var collision = Map.Objects.Hit(this, EntityPosition.Position, _damageBox.Box, HitType.ThrownObject, 2, false);
                if (collision != Values.HitCollision.None)
                {
                    _body.Velocity.X = -_body.Velocity.X * 0.45f;
                    _body.Velocity.Y = -_body.Velocity.Y * 0.45f;
                }
            }
        }

        private Values.HitCollision OnHit(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower)
        {
            // do not get hit by itself
            if (originObject == this)
                return Values.HitCollision.None;

            if (_repelSwitch.State)
            {
                _repelSwitch.Reset();
                _body.Velocity.X = direction.X * 0.5f;
                _body.Velocity.Y = direction.Y * 0.5f;
                return Values.HitCollision.RepellingParticle;
            }

            return Values.HitCollision.None;
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType pushType)
        {
            if (pushType == PushableComponent.PushType.Impact)
                return true;

            return false;
        }

        private Vector3 CarryInit()
        {
            // the ball was picked up
            _body.IsActive = false;

            return new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z);
        }

        private bool CarryUpdate(Vector3 newPosition)
        {
            EntityPosition.Set(new Vector3(newPosition.X, newPosition.Y, newPosition.Z));
            return true;
        }

        private void CarryThrow(Vector2 velocity)
        {
            Release();
            _body.Velocity = new Vector3(velocity * 0.825f, 0.5f);
            _body.Level = MapStates.GetLevel(MapManager.ObjLink._body.CurrentFieldState);
            _body.DragAir = 1.0f;
            _hitEnemies = true;
        }

        private void Release()
        {
            _body.JumpStartHeight = 0;
            _body.IsGrounded = false;
            _body.IsActive = true;
        }

        private void Collision(Values.BodyCollision direction)
        {
            if ((direction & Values.BodyCollision.Floor) != 0)
            {
                // stop hitting the player/boss when the ball touches the ground
                _damageField.IsActive = false;
                _hitEnemies = false;
                _body.Level = 0;
                _body.DragAir *= 0.965f;
                Game1.GameManager.PlaySoundEffect("D378-23-17");
            }

            if ((direction & Values.BodyCollision.Horizontal) != 0)
            {
                Game1.GameManager.PlaySoundEffect("D360-07-07");
                _body.Velocity.X = -_body.Velocity.X * 0.45f;
            }
            if ((direction & Values.BodyCollision.Vertical) != 0)
            {
                Game1.GameManager.PlaySoundEffect("D360-07-07");
                _body.Velocity.Y = -_body.Velocity.Y * 0.45f;
            }
        }

        private void LoadPosition()
        {
            if (string.IsNullOrEmpty(_saveStringPosX))
                return;

            var posX = Game1.GameManager.SaveManager.GetInt(_saveStringPosX, (int)EntityPosition.X);
            var posY = Game1.GameManager.SaveManager.GetInt(_saveStringPosY, (int)EntityPosition.Y);

            EntityPosition.Set(new Vector2(posX, posY));
        }

        private void SavePosition()
        {
            if (string.IsNullOrEmpty(_saveStringPosX))
                return;

            Game1.GameManager.SaveManager.SetInt(_saveStringPosX, (int)EntityPosition.X);
            Game1.GameManager.SaveManager.SetInt(_saveStringPosY, (int)EntityPosition.Y);
        }
    }
}
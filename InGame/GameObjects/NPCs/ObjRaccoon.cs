using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjRaccoon : GameObject
    {
        private readonly CSprite _sprite;
        private readonly BodyComponent _body;
        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly Animator _animator;

        private readonly RectangleF _laughRectangle;

        private float _rotationTimer = 0;

        private bool _isRotating;
        private bool _exploded;
        private bool _messageShown;
        private bool _spawnedTarin;

        public ObjRaccoon() : base("raccoon") { }

        public ObjRaccoon(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            // raccoon was already transformed?
            var value = Game1.GameManager.SaveManager.GetString("raccoon_transformed");
            if (value != null && value == "1")
            {
                IsDead = true;
                return;
            }

            _laughRectangle = new RectangleF(posX - 64, posY - 48, 64, 32);

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/raccoon");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, _sprite, new Vector2(-8, -16));

            _body = new BodyComponent(EntityPosition, -7, -10, 14, 10, 8)
            {
                IgnoresZ = true,
                MoveCollision = OnCollision,
                CollisionTypes = Values.CollisionTypes.Normal |
                                 Values.CollisionTypes.NPCWall
            };
            _bodyDrawComponent = new BodyDrawComponent(_body, _sprite, Values.LayerPlayer);

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(InteractComponent.Index, new InteractComponent(_body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(HittableComponent.Index, new HittableComponent(_body.BodyBox, OnHit));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, _bodyDrawComponent);
            AddComponent(DrawShadowComponent.Index, new BodyDrawShadowComponent(_body, _sprite));
        }

        private void Update()
        {
            // show the warning message
            if (!_messageShown && MapManager.ObjLink.EntityPosition.Y > EntityPosition.Y)
            {
                _messageShown = true;
                Game1.GameManager.SaveManager.SetString("raccoon_warning", null);
            }

            if (_isRotating)
                UpdateRotation();
            else
            {
                if (MapManager.ObjLink.BodyRectangle.Intersects(_laughRectangle))
                    _animator.Play("laugh");
                else
                    _animator.Play("idle");
            }
        }

        private void UpdateRotation()
        {
            MapManager.ObjLink.FreezePlayer();
            Game1.GameManager.InGameOverlay.DisableInventoryToggle = true;

            // look at the raccoon
            var playerDirection = EntityPosition.Position - MapManager.ObjLink.EntityPosition.Position;
            var playerDir = AnimationHelper.GetDirection(playerDirection);
            MapManager.ObjLink.SetWalkingDirection(playerDir);

            var direction = _body.VelocityTarget;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();

                var speed = 3.5f;
                if (_rotationTimer < 2000)
                    speed = 0.5f + (_rotationTimer / 2000) * 3.0f;

                _body.VelocityTarget = direction * speed;
                _animator.SpeedMultiplier = speed * 2;
            }

            _rotationTimer += Game1.DeltaTime;

            // move up
            if (_rotationTimer > 4500)
            {
                var height = (_rotationTimer - 4500) / 500f;
                EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y, height * 13));
            }

            if (_rotationTimer > 5000 && !_exploded)
            {
                _exploded = true;
                _sprite.IsVisible = false;
                _body.VelocityTarget = Vector2.Zero;

                // spawn explosion with sound effect
                Game1.GameManager.PlaySoundEffect("D378-12-0C");
                Map.Objects.SpawnObject(new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y - 8 - 13, Values.LayerTop, "Particles/explosionRaccoon", "run", true));
            }

            // spawn tarin
            if (_rotationTimer > 5250 && !_spawnedTarin)
            {
                _spawnedTarin = true;

                // the size cant be bigger than the size of the raccon; otherwise tarin could land on a wall
                var npcTarin = new ObjPersonNew(Map, (int)EntityPosition.X, (int)EntityPosition.Y, null, "tarin", "tarin_healed", null, new Rectangle(0, 0, 10, 10));
                npcTarin.EntityPosition.Set(new Vector3(EntityPosition.X, EntityPosition.Y, EntityPosition.Z - 4));
                npcTarin.Body.Gravity = -0.175f;
                Map.Objects.SpawnObject(npcTarin);

                Game1.GbsPlayer.Resume();
            }

            if (_rotationTimer > 6000)
            {
                Game1.GameManager.StartDialogPath("raccoon_transformed");
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void OnCollision(Values.BodyCollision collision)
        {
            Game1.GameManager.PlaySoundEffect("D360-09-09", true);

            if ((collision & Values.BodyCollision.Horizontal) != 0)
                _body.VelocityTarget.X = -_body.VelocityTarget.X;
            else if ((collision & Values.BodyCollision.Vertical) != 0)
                _body.VelocityTarget.Y = -_body.VelocityTarget.Y;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_isRotating)
                return Values.HitCollision.None;

            // powder?
            if (damageType == HitType.MagicPowder)
                StartMoving();

            return Values.HitCollision.Blocking;
        }

        private void StartMoving()
        {
            Game1.GbsPlayer.Pause();

            _isRotating = true;
            _animator.Play("rotate");
            _body.VelocityTarget = new Vector2(0.5f, 0.5f);

            _body.OffsetX = -5;
            _body.Width = 10;
        }

        private void KeyChanged()
        {
            var value = Game1.GameManager.SaveManager.GetString("raccoon_warning");
            if (value != null)
                _messageShown = false;
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath("raccoon");
            return true;
        }
    }
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjAlligator : GameObject
    {
        private GameObject _gameObjectBanana;

        private readonly BodyDrawComponent _bodyDrawComponent;
        private readonly Animator _animator;
        private readonly DictAtlasEntry _canSprite;

        private float _eatCountdown = 2100;

        private Vector2 _canPosition;
        private Vector2 _canVelocity;
        private float _canGravity = 0.035f;
        private bool _isCanActive;
        private bool _isEating;

        private bool _startEating;

        public ObjAlligator() : base("alligator") { }

        public ObjAlligator(Map.Map map, int posX, int posY) : base(map)
        {
            _canSprite = Resources.GetSprite("trade2");

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/alligator");
            _animator.Play("idle");

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-16, -24, 32, 24);

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(_animator, sprite, new Vector2(-13, -23));

            var body = new BodyComponent(EntityPosition, -12, -16, 20, 16, 8);
            _bodyDrawComponent = new BodyDrawComponent(body, sprite, 1);

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(BodyComponent.Index, body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(body, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(InteractComponent.Index, new InteractComponent(body.BodyBox, Interact));
            AddComponent(BaseAnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));

            SpawnBanana();
        }

        private void SpawnBanana()
        {
            if (Game1.GameManager.SaveManager.GetString("trade2") == "1")
                return;

            _gameObjectBanana = ObjectManager.GetGameObject(Map, "banana", null);
            _gameObjectBanana.EntityPosition.Set(new Vector2((int)EntityPosition.X - 8, (int)EntityPosition.Y + 20));
            Map.Objects.SpawnObject(_gameObjectBanana);
        }

        private void Update()
        {
            if (_startEating)
            {
                _startEating = false;
                ThrowCan();
            }

            if (_isCanActive)
            {
                MapManager.ObjLink.UpdatePlayer = false;

                _canPosition += _canVelocity;
                _canVelocity.Y += _canGravity * Game1.TimeMultiplier;

                if (_canPosition.Y > EntityPosition.Y - 7 - _canSprite.ScaledRectangle.Height)
                {
                    _animator.Play("eat");
                    _isCanActive = false;
                    _isEating = true;
                }
            }

            if (_isEating)
            {
                MapManager.ObjLink.UpdatePlayer = false;

                _eatCountdown -= Game1.DeltaTime;
                if (_eatCountdown <= 0)
                {
                    _isEating = false;
                    _animator.Play("idle");
                    Game1.GameManager.StartDialogPath("alligator_after_eat");
                }
            }
        }

        private void ThrowCan()
        {
            _animator.Play("open");
            _isCanActive = true;
            _canPosition = new Vector2(
                EntityPosition.X - 2 - _canSprite.ScaledRectangle.Width,
                EntityPosition.Y - 10 - _canSprite.ScaledRectangle.Height);
            _canVelocity = new Vector2(0, -1f);

            Game1.GameManager.PlaySoundEffect("D360-36-24");
        }

        private void KeyChanged()
        {
            var value = Game1.GameManager.SaveManager.GetString("alligator_eat");
            if (value != null && value == "eat")
            {
                _startEating = true;
                Game1.GameManager.SaveManager.SetString("alligator_eat", "nop");
            }

            var traded = Game1.GameManager.SaveManager.GetString("trade2");
            if (traded == "1" && _gameObjectBanana != null)
            {
                // remove the banana
                Map.Objects.DeleteObjects.Add(_gameObjectBanana);
                _gameObjectBanana = null;
            }
        }

        private bool Interact()
        {
            Game1.GameManager.StartDialogPath("alligator");
            return true;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            // draw the alligator
            _bodyDrawComponent.Draw(spriteBatch);

            // draw the can
            if (_isCanActive)
                DrawHelper.DrawNormalized(spriteBatch, _canSprite, _canPosition, Color.White);
        }
    }
}
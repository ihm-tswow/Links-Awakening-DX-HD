using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjLamp : GameObject
    {
        private readonly Animator _animator;

        private readonly Color _lightColor = new Color(255, 200, 200);
        private readonly Rectangle _lightRectangle;

        private readonly int _animationLength;

        private readonly string _lampKey;
        private readonly bool _powderLamp;

        private float _lampState = 1.0f;
        private float _liveTime;

        private const int PowderTime = 9000;

        private bool _lampKeyState = true;

        public ObjLamp(Map.Map map, int posX, int posY, string animationName, int rotation, bool hasCollision, bool powderLamp, string lampKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY + 8, 0);

            Tags = Values.GameObjectTag.Lamp;

            var lightSize = 160;
            EntitySize = new Rectangle(8 - lightSize / 2, -8 - lightSize / 2, lightSize, lightSize);
            _lightRectangle = new Rectangle(posX + 8 - lightSize / 2, posY + 8 - lightSize / 2, lightSize, lightSize);

            _animator = AnimatorSaveLoad.LoadAnimator(animationName);
            if (_animator == null)
            {
                Console.WriteLine("Object-ObjLamp: could not find animation name: {0}", animationName);
                IsDead = true;
                return;
            }
            _animator.Play("idle");

            SprEditorImage = _animator.SprTexture;
            EditorIconSource = _animator.CurrentFrame.SourceRectangle;

            foreach (var frame in _animator.CurrentAnimation.Frames)
                _animationLength += frame.FrameTime;

            EditorIconSource = _animator.CurrentFrame.SourceRectangle;

            var sprite = new CSprite(EntityPosition)
            {
                Rotation = (float)Math.PI / 2 * rotation,
                Center = new Vector2(8, 8)
            };

            // connect animation to sprite
            new AnimationComponent(_animator, sprite, new Vector2(8, 0));

            if (hasCollision)
            {
                var collisionBox = new CBox(posX, posY, 0, 16, 16, 16);
                AddComponent(CollisionComponent.Index, new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Normal | Values.CollisionTypes.ThrowWeaponIgnore));
            }

            _powderLamp = powderLamp;
            if (_powderLamp)
            {
                // the collision box is a little bit smaller so that we cant light up two lamps at the same time
                var collisionBox = new CBox(posX + 1, posY + 2, 0, 14, 14, 16);
                AddComponent(HittableComponent.Index, new HittableComponent(collisionBox, OnHit));
                if (!string.IsNullOrEmpty(lampKey))
                {
                    _lampKey = lampKey;
                    Game1.GameManager.SaveManager.SetString(_lampKey, "0");
                }
            }
            else
            {
                // lamp can be turned on/off by setting the lamp key
                if (!string.IsNullOrEmpty(lampKey))
                {
                    _lampKey = lampKey;
                    AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
                }
            }

            // start with the light off
            if (powderLamp)
                _lampState = 0;

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, hasCollision ? Values.LayerPlayer : Values.LayerBottom));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight));
        }

        public bool IsOn()
        {
            return _liveTime > 0;
        }

        private void OnKeyChange()
        {
            var keyState = Game1.GameManager.SaveManager.GetString(_lampKey);
            var newKeyState = keyState == "1";

            if (!_lampKeyState && newKeyState)
            {
                // play sound effect
                Game1.GameManager.PlaySoundEffect("D378-18-12");
            }

            _lampKeyState = newKeyState;
            _animator.Play(_lampKeyState ? "idle" : "dead");
        }

        private void Update()
        {
            // @HACK: this is used to sync all the animations with the same length
            // otherwise they would not be in sync if they did not get updated at the same time
            _animator.SetFrame(0);
            _animator.SetTime(Game1.TotalGameTime % _animationLength);
            _animator.Update();

            if (_powderLamp)
                UpdatePowderedLamp();
            else
                UpdateKeyLamp();
        }

        private void UpdatePowderedLamp()
        {
            _liveTime -= Game1.DeltaTime;
            if (_liveTime < 0)
            {
                if (_lampKeyState)
                {
                    _lampKeyState = false;
                    if (!string.IsNullOrEmpty(_lampKey))
                        Game1.GameManager.SaveManager.SetString(_lampKey, "0");
                }

                _animator.Play("dead");
            }
            else
            {
                _animator.Play("idle");
            }

            _lampState = AnimationHelper.MoveToTarget(_lampState, _lampKeyState ? 1 : 0, 0.1f * Game1.TimeMultiplier);
        }

        private void UpdateKeyLamp()
        {
            _lampState = AnimationHelper.MoveToTarget(_lampState, _lampKeyState ? 1 : 0, 0.075f * Game1.TimeMultiplier);
        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Resources.SprLight, _lightRectangle, _lightColor * _lampState);
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (damageType == HitType.MagicPowder || damageType == HitType.MagicRod)
            {
                _liveTime = PowderTime;

                // play sound effect
                Game1.GameManager.PlaySoundEffect("D378-18-12");

                _lampKeyState = true;
                if (!string.IsNullOrEmpty(_lampKey))
                    Game1.GameManager.SaveManager.SetString(_lampKey, "1");

                return Values.HitCollision.Blocking;
            }

            if ((damageType & HitType.Sword) != 0)
                return Values.HitCollision.None;

            return Values.HitCollision.NoneBlocking;
        }
    }
}
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonColorSwitch : GameObject
    {
        private readonly List<GameObject> _neighborSwitches = new List<GameObject>();
        private readonly Color[] _colors = { new Color(25, 132, 255), new Color(255, 8, 42), new Color(254, 123, 8) };
        private readonly CSprite _sprite;
        private readonly Animator _animator;

        private readonly string _strKey;
        private readonly string _strKeyMoved;

        private readonly int _stateCount;
        private int _stateIndex;

        private readonly int _positionIndex;
        private readonly int _neighbors;

        private bool _moving;
        private bool _colorChanged;
        private bool _resetKey;
        private bool _finished;

        public ObjDungeonColorSwitch() : base("dungeon_color_head") { }

        public ObjDungeonColorSwitch(Map.Map map, int posX, int posY, string strKey, int stateCount, int stateIndex, int position, int neighbors) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 14, 0);
            EntitySize = new Rectangle(-8, -14, 16, 16);

            _strKey = strKey;
            _stateCount = stateCount;
            _stateIndex = stateIndex;

            _positionIndex = 0x01 << position;
            _neighbors = neighbors;

            _strKeyMoved = _strKey + "_moved";

            var hittableBox = new CBox(posX, posY + 2, 0, 16, 14, 16);
            var collisionBox = new CBox(posX + 1, posY + 5, 0, 14, 10, 16);

            _animator = AnimatorSaveLoad.LoadAnimator("Objects/dungeon color switch");
            _animator.Play("idle");

            _sprite = new CSprite(EntityPosition);

            var activated = !string.IsNullOrEmpty(strKey) && Game1.GameManager.SaveManager.GetString(strKey) == "1";
            if (activated)
            {
                _stateIndex = 0;
            }
            else
            {
                if (!string.IsNullOrEmpty(strKey))
                    AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
                AddComponent(HittableComponent.Index, new HittableComponent(hittableBox, OnHit));
            }
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Normal));
            AddComponent(BaseAnimationComponent.Index, new AnimationComponent(_animator, _sprite, new Vector2(0, 2)));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(_sprite));

            Game1.GameManager.SaveManager.SetInt(_strKeyMoved, -1);
        }

        public override void Init()
        {
            var fieldRectangle = Map.GetField((int)EntityPosition.X, (int)EntityPosition.Y);
            Map.Objects.GetObjectsOfType(_neighborSwitches, typeof(ObjDungeonColorSwitch),
                fieldRectangle.X, fieldRectangle.Y, fieldRectangle.Width, fieldRectangle.Height);
        }

        public bool IsBlue()
        {
            return !_moving && _stateIndex == 0;
        }

        private void OnKeyChange()
        {
            var keyState = Game1.GameManager.SaveManager.GetInt(_strKeyMoved, -1);
            if (keyState >= 0)
            {
                if (keyState == _positionIndex)
                {
                    // make sure to reset the key; this should happen one frame after the move started
                    _resetKey = true;
                    StartMoving();
                }
                else if ((keyState & _neighbors) != 0)
                {
                    StartMoving();
                }
            }

            var keyValue = Game1.GameManager.SaveManager.GetString(_strKey, "0");
            if (keyValue == "1")
                _finished = true;
        }

        private bool CheckNeighbors()
        {
            foreach (var gameObject in _neighborSwitches)
            {
                if (gameObject is ObjDungeonColorSwitch neighborSwitch && !neighborSwitch.IsBlue())
                    return false;
            }

            return true;
        }

        private void StartMoving()
        {
            if (_moving)
                IncreaseIndex();

            Game1.GameManager.PlaySoundEffect("D360-03-03");

            _moving = true;
            _colorChanged = false;
            _animator.Play("move");
        }

        private void IncreaseIndex()
        {
            _stateIndex = (_stateIndex + 1) % _stateCount;

        }

        private void Update()
        {
            if (_resetKey)
            {
                _resetKey = false;
                Game1.GameManager.SaveManager.SetInt(_strKeyMoved, -1);
            }

            if (_moving)
            {
                // change the color at the second frame
                if (!_colorChanged && _animator.CurrentFrameIndex >= 1)
                {
                    _colorChanged = true;
                    IncreaseIndex();
                }

                // finished moving?
                if (!_animator.IsPlaying)
                {
                    StopMoving();
                }
            }
        }

        private void StopMoving()
        {
            _moving = false;
            _animator.Play("idle");

            // check if all switches are set to blue => set the strKey to 1
            if (CheckNeighbors())
            {
                _finished = true;
                if (!string.IsNullOrEmpty(_strKey) && Game1.GameManager.SaveManager.GetString(_strKey) != "1")
                    Game1.GameManager.SaveManager.SetString(_strKey, "1");
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _sprite.Draw(spriteBatch);

            // the colored part is 16 scaled pixels below
            var sourceRectangle = _sprite.SourceRectangle;
            _sprite.SourceRectangle.Y += (int)(16 / _sprite.Scale);
            _sprite.Color = _colors[_stateIndex];

            _sprite.Draw(spriteBatch);

            _sprite.Color = Color.White;
            _sprite.SourceRectangle = sourceRectangle;
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_moving || _finished)
                return Values.HitCollision.None;

            Game1.GameManager.SaveManager.SetInt(_strKeyMoved, _positionIndex);

            return Values.HitCollision.Enemy;
        }
    }
}
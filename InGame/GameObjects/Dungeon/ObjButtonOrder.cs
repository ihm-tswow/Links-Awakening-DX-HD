using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjButtonOrder : GameObject
    {
        private readonly CSprite _sprite;
        private readonly Rectangle _effectSourceRectangle = new Rectangle(66, 258, 12, 12);
        private readonly Box _collisionBox;

        private readonly string _strStateKey;
        private readonly string _strKey;
        private readonly int _index;

        private float _effectCounter;
        private bool _isActive;
        private bool _wasColliding;

        public ObjButtonOrder(Map.Map map, int posX, int posY, int index, string strStateKey, string strKey, bool drawSprite) : base(map, "button")
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _index = index;
            _strStateKey = strStateKey;
            _strKey = strKey;

            var animator = AnimatorSaveLoad.LoadAnimator("Particles/buttonOrder");
            animator.Play("idle");

            if (drawSprite)
            {
                _sprite = new CSprite(EntityPosition);
                var animationComponent = new AnimationComponent(animator, _sprite, new Vector2(8, 8));
                AddComponent(BaseAnimationComponent.Index, animationComponent);
            }

            _collisionBox = new Box(posX + 4, posY + 4, 0, 8, 8, 1);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        private void Update()
        {
            if (_effectCounter > 0)
                _effectCounter -= Game1.DeltaTime;
            else
                _effectCounter = 0;

            var isColliding = MapManager.ObjLink._body.BodyBox.Box.Intersects(_collisionBox);
            if (isColliding && !_wasColliding)
            {
                if (_isActive)
                {
                    _effectCounter = 375;

                    // activate the next field
                    Game1.GameManager.SaveManager.SetString(_strStateKey, (_index + 1).ToString());

                    if (!string.IsNullOrEmpty(_strKey))
                        Game1.GameManager.SaveManager.SetString(_strKey, "1");

                    Game1.GameManager.PlaySoundEffect("D360-19-13");
                }
                else
                {
                    if (!string.IsNullOrEmpty(_strStateKey))
                        Game1.GameManager.SaveManager.SetString(_strStateKey, "0");
                }
            }
            _wasColliding = isColliding;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (_isActive && _sprite != null)
                _sprite.Draw(spriteBatch);

            // effect gets played after pressing the button
            if (_effectCounter > 0)
            {
                var radian = (_effectCounter / 300) * MathF.PI;
                var offset = new Vector2(-MathF.Sin(radian), MathF.Cos(radian));

                var pos0 = new Vector2(EntityPosition.X + 8 - 6, EntityPosition.Y + 8 - 6) + offset * 14;
                spriteBatch.Draw(Resources.SprItem, pos0, _effectSourceRectangle, Color.White);

                var pos1 = new Vector2(EntityPosition.X + 8 - 6, EntityPosition.Y + 8 - 6) - offset * 14;
                spriteBatch.Draw(Resources.SprItem, pos1, _effectSourceRectangle, Color.White);
            }
        }

        private void KeyChanged()
        {
            if (!string.IsNullOrEmpty(_strStateKey))
            {
                var state = Game1.GameManager.SaveManager.GetString(_strStateKey);
                _isActive = state == _index.ToString();
            }

        }
    }
}
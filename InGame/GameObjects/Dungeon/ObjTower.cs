using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjTower : GameObject
    {
        private readonly Animator _animatorTop0;
        private readonly Animator _animatorTop1;
        private readonly Animator _animatorTop2;
        private readonly Animator _animatorBottom;

        private readonly string _strKey;

        private bool _opening;
        private bool _opened;
        private bool _isRotating;

        private float _shakeCounter;
        private bool _shakeScreen;

        public ObjTower() : base("tower") { }

        public ObjTower(Map.Map map, int posX, int posY, string strKey) : base(map)
        {
            EntityPosition = new CPosition(posX - 16, posY - 16, 0);
            EntitySize = new Rectangle(0, 0, 80, 80);

            _strKey = strKey;

            _animatorTop0 = SaveLoad.AnimatorSaveLoad.LoadAnimator("Objects/d7 tower");
            _animatorTop0.Play("idle");
            _animatorTop0.Pause();

            _animatorTop1 = SaveLoad.AnimatorSaveLoad.LoadAnimator("Objects/d7 tower top 1");
            _animatorTop1.Play("idle");
            _animatorTop1.Pause();

            _animatorTop2 = SaveLoad.AnimatorSaveLoad.LoadAnimator("Objects/d7 tower top 2");
            _animatorTop2.Play("idle");

            _animatorBottom = SaveLoad.AnimatorSaveLoad.LoadAnimator("Objects/d7 tower bottom");
            _animatorBottom.Play("idle");
            _animatorBottom.Pause();

            var opened = !string.IsNullOrEmpty(strKey) && Game1.GameManager.SaveManager.GetString(strKey) == "1";

            if (!opened)
            {
                AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
                AddComponent(CollisionComponent.Index, new BoxCollisionComponent(new CBox(posX + 8, posY + 48, 0, 16, 16, 16), Values.CollisionTypes.Normal));
                AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            }
            else
            {
                _animatorBottom.Play("opened");
            }

            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        private void Open()
        {
            if (_opening || _opened)
                return;

            _animatorTop0.Continue();
            _animatorTop1.Continue();
            _animatorBottom.Continue();

            _opening = true;

            Game1.GameManager.PlaySoundEffect("D378-04-04");
            Game1.GameManager.StopMusic();
        }

        private void OnKeyChange()
        {
            if (!string.IsNullOrEmpty(_strKey))
            {
                var keyState = Game1.GameManager.SaveManager.GetString(_strKey);
                if (keyState == "1")
                    Open();
            }
        }

        private void Update()
        {
            if (!_opening || _opened)
                return;

            MapManager.ObjLink.FreezePlayer();

            _animatorTop0.Update();
            _animatorTop1.Update();
            _animatorBottom.Update();

            _shakeCounter += Game1.DeltaTime;

            if (!_shakeScreen && _shakeCounter > 2000)
            {
                _shakeScreen = true;
                Game1.GameManager.ShakeScreen(2750, 1, 0, 5, 5);
                Game1.GameManager.PlaySoundEffect("D378-29-1D");
            }

            if (_opening && !_animatorTop0.IsPlaying)
            {
                if (!_isRotating)
                {
                    _isRotating = true;
                    _animatorTop0.Play("rotate");
                    _animatorTop1.Play("rotate");
                    _animatorBottom.Play("rotate");
                    Game1.GameManager.PlaySoundEffect("D360-46-2E");
                }
                else if (_isRotating)
                {
                    _opened = true;
                    Game1.GameManager.PlaySoundEffect("D360-02-02");
                    Game1.GameManager.PlayMusic();

                    RemoveComponent(CollisionComponent.Index);
                }
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _animatorTop0.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y), Color.White);
            _animatorTop1.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y), Color.White);
            _animatorTop2.Draw(spriteBatch, new Vector2(EntityPosition.X + 16, EntityPosition.Y - 96), Color.White);
            _animatorBottom.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y), Color.White);
        }
    }
}
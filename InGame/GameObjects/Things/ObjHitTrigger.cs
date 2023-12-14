using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjHitTrigger : GameObject
    {
        private readonly HitType _hitType;
        private readonly string _strKey;
        private readonly int _activationTime;

        private float _activationCounter;
        private bool _wasActivated;

        private readonly bool _delete;
        private readonly bool _soundEffect;

        public ObjHitTrigger() : base("editor hit trigger") { }

        public ObjHitTrigger(Map.Map map, int posX, int posY, int hitType, string strKey, int width, int height, int activationTime, bool delete, bool soundEffect) : base(map)
        {
            EntitySize = new Rectangle(0, 0, width, height);

            _hitType = (HitType)hitType;
            _strKey = strKey;

            _activationTime = activationTime;

            _delete = delete;
            _soundEffect = soundEffect;

            _activationCounter = _activationTime;

            if (_activationCounter > 0)
                AddComponent(UpdateComponent.Index, new UpdateComponent(Update));

            var hitBox = new CBox(posX, posY, 0, width, height, 16);
            AddComponent(HittableComponent.Index, new HittableComponent(hitBox, OnHit));
        }

        private void Update()
        {
            if (!_wasActivated)
                return;

            _activationCounter -= Game1.DeltaTime;
            if (_activationCounter <= 0)
                Activate();
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            if (_wasActivated)
                return Values.HitCollision.None;

            if (damageType == _hitType)
            {
                if (_activationCounter > 0)
                    _wasActivated = true;
                else
                    Activate();
            }

            return Values.HitCollision.None;
        }

        private void Activate()
        {
            Game1.GameManager.SaveManager.SetString(_strKey, "1");

            if (_soundEffect)
                Game1.GameManager.PlaySoundEffect("D378-04-04");

            if (_delete)
                Map.Objects.DeleteObjects.Add(this);
            else
            {
                _wasActivated = false;
                _activationCounter = _activationTime;
            }
        }
    }
}

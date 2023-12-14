using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class DamageFieldComponent : Component
    {
        public new static int Index = 10;
        public static int Mask = 0x01 << Index;

        public delegate bool OnDamageTemplate();
        public OnDamageTemplate OnDamage;

        public delegate void OnDamagedPlayerTemplate();
        public OnDamagedPlayerTemplate OnDamagedPlayer;

        public CBox CollisionBox;
        public HitType DamageType;

        public float PushMultiplier = 1.75f;
        public int Strength;
        public bool IsActive = true;

        public DamageFieldComponent(CBox collisionBox, HitType damageType, int damageStrength)
        {
            CollisionBox = collisionBox;
            DamageType = damageType;
            Strength = damageStrength;

            OnDamage = DamagePlayer;
        }

        public bool DamagePlayer()
        {
            var damagedPlayer = MapManager.ObjLink.HitPlayer(CollisionBox.Box, DamageType, Strength, PushMultiplier);
            if (damagedPlayer && OnDamagedPlayer != null)
                OnDamagedPlayer();

            return damagedPlayer;
        }
    }
}

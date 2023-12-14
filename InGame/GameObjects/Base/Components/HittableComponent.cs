using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Base.Components
{
    class HittableComponent : Component
    {
        public new static int Index = 7;
        public static int Mask = 0x01 << Index;

        public delegate Values.HitCollision HitTemplate(GameObject originObject, Vector2 direction, HitType type, int damage, bool pieceOfPower);
        public HitTemplate Hit;

        public CBox HittableBox;

        public bool IsActive = true;

        public HittableComponent(CBox hittableBox, HitTemplate hit)
        {
            HittableBox = hittableBox;
            Hit = hit;
        }
    }
}

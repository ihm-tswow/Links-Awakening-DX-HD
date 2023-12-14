using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjCactus : GameObject
    {
        public ObjCactus() : base("cactus") { }

        public ObjCactus(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            var sprite = new CSprite("cactus", EntityPosition);

            var collisionBox = new CBox(posX + 3, posY + 3, 0, 10, 12, 8);
            var damageBox = new CBox(posX + 2, posY + 2, 0, 12, 14, 8);

            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(collisionBox, Values.CollisionTypes.Normal));
            AddComponent(DamageFieldComponent.Index, new DamageFieldComponent(damageBox, HitType.Enemy, 2));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
        }
    }
}
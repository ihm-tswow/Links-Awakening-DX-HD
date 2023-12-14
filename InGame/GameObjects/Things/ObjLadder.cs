using Microsoft.Xna.Framework;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjLadder : GameObject
    {
        private readonly Box _collisionRectangle;
        private readonly bool _isTop;

        public ObjLadder(Map.Map map, int posX, int posY, bool isTop) : base(map)
        {
            var sprite = Resources.GetSprite(isTop ? "editor ladder top" : "editor ladder");
            SprEditorImage = sprite.Texture;
            EditorIconSource = sprite.ScaledRectangle;
            EditorIconScale = sprite.Scale;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(5, 0, 6, 16);
            _isTop = isTop;

            if (isTop)
                _collisionRectangle = new Box(posX, posY, 0, 16, 16, 8);
            else
                _collisionRectangle = new Box(posX + 4, posY, 0, 8, 16, 8);

            AddComponent(CollisionComponent.Index, new CollisionComponent(Collision)
            {
                CollisionType = !isTop ? Values.CollisionTypes.Ladder :
                Values.CollisionTypes.Ladder | Values.CollisionTypes.LadderTop
            });
        }

        private bool Collision(Box box, int dir, int level, ref Box collidingBox)
        {
            // only collide if the entity was on top of the ladder the frame before
            if ((!_isTop || dir == 3) && _collisionRectangle.Intersects(box))
            {
                collidingBox = _collisionRectangle;
                return true;
            }

            return false;
        }
    }
}
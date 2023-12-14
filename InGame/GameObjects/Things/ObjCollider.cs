using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjCollider : GameObject
    {
        private Box _singleCollisionBox;
        private Box[] CollisionBoxes { get; }

        private readonly Color _editorColor = Color.DarkRed * 0.65f;
        private readonly int _level = -1;

        public ObjCollider(Map.Map map, int posX, int posY, Color editorColor, Values.CollisionTypes type, params Rectangle[] rectangles) : base(map)
        {
            EditorIconSource = new Rectangle(0, 0, 16, 16);
            _editorColor = editorColor;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            CollisionBoxes = new Box[rectangles.Length];
            for (var i = 0; i < rectangles.Length; i++)
                CollisionBoxes[i] = new Box(
                    posX + rectangles[i].X, posY + rectangles[i].Y, 0,
                    rectangles[i].Width, rectangles[i].Height, 16);

            AddComponent(CollisionComponent.Index, new CollisionComponent(MultiBoxCollision) { CollisionType = type });
        }

        public ObjCollider(Map.Map map, int posX, int posY, int height, Rectangle rectangle, Values.CollisionTypes type, int level) : base(map)
        {
            EditorIconSource = new Rectangle(0, 0, 16, 16);

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _level = level;
            _singleCollisionBox = new Box(
                posX + rectangle.X, posY + rectangle.Y, 0,
                rectangle.Width, rectangle.Height, height);

            AddComponent(CollisionComponent.Index, new CollisionComponent(SingeBoxCollision) { CollisionType = type });
        }

        private bool MultiBoxCollision(Box box, int dir, int level, ref Box collidingBox)
        {
            foreach (var singleBox in CollisionBoxes)
                if (singleBox.Intersects(box))
                {
                    collidingBox = singleBox;
                    return true;
                }

            return false;
        }

        private bool SingeBoxCollision(Box box, int dir, int level, ref Box collidingBox)
        {
            if ((_level != -1 && _level < level) || !_singleCollisionBox.Intersects(box))
                return false;

            collidingBox = _singleCollisionBox;
            return true;
        }

        public override void DrawEditor(SpriteBatch spriteBatch, Vector2 drawPosition)
        {
            if (CollisionBoxes != null)
                for (var i = 0; i < CollisionBoxes.Length; i++)
                {
                    spriteBatch.Draw(Resources.SprWhite,
                        new Rectangle(
                            (int)(drawPosition.X + CollisionBoxes[i].X),
                            (int)(drawPosition.Y + CollisionBoxes[i].Y),
                            (int)CollisionBoxes[i].Width,
                            (int)CollisionBoxes[i].Height), _editorColor);
                }
            else
            {
                spriteBatch.Draw(Resources.SprWhite,
                    new Rectangle(
                        (int)(drawPosition.X + _singleCollisionBox.X),
                        (int)(drawPosition.Y + _singleCollisionBox.Y),
                        (int)_singleCollisionBox.Width,
                        (int)_singleCollisionBox.Height), _editorColor);
            }
        }
    }
}
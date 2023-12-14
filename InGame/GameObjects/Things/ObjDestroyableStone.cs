using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjDestroyableStone : GameObject
    {
        private readonly string _saveKey;

        public ObjDestroyableStone(Map.Map map, int posX, int posY, Rectangle sourceRectangle, string saveKey) : base(map)
        {
            EditorIconSource = sourceRectangle;
            SprEditorImage = Resources.SprObjects;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, sourceRectangle.Width, sourceRectangle.Height);

            // don't spawn the object if it was already destroyed
            if (saveKey != null && Game1.GameManager.SaveManager.GetString(saveKey) == "1")
            {
                IsDead = true;
                return;
            }

            _saveKey = saveKey;

            var rectangle = new CBox(posX, posY, 0, sourceRectangle.Width, sourceRectangle.Height, 16);
            var sprite = new CSprite(Resources.SprObjects, EntityPosition, EditorIconSource, new Vector2(0, 0));

            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(rectangle, Values.CollisionTypes.Normal));
            AddComponent(HittableComponent.Index, new HittableComponent(rectangle, OnHit));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBottom));
        }

        private Values.HitCollision OnHit(GameObject gameObject, Vector2 direction, HitType damageType, int damage, bool pieceOfPower)
        {
            // gets destroyed by a bomb
            if (damageType == HitType.Bomb)
            {
                Game1.GameManager.PlaySoundEffect("D360-02-02");

                Map.Objects.DeleteObjects.Add(this);

                if (!string.IsNullOrEmpty(_saveKey))
                    Game1.GameManager.SaveManager.SetString(_saveKey, "1");

                for (var y = 0; y < 2; y++)
                {
                    for (var x = 0; x < 2; x++)
                    {
                        const float upVec = 1.5f;
                        const float spread = 200f;
                        var vector0 = new Vector3(-1, -1, 0) * Game1.RandomNumber.Next(50, 150) / spread + new Vector3(0, 0, upVec);
                        var vector1 = new Vector3(-1, 0, 0) * Game1.RandomNumber.Next(50, 150) / spread + new Vector3(0, 0, upVec);
                        var vector2 = new Vector3(1, -1, 0) * Game1.RandomNumber.Next(50, 150) / spread + new Vector3(0, 0, upVec);
                        var vector3 = new Vector3(1, 0, 0) * Game1.RandomNumber.Next(50, 150) / spread + new Vector3(0, 0, upVec);

                        var stone0 = new ObjSmallStone(Map, (int)EntityPosition.X + 4 + x * 16, (int)EntityPosition.Y + 4 + y * 16, (int)EntityPosition.Z, vector0);
                        var stone1 = new ObjSmallStone(Map, (int)EntityPosition.X + 4 + x * 16, (int)EntityPosition.Y + 12 + y * 16, (int)EntityPosition.Z, vector1);
                        var stone2 = new ObjSmallStone(Map, (int)EntityPosition.X + 12 + x * 16, (int)EntityPosition.Y + 4 + y * 16, (int)EntityPosition.Z, vector2);
                        var stone3 = new ObjSmallStone(Map, (int)EntityPosition.X + 12 + x * 16, (int)EntityPosition.Y + 12 + y * 16, (int)EntityPosition.Z, vector3);

                        Map.Objects.SpawnObject(stone0);
                        Map.Objects.SpawnObject(stone1);
                        Map.Objects.SpawnObject(stone2);
                        Map.Objects.SpawnObject(stone3);
                    }
                }

                return Values.HitCollision.Blocking;
            }

            return Values.HitCollision.None;
        }
    }
}

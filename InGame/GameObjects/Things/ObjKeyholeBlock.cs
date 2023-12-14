using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjKeyholeBlock : GameObject
    {
        private readonly string _saveKey;

        public ObjKeyholeBlock() : base("keyhole_block") { }

        public ObjKeyholeBlock(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            _saveKey = saveKey;

            if (!string.IsNullOrEmpty(_saveKey) && Game1.GameManager.SaveManager.GetString(_saveKey, "0") == "1")
            {
                IsDead = true;
                return;
            }

            var box = new CBox(EntityPosition, 0, 0, 16, 16, 8);
            AddComponent(PushableComponent.Index, new PushableComponent(box, OnPush) { InertiaTime = 175 });
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(box, Values.CollisionTypes.Normal));
            AddComponent(DrawComponent.Index, new DrawSpriteComponent("keyhole_block", EntityPosition, Vector2.Zero, Values.LayerBottom));
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                return false;

            // does the player even have a small key?
            var keyItems = Game1.GameManager.GetItem("smallkey");
            if (keyItems == null || Game1.GameManager.GetItem("smallkey").Count <= 0)
            {
                Game1.GameManager.StartDialogPath("dungeon_keyhole_block");
                return true;
            }

            Game1.GameManager.RemoveItem("smallkey", 1);

            if (!string.IsNullOrEmpty(_saveKey))
                Game1.GameManager.SaveManager.SetString(_saveKey, "1");

            // spawn explosion effect
            Map.Objects.SpawnObject(new ObjAnimator(Map, (int)EntityPosition.X, (int)EntityPosition.Y, Values.LayerBottom, "Particles/spawn", "run", true));

            Game1.GameManager.PlaySoundEffect("D378-04-04");

            // remove the blockade
            Map.Objects.DeleteObjects.Add(this);

            return true;
        }
    }
}
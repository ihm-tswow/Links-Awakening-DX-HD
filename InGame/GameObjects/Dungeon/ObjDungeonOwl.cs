using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjDungeonOwl : GameObject
    {
        private readonly string _signText;

        public ObjDungeonOwl() : base("dungeon_owl") { }

        public ObjDungeonOwl(Map.Map map, int posX, int posY, string signText) : base(map)
        {
            var sourceRectangle = Resources.SourceRectangle("dungeon_owl");

            EntityPosition = new CPosition(posX, posY + 16, 0);
            EntitySize = new Rectangle(0, -16, 16, 16);

            _signText = signText;

            var interactBox = new CBox(posX, posY, 0, 16, 16, 16);

            AddComponent(InteractComponent.Index, new InteractComponent(interactBox, OnInteract));
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(interactBox, Values.CollisionTypes.Normal));
            AddComponent(DrawComponent.Index, new DrawSpriteComponent(
                Resources.SprObjects, EntityPosition, sourceRectangle, new Vector2(0, -16), Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowSpriteComponent(
                Resources.SprObjects, EntityPosition, sourceRectangle, new Vector2(0, -16)));
        }

        private bool OnInteract()
        {
            Game1.GameManager.StartDialogPath(_signText);
            return true;
        }
    }
}
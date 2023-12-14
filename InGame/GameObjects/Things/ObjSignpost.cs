using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjSignpost : GameObject
    {
        private readonly string _signText;
        private readonly int _direction;

        public ObjSignpost() : base("signpost_0") { }

        public ObjSignpost(Map.Map map, int posX, int posY, string signText, string spriteId, Rectangle interactionRectangle, int direction) : base(map)
        {
            _signText = signText;
            _direction = direction;

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            var interactBox = new CBox(
                posX + interactionRectangle.X, posY + interactionRectangle.Y, 0,
                interactionRectangle.Width, interactionRectangle.Height, 16);
            AddComponent(InteractComponent.Index, new InteractComponent(interactBox, OnInteract));

            if (string.IsNullOrEmpty(spriteId))
                return;

            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(interactBox, Values.CollisionTypes.Normal));
            AddComponent(DrawComponent.Index, new DrawSpriteComponent(spriteId, EntityPosition, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowSpriteComponent(spriteId, EntityPosition));
        }

        private bool OnInteract()
        {
            if (_direction >= 0 && MapManager.ObjLink.Direction != _direction)
                return false;

            Game1.GameManager.StartDialogPath(_signText);

            return true;
        }
    }
}
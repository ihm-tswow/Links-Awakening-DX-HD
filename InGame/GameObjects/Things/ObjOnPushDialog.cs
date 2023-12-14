using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjOnPushDialog : GameObject
    {
        private readonly string _signText;

        public ObjOnPushDialog() : base("signpost_0") { }

        public ObjOnPushDialog(Map.Map map, int posX, int posY, string signText, int width, int height) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, width, height);

            _signText = signText;

            var box = new CBox(EntityPosition, 0, 0, width, height, 16);
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(box, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(PushableComponent.Index, new PushableComponent(box, OnPush));
        }

        public ObjOnPushDialog(Map.Map map, int posX, int posY, int width, int height, string signText) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, width, height);

            _signText = signText;

            var box = new CBox(EntityPosition, 0, 0, width, height, 16);
            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(box, Values.CollisionTypes.Normal | Values.CollisionTypes.PushIgnore));
            AddComponent(PushableComponent.Index, new PushableComponent(box, OnPush));
        }

        private bool OnPush(Vector2 direction, PushableComponent.PushType type)
        {
            if (type == PushableComponent.PushType.Impact)
                return false;

            Game1.GameManager.StartDialogPath(_signText);
            return true;
        }
    }
}
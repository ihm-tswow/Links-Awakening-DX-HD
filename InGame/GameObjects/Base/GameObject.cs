using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;
using System.Diagnostics;

namespace ProjectZ.InGame.GameObjects.Base
{
    public class GameObject
    {
        // editor stuff
        public Texture2D SprEditorImage;
        public Rectangle EditorIconSource;
        public Color EditorColor = new Color(255, 255, 255) * 0.65f;
        public float EditorIconScale = 1.0f;

        public Map.Map Map;

        public Values.GameObjectTag Tags;

        // entity component system stuff
        public CPosition EntityPosition;
        public Component[] Components = new Component[32];   // TODO_End: replace with actual component count
                                                             // should have probably used an enum...

        public Rectangle EntitySize = new Rectangle(-16, -16, 48, 48);
        public Point EntityPoolPosition;

        public int ComponentsMask;

        public virtual bool IsActive { get; set; } = true;
        public bool IsDead;

        public GameObject() { }

        // constructor used for the editor objects
        public GameObject(string spriteId)
        {
            var sprite = Resources.GetSprite(spriteId);

            SprEditorImage = sprite.Texture;
            EditorIconSource = sprite.ScaledRectangle;
            EditorIconScale = sprite.Scale;
        }

        public GameObject(Map.Map map, string spriteId) : this(spriteId)
        {
            Map = map;
        }

        public GameObject(Map.Map map)
        {
            Map = map;
        }

        public virtual void Init() { }

        public virtual void DrawEditor(SpriteBatch spriteBatch, Vector2 position)
        {
            if (SprEditorImage != null)
                spriteBatch.Draw(SprEditorImage, position, EditorIconSource, EditorColor,
                    0, Vector2.Zero, new Vector2(EditorIconScale), SpriteEffects.None, 0);
        }

        public void AddComponent(int index, Component newComponent)
        {
            ComponentsMask |= (0x01 << index);
            Components[index] = newComponent;
            newComponent.Owner = this;

#if DEBUG
            if (EntityPosition == null)
                return;

            if (newComponent is CarriableComponent)
            {
                var carriableComponent = (CarriableComponent)newComponent;
                var rectangle = carriableComponent.Rectangle.Rectangle;
                Debug.Assert(EntityPosition.X + EntitySize.X <= rectangle.X);
                Debug.Assert(EntityPosition.Y + EntitySize.Y <= rectangle.Y);
                Debug.Assert(rectangle.Right <= EntityPosition.X + EntitySize.X + EntitySize.Width);
                Debug.Assert(rectangle.Bottom <= EntityPosition.Y + EntitySize.Y + EntitySize.Height);
            }
            if (newComponent is ObjectCollisionComponent)
            {
                var objectCollider = (ObjectCollisionComponent)newComponent;
                var rectangle = objectCollider.CollisionRectangle.Rectangle;
                Debug.Assert(EntityPosition.X + EntitySize.X <= rectangle.X);
                Debug.Assert(EntityPosition.Y + EntitySize.Y <= rectangle.Y);
                Debug.Assert(rectangle.Right <= EntityPosition.X + EntitySize.X + EntitySize.Width);
                Debug.Assert(rectangle.Bottom <= EntityPosition.Y + EntitySize.Y + EntitySize.Height);
            }
            if (newComponent is BodyCollisionComponent)
            {
                var bodyComponent = (BodyCollisionComponent)newComponent;
                var bodyBox = bodyComponent.Body.BodyBox.Box;
                Debug.Assert(EntityPosition.X + EntitySize.X <= bodyBox.X);
                Debug.Assert(EntityPosition.Y + EntitySize.Y <= bodyBox.Y);
                Debug.Assert(bodyBox.Right <= EntityPosition.X + EntitySize.X + EntitySize.Width);
                Debug.Assert(bodyBox.Front <= EntityPosition.Y + EntitySize.Y + EntitySize.Height);
            }
            if (newComponent is BoxCollisionComponent)
            {
                var boxCollisionComponent = (BoxCollisionComponent)newComponent;
                var box = boxCollisionComponent.CollisionBox.Box;
                Debug.Assert(EntityPosition.X + EntitySize.X <= box.X);
                Debug.Assert(EntityPosition.Y + EntitySize.Y <= box.Y);
                Debug.Assert(box.Right <= EntityPosition.X + EntitySize.X + EntitySize.Width);
                Debug.Assert(box.Front <= EntityPosition.Y + EntitySize.Y + EntitySize.Height);
            }
            if (newComponent is DamageFieldComponent)
            {
                var damageComponent = (DamageFieldComponent)newComponent;
                var box = damageComponent.CollisionBox.Box;
                Debug.Assert(EntityPosition.X + EntitySize.X <= box.X);
                Debug.Assert(EntityPosition.Y + EntitySize.Y <= box.Y);
                Debug.Assert(box.Right <= EntityPosition.X + EntitySize.X + EntitySize.Width);
                Debug.Assert(box.Front <= EntityPosition.Y + EntitySize.Y + EntitySize.Height);
            }
            //if (newComponent is HittableComponent)
            //{
            //    var hittableComponent = (HittableComponent)newComponent;
            //    var box = hittableComponent.HittableBox.Box;
            //    Debug.Assert(EntityPosition.X + EntitySize.X <= box.X);
            //    Debug.Assert(EntityPosition.Y + EntitySize.Y <= box.Y);
            //    Debug.Assert(box.Right <= EntityPosition.X + EntitySize.X + EntitySize.Width);
            //    Debug.Assert(box.Front <= EntityPosition.Y + EntitySize.Y + EntitySize.Height);
            //}
            if (newComponent is InteractComponent)
            {
                var interactionComponent = (InteractComponent)newComponent;
                var box = interactionComponent.BoxInteractabel.Box;
                Debug.Assert(EntityPosition.X + EntitySize.X <= box.X);
                Debug.Assert(EntityPosition.Y + EntitySize.Y <= box.Y);
                Debug.Assert(box.Right <= EntityPosition.X + EntitySize.X + EntitySize.Width);
                Debug.Assert(box.Front <= EntityPosition.Y + EntitySize.Y + EntitySize.Height);
            }
            if (newComponent is PushableComponent)
            {
                var pushableComponent = (PushableComponent)newComponent;
                var box = pushableComponent.PushableBox.Box;
                Debug.Assert(EntityPosition.X + EntitySize.X <= box.X);
                Debug.Assert(EntityPosition.Y + EntitySize.Y <= box.Y);
                Debug.Assert(box.Right <= EntityPosition.X + EntitySize.X + EntitySize.Width);
                Debug.Assert(box.Front <= EntityPosition.Y + EntitySize.Y + EntitySize.Height);
            }
#endif
        }

        public void RemoveComponent(int index)
        {
            ComponentsMask &= (ComponentsMask ^ (0x01 << index));
            Components[index].Owner = null;
            Components[index] = null;
        }
    }
}

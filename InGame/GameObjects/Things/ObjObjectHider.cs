using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.NPCs;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjObjectHider : GameObject
    {
        private readonly List<GameObject> _objectList = new List<GameObject>();
        private readonly Rectangle _hiddenField;
        private bool _init;

        public ObjObjectHider() : base("editor object hider") { }

        public ObjObjectHider(Map.Map map, int posX, int posY) : base(map)
        {
            if (Game1.GameManager.HasMagnifyingLens)
            {
                IsDead = true;
                return;
            }

            _hiddenField = map.GetField(posX, posY);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        private void Update()
        {
            if (!_init)
            {
                _init = true;

                // if it was possible to edit the tags of gameobjects we could have a "hiden" tag and only get those objects
                Map.Objects.GetComponentList(_objectList, _hiddenField.X, _hiddenField.Y, _hiddenField.Width, _hiddenField.Height, DrawComponent.Mask);

                SetVisibility(false);
            }

            if (Game1.GameManager.HasMagnifyingLens)
            {
                SetVisibility(true);
                Map.Objects.DeleteObjects.Add(this);
            }
        }

        private void SetVisibility(bool visibility)
        {
            foreach (GameObject gameObject in _objectList)
            {
                if (gameObject.Tags != Values.GameObjectTag.Enemy &&
                    !(gameObject is ObjSprite) &&
                    !(gameObject is ObjPersonNew))
                    continue;

                // deactivate the person and the sprites
                if (gameObject is ObjPersonNew || gameObject is ObjSprite)
                {
                    gameObject.IsActive = visibility;
                    continue;
                }

                if (gameObject.Components[DrawComponent.Index] != null)
                    ((DrawComponent)gameObject.Components[DrawComponent.Index]).IsActive = visibility;
                if (gameObject.Components[DrawShadowComponent.Index] != null)
                    ((DrawShadowComponent)gameObject.Components[DrawShadowComponent.Index]).IsActive = visibility;
            }
        }
    }
}
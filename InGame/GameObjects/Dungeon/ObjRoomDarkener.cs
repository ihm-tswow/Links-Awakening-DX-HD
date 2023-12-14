using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    internal class ObjRoomDarkener : GameObject
    {
        private readonly DictAtlasEntry _sprite;
        private readonly List<GameObject> _lamps = new List<GameObject>();
        private readonly Rectangle _roomRectangle;

        private readonly float _dark;
        private readonly float _bright;

        private float _state;

        public ObjRoomDarkener() : base("editor room darkener") { }

        public ObjRoomDarkener(Map.Map map, int posX, int posY, float dark, float bright) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-16, -16, Values.FieldWidth + 32, Values.FieldHeight + 32);

            _roomRectangle = new Rectangle(posX, posY, Values.FieldWidth, Values.FieldHeight);

            _dark = dark;
            _bright = bright;

            _sprite = Resources.GetSprite("room blur");

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(LightDrawComponent.Index, new LightDrawComponent(DrawLight) { Layer = Values.LightLayer2 });
        }

        public override void Init()
        {
            // get all the lamps in the area
            Map.Objects.GetGameObjectsWithTag(_lamps, Values.GameObjectTag.Lamp,
                _roomRectangle.X, _roomRectangle.Y, _roomRectangle.Width, _roomRectangle.Height);

            if (_lamps.Count == 0)
            {
                _state = _dark;
                RemoveComponent(UpdateComponent.Index);
            }
            else
                UpdateLampState(true);
        }

        private void Update()
        {
            UpdateLampState(false);
        }

        private void UpdateLampState(bool instantTransition)
        {
            var onCount = 0;

            foreach (var gameObject in _lamps)
            {
                if (gameObject is ObjLamp lamp)
                {
                    if (lamp.IsOn())
                        onCount++;
                }
            }

            // blend from _bright to _dark depending on how many lamps in the room are on
            var targetState = MathHelper.Lerp(_dark, _bright, onCount / (float)_lamps.Count);

            if (instantTransition)
                _state = targetState;
            else
            {
                // smoothly transition to the target state
                var amount = Math.Clamp(0.025f / Math.Abs(targetState - _state) * Game1.TimeMultiplier, 0, 1);
                _state = MathHelper.Lerp(_state, targetState, amount);
            }

        }

        private void DrawLight(SpriteBatch spriteBatch)
        {
            DrawHelper.DrawNormalized(spriteBatch, _sprite, EntityPosition.Position, Color.Black * _state);
        }
    }
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjPickupAnimation : GameObject
    {
        private Rectangle _sourceRectangle0 = new Rectangle(66, 258, 12, 12);
        private Rectangle _sourceRectangle1 = new Rectangle(80, 256, 16, 16);

        private double _counter;
        private int _state;

        public ObjPickupAnimation(Map.Map map, float posX, float posY) : base(map)
        {
            SprEditorImage = Resources.SprItem;
            EditorIconSource = new Rectangle(64, 168, 16, 16);

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(-29, -29, 58, 58);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));
        }

        private void Update()
        {
            if (_state == 0)
            {
                if (_counter > 380)
                {
                    _state = 1;
                    _counter = 0;
                }
            }
            else if (_state == 1)
            {
                if (_counter > 66)
                {
                    _state = 2;
                    _counter = 0;
                }
            }
            else if (_state == 2)
            {
                if (_counter > 116)
                    Map.Objects.DeleteObjects.Add(this);
            }

            _counter += Game1.DeltaTime;
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            if (_state == 0)
            {
                for (var y = 0; y < 2; y++)
                    for (var x = 0; x < 2; x++)
                    {
                        var distance = (float)(23 * (1 - _counter / 380));
                        var position = EntityPosition.Position + new Vector2(x * 2 - 1, y * 2 - 1) * distance + new Vector2(-6, -6);
                        spriteBatch.Draw(Resources.SprItem, position, _sourceRectangle0, Color.White);
                    }
            }
            else if (_state == 1)
                spriteBatch.Draw(Resources.SprItem, EntityPosition.Position + new Vector2(-6, -6), _sourceRectangle0, Color.White);
            else if (_state == 2)
                spriteBatch.Draw(Resources.SprItem, EntityPosition.Position + new Vector2(-8, -8), _sourceRectangle1, Color.White);
        }
    }
}
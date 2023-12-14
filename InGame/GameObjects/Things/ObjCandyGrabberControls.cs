using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjCandyGrabberControls : GameObject
    {
        private readonly Rectangle _sourceTop;
        private readonly Rectangle _sourceButton0;
        private readonly Rectangle _sourceButton1;

        private readonly Box _collisionBox;

        private int _buttonFrame;
        private int _animationSpeed = 100;

        public ObjCandyGrabberControls() : base("candy_grabber_controls_top") { }

        public ObjCandyGrabberControls(Map.Map map, int posX, int posY) : base(map)
        {
            _sourceTop = Resources.SourceRectangle("candy_grabber_controls_top");
            _sourceButton0 = Resources.SourceRectangle("candy_grabber_controls_button_0");
            _sourceButton1 = Resources.SourceRectangle("candy_grabber_controls_button_1");

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _collisionBox = new Box(posX + 6, posY + 32, 0, 4, 10, 8);

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerBottom, EntityPosition));
        }

        public void Update()
        {
            _buttonFrame = ((int)Game1.TotalGameTime % (2 * _animationSpeed)) / _animationSpeed;

            // check if the player is standing on the right spot and has payed for the game
            var allowedToStartGame = MapManager.ObjLink._body.BodyBox.Box.Intersects(_collisionBox) &&
                                     Game1.GameManager.SaveManager.GetString("can_play") == "1";
            Game1.GameManager.SaveManager.SetBool("trendy_ready", allowedToStartGame);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // draw the top
            spriteBatch.Draw(Resources.SprObjects, EntityPosition.Position + new Vector2(1, 1), _sourceTop, Color.White);

            // left button
            var pressFirstButton = Game1.GameManager.SaveManager.GetString("trendy_button_1") == "1";
            spriteBatch.Draw(Resources.SprObjects, EntityPosition.Position + new Vector2(1, 17),
                pressFirstButton && _buttonFrame == 1 ? _sourceButton1 : _sourceButton0, Color.White);

            // right button
            var pressSecondButton = Game1.GameManager.SaveManager.GetString("trendy_button_2") == "1";
            spriteBatch.Draw(Resources.SprObjects, EntityPosition.Position + new Vector2(9, 17),
                pressSecondButton && _buttonFrame == 1 ? _sourceButton1 : _sourceButton0, Color.White);
        }
    }
}

using ProjectZ.InGame.GameObjects.Base;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjMusic : GameObject
    {
        private string _title;

        public ObjMusic() : base("editor music") { }

        public ObjMusic(Map.Map map, int posX, int posY, string title) : base(map)
        {
            _title = title;

            if (int.TryParse(_title, out var songNr))
                Map.MapMusic[0] = songNr;

            IsDead = true;
        }
    }
}

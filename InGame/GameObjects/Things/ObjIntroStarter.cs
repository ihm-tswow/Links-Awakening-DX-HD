using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.InGame.GameObjects.Things
{
    public class ObjIntroStarter : GameObject
    {
        private bool _init;

        public ObjIntroStarter() : base("editor intro") { }

        public ObjIntroStarter(Map.Map map, int posX, int posY) : base(map)
        {
            if (Game1.GameManager.SaveManager.GetString("played_intro") == "1")
            {
                IsDead = true;
                return;
            }

            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
        }

        public void Update()
        {
            if (!_init && MapManager.ObjLink.Map != null)
            {
                _init = true;
                // start the intro
                MapManager.ObjLink.StartIntro();
                // create a save state
                SaveGameSaveLoad.SaveGame(Game1.GameManager);
            }

            if (MapManager.ObjLink.Animation.IsPlaying)
                return;

            // start sitting animation
            MapManager.ObjLink.Animation.Play("intro_sit");

            Game1.GameManager.StartDialogPath("marin_intro");

            Map.Objects.DeleteObjects.Add(this);
        }
    }
}
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Dungeon
{
    class ObjGraveTrigger : GameObject
    {
        private readonly int[] _correctDirection = { 3, 0, 1, 2, 1 };
        private readonly string _triggerKey;

        private int _currentState;

        // object to set a key if the gravestones get moved in the right order in the right direction
        public ObjGraveTrigger() : base("editor grave trigger") { }

        public ObjGraveTrigger(Map.Map map, int posX, int posY, string triggerKey) : base(map)
        {
            Tags = Values.GameObjectTag.None;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            _triggerKey = triggerKey;

            if (string.IsNullOrEmpty(_triggerKey))
            {
                IsDead = true;
                return;
            }

            Game1.GameManager.SaveManager.SetString(_triggerKey, "0");

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
        }

        private void OnKeyChange()
        {
            var reset = true;

            for (var i = 0; i < 5; i++)
            {
                var strKey = Game1.GameManager.SaveManager.GetString("ow_grave_" + i + "_dir");

                // key is set?
                if (!string.IsNullOrEmpty(strKey) && strKey != "-1")
                {
                    reset = false;
                    var correctDirection = _correctDirection[i].ToString() == strKey;

                    // player moved the next gravestone in the correct direction
                    if (correctDirection)
                    {
                        if (_currentState == i)
                        {
                            _currentState++;
                            if (_currentState == 5)
                            {
                                Game1.GameManager.SaveManager.SetString(_triggerKey, "1");

                                // remove the object
                                Map.Objects.DeleteObjects.Add(this);
                            }
                        }
                    }
                    else
                    {
                        // not the correct gravestone moved or in the wrong direction
                        _currentState = 5;
                    }
                }
            }

            // was reset?
            if (reset)
            {
                _currentState = 0;
            }
        }
    }
}

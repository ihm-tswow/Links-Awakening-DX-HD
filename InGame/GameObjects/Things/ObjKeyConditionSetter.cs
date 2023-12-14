using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.Components;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjKeyConditionSetter : GameObject
    {
        private readonly SaveLoad.ConditionNode _condition;
        private readonly string _strKey;

        private bool _init;
        private bool _state;
        // key will be set and reset or just set
        private bool _reset;

        public ObjKeyConditionSetter() : base("editor key condition setter")
        {
            EditorColor = Color.Green;
        }

        public ObjKeyConditionSetter(Map.Map map, int posX, int posY, string strKey, string strCondition, bool reset) : base(map)
        {
            // set the key and delete the object
            if (string.IsNullOrEmpty(strKey) || string.IsNullOrEmpty(strCondition))
            {
                IsDead = true;
                return;
            }

            _strKey = strKey;
            _condition = SaveLoad.SaveCondition.GetConditionNode(strCondition);
            _reset = reset;

            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
        }

        public void KeyChanged()
        {
            var active = _condition.Check();

            if (_init && active == _state)
                return;

            _init = true;

            if (active || _reset)
            {
                // update the state
                _state = active;

                Game1.GameManager.SaveManager.SetString(_strKey, active ? "1" : "0");
                // trigger event on the right map
                Map.Objects.TriggerKeyChange();
            }
        }
    }
}
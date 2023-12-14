using System;

namespace ProjectZ.InGame.GameObjects
{
    public class GameObjectTemplate
    {
        public Type ObjectType;
        public object[] Parameter;

        public GameObjectTemplate(Type objectType, object[] parameter)
        {
            ObjectType = objectType;
            Parameter = parameter;
        }
    }
}
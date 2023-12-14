using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjWater : GameObject
    {
        public override bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                UpdateFieldState();
            }
        }
        private bool _isActive = true;

        private int fieldX;
        private int fieldY;

        public ObjWater() : base("editor water")
        {
            EditorColor = Color.AliceBlue * 0.65f;

        }

        public ObjWater(Map.Map map, int posX, int posY, int depth) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            fieldX = posX / 16;
            fieldY = posY / 16;
            UpdateFieldState();

            AddComponent(CollisionComponent.Index,
                new BoxCollisionComponent(new CBox(posX, posY, -10 + depth, 16, 16, 10), Values.CollisionTypes.Normal));
        }

        private void UpdateFieldState()
        {
            if(_isActive)
                Map.AddFieldState(fieldX, fieldY, MapStates.FieldStates.Water);
            else
            {
                // remove the water state
                var fieldState = Map.GetFieldState(fieldX, fieldY);
                Map.SetFieldState(fieldX, fieldY, fieldState ^ MapStates.FieldStates.Water);
            }
        }
    }
}
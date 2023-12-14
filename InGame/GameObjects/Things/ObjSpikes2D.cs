using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    class ObjSpikes2D : GameObject
    {
        private readonly DamageFieldComponent _damageField;

        public ObjSpikes2D(Map.Map map, int posX, int posY) : base(map)
        {
            SprEditorImage = Resources.SprWhite;
            EditorIconSource = new Rectangle(0, 0, 16, 16);
            EditorColor = Color.Red;

            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            var box = new CBox(posX, posY + 10, 0, 16, 6, 8);
            AddComponent(DamageFieldComponent.Index, _damageField = new DamageFieldComponent(box, HitType.Object, 2)
            {
                OnDamage = DamagePlayer
            });
        }

        private bool DamagePlayer()
        {
            MapManager.ObjLink.InflictSpikeDamage2D();

            return MapManager.ObjLink.HitPlayer(new Vector2(0, -1), _damageField.DamageType, _damageField.Strength, true);
        }
    }
}

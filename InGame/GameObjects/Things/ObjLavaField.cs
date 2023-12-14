using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjLavaField : ObjAnimatedTile
    {
        public ObjLavaField(Map.Map map, int posX, int posY, string spriteId, int frames, int animationSpeed, bool sync, int spriteEffects, int drawLayer)
                     : base(map, posX, posY, spriteId, frames, animationSpeed, sync, spriteEffects, drawLayer)
        {
            Map.AddFieldState(posX / 16, posY / 16, MapStates.FieldStates.Lava);
        }
    }
}
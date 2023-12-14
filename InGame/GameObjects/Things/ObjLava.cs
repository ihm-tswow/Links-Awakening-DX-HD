using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjLava : GameObject
    {
        private readonly Animator _animator;
        private readonly int _animationLength;

        private int _fieldX;
        private int _fieldY;

        public ObjLava() : base("lava") { }

        public ObjLava(Map.Map map, int posX, int posY) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 16, 16);

            Tags = Values.GameObjectTag.Trap;

            _fieldX = posX / 16;
            _fieldY = posY / 16;
            SetActive(true);

            _animator = AnimatorSaveLoad.LoadAnimator("Objects/lava");
            _animator.Play("idle");

            _animationLength = _animator.GetAnimationTime(0, _animator.CurrentAnimation.Frames.Length);

            var sprite = new CSprite(EntityPosition);
            new AnimationComponent(_animator, sprite, Vector2.Zero);

            AddComponent(CollisionComponent.Index, new BoxCollisionComponent(new CBox(posX, posY, -4, 16, 16, 8), Values.CollisionTypes.DeepWater));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawCSpriteComponent(sprite, Values.LayerBackground));
        }

        private void Update()
        {
            // @HACK: this is used to sync all the animations with the same length
            // otherwise they would not be in sync if they did not get updated at the same time
            _animator.SetFrame(0);
            _animator.SetTime(Game1.TotalGameTime % _animationLength);
            _animator.Update();
        }

        public void SetActive(bool active)
        {
            IsActive = active;

            if (active)
                Map.AddFieldState(_fieldX, _fieldY,
                    MapStates.FieldStates.DeepWater | MapStates.FieldStates.Lava);
            else
                Map.SetFieldState(_fieldX, _fieldY, MapStates.FieldStates.None);
        }
    }
}
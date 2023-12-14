using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjFisherman : GameObject
    {
        public BodyComponent Body;
        public readonly Animator Animator;

        private readonly string _personId = "npc_fisherman";

        private float _talkCount;
        private bool _isTransitioning;

        public ObjFisherman() : base("fisherman") { }

        public ObjFisherman(Map.Map map, int posX, int posY) : base(map)
        {
            Animator = AnimatorSaveLoad.LoadAnimator("NPCs/" + _personId);
            Animator.Play("stand");

            EntityPosition = new CPosition(posX + 8, posY + 16, 0);
            EntitySize = new Rectangle(-8, -16, 16, 16);

            var sprite = new CSprite(EntityPosition);
            var animationComponent = new AnimationComponent(Animator, sprite, new Vector2(0, 0));

            Game1.GameManager.SaveManager.SetString("enterPond", "no");

            Body = new BodyComponent(EntityPosition, -8, -11, 15, 11, 8);

            AddComponent(BodyComponent.Index, Body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(Body, Values.CollisionTypes.Normal));
            AddComponent(InteractComponent.Index, new InteractComponent(Body.BodyBox, Interact));
            AddComponent(AnimationComponent.Index, animationComponent);
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new BodyDrawComponent(Body, sprite, Values.LayerPlayer));
            AddComponent(DrawShadowComponent.Index, new DrawShadowCSpriteComponent(sprite));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(KeyChanged));
        }

        private void Update()
        {
            if (_talkCount > 0)
                _talkCount -= Game1.DeltaTime;
            else
            {
                Animator.Play("stand");
            }
        }

        private bool Interact()
        {
            Animator.Play("talk");
            _talkCount = 250;

            Game1.GameManager.StartDialogPath(_personId);
            return true;
        }

        private void KeyChanged()
        {
            // spawn object
            if (_isTransitioning || Game1.GameManager.SaveManager.GetString("enterPond") != "yes") return;

            _isTransitioning = true;

            MapManager.ObjLink.MapTransitionStart = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.MapTransitionEnd = MapManager.ObjLink.EntityPosition.Position;
            MapManager.ObjLink.TransitionOutWalking = false;

            // append a map change
            ((MapTransitionSystem)Game1.GameManager.GameSystems[typeof(MapTransitionSystem)]).AppendMapChange(
                "pond.map", "entry", true, false, Values.MapTransitionColor, false);
        }
    }
}
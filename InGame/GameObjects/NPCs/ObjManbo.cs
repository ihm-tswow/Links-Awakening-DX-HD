using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.NPCs
{
    internal class ObjManbo : GameObject
    {
        struct AnimationKeyframe
        {
            public float Time;
            public int Animation;

            public AnimationKeyframe(float time, int animation)
            {
                Time = time;
                Animation = animation;
            }
        }

        // 0 idle
        // 1 close mouth
        // 2 angry
        // 3 eye
        private AnimationKeyframe[] _songKeyframes = new AnimationKeyframe[]
        {
            new AnimationKeyframe( 0.00f, 0),
            new AnimationKeyframe( 2.65f, 1),
            new AnimationKeyframe( 3.35f, 2),
            new AnimationKeyframe( 4.05f, 1),
            new AnimationKeyframe( 4.55f, 0),
            new AnimationKeyframe( 8.00f, 3),
            new AnimationKeyframe(13.35f, 3),
            new AnimationKeyframe(18.75f, 3),
            new AnimationKeyframe(22.75f, 3),
            new AnimationKeyframe(24.55f, 2),
            new AnimationKeyframe(25.25f, 1)
        };

        private readonly BodyComponent _body;
        private readonly Animator _animator;
        private readonly Animator _animatorEye;
        private readonly Animator _animatorMouth;
        private readonly DictAtlasEntry _spriteTextbox;

        private readonly ObjDancingFish _leftFish;
        private readonly ObjDancingFish _rightFish;
        private ObjOnPushDialog _objPushDialog;

        private float _songCounter;
        private int _songIndex;
        private int _animationIndex;

        private int _fishAnimationIndex;
        private int _fishAnimationDirection = -1;
        private int _lastEyeIndex;

        private Vector2 _startPosition;

        private bool _isPlaying;
        private bool _startedPlaying;

        public ObjManbo() : base("manbo") { }

        public ObjManbo(Map.Map map, int posX, int posY, string saveKey) : base(map)
        {
            EntityPosition = new CPosition(posX, posY, 0);
            EntitySize = new Rectangle(0, 0, 32, 48);

            _startPosition = new Vector2(posX, posY);

            _spriteTextbox = Resources.GetSprite("manbo oh");

            _animator = AnimatorSaveLoad.LoadAnimator("NPCs/manbo");
            _animatorEye = AnimatorSaveLoad.LoadAnimator("NPCs/manbo");
            _animatorMouth = AnimatorSaveLoad.LoadAnimator("NPCs/manbo");
            _animator.Play("fish_static");
            _animatorEye.Play("eye");
            _animatorMouth.Play("mouth_idle");

            _body = new BodyComponent(EntityPosition, 0, 12, 32, 24, 8)
            {
                IgnoresZ = true
            };

            var interactBox = new CBox(posX, posY, 0, 32, 48, 8);
            AddComponent(InteractComponent.Index, new InteractComponent(interactBox, OnInteract));

            AddComponent(BodyComponent.Index, _body);
            AddComponent(CollisionComponent.Index, new BodyCollisionComponent(_body, Values.CollisionTypes.Normal));
            AddComponent(KeyChangeListenerComponent.Index, new KeyChangeListenerComponent(OnKeyChange));
            AddComponent(UpdateComponent.Index, new UpdateComponent(Update));
            AddComponent(DrawComponent.Index, new DrawComponent(Draw, Values.LayerPlayer, EntityPosition));

            _leftFish = new ObjDancingFish(map, new Vector2(posX - 25, posY + 70));
            _rightFish = new ObjDancingFish(map, new Vector2(posX + 23, posY + 70));
            map.Objects.SpawnObject(_leftFish);
            map.Objects.SpawnObject(_rightFish);

            if (Game1.GameManager.SaveManager.GetString("manbo") != "1")
            {
                _objPushDialog = new ObjOnPushDialog(map, posX - 24, posY - 32, 32, 90 + 32, "manbo");
                map.Objects.SpawnObject(_objPushDialog);
            }
        }

        private bool OnInteract()
        {
            Game1.GameManager.StartDialogPath("manbo");

            return true;
        }

        private void OnKeyChange()
        {
            var startSing = Game1.GameManager.SaveManager.GetString("manbo_start_song");
            if (!_startedPlaying && startSing == "1")
            {
                Game1.GameManager.SaveManager.SetString("manbo_start_song", "0");
                StartSong();
            }
        }

        private void StartSong()
        {
            Game1.GameManager.SetMusic(47, 2);
            _isPlaying = true;
            _startedPlaying = true;

            _animator.Play("fish");
            PlayFishAnimation("forward");

            if (_objPushDialog != null)
            {
                Map.Objects.DeleteObjects.Add(_objPushDialog);
                _objPushDialog = null;
            }
        }

        private void Update()
        {
            _lastEyeIndex = _animatorEye.CurrentFrameIndex;

            _animator.Update();
            _animatorMouth.Update();
            _animatorEye.Update();

            if (_animationIndex == 2 && _songIndex > 6)
            {
                PlayFishAnimation("splash");
            }
            
            // fish eye roll animation
            if (_animationIndex == 3)
            {
                if (_lastEyeIndex < _animatorEye.CurrentFrameIndex)
                {
                    _fishAnimationIndex += _fishAnimationDirection;

                    if (_fishAnimationIndex == 1)
                        PlayFishAnimation("left");
                    else if (_fishAnimationIndex == 2)
                        PlayFishAnimation("forward");
                    else if (_fishAnimationIndex == 3)
                        PlayFishAnimation("right");
                }

                if (!_animatorEye.IsPlaying)
                {
                    PlayAnimation(0);
                }
            }

            if (_isPlaying)
            {
                MapManager.ObjLink.FreezePlayer();
                Game1.GameManager.InGameOverlay.DisableInventoryToggle = true;

                _songCounter += Game1.DeltaTime;

                // new keyframe?
                if (_songCounter >= _songKeyframes[_songIndex].Time * 1000)
                {
                    if (_animationIndex == 2)
                        PlayFishAnimation("idle");
                    
                    // set the animations
                    PlayAnimation(_songKeyframes[_songIndex].Animation);

                    // finished playing?
                    _songIndex++;
                    if (_songIndex >= _songKeyframes.Length)
                    {
                        _isPlaying = false;
                        _animator.Play("fish_static");
                        Game1.GameManager.StartDialogPath("manbo_finished");
                    }
                }
            }
        }

        private void PlayFishAnimation(string animationId)
        {
            _leftFish.Animator.Play(animationId);
            _rightFish.Animator.Play(animationId);
        }

        private void PlayAnimation(int animationIndex)
        {
            _animationIndex = animationIndex;

            // 0 idle
            // 1 close mouth
            // 2 angry
            // 3 eye
            switch (_animationIndex)
            {
                case 0:
                    _animator.Continue();
                    _animatorEye.Play("eye");
                    _animatorMouth.Play("mouth_slow");
                    break;
                case 1:
                    _animator.Pause();
                    _animatorEye.Play("eye");
                    _animatorMouth.Play("mouth_closed");
                    break;
                case 2:
                    _animator.Continue();
                    _animatorEye.Play("eye_angry");
                    _animatorMouth.Play("mouth_slow");
                    break;
                case 3:
                    _lastEyeIndex = 1;
                    _fishAnimationDirection = -_fishAnimationDirection;
                    _animator.Pause();
                    _animatorEye.Play("eye_roll");
                    _animatorMouth.Play("mouth_slow");
                    break;
            }
        }

        private void Draw(SpriteBatch spriteBatch)
        {
            _animator.Draw(spriteBatch, EntityPosition.Position, Color.White);
            _animatorMouth.Draw(spriteBatch, new Vector2(EntityPosition.X, EntityPosition.Y + 17), Color.White);
            _animatorEye.Draw(spriteBatch, new Vector2(EntityPosition.X + 1, EntityPosition.Y + 12), Color.White);

            if (_animationIndex == 2)
            {
                DrawHelper.DrawNormalized(spriteBatch, _spriteTextbox, new Vector2(_startPosition.X - 24, _startPosition.Y - 8), Color.White);
                if (_songIndex > 6)
                {
                    DrawHelper.DrawNormalized(spriteBatch, _spriteTextbox, new Vector2(_startPosition.X - 56, _startPosition.Y + 42), Color.White);
                    DrawHelper.DrawNormalized(spriteBatch, _spriteTextbox, new Vector2(_startPosition.X - 8, _startPosition.Y + 42), Color.White);
                }
            }
        }
    }
}
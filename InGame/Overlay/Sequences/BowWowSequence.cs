using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;
using System;

namespace ProjectZ.InGame.Overlay.Sequences
{
    class BowWowSequence : GameSequence
    {
        private SeqAnimation _aniLink;
        private SeqAnimation _aniBowWow;
        private SeqSprite _spriteParticle;
        private SeqSprite _spriteSmoke0;
        private SeqSprite _spriteSmoke1;

        private SeqSprite _spritePhoto;

        private SeqColor _spritePhotoFlash;
        private float _flashPercentage = 0;

        private Vector2 _bowWowVelocity;
        private int _bowWowDirection;

        private Vector2 _chainStartPosition;
        private float _linkVelocity;

        private SeqSprite[] _chain = new SeqSprite[5];

        private bool _isWalking;
        private bool _blocked;
        private bool _ending;
        private bool _showPicture;
        private bool _attack;

        private int _blockCount;

        private float _bowWowAttackPosition;

        private double _counter;
        private int _sequenceIndex;

        private double _particleCounter;

        public BowWowSequence()
        {
            _sequenceWidth = 160;
            _sequenceHeight = 144;
        }

        public override void OnStart()
        {
            Sprites.Clear();
            SpriteDict.Clear();

            var position = Vector2.Zero;

            _isWalking = false;
            _blocked = false;
            _ending = false;
            _attack = false;
            _showPicture = false;

            _blockCount = 0;
            _linkVelocity = 0;
            _particleCounter = 0;

            _counter = 0;
            _sequenceIndex = 0;

            _flashPercentage = 0;

            // background
            Sprites.Add(new SeqSprite("bowWow_background", position, 0));
            Sprites.Add(_spritePhoto = new SeqSprite("photo_6", position, 5) { Color = Color.Transparent });

            _chainStartPosition = new Vector2(position.X + 90, position.Y + 103 - 6);
            for (var i = 0; i < _chain.Length; i++)
                Sprites.Add(_chain[i] = new SeqSprite("seqBowWowChain", _chainStartPosition, 1) { Color = Color.White * 0.75f });

            // link and marin
            Sprites.Add(_aniLink = new SeqAnimation("Sequences/bowWow link", "stand", new Vector2(position.X + 134, position.Y + 104), 3) { Shader = Resources.ColorShader, Color = Game1.GameManager.CloakColor });
            Sprites.Add(_aniBowWow = new SeqAnimation("Sequences/bowWow", "idle_-1", new Vector2(position.X + 95, position.Y + 103), 2));

            // block particle
            Sprites.Add(_spriteParticle = new SeqSprite("seqBowWowParticle", _chainStartPosition, 4) { Color = Color.Transparent });
            Sprites.Add(_spriteSmoke0 = new SeqSprite("seqBowWowSmoke", _chainStartPosition, 4) { Color = Color.Transparent });
            Sprites.Add(_spriteSmoke1 = new SeqSprite("seqBowWowSmoke", _chainStartPosition, 4) { Color = Color.Transparent, SpriteEffect = SpriteEffects.FlipHorizontally });

            Sprites.Add(_spritePhotoFlash = new SeqColor(new Rectangle((int)position.X, (int)position.Y, 160, 144), Color.Transparent, 5));

            // start the sequence path
            Game1.GameManager.StartDialogPath("photo_sequence_3");

            _bowWowDirection = -1;
            _bowWowVelocity.X = 0.75f * _bowWowDirection;

            base.OnStart();
        }

        public override void Update()
        {
            if (_flashPercentage > 0 && _counter > 125)
                _flashPercentage = AnimationHelper.MoveToTarget(_flashPercentage, 0, Game1.TimeMultiplier * 0.075f);
            _spritePhotoFlash.Color = Color.White * _flashPercentage;

            // do not update the sceen while the dialog box is open
            if (Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen)
                return;

            if (_showPicture)
            {
                _counter += Game1.DeltaTime;
                if (_counter > 2500)
                    Game1.GameManager.InGameOverlay.CloseOverlay();

                return;
            }

            base.Update();

            _counter += Game1.DeltaTime;
            if (_sequenceIndex == 0 && _counter > 1000)
            {
                _sequenceIndex = 1;
                _counter -= 1000;
                Game1.GameManager.StartDialogPath("photo_sequence_3");
            }
            else if (_sequenceIndex == 1 && _counter > 250)
            {
                _sequenceIndex = 2;
                _counter -= 250;
                _isWalking = true;
                _aniLink.Animator.Play("walk");
            }

            // draw the particle?
            if (_particleCounter > 0)
                _particleCounter -= Game1.DeltaTime;
            _spriteParticle.Color = _particleCounter <= 0 ? Color.Transparent : Color.White;

            if (_attack)
            {
                if (_counter > 250)
                {
                    _spriteSmoke0.Position = new Vector2(_aniBowWow.Position.X - 17, _aniBowWow.Position.Y - 30);
                    _spriteSmoke1.Position = new Vector2(_aniBowWow.Position.X + 17, _aniBowWow.Position.Y - 30);
                }
                if (_counter > 500)
                {
                    _flashPercentage = 1;
                    _spritePhotoFlash.Color = Color.White * _flashPercentage;
                    Game1.GameManager.PlaySoundEffect("D378-63-40");

                    _showPicture = true;
                    _spritePhoto.Color = Color.White;
                    _counter = 0;
                }

                return;
            }

            if (_ending)
            {
                if (_counter > 1500)
                    _aniLink.Animator.Play("piece");
                else if (_counter > 500)
                    _aniLink.Animator.Play("stand");

                // move BowWow up
                if (_counter > 2500)
                {
                    // move up
                    _aniBowWow.Position.X = _bowWowAttackPosition + MathF.Sin(((float)_counter - 2500) / 75);
                    _aniBowWow.Position.Y -= 0.25f * Game1.TimeMultiplier;

                    if (_aniBowWow.Animator.CurrentAnimation.Id != "pre_attack")
                        _aniBowWow.Animator.Play("pre_attack");
                    else if (!_aniBowWow.Animator.IsPlaying)
                    {
                        _attack = true;
                        _counter = 0;
                        _aniBowWow.Animator.Play("attack");
                        _spriteSmoke0.Color = Color.White;
                        _spriteSmoke0.Position = new Vector2(_aniBowWow.Position.X - 15, _aniBowWow.Position.Y - 28);
                        _spriteSmoke1.Color = Color.White;
                        _spriteSmoke1.Position = new Vector2(_aniBowWow.Position.X + 15, _aniBowWow.Position.Y - 28);
                    }
                }
                else
                {
                    _bowWowAttackPosition = _aniBowWow.Position.X;
                }

                return;
            }

            // BowWow movement (do not move after getting blocked and falling on the floor)
            _aniBowWow.Position += _bowWowVelocity * Game1.TimeMultiplier;
            _bowWowVelocity.Y += 0.15f * Game1.TimeMultiplier;

            // BowWow jump
            if (_aniBowWow.Position.Y > 103)
            {
                _bowWowVelocity.X = 0;
                _aniBowWow.Position.Y = 103;

                if (_blockCount < 3)
                {
                    if (!_blocked)
                    {
                        _bowWowVelocity.X = 0.75f * _bowWowDirection;

                        if (Game1.RandomNumber.Next(0, 3) == 0)
                            _bowWowVelocity.Y = -1.75f;
                        else
                            _bowWowVelocity.Y = -1.0f;
                    }
                    else if (_aniBowWow.Animator.CurrentAnimation.Id != "open_-1")
                    {
                        // open the mouth after landing after a block
                        _aniBowWow.Animator.Play("open_-1");
                    }
                }
                else
                {
                    _ending = true;
                }
            }

            // update link walking towards BowWow
            if (_isWalking)
            {
                if (!_blocked && _blockCount < 3)
                {
                    _aniLink.Animator.Play("walk");
                    _aniLink.Position.X -= 0.125f * Game1.TimeMultiplier;
                }
                else
                    _aniLink.Position.X += _linkVelocity * Game1.TimeMultiplier;

                _linkVelocity = AnimationHelper.MoveToTarget(_linkVelocity, 0, 0.065f * Game1.TimeMultiplier);

                if (_blocked && _aniBowWow.Position.Y == 103 && !_aniBowWow.Animator.IsPlaying && _blockCount <= 3)
                {
                    _blocked = false;
                    Game1.GameManager.StartDialogPath("photo_sequence_3");
                }

                // collision with BowWow
                if (_aniBowWow.Position.X + 12 > _aniLink.Position.X)
                {
                    if (_blockCount < 3)
                    {
                        _blockCount++;

                        Game1.GameManager.PlaySoundEffect("D360-07-07");

                        // show block particle
                        _particleCounter = 175;
                        _spriteParticle.Position = new Vector2(_aniLink.Position.X - 12, _aniLink.Position.Y - 14);

                        _aniBowWow.Animator.Play("idle_-1");
                        if (_blockCount < 3)
                        {
                            _bowWowVelocity.X = -_bowWowVelocity.X;
                            _bowWowVelocity.Y = -1;
                            _bowWowDirection = -_bowWowDirection;
                        }
                        else if (!_blocked)
                        {
                            _counter = 0;
                            _bowWowVelocity.Y = -1;
                        }

                        _blocked = true;
                        _aniLink.Animator.Play("blocked");
                        _linkVelocity += 1;
                    }
                }
            }

            if (_aniBowWow.Position.X < 56 && _bowWowDirection < 0)
            {
                _bowWowVelocity.X = -_bowWowVelocity.X;
                _bowWowDirection = -_bowWowDirection;
                _aniBowWow.Animator.Play("idle_1");
            }
            else if (_aniBowWow.Position.X > 118 && _bowWowDirection > 0)
            {
                _bowWowVelocity.X = -_bowWowVelocity.X;
                _bowWowDirection = -_bowWowDirection;
                _aniBowWow.Animator.Play("idle_-1");
            }

            // update the chain position
            for (var i = 0; i < _chain.Length; i++)
            {
                if (_chain[i].Position.Y < _chainStartPosition.Y)
                    _chain[i].Position.Y += 0.25f * Game1.TimeMultiplier;

                if (_chain[i].Position.Y > _chainStartPosition.Y)
                    _chain[i].Position.Y = _chainStartPosition.Y;
            }

            var lastPosition = new Vector2(_aniBowWow.Position.X + _bowWowDirection * 4, _aniBowWow.Position.Y - 8);
            for (var i = _chain.Length - 1; i > 0; i--)
            {
                var direction = _chain[i].Position - lastPosition;
                if (direction.Length() > 8)
                {
                    direction.Normalize();
                    _chain[i].Position = lastPosition + direction * 8;
                }
                lastPosition = _chain[i].Position;
            }

            lastPosition = _chainStartPosition;
            for (var i = 0; i < _chain.Length; i++)
            {
                var direction = _chain[i].Position - lastPosition;
                if (direction.Length() > 8)
                {
                    direction.Normalize();
                    _chain[i].Position = lastPosition + direction * 8;
                }
                lastPosition = _chain[i].Position;
            }
        }
    }
}

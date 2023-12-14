using System;
using System.IO;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.SaveLoad
{
    public class AnimatorSaveLoad
    {
        public static void SaveAnimator(string path, Animator animator)
        {
            var pathTemp = path + ".temp";
            var writer = new StreamWriter(pathTemp);

            var saveString = animator.SpritePath;
            // animation version
            writer.WriteLine("1");
            writer.WriteLine(saveString);

            for (var i = 0; i < animator.Animations.Count; i++)
            {
                saveString = animator.Animations[i].Id + ";";
                saveString += animator.Animations[i].NextAnimation + ";";
                saveString += animator.Animations[i].LoopCount + ";";

                saveString += animator.Animations[i].Offset.X + ";";
                saveString += animator.Animations[i].Offset.Y + ";";

                saveString += animator.Animations[i].Frames.Length;

                // write frames
                for (var j = 0; j < animator.Animations[i].Frames.Length; j++)
                {
                    saveString += ";" +
                                    animator.Animations[i].Frames[j].FrameTime + ";" +

                                    animator.Animations[i].Frames[j].SourceRectangle.X + ";" +
                                    animator.Animations[i].Frames[j].SourceRectangle.Y + ";" +
                                    animator.Animations[i].Frames[j].SourceRectangle.Width + ";" +
                                    animator.Animations[i].Frames[j].SourceRectangle.Height + ";" +

                                    animator.Animations[i].Frames[j].Offset.X + ";" +
                                    animator.Animations[i].Frames[j].Offset.Y + ";" +

                                    animator.Animations[i].Frames[j].CollisionRectangle.X + ";" +
                                    animator.Animations[i].Frames[j].CollisionRectangle.Y + ";" +
                                    animator.Animations[i].Frames[j].CollisionRectangle.Width + ";" +
                                    animator.Animations[i].Frames[j].CollisionRectangle.Height + ";" +

                                    animator.Animations[i].Frames[j].MirroredV + ";" +
                                    animator.Animations[i].Frames[j].MirroredH;
                }

                writer.WriteLine(saveString);
            }

            writer.Close();

            File.Delete(path);
            File.Move(pathTemp, path);
        }

        public static Animator LoadAnimator(string animatorId)
        {
            // TODO_End: preload all the animations
            return LoadAnimatorFile(Values.PathAnimationFolder + animatorId + ".ani");
        }

        public static Animator LoadAnimatorFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var reader = new StreamReader(filePath);

            var animator = new Animator();

            var version = reader.ReadLine();
            animator.SpritePath = reader.ReadLine();
            animator.SprTexture = Resources.GetTexture(animator.SpritePath);

            // load the animations
            while (!reader.EndOfStream)
            {
                var strLine = reader.ReadLine();

                if (strLine == null)
                    continue;

                var strSplit = strLine.Split(';');

                if (strSplit.Length < 16)
                    continue;

                var pos = 0;
                var animationId = strSplit[pos].ToLower();

                var animation = new Animation(animationId);

                animation.NextAnimation = strSplit[pos += 1].ToLower();
                animation.LoopCount = Convert.ToInt32(strSplit[pos += 1]);
                animation.Offset.X = Convert.ToInt32(strSplit[pos += 1]);
                animation.Offset.Y = Convert.ToInt32(strSplit[pos += 1]);

                var frames = Convert.ToInt32(strSplit[pos += 1]);
                animation.Frames = new Frame[frames];

                animator.AddAnimation(animation);

                for (var i = 0; i < frames; i++)
                {
                    animator.SetFrameAt(animationId, i, new Frame()
                    {
                        FrameTime = Convert.ToInt32(strSplit[pos += 1]),

                        SourceRectangle = new Rectangle(
                            Convert.ToInt32(strSplit[pos += 1]), Convert.ToInt32(strSplit[pos += 1]),
                            Convert.ToInt32(strSplit[pos += 1]), Convert.ToInt32(strSplit[pos += 1])),

                        Offset = new Point(Convert.ToInt32(strSplit[pos += 1]), Convert.ToInt32(strSplit[pos += 1])),

                        CollisionRectangle = new Rectangle(
                            Convert.ToInt32(strSplit[pos += 1]), Convert.ToInt32(strSplit[pos += 1]),
                            Convert.ToInt32(strSplit[pos += 1]), Convert.ToInt32(strSplit[pos += 1])),

                        MirroredV = Convert.ToBoolean(strSplit[pos += 1]),
                        MirroredH = Convert.ToBoolean(strSplit[pos += 1])
                    });
                }
            }

            reader.Close();

            return animator;
        }
    }
}

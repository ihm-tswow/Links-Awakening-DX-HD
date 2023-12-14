using Microsoft.Xna.Framework;

namespace ProjectZ.Base
{
    // source:
    // http://community.monogame.net/t/a-simple-monogame-fps-display-class/10545

    public class SimpleFps
    {
        public double MsgFrequency = 1.0f;
        public string Msg = "";

        private double _frames;
        private double _updates;
        private double _elapsed;
        private double _last;
        private double _now;

        /// <summary>
        /// The msgFrequency here is the reporting time to update the message.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            _now = gameTime.TotalGameTime.TotalSeconds;
            _elapsed = _now - _last;

            if (_elapsed > MsgFrequency)
            {
                Msg = $"fps:          {_frames / _elapsed,7:N3}" +
                      $"\nupdates:      {_updates,3:N0}" +
                      $"\nframes:       {_frames,3:N0}" +
                      $"\nelapsed time: {_elapsed,7:N3}";

                _frames = 0;
                _updates = 0;
                _last = _now;
            }

            _updates++;
        }

        public void CountDraw()
        {
            _frames++;
        }
    }
}
using System;
using System.Windows.Forms;

namespace ProjectZ
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var editorMode = false;
            var loadFirstSave = false;

            foreach (var arg in args)
            {
                if (arg == "editor")
                    editorMode = true;
                else if (arg == "loadSave")
                    loadFirstSave = true;
            }

            try
            {
                using (var game = new Game1(editorMode, loadFirstSave))
                    game.Run();
            }
            catch (Exception exception)
            {
               MessageBox.Show(exception.StackTrace, exception.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
               throw;
            }
        }
    }
}
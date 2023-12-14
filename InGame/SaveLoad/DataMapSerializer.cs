using System.IO;
#if WINDOWS
using System.Windows.Forms;
#endif

namespace ProjectZ.InGame.SaveLoad
{
    class DataMapSerializer
    {
        public static void SaveDialog(string[,] data)
        {
#if WINDOWS
            var openFileDialog = new SaveFileDialog
            {
                RestoreDirectory = true,
                Filter = "Data (*.data)|*.data"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                SaveData(openFileDialog.FileName, data);
#endif
        }

        public static void LoadDialog(ref string[,] data)
        {
#if WINDOWS
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Data (*.data)|*.data",
                RestoreDirectory = true,
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            data = LoadData(openFileDialog.FileName);
#endif
        }
        
        public static void SaveData(string path, string[,] data)
        {
            var writer = new StreamWriter(path);

            // write down the size
            writer.WriteLine(data.GetLength(0));
            writer.WriteLine(data.GetLength(1));

            for (var y = 0; y < data.GetLength(1); y++)
            {
                var line = "";
                for (var x = 0; x < data.GetLength(0); x++)
                    line += data[x, y] + ";";

                writer.WriteLine(line);
            }

            writer.Close();
        }

        public static string[,] LoadData(string path)
        {
            var reader = new StreamReader(path);

            var lengthX = int.Parse(reader.ReadLine());
            var lengthY = int.Parse(reader.ReadLine());

            var output = new string[lengthX, lengthY];

            for (var y = 0; y < lengthY; y++)
            {
                var line = reader.ReadLine();
                var split = line.Split(';');

                for (var x = 0; x < lengthX; x++)
                    output[x, y] = split[x];
            }

            reader.Close();

            return output;
        }
    }
}

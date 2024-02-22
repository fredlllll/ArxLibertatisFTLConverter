using System.IO;

namespace ArxLibertatisFTLConverter
{
    public static class Util
    {
        public const string GltfVertexAttributePosition = "POSITION";
        public const string GltfVertexAttributeNormal = "NORMAL";
        public const string GltfVertexAttributeTextureCoordinate0 = "TEXCOORD_0";

        public static string GetParentWithName(string dirPath, string name)
        {
            DirectoryInfo di = new(dirPath);
            while (true)
            {
                if (di.Name == name)
                {
                    return di.FullName;
                }
                di = di.Parent;
                if (di == null)
                {
                    return null;
                }
            }
        }

        public static string GetActualTexturePath(string dataDir, string texture)
        {
            string tmpName = Path.Combine(dataDir, Path.GetDirectoryName(texture), Path.GetFileNameWithoutExtension(texture));
            if (File.Exists(tmpName + ".jpg"))
            {
                return tmpName + ".jpg";
            }
            else if (File.Exists(tmpName + ".bmp"))
            {
                return tmpName + ".bmp";
            }
            return null;
        }

        public static string GuessFtlDataDir(string filePath)
        {
            string fileDir = Path.GetDirectoryName(filePath);
            string gameDir = GetParentWithName(fileDir, "game");
            if (gameDir == null)
            {
                return fileDir;
            }
            return Path.GetDirectoryName(gameDir);
        }
    }
}

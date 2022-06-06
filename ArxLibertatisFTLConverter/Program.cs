using System;
using System.IO;

namespace ArxLibertatisFTLConverter
{
    class Program
    {
        static void ConvertFile(string file)
        {
            var fileLower = file.ToLowerInvariant();

            if (fileLower.EndsWith(".ftl"))
            {
                ConvertFTLToOBJ.Convert(file);
            }
            else if (fileLower.EndsWith(".obj"))
            {
                ConvertOBJToFTL.Convert(file);
            }
        }

        static void Main(string[] args)
        {
            foreach (string path in args)
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine("Can't find file " + path);
                    continue;
                }
                ConvertFile(path);
            }
        }
    }
}

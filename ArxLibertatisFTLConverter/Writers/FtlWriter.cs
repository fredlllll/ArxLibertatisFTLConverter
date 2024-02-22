using ArxLibertatisEditorIO.RawIO.FTL;
using System;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ArxLibertatisFTLConverter.Writers
{
    public class FtlWriter : IModelWriter
    {
        public string FilePath { get; set; }

        public void Write(IntermediateModel intermediateModel)
        {
            string fileDir = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(fileDir);

            FTL_IO ftl_io = new();
            intermediateModel.Ftl.SaveTo(ftl_io);

            using MemoryStream ms = new();
            ftl_io.WriteTo(ms);
            ms.Position = 0;
            using FileStream fs = new(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            FTL_IO.EnsurePacked(ms).CopyTo(fs);

            //check groups debug
            foreach(var group in ftl_io._3DDataSection.groups)
            {
                foreach(var index in group.indices)
                {
                    if(index < 0 || index >= ftl_io._3DDataSection.vertexList.Length)
                    {
                        throw new Exception();
                    }
                }
            }

            if (intermediateModel.DataDir != null)
            {
                foreach (var texture in intermediateModel.Ftl.dataSection3D.textures)
                {
                    string texPath = Util.GetActualTexturePath(intermediateModel.DataDir, texture);
                    if (texPath != null)
                    {
                        var relativeTexPath = Path.GetRelativePath(intermediateModel.DataDir, texPath);
                        var relativeTexDir = Path.GetDirectoryName(relativeTexPath);
                        if (!string.IsNullOrEmpty(relativeTexDir))
                        {
                            Directory.CreateDirectory(Path.Combine(fileDir, relativeTexDir));
                        }
                        File.Copy(texPath, Path.Combine(fileDir, relativeTexPath), true);
                    }
                }
            }
        }
    }
}

using ArxLibertatisEditorIO.MediumIO.FTL;
using ArxLibertatisEditorIO.RawIO.FTL;
using System;
using System.IO;

namespace ArxLibertatisFTLConverter.Readers
{
    public class FtlReader : IModelReader
    {
        public string FilePath { get; set; }

        public IntermediateModel Read()
        {
            FTL_IO ftl_io = new();

            using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var s = FTL_IO.EnsureUnpacked(fs);
            ftl_io.ReadFrom(s);

            Ftl ftl = new();
            ftl.LoadFrom(ftl_io);

            return new IntermediateModel()
            {
                Ftl = ftl,
                DataDir = Util.GuessFtlDataDir(FilePath),
            };
        }
    }
}

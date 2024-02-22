using ArxLibertatisFTLConverter.Readers;
using ArxLibertatisFTLConverter.Writers;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;

namespace ArxLibertatisFTLConverter
{
    class Program
    {
        static IModelReader GetReader(string filePath)
        {
            filePath = Path.GetFullPath(filePath);
            Console.WriteLine($"Input File: {filePath}");
            var fileExt = Path.GetExtension(filePath.ToLowerInvariant());
            switch (fileExt)
            {
                case ".ftl":
                    return new FtlReader() { FilePath = filePath };
                case ".obj":
                    return new ObjReader() { FilePath = filePath };
                case ".gltf":
                case ".glb":
                    return new GltfReader() { FilePath = filePath };
            }
            return null;
        }

        static IModelWriter GetWriter(string filePath)
        {
            filePath = Path.GetFullPath(filePath);
            Console.WriteLine($"Output File: {filePath}");
            var fileExt = Path.GetExtension(filePath.ToLowerInvariant());
            switch (fileExt)
            {
                case ".ftl":
                    return new FtlWriter() { FilePath = filePath };
                case ".obj":
                    return new ObjWriter() { FilePath = filePath };
                case ".gltf":
                case ".glb":
                    return new GltfWriter() { FilePath = filePath };
            }
            return null;
        }

        static void MainParsed(CommandLineOptions options)
        {
            var reader = GetReader(options.InputFile);
            if (reader == null)
            {
                Console.WriteLine("Can't find reader for file " + options.InputFile);
                return;
            }
            var writer = GetWriter(options.OutputFile);
            if (writer == null)
            {
                Console.WriteLine("Can't find writer for file " + options.OutputFile);
                return;
            }

            var intermediateModel = reader.Read();
            writer.Write(intermediateModel);
        }

        static void MainNotParsed(IEnumerable<Error> errors)
        {
            foreach (var err in errors)
            {
                Console.WriteLine(err);
            }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(MainParsed).WithNotParsed(MainNotParsed);
        }
    }
}

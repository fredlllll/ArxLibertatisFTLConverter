using CommandLine;

namespace ArxLibertatisFTLConverter
{

    public class CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "Input file path")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file path")]
        public string OutputFile { get; set; }
    }
}

namespace ArxLibertatisFTLConverter
{
    public interface IModelReader
    {
        string FilePath { get; set; }
        IntermediateModel Read();
    }
}

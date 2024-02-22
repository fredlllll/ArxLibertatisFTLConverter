namespace ArxLibertatisFTLConverter
{
    public interface IModelWriter
    {
        string FilePath { get; set; }
        void Write(IntermediateModel intermediateModel);
    }
}

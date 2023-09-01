namespace Entities.LinkModels
{
    // since our response needs to describe the root of the controller
    // we need a warpper for our links:
    public class LinkCollectionWrapper<T> : LinkResourceBase
    {
        public List<T> Value { get; set; } = new List<T>();
        public LinkCollectionWrapper() 
        { }
        public LinkCollectionWrapper(List<T> value)
        {
            Value = value;
        }
    }
}

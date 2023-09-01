namespace Entities.Excepions
{
    public abstract class BadRequestException : Exception
    {
        public BadRequestException(string message) 
            : base(message) { }
    }
}

namespace Entities.Excepions
{
    public sealed class CollectionsByIdsBadRequestException : BadRequestException
    {
        public CollectionsByIdsBadRequestException()
            : base("Collections count mismatch comparing to ids")
        { }
    }
}

namespace Entities.Excepions
{
    public sealed class IdParametrBadRequestException : BadRequestException
    {
        public IdParametrBadRequestException() :
            base("Parametr id is null")
        { }
    }
}

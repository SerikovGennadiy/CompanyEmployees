namespace Entities.Excepions
{
    public sealed class CompanyCollectionBadRequestException : BadRequestException
    {
       public CompanyCollectionBadRequestException()
            : base("Company collection send from a client is null")
        { }
    }
}

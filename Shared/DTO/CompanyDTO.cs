namespace Shared.DTO
{
    //[Serializable]
    //public record CompanyDTO(Guid Id, string Name, string FullAddress);
    public record CompanyDTO
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? FullAddress { get; init; }
    }
}
namespace Entities.Responses
{
    // base absract class for any NotFounds (incapsulate data for error handling)
    public abstract class ApiNotFoundResponse : ApiBaseResponse
    {
        public string Message { get; set; }
        public ApiNotFoundResponse(string message) 
            : base(false)
        {
            Message = message;
        }
    }
}

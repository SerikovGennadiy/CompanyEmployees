namespace Entities.Responses
{
    // base absract class for any BadRequests (incapsulate data for error handling)
    public abstract class ApiBadRequestResponse : ApiBaseResponse
    {
        public string Message { get; set; }
        public ApiBadRequestResponse(string message) 
            : base(false)
        {
            Message = message;
        }
    }
}

namespace Entities.Responses
{
    public sealed class ApiOkResponse<TResult> : ApiBaseResponse
    {
        // for query response returned data ( anything data )
        public TResult result { get; set; }

        public ApiOkResponse(TResult result)
            : base(true)
        {
            this.result = result;
        }
    }
}

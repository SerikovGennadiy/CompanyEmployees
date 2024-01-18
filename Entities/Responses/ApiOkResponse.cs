namespace Entities.Responses
{
    public sealed class ApiOkResponse<TResult> : ApiBaseResponse
    {
        // for query response returned data ( anything data )
        public TResult Result { get; set; }

        public ApiOkResponse(TResult result)
            : base(true)
        {
            this.Result = result;
        }
    }
}

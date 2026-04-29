namespace TranNgoc.Services.Dto
{
    public class BaseResponse<T>
    {
        public bool IsSuccess { get; set; } = true;

        public string Message { get; set; }

        public T? Data { get; set; }
    }
}

namespace LostAndFound.Application.Common
{
    public class BaseResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static BaseResponse<T> SuccessResult(T data, string message = "Operation completed successfully")
        {
            return new BaseResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static BaseResponse<T> FailureResult(string message, List<string>? errors = null)
        {
            return new BaseResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }

    public class BaseResponse : BaseResponse<object>
    {
        public static BaseResponse SuccessResult(string message = "Operation completed successfully")
        {
            return new BaseResponse
            {
                Success = true,
                Message = message
            };
        }

        public static new BaseResponse FailureResult(string message, List<string>? errors = null)
        {
            return new BaseResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }
}

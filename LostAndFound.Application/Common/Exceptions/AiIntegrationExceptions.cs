namespace LostAndFound.Application.Common.Exceptions
{
    public class EmbeddingDimensionException : Exception
    {
        public int Actual { get; }
        public int Expected { get; }

        public EmbeddingDimensionException(int actual, int expected)
            : base($"Embedding length is {actual}; expected exactly {expected}.")
        {
            Actual = actual;
            Expected = expected;
        }
    }

    public class EmbeddingProviderApiException : Exception
    {
        public int? StatusCode { get; }

        public EmbeddingProviderApiException(string message, int? statusCode = null, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }

    public class ModalApiException : Exception
    {
        public int? StatusCode { get; }

        public ModalApiException(string message, int? statusCode = null, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }

    public class AiServiceException : Exception
    {
        public int StatusCode { get; }
        public string Endpoint { get; }
        public string? ResponseBody { get; }

        public AiServiceException(string message, int statusCode, string endpoint, string? responseBody = null, Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            Endpoint = endpoint;
            ResponseBody = responseBody;
        }
    }
}

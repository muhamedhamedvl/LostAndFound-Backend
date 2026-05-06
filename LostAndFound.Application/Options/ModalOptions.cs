namespace LostAndFound.Application.Options
{
    public class ModalOptions
    {
        public const string SectionName = "Modal";

        public string BaseUrl { get; set; } = string.Empty;

        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Vector index name required by Modal SearchVectorRequest.</summary>
        public string DefaultIndexName { get; set; } = string.Empty;

        /// <summary>Relative path, e.g. /search-vector</summary>
        public string SearchPath { get; set; } = "/search-vector";

        /// <summary>Relative path used to initialise a missing index, e.g. /add-vector</summary>
        public string AddVectorPath { get; set; } = "/add-vector";

        public int K { get; set; } = 5;
    }
}

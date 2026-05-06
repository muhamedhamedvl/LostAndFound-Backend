using LostAndFound.Application.Common;
using LostAndFound.Application.Common.Exceptions;
using LostAndFound.Application.DTOs.Ai;
using LostAndFound.Application.Interfaces;
using LostAndFound.Application.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace LostAndFound.Api.Controllers
{
    /// <summary>
    /// AI search: embeds text, then queries Modal with the embedding only (never sends raw text to Modal).
    /// </summary>
    [ApiController]
    [Route("api/ai")]
    [Authorize]
    [EnableRateLimiting("api")]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IModalService _modalService;
        private readonly ModalOptions _modalOptions;
        private readonly ILogger<AiController> _logger;

        public AiController(
            IAiService aiService,
            IEmbeddingService embeddingService,
            IModalService modalService,
            IOptions<ModalOptions> modalOptions,
            ILogger<AiController> logger)
        {
            _aiService = aiService;
            _embeddingService = embeddingService;
            _modalService = modalService;
            _modalOptions = modalOptions.Value;
            _logger = logger;
        }

        [HttpPost("search-text")]
        [ProducesResponseType(typeof(List<AiResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status502BadGateway)]
        [SwaggerOperation(Summary = "Search using AI text similarity")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SearchText([FromForm] AiTextSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var results = await _aiService.SearchTextAsync(request.Text, request.K, cancellationToken);
                return Ok(results);
            }
            catch (AiServiceException ex)
            {
                return HandleAiFailure(ex);
            }
        }

        [HttpPost("add-text")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status502BadGateway)]
        [SwaggerOperation(Summary = "Index report text in the external AI service")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddText([FromForm] AiAddTextRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _aiService.AddTextAsync(request.PostId, request.Text, cancellationToken);
                return Ok(response);
            }
            catch (AiServiceException ex)
            {
                return HandleAiFailure(ex);
            }
        }

        [HttpPost("search-image")]
        [ProducesResponseType(typeof(List<AiResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status502BadGateway)]
        [SwaggerOperation(Summary = "Search using AI image similarity")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SearchImage([FromForm] AiImageSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var results = await _aiService.SearchImageAsync(request.Image, request.K, cancellationToken);
                return Ok(results);
            }
            catch (AiServiceException ex)
            {
                return HandleAiFailure(ex);
            }
        }

        [HttpPost("face-match")]
        [ProducesResponseType(typeof(List<AiResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status502BadGateway)]
        [SwaggerOperation(Summary = "Find similar faces using AI")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> FaceMatch([FromForm] AiImageSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var results = await _aiService.FaceMatchAsync(request.Image, request.K, cancellationToken);
                return Ok(results);
            }
            catch (AiServiceException ex)
            {
                return HandleAiFailure(ex);
            }
        }

        [HttpPost("multimodal-search")]
        [ProducesResponseType(typeof(List<AiResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status502BadGateway)]
        [SwaggerOperation(Summary = "Search using text+image multimodal AI")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> MultiModalSearch([FromForm] AiMultiModalSearchRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var results = await _aiService.MultiModalSearchAsync(request.Text, request.Image, request.K, cancellationToken);
                return Ok(results);
            }
            catch (AiServiceException ex)
            {
                return HandleAiFailure(ex);
            }
        }

        [HttpGet("health")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status502BadGateway)]
        [SwaggerOperation(Summary = "Check external AI service health")]
        public async Task<IActionResult> Health(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _aiService.GetHealthAsync(cancellationToken);
                return Ok(response);
            }
            catch (AiServiceException ex)
            {
                return HandleAiFailure(ex);
            }
        }

        /// <summary>
        /// Semantic search: embedding → Modal vector search. Modal request contains embedding + index metadata only.
        /// </summary>
        [HttpPost("search")]
        [SwaggerOperation(Summary = "AI vector search", Description = "Embeds `text`, validates dimensions, then calls Modal /search-vector with embedding only (no user text).")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Search([FromBody] AiSearchRequestDto dto, CancellationToken cancellationToken)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Text))
            {
                return BadRequest(BaseResponse<object>.FailureResult("Request body must include non-empty text."));
            }

            var indexName = string.IsNullOrWhiteSpace(dto.IndexName)
                ? _modalOptions.DefaultIndexName
                : dto.IndexName;

            if (string.IsNullOrWhiteSpace(indexName))
            {
                return BadRequest(BaseResponse<object>.FailureResult("Index name is required"));
            }

            try
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(dto.Text.Trim(), cancellationToken);
                var modalResult = await _modalService.SearchByEmbeddingAsync(embedding, indexName, cancellationToken);

                // Return Modal JSON as-is (dynamic shape).
                return Ok(modalResult);
            }
            catch (EmbeddingDimensionException ex)
            {
                _logger.LogError(ex, "Embedding dimension mismatch.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (EmbeddingProviderApiException ex)
            {
                _logger.LogWarning(ex, "Embedding provider API error.");
                return StatusCode(StatusCodes.Status502BadGateway,
                    BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (ModalApiException ex)
            {
                _logger.LogWarning(ex, "Modal API error.");
                return StatusCode(StatusCodes.Status502BadGateway,
                    BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "AI search configuration error.");
                return BadRequest(BaseResponse<object>.FailureResult(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 FULL AI ERROR");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = ex.Message,
                        details = ex.ToString()
                    });
            }
        }

        private IActionResult HandleAiFailure(AiServiceException ex)
        {
            _logger.LogError(
                ex,
                "AI integration failed for endpoint {Endpoint}. Returning status {StatusCode}. Upstream body: {ResponseBody}",
                ex.Endpoint,
                ex.StatusCode,
                ex.ResponseBody);

            var details = new List<string> { ex.Message };
            if (!string.IsNullOrWhiteSpace(ex.ResponseBody))
            {
                details.Add(ex.ResponseBody);
            }

            return StatusCode(
                ex.StatusCode,
                BaseResponse<object>.FailureResult("AI service request failed", details));
        }
    }
}

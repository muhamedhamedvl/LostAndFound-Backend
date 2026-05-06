using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.DTOs.Ai
{
    public class AiMultiModalSearchRequestDto : IValidatableObject
    {
        public string? Text { get; set; }

        public IFormFile? Image { get; set; }

        [Range(1, 100)]
        public int K { get; set; } = 5;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Text) && Image is null)
            {
                yield return new ValidationResult(
                    "Either text or image is required.",
                    new[] { nameof(Text), nameof(Image) });
            }
        }
    }
}

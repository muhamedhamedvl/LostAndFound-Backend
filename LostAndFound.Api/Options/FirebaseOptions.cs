namespace LostAndFound.Api.Options
{
    /// <summary>
    /// Firebase configuration. Bind from "Firebase" section.
    /// Store sensitive values (PrivateKey) in User Secrets.
    /// </summary>
    public class FirebaseOptions
    {
        public const string SectionName = "Firebase";

        public string ProjectId { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        /// <summary>
        /// PEM private key (use \n for newlines when storing in User Secrets).
        /// </summary>
        public string PrivateKey { get; set; } = string.Empty;
        /// <summary>
        /// VAPID public key for web push (safe to expose to frontend).
        /// </summary>
        public string VapidKey { get; set; } = string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(ProjectId)
            && !string.IsNullOrWhiteSpace(ClientEmail)
            && !string.IsNullOrWhiteSpace(PrivateKey);
    }
}

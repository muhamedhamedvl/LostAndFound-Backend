using System.Text;
using System.Text.Json;

namespace LostAndFound.Api
{
    /// <summary>
    /// Builds Google credential from Firebase options (no hardcoded keys).
    /// </summary>
    public static class FirebaseCredentialHelper
    {
        /// <summary>
        /// Builds minimal service account JSON from options for GoogleCredential.FromStream.
        /// </summary>
        public static string BuildServiceAccountJson(string projectId, string clientEmail, string privateKey)
        {
            // Ensure private key newlines are valid (User Secrets may store as \n or actual newline)
            var keyNormalized = privateKey?.Replace("\\n", "\n") ?? string.Empty;
            var keyEscaped = JsonSerializer.Serialize(keyNormalized);

            return $@"{{
""type"":""service_account"",
""project_id"":{JsonSerializer.Serialize(projectId)},
""private_key_id"":""firebase-dotnet"",
""private_key"":{keyEscaped},
""client_email"":{JsonSerializer.Serialize(clientEmail)},
""client_id"":"""",
""auth_uri"":""https://accounts.google.com/o/oauth2/auth"",
""token_uri"":""https://oauth2.googleapis.com/token"",
""auth_provider_x509_cert_url"":""https://www.googleapis.com/oauth2/v1/certs"",
""client_x509_cert_url"":""""
}}";
        }
    }
}

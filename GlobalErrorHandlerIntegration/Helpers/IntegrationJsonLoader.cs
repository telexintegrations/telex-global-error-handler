using System.Text.Json;

namespace GlobalErrorHandlerIntegration.Helpers
{
    public class IntegrationJsonLoader
    {
        private static string _cachedJson; // Store JSON in memory after first load
        private static readonly object _lock = new();

        public static string LoadTelexIntegration()
        {
            if (_cachedJson != null)
            {
                return _cachedJson;
            }

            lock (_lock)
            {
                if (_cachedJson == null) // Double-check locking to prevent race conditions
                {
                    var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Helpers", "TelexIntegration.json");

                    if (!File.Exists(jsonFilePath))
                    {
                        throw new FileNotFoundException("integration.json file not found.");
                    }

                    string json = File.ReadAllText(jsonFilePath);
                    using JsonDocument doc = JsonDocument.Parse(json);
                    _cachedJson = JsonSerializer.Serialize(doc.RootElement);
                }
            }

            return _cachedJson;
        }

    }
}

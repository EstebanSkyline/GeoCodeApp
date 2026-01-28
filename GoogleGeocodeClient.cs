using System.Text.Json;

namespace AddressToCoordinatesLambda.Infrastructure
{
    public class GoogleGeocodeClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GoogleGeocodeClient(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClient = new HttpClient();
        }

        public async Task<string> GetGeocodeRawAsync(string address)
        {
            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error calling Google Geocoding API");
            }


            return await response.Content.ReadAsStringAsync();
        }
    }
}

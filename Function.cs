using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using AddressToCoordinatesLambda.Infrastructure;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AddressToCoordinatesLambda
{
    public class Function
    {
        private static readonly AmazonDynamoDBClient dynamoClient = new AmazonDynamoDBClient();

        private static readonly GeocodeCacheRepository cacheRepository =
            new GeocodeCacheRepository(dynamoClient, "GeocodingCache");

        private static readonly GoogleGeocodeClient geocodeClient =
        new GoogleGeocodeClient(
        Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")
        ?? throw new Exception("GOOGLE_MAPS_API_KEY is not set")
    );

        public async Task<APIGatewayProxyResponse> FunctionHandler(
            APIGatewayProxyRequest request,
            ILambdaContext context)
        {
            var address =
                request.QueryStringParameters != null &&
                request.QueryStringParameters.TryGetValue("address", out var value)
                    ? value
                    : null;

            if (string.IsNullOrWhiteSpace(address))
            {
                return Response(400, "Missing 'address' query parameter");
            }

            //Cache
            var cached = await cacheRepository.GetAsync(address);
            if (cached != null)
            {
                context.Logger.LogLine("Returned from cache");
                return JsonResponse(200, cached);
            }

            // Google API
            var googleJson = await geocodeClient.GetGeocodeRawAsync(address);

            // Save cache (30 days)
            await cacheRepository.SaveAsync(address, googleJson, ttlDays: 30);

            //  Return
            return JsonResponse(200, googleJson);
        }

        /* ===== Helpers ===== */

        private static APIGatewayProxyResponse JsonResponse(int status, string json) =>
            new APIGatewayProxyResponse
            {
                StatusCode = status,
                Body = json,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" }
                }
            };

        private static APIGatewayProxyResponse Response(int status, string message) =>
            new APIGatewayProxyResponse
            {
                StatusCode = status,
                Body = message
            };
    }
}

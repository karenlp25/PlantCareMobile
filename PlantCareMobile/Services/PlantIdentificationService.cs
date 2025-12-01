using PlantCareMobile.Models;
using System.Text.Json;

namespace PlantCareMobile.Services
{
    public class PlantIdentificationService
    {
        private readonly string API_KEY = "2b10iFt884oI9biYsj5vzHwu4O";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private HttpClientHandler GetInsecureHandler()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            return handler;
        }

        public async Task<PlantNetResponse?> IdentifyPlantAsync(FileResult imageFile)
        {
            using var httpClient = new HttpClient(GetInsecureHandler());
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            using var formData = new MultipartFormDataContent();

            var stream = await imageFile.OpenReadAsync();
            var content = new StreamContent(stream);

            formData.Add(content, "images", imageFile.FileName ?? "plant.jpg");
            formData.Add(new StringContent("auto"), "organs");

            var response = await httpClient.PostAsync(
                $"https://my-api.plantnet.org/v2/identify/all?api-key={API_KEY}&lang=es",
                formData);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"API Error: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PlantNetResponse>(jsonResponse, JsonOptions);
        }
    }
}
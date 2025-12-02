using System.Net.Http.Json;
using PlantCareMobile.Models;

namespace PlantCareMobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        
        // Usaremos este email por defecto mientras implementamos un Login real
        private const string DefaultEmail = "200251@upbc.edu.mx";

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Endpoint 5: /logs/{email}/{udid} (Consulta validada)
        public async Task<List<SensorLog>> GetLogsByUdidAsync(string udid)
        {
            try
            {
                // Usamos el endpoint validado: /logs/admin@test.com/ESP32-TEST
                var response = await _httpClient.GetFromJsonAsync<List<SensorLog>>($"logs/{DefaultEmail}/{udid}?page_size=10");
                return response ?? new List<SensorLog>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo logs: {ex.Message}");
                return new List<SensorLog>(); 
            }
        }

        // Endpoint 5 variante: /logs/{email}/{udid}?latest=true
        public async Task<SensorLog?> GetLatestLogAsync(string udid)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<SensorLog>>($"logs/{DefaultEmail}/{udid}?latest=true");
                return response?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo último log: {ex.Message}");
                return null;
            }
        }
        
        // Endpoint 3: /iot/{email} (Ver qué dispositivos tiene el usuario)
        public async Task<List<string>> GetUserDevicesAsync()
        {
            try
            {
                // Llama a /iot/200251@upbc.edu.mx
                var response = await _httpClient.GetFromJsonAsync<List<string>>($"iot/{DefaultEmail}");
                return response ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo dispositivos: {ex.Message}");
                return new List<string>();
            }
        }
    }
}
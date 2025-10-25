using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PropamaPOS.Services
{
    public class EmailServiceClient
    {
        private readonly HttpClient _http;

        public EmailServiceClient(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("EmailService");
            //timeout de 10 segundos para la solicitud HTTP
            _http.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<(bool success, string message)> SendAsync(string to, string subject, string body)
        {
            var payload = new
            {
                To = to,
                Subject = subject,
                Body = body
            };

            //convierte el payload a un texto json (solo un string en memoria)
            var json = JsonSerializer.Serialize(payload);

            //crea el contenido de la solicitud HTTP con el JSON y el tipo de contenido
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await _http.PostAsync("/api/email/send", content);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Correo enviado correctamente");
                }
                else
                {
                    return (false, $"El microservicio respondió con error: {response.StatusCode}");
                }
            }
            catch (TaskCanceledException)
            {
                //timeout
                return (false, "El servicio de correo no respondió a tiempo.");
            }
            catch (HttpRequestException ex)
            {
                // conexión o el servicio está caído
                return (false, $"No se pudo conectar con el microservicio: {ex.Message}");
            }
            catch (Exception ex)
            {
                //captura cualquier otro error inesperado
                return (false, $"Error inesperado: {ex.Message}");
            }
        }
    }
}

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
        }

        public async Task<bool> SendAsync(string to, string subject, string body)
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

            var response = await _http.PostAsync("/api/email/send", content);

            return response.IsSuccessStatusCode;
        }
    }
}

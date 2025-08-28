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

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/api/email/send", content);

            return response.IsSuccessStatusCode;
        }
    }
}

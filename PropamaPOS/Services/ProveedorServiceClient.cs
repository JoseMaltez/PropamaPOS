using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PropamaPOS.Services
{
    public class ProveedorDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public string Direccion { get; set; }
    }

    public class ProveedorServiceClient
    {
        private readonly HttpClient _httpClient;

        public ProveedorServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ProveedorDto>> GetProveedoresAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<ProveedorDto>>("api/proveedores");
        }

        public async Task<ProveedorDto> GetProveedorAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<ProveedorDto>($"api/proveedores/{id}");
        }

        public async Task CreateProveedorAsync(ProveedorDto proveedor)
        {
            await _httpClient.PostAsJsonAsync("api/proveedores", proveedor);
        }

        public async Task UpdateProveedorAsync(int id, ProveedorDto proveedor)
        {
            await _httpClient.PutAsJsonAsync($"api/proveedores/{id}", proveedor);
        }

        public async Task DeleteProveedorAsync(int id)
        {
            await _httpClient.DeleteAsync($"api/proveedores/{id}");
        }
    }
}

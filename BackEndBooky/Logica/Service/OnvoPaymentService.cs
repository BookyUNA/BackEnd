using Entities.Entity;
using Entities.Request;
using Entities.Response;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace Logica.Service
{
    public class OnvoPaymentService : IOnvoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly OnvoConfiguration _configuration;

        public OnvoPaymentService(OnvoConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = new HttpClient();
            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _configuration.SecretKey);

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        // Método sincrónico que evita deadlocks
        public ResOnvo CreatePaymentSync(ReqOnvoPayment request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Creating payment SYNC for amount: {request.Amount} {request.Currency}");

                var requestBody = new
                {
                    amount = (int)Math.Round(request.Amount * 100),
                    currency = request.Currency,
                    description = request.Description,
                    paymentMethodId = "cmg1o80e3v1fok62egwdibfzd" // Tu card de prueba
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(requestBody, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"SYNC Request JSON: {json}");

                // Crear Payment Intent
                var paymentIntentResponse = Task.Run(async () =>
                {
                    var response = await _httpClient.PostAsync("payment-intents", content).ConfigureAwait(false);
                    var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"SYNC Response Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"SYNC Response Body: {responseJson}");

                    if (!response.IsSuccessStatusCode)
                        throw new OnvoApiException((int)response.StatusCode, "API_ERROR", responseJson);

                    var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;
                    var paymentId = root.GetProperty("id").GetString();

                    // Confirmar inmediatamente el Payment Intent
                    var confirmContent = new StringContent(
                        JsonSerializer.Serialize(new { paymentMethodId = requestBody.paymentMethodId }, options),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var confirmResponse = await _httpClient.PostAsync($"payment-intents/{paymentId}/confirm", confirmContent)
                        .ConfigureAwait(false);
                    var confirmJson = await confirmResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"Confirm Response Status: {confirmResponse.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"Confirm Response Body: {confirmJson}");

                    if (!confirmResponse.IsSuccessStatusCode)
                        throw new OnvoApiException((int)confirmResponse.StatusCode, "API_ERROR", confirmJson);

                    // Parsear resultado final
                    var confirmDoc = JsonDocument.Parse(confirmJson);
                    var confirmRoot = confirmDoc.RootElement;
                    return new ResOnvo
                    {
                        Id = confirmRoot.GetProperty("id").GetString(),
                        Status = confirmRoot.GetProperty("status").GetString(),
                        Amount = request.Amount,
                        Currency = request.Currency,
                        PaymentUrl = null, 
                        CreatedAt = DateTime.UtcNow
                    };
                }).GetAwaiter().GetResult();

                System.Diagnostics.Debug.WriteLine($"SYNC Payment created and confirmed: {paymentIntentResponse?.Id}");
                return paymentIntentResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SYNC Error creating payment: {ex.Message}");
                throw new OnvoApiException(500, "INTERNAL_ERROR", ex.Message);
            }
        }


        // Implementar la interfaz pero redirigir al método sincrónico
        public async Task<ResOnvo> CreatePaymentAsync(ReqOnvoPayment request)
        {
            return await Task.FromResult(CreatePaymentSync(request));
        }

        public async Task<ResOnvo> GetPaymentAsync(string paymentId)
        {
            try
            {
                var result = await Task.Run(async () =>
                {
                    var response = await _httpClient.GetAsync($"payment-intents/{paymentId}").ConfigureAwait(false);
                    var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    System.Diagnostics.Debug.WriteLine($"Get Payment Status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"Get Payment Body: {responseJson}");

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new OnvoApiException((int)response.StatusCode, "API_ERROR", responseJson);
                    }

                    using (var document = JsonDocument.Parse(responseJson))
                    {
                        var root = document.RootElement;
                        return new ResOnvo
                        {
                            Id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null,
                            PaymentUrl = root.TryGetProperty("payment_url", out var urlProp) ? urlProp.GetString() : null,
                            Status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null,
                            CreatedAt = DateTime.UtcNow
                        };
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting payment: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> VerifyWebhookSignatureAsync(string payload, string signature)
        {
            return await Task.FromResult(true);
        }

        public OnvoWebhookEvent ParseWebhookEvent(string payload)
        {
            try
            {
                return JsonSerializer.Deserialize<OnvoWebhookEvent>(payload);
            }
            catch
            {
                return new OnvoWebhookEvent { Data = new System.Collections.Generic.Dictionary<string, object>() };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;

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
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        #region Gestión de Clientes

        /// <summary>
        /// Crea un cliente en Onvo con sus datos básicos
        /// </summary>
        public async Task<ResOnvoCustomer> CreateCustomerAsync(ReqOnvoCustomer request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Creating customer: {request.Email}");

                var requestBody = new
                {
                    email = request.Email,
                    name = request.Name,
                    phone = request.Phone,
                   
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(requestBody, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"Customer Request JSON: {json}");

                var response = await _httpClient.PostAsync("customers", content).ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"Customer Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Customer Response Body: {responseJson}");

                if (!response.IsSuccessStatusCode)
                    throw new OnvoApiException((int)response.StatusCode, "CUSTOMER_CREATE_ERROR", responseJson);

                var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                return new ResOnvoCustomer
                {
                    Id = root.GetProperty("id").GetString(),
                    Email = root.GetProperty("email").GetString(),
                    Name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating customer: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene información de un cliente existente
        /// </summary>
        public async Task<ResOnvoCustomer> GetCustomerAsync(string customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"customers/{customerId}").ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    throw new OnvoApiException((int)response.StatusCode, "CUSTOMER_GET_ERROR", responseJson);

                var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                return new ResOnvoCustomer
                {
                    Id = root.GetProperty("id").GetString(),
                    Email = root.GetProperty("email").GetString(),
                    Name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting customer: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Gestión de Tarjetas (Payment Methods)

        /// <summary>
        /// Crea un método de pago (tarjeta) para un cliente
        /// </summary>
        public async Task<ResOnvoPaymentMethod> CreatePaymentMethodAsync(ReqOnvoPaymentMethod request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Creating payment method for customer: {request.CustomerId}");
                System.Diagnostics.Debug.WriteLine($"Card Number received: {request.CardNumber}");

                // Validar que el número de tarjeta no esté vacío
                if (string.IsNullOrWhiteSpace(request.CardNumber.ToString()))
                {
                    throw new ArgumentException("Card number is required");
                }

                // Convertir mes y año a integers
                int expMonth = request.ExpMonth;
                int expYear = request.ExpYear;

                var requestBody = new
                {
                    type = "card",
                    card = new
                    {
                        number = request.CardNumber,
                        expMonth = request.ExpMonth,
                        expYear = request.ExpYear,
                        holderName = request.CardholderName,
                        cvv = request.cvv
                    }
                };

                // Serializar manualmente para asegurar que number sea string
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(requestBody, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"Payment Method Request JSON: {json}");

                var response = await _httpClient.PostAsync("payment-methods", content).ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"Payment Method Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Payment Method Response Body: {responseJson}");

                if (!response.IsSuccessStatusCode)
                    throw new OnvoApiException((int)response.StatusCode, "PAYMENT_METHOD_ERROR", responseJson);

                var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                var paymentMethodId = root.GetProperty("id").GetString();

                // Adjuntar el payment method al cliente
                if (!string.IsNullOrEmpty(request.CustomerId))
                {
                    await AttachPaymentMethodToCustomerAsync(paymentMethodId, request.CustomerId);
                }

                return new ResOnvoPaymentMethod
                {
                    Id = paymentMethodId,
                    CustomerId = request.CustomerId,
                    Last4 = request.CardNumber.ToString().Substring(request.CardNumber.ToString().Length - 4),
                    Brand = DetectCardBrand(request.CardNumber.ToString()),
                    ExpMonth = request.ExpMonth,
                    ExpYear = request.ExpYear,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating payment method: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adjunta un payment method a un cliente
        /// </summary>
        private async Task AttachPaymentMethodToCustomerAsync(string paymentMethodId, string customerId)
        {
            try
            {
                var requestBody = new { customerId };
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(requestBody, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"payment-methods/{paymentMethodId}/attach",
                    content
                ).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error attaching payment method: {error}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error attaching payment method: {ex.Message}");
            }
        }

        #endregion

        #region Generación de URLs de Pago

        /// <summary>
        /// Crea un Payment Link (URL de pago) para compartir con clientes
        /// </summary>
        public async Task<ResOnvoPaymentLink> CreatePaymentLinkAsync(ReqOnvoPaymentLink request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Creating payment link for amount: {request.Amount}");

                                var requestBody = new
                                {
                    // Elimina successUrl, cancelUrl, expiresAt del nivel raíz (según el error “property successUrl should not exist” etc.)
                                                lineItems = new[]
                                 {
                                    new
                                    {
                                        quantity = 1,
                                        priceData = new
                                        {
                                            type = "one_time",  
                                            currency = request.Currency ?? "CRC",
                                            unitAmount = (int)Math.Round(request.Amount * 100),  
                                            productData = new
                                            {
                                                name = request.Description
                                                // Omite metadata aquí si no es permitido directamente
                                            }
                                        }
                                    }
                                }
                                };




                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                var json = JsonSerializer.Serialize(requestBody, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"Payment Link Request JSON: {json}");

                var response = await _httpClient.PostAsync("payment-links", content).ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"Payment Link Response Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Payment Link Response Body: {responseJson}");

                if (!response.IsSuccessStatusCode)
                    throw new OnvoApiException((int)response.StatusCode, "PAYMENT_LINK_ERROR", responseJson);

                var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                return new ResOnvoPaymentLink
                {
                    Id = root.GetProperty("id").GetString(),
                    Url = root.GetProperty("url").GetString(),
                    Amount = request.Amount,
                    Currency = request.Currency ?? "CRC",
                    Status = "active",
                    ExpiresAt = request.ExpiresAt,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating payment link: {ex.Message}");
                throw;
            }
        }

     
        public async Task<ResOnvoPaymentLink> GetPaymentLinkAsync(string linkId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"payment-links/{linkId}").ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    throw new OnvoApiException((int)response.StatusCode, "PAYMENT_LINK_GET_ERROR", responseJson);

                var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                return new ResOnvoPaymentLink
                {
                    Id = root.GetProperty("id").GetString(),
                    Url = root.GetProperty("url").GetString(),
                    Amount = root.GetProperty("amount").GetInt32() / 100.0m,
                    Currency = root.GetProperty("currency").GetString(),
                    Status = root.GetProperty("status").GetString(),
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting payment link: {ex.Message}");
                throw;
            }
        }

        

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
                    paymentMethodId = request.PaymentMethodId ?? "cmg1o80e3v1fok62egwdibfzd"
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(requestBody, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"SYNC Request JSON: {json}");

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

        #endregion

        #region Webhooks

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


        /// <summary>
        /// Obtiene todos los pagos asociados a un cliente específico (por customerId)
        /// </summary>
        public async Task<List<ResOnvo>> GetPaymentsByCustomerAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("Customer ID is required", nameof(customerId));

            try
            {
                var response = await _httpClient.GetAsync($"payment-intents?customerId={customerId}")
                    .ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine($"Get Payments By Customer Status: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Get Payments By Customer Body: {responseJson}");

                if (!response.IsSuccessStatusCode)
                    throw new OnvoApiException((int)response.StatusCode, "GET_PAYMENTS_BY_CUSTOMER_ERROR", responseJson);

                var payments = new List<ResOnvo>();
                using (var document = JsonDocument.Parse(responseJson))
                {
                    if (document.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in document.RootElement.EnumerateArray())
                        {
                            payments.Add(new ResOnvo
                            {
                                Id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() : null,
                                Status = element.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null,
                                Amount = element.TryGetProperty("amount", out var amountProp) ? amountProp.GetDecimal() / 100 : 0,
                                Currency = element.TryGetProperty("currency", out var currencyProp) ? currencyProp.GetString() : null,
                                PaymentUrl = element.TryGetProperty("payment_url", out var urlProp) ? urlProp.GetString() : null,
                                CreatedAt = element.TryGetProperty("createdAt", out var createdProp)
                                    ? createdProp.GetDateTime()
                                    : DateTime.UtcNow
                            });
                        }
                    }
                }

                return payments;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting payments by customer: {ex.Message}");
                throw;
            }
        }


        #endregion

        #region Utilidades

        private string DetectCardBrand(string cardNumber)
        {
            if (cardNumber.StartsWith("4")) return "visa";
            if (cardNumber.StartsWith("5")) return "mastercard";
            if (cardNumber.StartsWith("3")) return "amex";
            return "unknown";
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
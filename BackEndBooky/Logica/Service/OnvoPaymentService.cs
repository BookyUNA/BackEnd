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
using System.Linq;

namespace Logica.Service
{
    public class OnvoPaymentService : IOnvoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly OnvoConfiguration _configuration;
        private readonly Random _random;
        private readonly bool _simulationMode = true; 

        public OnvoPaymentService(OnvoConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = new HttpClient();
            _random = new Random();
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

        #region Simulación Aleatoria

        private bool SimulateRandomSuccess(double successRate = 0.7)
        {
            return _random.NextDouble() < successRate;
        }

        private string GetRandomErrorCode()
        {
            var errors = new[]
            {
                "insufficient_funds",
                "card_declined",
                "expired_card",
                "incorrect_cvc",
                "processing_error",
                "card_not_supported",
                "issuer_not_available",
                "exceeded_limit"
            };
            return errors[_random.Next(errors.Length)];
        }

        private string GetRandomErrorMessage(string errorCode)
        {
            var messages = new Dictionary<string, string>
            {
                ["insufficient_funds"] = "Fondos insuficientes en la tarjeta",
                ["card_declined"] = "La tarjeta fue rechazada por el banco emisor",
                ["expired_card"] = "La tarjeta ha expirado",
                ["incorrect_cvc"] = "El código CVC es incorrecto",
                ["processing_error"] = "Error al procesar el pago",
                ["card_not_supported"] = "Tipo de tarjeta no soportado",
                ["issuer_not_available"] = "Banco emisor no disponible",
                ["exceeded_limit"] = "Se ha excedido el límite de la tarjeta"
            };
            return messages.ContainsKey(errorCode) ? messages[errorCode] : "Error desconocido";
        }

        private string GenerateRandomId(string prefix = "sim")
        {
            return $"{prefix}_{Guid.NewGuid().ToString("N").Substring(0, 16)}";
        }

        private async Task SimulateDelay()
        {
            await Task.Delay(_random.Next(60,70));
        }

        #endregion

        #region Gestión de Clientes

        public async Task<ResOnvoCustomer> CreateCustomerAsync(ReqOnvoCustomer request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Creating customer: {request.Email}");
    

                if (_simulationMode)
                {
                    // Simular éxito el 95% de las veces para clientes
                    if (!SimulateRandomSuccess(0.95))
                    {
                        var errorCode = "customer_creation_failed";
                        throw new OnvoApiException(400, errorCode, GetRandomErrorMessage(errorCode));
                    }

                    return new ResOnvoCustomer
                    {
                        Id = GenerateRandomId("cust"),
                        Email = request.Email,
                        Name = request.Name,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                // Código original para API real...
                var requestBody = new
                {
                    email = request.Email,
                    name = request.Name,
                    phone = request.Phone,
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(requestBody, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("customers", content).ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

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

        public async Task<ResOnvoCustomer> GetCustomerAsync(string customerId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Getting customer: {customerId}");
                await SimulateDelay();

                if (_simulationMode)
                {
                    if (!SimulateRandomSuccess(0.9))
                    {
                        throw new OnvoApiException(404, "customer_not_found", "Cliente no encontrado");
                    }

                    return new ResOnvoCustomer
                    {
                        Id = customerId,
                        Email = $"customer_{customerId.Substring(0, 8)}@example.com",
                        Name = "Cliente Simulado",
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    };
                }

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

        public async Task<ResOnvoPaymentMethod> CreatePaymentMethodAsync(ReqOnvoPaymentMethod request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Creating payment method for customer: {request.CustomerId}");
             
                if (_simulationMode)
                {
                    // Validar tarjeta
                    if (string.IsNullOrWhiteSpace(request.CardNumber.ToString()))
                    {
                        throw new ArgumentException("Card number is required");
                    }

                    // Simular validación de tarjeta con 70% de éxito
                    if (!SimulateRandomSuccess(0.7))
                    {
                        var errorCode = GetRandomErrorCode();
                        var errorMessage = GetRandomErrorMessage(errorCode);
                        System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Tarjeta rechazada: {errorCode} - {errorMessage}");
                        throw new OnvoApiException(400, errorCode, errorMessage);
                    }

                    var cardNumber = request.CardNumber.ToString();
                    return new ResOnvoPaymentMethod
                    {
                        Id = GenerateRandomId("pm"),
                        CustomerId = request.CustomerId,
                        Last4 = cardNumber.Substring(cardNumber.Length - 4),
                        Brand = DetectCardBrand(cardNumber),
                        ExpMonth = request.ExpMonth,
                        ExpYear = request.ExpYear,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                // Código original para API real...
                if (string.IsNullOrWhiteSpace(request.CardNumber.ToString()))
                {
                    throw new ArgumentException("Card number is required");
                }

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

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(requestBody, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("payment-methods", content).ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    throw new OnvoApiException((int)response.StatusCode, "PAYMENT_METHOD_ERROR", responseJson);

                var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                var paymentMethodId = root.GetProperty("id").GetString();

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

        private async Task AttachPaymentMethodToCustomerAsync(string paymentMethodId, string customerId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Attaching payment method {paymentMethodId} to customer {customerId}");
                await SimulateDelay();

                if (_simulationMode)
                {
                    // Simular siempre éxito para attach
                    return;
                }

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

        public async Task<ResOnvoPaymentLink> CreatePaymentLinkAsync(ReqOnvoPaymentLink request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Creating payment link for amount: {request.Amount}");
                await SimulateDelay();

                if (_simulationMode)
                {
                    // Simular éxito el 85% de las veces
                    if (!SimulateRandomSuccess(0.85))
                    {
                        throw new OnvoApiException(400, "payment_link_error", "Error al crear el enlace de pago");
                    }

                    var linkId = GenerateRandomId("link");
                    return new ResOnvoPaymentLink
                    {
                        Id = linkId,
                        Url = $"https://checkout.onvo.me/pay/{linkId}",
                        Amount = request.Amount,
                        Currency = request.Currency ?? "CRC",
                        Status = "active",
                        ExpiresAt = request.ExpiresAt,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                // Código original para API real...
                var requestBody = new
                {
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

                var response = await _httpClient.PostAsync("payment-links", content).ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

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
                System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Getting payment link: {linkId}");
                await SimulateDelay();

                if (_simulationMode)
                {
                    var statuses = new[] { "active", "completed", "expired" };
                    return new ResOnvoPaymentLink
                    {
                        Id = linkId,
                        Url = $"https://checkout.onvo.me/pay/{linkId}",
                        Amount = _random.Next(1000, 50000),
                        Currency = "CRC",
                        Status = statuses[_random.Next(statuses.Length)],
                        CreatedAt = DateTime.UtcNow.AddHours(-_random.Next(1, 48))
                    };
                }

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

        #endregion

        #region Procesamiento de Pagos

        public ResOnvo CreatePaymentSync(ReqOnvoPayment request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Creating payment SYNC for amount: {request.Amount} {request.Currency}");

                if (_simulationMode)
                {
                    // Simular delay
                    Task.Delay(_random.Next(800, 2500)).Wait();

                    // Simular éxito solo el 60% de las veces en pagos
                    if (!SimulateRandomSuccess(0.6))
                    {
                        var errorCode = GetRandomErrorCode();
                        var errorMessage = GetRandomErrorMessage(errorCode);
                        System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Pago rechazado: {errorCode} - {errorMessage}");
                        throw new OnvoApiException(402, errorCode, errorMessage);
                    }

                    System.Diagnostics.Debug.WriteLine("[SIMULACIÓN] Pago aprobado exitosamente");
                    return new ResOnvo
                    {
                        Id = GenerateRandomId("pi"),
                        Status = "succeeded",
                        Amount = request.Amount,
                        Currency = request.Currency,
                        PaymentUrl = null,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                // Código original para API real...
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

                var paymentIntentResponse = Task.Run(async () =>
                {
                    var response = await _httpClient.PostAsync("payment-intents", content).ConfigureAwait(false);
                    var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

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

                return paymentIntentResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating payment: {ex.Message}");
                throw;
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
                System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Getting payment: {paymentId}");
                await SimulateDelay();

                if (_simulationMode)
                {
                    var statuses = new[] { "succeeded", "processing", "requires_payment_method", "canceled" };
                    return new ResOnvo
                    {
                        Id = paymentId,
                        PaymentUrl = null,
                        Status = statuses[_random.Next(statuses.Length)],
                        Amount = _random.Next(1000, 100000) / 100m,
                        Currency = "CRC",
                        CreatedAt = DateTime.UtcNow.AddMinutes(-_random.Next(1, 60))
                    };
                }

                var result = await Task.Run(async () =>
                {
                    var response = await _httpClient.GetAsync($"payment-intents/{paymentId}").ConfigureAwait(false);
                    var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

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

        public async Task<List<ResOnvo>> GetPaymentsByCustomerAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("Customer ID is required", nameof(customerId));

            try
            {
                System.Diagnostics.Debug.WriteLine($"[SIMULACIÓN] Getting payments for customer: {customerId}");
                await SimulateDelay();

                if (_simulationMode)
                {
                    var paymentsVar = new List<ResOnvo>();
                    var count = _random.Next(0, 5);

                    for (int i = 0; i < count; i++)
                    {
                        var statuses = new[] { "succeeded", "processing", "requires_payment_method", "canceled" };
                        paymentsVar.Add(new ResOnvo
                        {
                            Id = GenerateRandomId("pi"),
                            Status = statuses[_random.Next(statuses.Length)],
                            Amount = _random.Next(1000, 100000) / 100m,
                            Currency = "CRC",
                            PaymentUrl = null,
                            CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 30))
                        });
                    }

                    return paymentsVar;
                }

                var response = await _httpClient.GetAsync($"payment-intents?customerId={customerId}")
                    .ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

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
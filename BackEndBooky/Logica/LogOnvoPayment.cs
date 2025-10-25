using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic;
using Logica.Service;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

public class LogOnvoPayment
{
    private readonly IOnvoPaymentService _onvoService;

    // Constructor para Unity (CON parámetros)
    public LogOnvoPayment(IOnvoPaymentService onvoService)
    {
        _onvoService = onvoService ?? throw new ArgumentNullException(nameof(onvoService));
        System.Diagnostics.Debug.WriteLine("LogOnvoPayment created with service injection");
    }

    // Constructor sin parámetros - crear el servicio manualmente
    public LogOnvoPayment()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("LogOnvoPayment created WITHOUT service injection - Creating service manually");

            var config = new OnvoConfiguration
            {
                BaseUrl = System.Configuration.ConfigurationManager.AppSettings["OnvoBaseUrl"] ?? "https://api.onvopay.com/v1",
                SecretKey = System.Configuration.ConfigurationManager.AppSettings["OnvoSecretKey"] ?? "onvo_test_secret_key_2d6dYlKDwPn_sjIuu3ucgZ1o6ncCBm2kicZD-B03k9bwY3IySLRI3iMcLIqKFVXACBSyJzvA9uXcUTQ6XUpIyg",
                TimeoutSeconds = 30
            };

            _onvoService = new OnvoPaymentService(config);
            System.Diagnostics.Debug.WriteLine("OnvoPaymentService created manually successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating OnvoPaymentService manually: {ex.Message}");
        }
    }

    public bool HasService()
    {
        return _onvoService != null;
    }

    #region GESTIÓN DE CLIENTES

    /// <summary>
    /// Crea un cliente en Onvo y lo guarda en la BD local
    /// </summary>
    public ResOnvoCustomerPayment CrearClienteAsync(ReqOnvoCustomer req, string token)
    {
        ResOnvoCustomerPayment res = new ResOnvoCustomerPayment();
        res.error = new List<Error>();
        bool? resultadoBd = false;
        int? errorID = 0;

        try
        {
            if (_onvoService == null)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50001, Message = "Servicio de pago no disponible" });
                return res;
            }

            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
            if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                return res;
            }

            // Crear cliente en Onvo
            Task<ResOnvoCustomer> onvoTask = _onvoService.CreateCustomerAsync(req);
            ResOnvoCustomer onvoRes = onvoTask.Result;

            if (onvoRes != null && !string.IsNullOrEmpty(onvoRes.Id))
            {
                using (var linq = new DataClasses1DataContext())
                {
                    // Guardar cliente en BD local
                    linq.SP_REGISTRAR_CLIENTE_ONVO(
                        idUsuarioToken,
                        onvoRes.Id,
                        req.Email,
                        req.Name,
                        req.Phone,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.mensaje = "Cliente creado exitosamente";
                        res.customerId = onvoRes.Id;
                        res.email = onvoRes.Email;
                        res.name = onvoRes.Name;

                        System.Diagnostics.Debug.WriteLine($"✅ Cliente creado: {onvoRes.Id}");
                    }
                    else
                    {
                        res.resultado = false;
                        res.error.Add(new Error { ErrorCode = errorID ?? 50006, Message = "Error al guardar cliente en BD local" });
                    }
                }
            }
            else
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50007, Message = "Error al crear cliente en Onvo" });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in CrearClienteAsync: {ex.Message}");
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50002, Message = $"Error al crear cliente: {ex.Message}" });
        }

        return res;
    }

    /// <summary>
    /// Obtiene información de un cliente
    /// </summary>
    public ResOnvoCustomerPayment ObtenerClienteAsync(string customerId, string token)
    {
        ResOnvoCustomerPayment res = new ResOnvoCustomerPayment();
        res.error = new List<Error>();

        try
        {
            if (_onvoService == null)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50001, Message = "Servicio de pago no disponible" });
                return res;
            }

            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
            if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                return res;
            }

            Task<ResOnvoCustomer> onvoTask = _onvoService.GetCustomerAsync(customerId);
            ResOnvoCustomer onvoRes = onvoTask.Result;

            if (onvoRes != null)
            {
                res.resultado = true;
                res.customerId = onvoRes.Id;
                res.email = onvoRes.Email;
                res.name = onvoRes.Name;
            }
            else
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50008, Message = "Cliente no encontrado" });
            }
        }
        catch (Exception ex)
        {
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50002, Message = $"Error al obtener cliente: {ex.Message}" });
        }

        return res;
    }

    #endregion

    #region GESTIÓN DE MÉTODOS DE PAGO (TARJETAS)

    /// <summary>
    /// Guarda una tarjeta para un cliente y la vincula en Onvo
    /// </summary>
    public ResOnvoPaymentMethodResponse GuardarTarjetaAsync(ReqOnvoPaymentMethod req, string token)
    {
        ResOnvoPaymentMethodResponse res = new ResOnvoPaymentMethodResponse();
        res.error = new List<Error>();
        bool? resultadoBd = false;
        int? errorID = 0;

        try
        {
            if (_onvoService == null)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50001, Message = "Servicio de pago no disponible" });
                return res;
            }

            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
            if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                return res;
            }

            // Crear payment method en Onvo
            Task<ResOnvoPaymentMethod> onvoTask = _onvoService.CreatePaymentMethodAsync(req);
            ResOnvoPaymentMethod onvoRes = onvoTask.Result;

            if (onvoRes != null && !string.IsNullOrEmpty(onvoRes.Id))
            {
                using (var linq = new DataClasses1DataContext())
                {
                    // Guardar payment method en BD local
                    linq.SP_REGISTRAR_METODO_PAGO_ONVO(
                        idUsuarioToken,
                        req.CustomerId,
                        onvoRes.Id,
                        onvoRes.Last4,
                        onvoRes.Brand,
                        req.ExpMonth.ToString(),
                        req.ExpYear.ToString(),
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.mensaje = "Tarjeta guardada exitosamente";
                        res.paymentMethodId = onvoRes.Id;
                        res.last4 = onvoRes.Last4;
                        res.brand = onvoRes.Brand;
                        res.expMonth = onvoRes.ExpMonth.ToString();
                        res.expYear = onvoRes.ExpYear.ToString();

                        System.Diagnostics.Debug.WriteLine($"✅ Tarjeta guardada: {onvoRes.Id} - **** {onvoRes.Last4}");
                    }
                    else
                    {
                        res.resultado = false;
                        res.error.Add(new Error { ErrorCode = errorID ?? 50009, Message = "Error al guardar tarjeta en BD local" });
                    }
                }
            }
            else
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50010, Message = "Error al crear método de pago en Onvo" });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in GuardarTarjetaAsync: {ex.Message}");
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50002, Message = $"Error al guardar tarjeta: {ex.Message}" });
        }

        return res;
    }

    #endregion

    #region GENERACIÓN DE URLS DE PAGO

    /// <summary>
    /// Genera un Payment Link (URL de pago) para compartir con clientes
    /// </summary>
    public ResOnvoPaymentLinkResponse GenerarUrlPagoAsync(ReqOnvoPaymentLink req, string token)
    {
        ResOnvoPaymentLinkResponse res = new ResOnvoPaymentLinkResponse();
        res.error = new List<Error>();
        bool? resultadoBd = false;
        int? errorID = 0;

        try
        {
            if (_onvoService == null)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50001, Message = "Servicio de pago no disponible" });
                return res;
            }

            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
            if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                return res;
            }

            // Generar referencia única
            string referenceId = Guid.NewGuid().ToString();

            // Crear payment link en Onvo
            Task<ResOnvoPaymentLink> onvoTask = _onvoService.CreatePaymentLinkAsync(req);
            ResOnvoPaymentLink onvoRes = onvoTask.Result;

            if (onvoRes != null && !string.IsNullOrEmpty(onvoRes.Url))
            {
                using (var linq = new DataClasses1DataContext())
                {
                    // Guardar payment link en BD local
                    linq.SP_REGISTRAR_PAYMENT_LINK_ONVO(
                        idUsuarioToken,
                        req.CustomerId,
                        onvoRes.Id,
                        referenceId,
                        onvoRes.Url,
                        req.Amount,
                        req.Currency,
                        req.Description,
                        onvoRes.ExpiresAt,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.mensaje = "URL de pago generada exitosamente";
                        res.paymentLinkId = onvoRes.Id;
                        res.paymentUrl = onvoRes.Url;
                        res.amount = onvoRes.Amount;
                        res.currency = onvoRes.Currency;
                        res.status = onvoRes.Status;
                        res.expiresAt = onvoRes.ExpiresAt;

                        System.Diagnostics.Debug.WriteLine($"✅ URL de pago generada: {onvoRes.Url}");
                    }
                    else
                    {
                        res.resultado = false;
                        res.error.Add(new Error { ErrorCode = errorID ?? 50011, Message = "Error al guardar URL de pago en BD local" });
                    }
                }
            }
            else
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50012, Message = "Error al generar URL de pago en Onvo" });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in GenerarUrlPagoAsync: {ex.Message}");
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50002, Message = $"Error al generar URL de pago: {ex.Message}" });
        }

        return res;
    }

    /// <summary>
    /// Obtiene información de un Payment Link
    /// </summary>
    public ResOnvoPaymentLinkResponse ObtenerUrlPagoAsync(string linkId, string token)
    {
        ResOnvoPaymentLinkResponse res = new ResOnvoPaymentLinkResponse();
        res.error = new List<Error>();

        try
        {
            if (_onvoService == null)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50001, Message = "Servicio de pago no disponible" });
                return res;
            }

            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
            if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                return res;
            }

            Task<ResOnvoPaymentLink> onvoTask = _onvoService.GetPaymentLinkAsync(linkId);
            ResOnvoPaymentLink onvoRes = onvoTask.Result;

            if (onvoRes != null)
            {
                res.resultado = true;
                res.paymentLinkId = onvoRes.Id;
                res.paymentUrl = onvoRes.Url;
                res.amount = onvoRes.Amount;
                res.currency = onvoRes.Currency;
                res.status = onvoRes.Status;
                res.expiresAt = onvoRes.ExpiresAt;
            }
            else
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50013, Message = "URL de pago no encontrada" });
            }
        }
        catch (Exception ex)
        {
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50002, Message = $"Error al obtener URL de pago: {ex.Message}" });
        }

        return res;
    }

    #endregion

    #region FLUJO COMPLETO: Cliente + Tarjeta + URL

    /// <summary>
    /// Flujo completo: Crea cliente, guarda tarjeta y genera URL de pago
    /// </summary>
    public ResOnvoFlowCompleto CrearClienteConTarjetaYUrlAsync(ReqOnvoFlowCompleto req, string token)
    {
        ResOnvoFlowCompleto res = new ResOnvoFlowCompleto();
        res.error = new List<Error>();

        try
        {
            // PASO 1: Crear cliente
            var customerReq = new ReqOnvoCustomer
            {
                Email = req.Email,
                Name = req.Name,
                Phone = req.Phone
            };
            var customerRes = CrearClienteAsync(customerReq, token);

            if (!customerRes.resultado)
            {
                res.resultado = false;
                res.error = customerRes.error;
                return res;
            }

            res.customerId = customerRes.customerId;

            // PASO 2: Guardar tarjeta
            var paymentMethodReq = new ReqOnvoPaymentMethod
            {
                CustomerId = customerRes.customerId,
                CardNumber = req.CardNumber,
                ExpMonth = req.ExpMonth,
                ExpYear = req.ExpYear,
                cvv = req.Cvc,

                CardholderName = req.CardholderName ?? req.Name,
                
            };
            var paymentMethodRes = GuardarTarjetaAsync(paymentMethodReq, token);

            if (!paymentMethodRes.resultado)
            {
                res.resultado = false;
                res.error = paymentMethodRes.error;
                return res;
            }

            res.paymentMethodId = paymentMethodRes.paymentMethodId;
            res.last4 = paymentMethodRes.last4;
            res.brand = paymentMethodRes.brand;

            // PASO 3: Generar URL de pago
            var paymentLinkReq = new ReqOnvoPaymentLink
            {
                Amount = req.Amount,
                Currency = req.Currency ?? "CRC",
                Description = req.Description,
                CustomerId = customerRes.customerId,
                
                ExpiresAt = req.ExpiresAt
            };
            var paymentLinkRes = GenerarUrlPagoAsync(paymentLinkReq, token);

            if (!paymentLinkRes.resultado)
            {
                res.resultado = false;
                res.error = paymentLinkRes.error;
                return res;
            }

            res.paymentLinkId = paymentLinkRes.paymentLinkId;
            res.paymentUrl = paymentLinkRes.paymentUrl;
            res.amount = paymentLinkRes.amount;
            res.currency = paymentLinkRes.currency;

            res.resultado = true;
            res.mensaje = "Cliente, tarjeta y URL de pago creados exitosamente";

            System.Diagnostics.Debug.WriteLine($"✅ Flujo completo exitoso - URL: {res.paymentUrl}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in CrearClienteConTarjetaYUrlAsync: {ex.Message}");
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50014, Message = $"Error en flujo completo: {ex.Message}" });
        }

        return res;
    }

    

    #region CONSULTAS DE PAGOS

    /// <summary>
    /// Obtiene todos los pagos asociados a un cliente (por customerId)
    /// </summary>
    public ResOnvoPaymentList ObtenerPagosPorClienteAsync(string customerId, string token)
    {
        ResOnvoPaymentList res = new ResOnvoPaymentList();
        res.error = new List<Error>();
        res.pagos = new List<ResOnvo>();

        try
        {
            if (_onvoService == null)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50001, Message = "Servicio de pago no disponible" });
                return res;
            }

            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
            if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                return res;
            }

            // Llamar al servicio Onvo
            Task<List<ResOnvo>> pagosTask = _onvoService.GetPaymentsByCustomerAsync(customerId);
            List<ResOnvo> pagos = pagosTask.Result;

            if (pagos != null && pagos.Count > 0)
            {
                res.resultado = true;
               
                res.pagos = pagos;
            }
            else
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40401, Message = "No se encontraron pagos para este cliente" });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in ObtenerPagosPorClienteAsync: {ex.Message}");
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50002, Message = $"Error al obtener pagos: {ex.Message}" });
        }

        return res;
    }

    #endregion

    public ResOnvoPayment ObtenerPagoAsync(string paymentId, string token)
    {
        ResOnvoPayment res = new ResOnvoPayment();
        res.error = new List<Error>();

        try
        {
            if (_onvoService == null)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50001, Message = "Servicio de pago no disponible" });
                return res;
            }

            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
            if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                return res;
            }

            Task<ResOnvo> onvoTask = _onvoService.GetPaymentAsync(paymentId);
            ResOnvo onvoRes = onvoTask.Result;

            if (onvoRes != null)
            {
                res.resultado = true;
                res.mensaje = "Pago realizado exitosamente";
            }
            else
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50005, Message = "Pago no encontrado" });
            }
        }
        catch (Exception ex)
        {
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50002, Message = $"Error al obtener el pago: {ex.Message}" });
        }

        return res;
    }

    public ResOnvoPayment VerificarPagoAsync(ReqVerifyPayment req, string token)
    {
        return ObtenerPagoAsync(req.PaymentId, token);
    }

    #endregion

    #region WEBHOOKS

    public void ProcesarWebhook(string payload)
    {
        try
        {
            if (_onvoService != null)
            {
                var webhookEvent = _onvoService.ParseWebhookEvent(payload);
                System.Diagnostics.Debug.WriteLine($"Webhook processed");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Cannot process webhook: _onvoService is null");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing webhook: {ex.Message}");
        }
    }

    #endregion


    #region OBTENER HISTORIAL DE PAGOS

    /// <summary>
    /// Obtiene todos los payment links de un cliente con filtro opcional por status
    /// </summary>
    public ResOnvoHistorialPagos ObtenerHistorialPagosClienteAsync(string customerId, string status, string token)
    {
        ResOnvoHistorialPagos res = new ResOnvoHistorialPagos();
        res.error = new List<Error>();
        res.pagos = new List<OnvoPaymentLinkDetalle>();

        try
        {
            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
            if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                return res;
            }

            using (var linq = new DataClasses1DataContext())
            {
                // Obtener payment links de la BD
                var pagos = linq.SP_OBTENER_PAYMENT_LINKS_POR_CLIENTE(customerId, status);

                if (pagos != null)
                {
                    foreach (var pago in pagos)
                    {
                        res.pagos.Add(new OnvoPaymentLinkDetalle
                        {
                            Id = pago.Id,
                            PaymentLinkId = pago.OnvoPaymentLinkId,
                            ReferenceId = pago.ReferenceId,
                            PaymentUrl = pago.PaymentUrl,
                            Amount = pago.Amount  ,
                            Currency = pago.Currency,
                            Description = pago.Description,
                            Status = pago.Status,
                            StatusDescripcion = pago.StatusDescripcion,
                            ExpiresAt = pago.ExpiresAt,
                            FechaCreacion = pago.FechaCreacion ?? DateTime.Now,
                            FechaActualizacion = pago.FechaActualizacion
                        });
                    }

                    res.resultado = true;
                    res.totalRegistros = res.pagos.Count;
                    res.mensaje = $"Se encontraron {res.totalRegistros} registros";
                }
                else
                {
                    res.resultado = true;
                    res.totalRegistros = 0;
                    res.mensaje = "No se encontraron pagos para este cliente";
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in ObtenerHistorialPagosClienteAsync: {ex.Message}");
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50021, Message = $"Error al obtener historial: {ex.Message}" });
        }

        return res;
    }

    /// <summary>
    /// Obtiene un resumen estadístico de pagos del cliente
    /// </summary>
    public ResOnvoResumenPagos ObtenerResumenPagosClienteAsync(string customerId, string token)
    {
        ResOnvoResumenPagos res = new ResOnvoResumenPagos();
        res.error = new List<Error>();

        try
        {
            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
            if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                return res;
            }

            using (var linq = new DataClasses1DataContext())
            {
                var resumen = linq.SP_OBTENER_RESUMEN_PAGOS_CLIENTE(customerId).FirstOrDefault();

                if (resumen != null)
                {
                    res.resultado = true;
                    res.totalPagos = resumen.TotalPagos ?? 0;
                    res.pagosCompletados = resumen.PagosCompletados ?? 0;
                    res.pagosPendientes = resumen.PagosPendientes ?? 0;
                    res.pagosCancelados = resumen.PagosCancelados ?? 0;
                    res.pagosExpirados = resumen.PagosExpirados ?? 0;
                    res.totalPagado = resumen.TotalPagado ?? 0;
                    res.totalPendiente = resumen.TotalPendiente ?? 0;
                    res.currency = resumen.Currency;
                    
                }
                else
                {
                    res.resultado = true;
                 
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in ObtenerResumenPagosClienteAsync: {ex.Message}");
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50022, Message = $"Error al obtener resumen: {ex.Message}" });
        }

        return res;
    }

    /// <summary>
    /// Obtiene pagos pendientes de un cliente
    /// </summary>
    public ResOnvoHistorialPagos ObtenerPagosPendientesAsync(string customerId, string token)
    {
        return ObtenerHistorialPagosClienteAsync(customerId, "active", token);
    }

    /// <summary>
    /// Obtiene pagos completados de un cliente
    /// </summary>
    public ResOnvoHistorialPagos ObtenerPagosCompletadosAsync(string customerId, string token)
    {
        return ObtenerHistorialPagosClienteAsync(customerId, "completed", token);
    }

    /// <summary>
    /// Obtiene pagos cancelados de un cliente
    /// </summary>
    public ResOnvoHistorialPagos ObtenerPagosCanceladosAsync(string customerId, string token)
    {
        return ObtenerHistorialPagosClienteAsync(customerId, "cancelled", token);
    }

    #endregion

}

using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic;
using Logica.Service;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

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

            // Crear la configuración manualmente
            var config = new OnvoConfiguration
            {
                BaseUrl = System.Configuration.ConfigurationManager.AppSettings["OnvoBaseUrl"] ?? "https://api.onvopay.com/v1",
                SecretKey = System.Configuration.ConfigurationManager.AppSettings["OnvoSecretKey"] ?? "onvo_test_secret_key_2d6dYlKDwPn_sjIuu3ucgZ1o6ncCBm2kicZD-B03k9bwY3IySLRI3iMcLIqKFVXACBSyJzvA9uXcUTQ6XUpIyg",
                TimeoutSeconds = 30
            };

            // Crear el servicio manualmente
            _onvoService = new OnvoPaymentService(config);

            System.Diagnostics.Debug.WriteLine("OnvoPaymentService created manually successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating OnvoPaymentService manually: {ex.Message}");
            // _onvoService quedará null, se manejará en los métodos
        }
    }

    // Método para verificar si el servicio está disponible
    public bool HasService()
    {
        return _onvoService != null;
    }

    public ResOnvoPayment CrearPagoAsync(ReqOnvoPayment req, string token)
    {
        ResOnvoPayment res = new ResOnvoPayment();
        res.error = new List<Error>();
        bool? resultadoBd = false;
        int? errorID = 0;

        try
        {
            // Verificar que el servicio está disponible
            if (_onvoService == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: _onvoService is null in CrearPagoAsync");
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

            using (var linq = new DataClasses1DataContext())
            {
                // Traer info del usuario
                var usuario = linq.SP_OBTENER_USUARIO_POR_ID(idUsuarioToken, ref resultadoBd, ref errorID);
                if (!(resultadoBd.HasValue && resultadoBd.Value && usuario != null))
                {
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 40002, Message = "Usuario no encontrado o inactivo" });
                    return res;
                }

                // Generar ReferenceId
                string referenceId = Guid.NewGuid().ToString();

                // Guardar pago en la base de datos
                linq.SP_REGISTRAR_PAGO_ONVO(
                    idUsuarioToken,
                    referenceId,
                    req.Amount,
                    req.Currency,
                    req.Description,
                    ref resultadoBd,
                    ref errorID
                );

                if (!(resultadoBd.HasValue && resultadoBd.Value))
                {
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = errorID ?? 50003, Message = "Error al guardar referencia de pago" });
                    return res;
                }

                var onvoReq = new ReqOnvoPayment
                {
                    Amount = req.Amount,
                    Currency = req.Currency,
                    Description = req.Description,
                    
                   
                };

                System.Diagnostics.Debug.WriteLine($"Calling Onvo API with amount: {onvoReq.Amount}");

                // Llamar al servicio de Onvo
                Task<ResOnvo> onvoTask = _onvoService.CreatePaymentAsync(onvoReq);
                ResOnvo onvoRes = onvoTask.Result; // Usar .Result para hacerlo sincrónico

                if (onvoRes != null && onvoRes.Status.Equals("succeeded"))
                {
                    res.resultado = true;
                    

                    System.Diagnostics.Debug.WriteLine($"Payment created successfully: {onvoRes.PaymentUrl}");
                }
                else
                {
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 50004, Message = "Error al crear el pago en Onvo" });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in CrearPagoAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

            res.resultado = false;
            res.error.Add(new Error { ErrorCode = 50002, Message = $"Error en la lógica al generar el pago: {ex.Message}" });
        }

        return res;
    }

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
        // Implementación similar usando _onvoService
        return ObtenerPagoAsync(req.PaymentId, token);
    }

    public void ProcesarWebhook(string payload)
    {
        try
        {
            if (_onvoService != null)
            {
                var webhookEvent = _onvoService.ParseWebhookEvent(payload);
                // Procesar el evento según tu lógica de negocio
                System.Diagnostics.Debug.WriteLine($"Webhook processed: ");
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
}


public class ReqVerifyPayment
{
    public string PaymentId { get; set; }
    public string ReferenceId { get; set; }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Entities.Request;
using Entities.Response;
using Entities.Entity;
using Logica;
using Logica.Service;

namespace APIs.Controllers
{
    [Authorize]
    public class OnvoController : ApiController
    {
        private readonly LogOnvoPayment _logOnvo;

        // Constructor para Unity (CON parámetros)
        public OnvoController(LogOnvoPayment logOnvo)
        {
            _logOnvo = logOnvo ?? throw new ArgumentNullException(nameof(logOnvo));
            System.Diagnostics.Debug.WriteLine("OnvoController created with dependency injection");
        }

        public OnvoController()
        {
            _logOnvo = new LogOnvoPayment();
            System.Diagnostics.Debug.WriteLine("OnvoController created with default constructor (fallback)");
        }

        [HttpPost]
        [Route("api/Onvo/CreatePayment")]
        public ResOnvoPayment CreatePayment([FromBody] ReqOnvoPayment req)
        {
            try
            {
                // Validar que _logOnvo no sea null
                if (_logOnvo == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: _logOnvo is null in CreatePayment");
                    return new ResOnvoPayment
                    {
                        resultado = false,
                        error = new List<Error>
                        {
                            new Error
                            {
                                ErrorCode = 500,
                                Message = "Servicio de pago no inicializado"
                            }
                        }
                    };
                }

                // Validar el request
                if (req == null)
                {
                    return new ResOnvoPayment
                    {
                        resultado = false,
                        error = new List<Error>
                        {
                            new Error
                            {
                                ErrorCode = 400,
                                Message = "Request no puede ser nulo"
                            }
                        }
                    };
                }

                var token = Request.Headers.Authorization?.Parameter;

                // Validar token
                if (string.IsNullOrEmpty(token))
                {
                    return new ResOnvoPayment
                    {
                        resultado = false,
                        error = new List<Error>
                        {
                            new Error
                            {
                                ErrorCode = 401,
                                Message = "Token de autorización requerido"
                            }
                        }
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Processing payment for amount: {req.Amount}");

                return _logOnvo.CrearPagoAsync(req, token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CreatePayment: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return new ResOnvoPayment
                {
                    resultado = false,
                    error = new List<Error>
                    {
                        new Error
                        {
                            ErrorCode = 500,
                            Message = ex.Message.ToString()
                        }
                    }
                };
            }
        }



        [HttpPost]
        [Route("api/Onvo/clientes")]
        public IHttpActionResult CrearCliente([FromBody] ReqOnvoCustomer request)
        {
            try
            {
                string token = Request.Headers.Authorization?.Parameter;
                if (string.IsNullOrEmpty(token))
                    return Unauthorized();

                var resultado = _logOnvo.CrearClienteAsync(request, token);

                if (resultado.resultado)
                    return Ok(resultado);
                else
                    return BadRequest(resultado.mensaje);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // EJEMPLO 2: Guardar tarjeta para un cliente
        // ========================================
        [HttpPost]
        [Route("api/Onvo/metodos-pago")]
        public IHttpActionResult GuardarTarjeta([FromBody] ReqOnvoPaymentMethod request)
        {
            try
            {
                string token = Request.Headers.Authorization?.Parameter; ;
                if (string.IsNullOrEmpty(token))
                    return Unauthorized();

                var resultado = _logOnvo.GuardarTarjetaAsync(request, token);

                if (resultado.resultado)
                    return Ok(resultado);
                else
                    return BadRequest(resultado.mensaje);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ========================================
        // EJEMPLO 3: Generar URL de pago
        // ========================================
        [HttpPost]
        [Route("payment-links")]
        public IHttpActionResult GenerarUrlPago([FromBody] ReqOnvoPaymentLink request)
        {
            try
            {
                string token = Request.Headers.Authorization?.Parameter;
                if (string.IsNullOrEmpty(token))
                    return Unauthorized();

                var resultado = _logOnvo.GenerarUrlPagoAsync(request, token);

                if (resultado.resultado)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            paymentLinkId = resultado.paymentLinkId,
                            paymentUrl = resultado.paymentUrl,
                            amount = resultado.amount,
                            currency = resultado.currency,
                            expiresAt = resultado.expiresAt
                        }
                    });
                }
                else
                {
                    return BadRequest(resultado.mensaje);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }





        // ========================================
        // NUEVO: Obtener pagos por cliente
        // ========================================
        [HttpGet]
        [Route("api/Onvo/GetPaymentsByCustomer/{customerId}")]
        public IHttpActionResult GetPaymentsByCustomer(string customerId)
        {
            try
            {
                string token = Request.Headers.Authorization?.Parameter;
                if (string.IsNullOrEmpty(token))
                    return Unauthorized();

                if (string.IsNullOrEmpty(customerId))
                    return BadRequest("El parámetro customerId es requerido");

                var resultado = _logOnvo.ObtenerPagosPorClienteAsync(customerId, token);

                if (resultado.resultado)
                    return Ok(new
                    {
                        success = true,
                        count = resultado.pagos.Count,
                        pagos = resultado.pagos
                    });
                else
                    return Content(System.Net.HttpStatusCode.NotFound, new
                    {
                        success = false,
                        errores = resultado.error.Select(e => e.Message)
                    });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetPaymentsByCustomer: {ex.Message}");
                return InternalServerError(ex);
            }
        }


    







        // ========================================
    // Obtener historial de pagos por cliente con filtro opcional
    // ========================================
    [HttpGet]
            [Route("api/Onvo/clientes/{customerId}/pagos")]
            public IHttpActionResult ObtenerHistorialPagos(string customerId, string status = null)
            {
                try
                {
                    string token = Request.Headers.Authorization?.Parameter;
                    if (string.IsNullOrEmpty(token))
                        return Unauthorized();

                    if (string.IsNullOrEmpty(customerId))
                        return BadRequest("El parámetro customerId es requerido");

                    var resultado = _logOnvo.ObtenerHistorialPagosClienteAsync(customerId, status, token);

                    if (resultado.resultado)
                        return Ok(new
                        {
                            success = true,
                            total = resultado.totalRegistros,
                            data = resultado.pagos
                        });
                    else
                        return Content(System.Net.HttpStatusCode.NotFound, new
                        {
                            success = false,
                            errores = resultado.error.Select(e => e.Message)
                        });

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ObtenerHistorialPagos: {ex.Message}");
                    return InternalServerError(ex);
                }
            }

            // ========================================
            // Obtener resumen estadístico de pagos
            // ========================================
            [HttpGet]
            [Route("api/Onvo/clientes/{customerId}/resumen-pagos")]
            public IHttpActionResult ObtenerResumenPagos(string customerId)
            {
                try
                {
                    string token = Request.Headers.Authorization?.Parameter;
                    if (string.IsNullOrEmpty(token))
                        return Unauthorized();

                    if (string.IsNullOrEmpty(customerId))
                        return BadRequest("El parámetro customerId es requerido");

                    var resultado = _logOnvo.ObtenerResumenPagosClienteAsync(customerId, token);

                    if (resultado.resultado)
                        return Ok(new
                        {
                            success = true,
                            data = new
                            {
                                totalPagos = resultado.totalPagos,
                                completados = resultado.pagosCompletados,
                                pendientes = resultado.pagosPendientes,
                                cancelados = resultado.pagosCancelados,
                                expirados = resultado.pagosExpirados,
                                montos = new
                                {
                                    totalPagado = resultado.totalPagado,
                                    totalPendiente = resultado.totalPendiente,
                                    currency = resultado.currency
                                }
                            }
                        });
                    else
                        return Content(System.Net.HttpStatusCode.NotFound, new
                        {
                            success = false,
                            errores = resultado.error.Select(e => e.Message)
                        });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ObtenerResumenPagos: {ex.Message}");
                    return InternalServerError(ex);
                }
            }

            // ========================================
            // Obtener solo pagos pendientes
            // ========================================
            [HttpGet]
            [Route("api/Onvo/clientes/{customerId}/pagos/pendientes")]
            public IHttpActionResult ObtenerPagosPendientes(string customerId)
            {
                try
                {
                    string token = Request.Headers.Authorization?.Parameter;
                    if (string.IsNullOrEmpty(token))
                        return Unauthorized();

                    var resultado = _logOnvo.ObtenerPagosPendientesAsync(customerId, token);

                    if (resultado.resultado)
                        return Ok(new { success = true, data = resultado.pagos });
                    else
                        return Content(System.Net.HttpStatusCode.NotFound, new
                        {
                            success = false,
                            errores = resultado.error.Select(e => e.Message)
                        });
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }

            // ========================================
            // Obtener solo pagos completados
            // ========================================
            [HttpGet]
            [Route("api/Onvo/clientes/{customerId}/pagos/completados")]
            public IHttpActionResult ObtenerPagosCompletados(string customerId)
            {
                try
                {
                    string token = Request.Headers.Authorization?.Parameter;
                    if (string.IsNullOrEmpty(token))
                        return Unauthorized();

                    var resultado = _logOnvo.ObtenerPagosCompletadosAsync(customerId, token);

                    if (resultado.resultado)
                        return Ok(new { success = true, data = resultado.pagos });
                    else
                        return Content(System.Net.HttpStatusCode.NotFound, new
                        {
                            success = false,
                            errores = resultado.error.Select(e => e.Message)
                        });
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }

        }
    }



















/*

        [HttpGet]
        [Route("api/Onvo/GetPayment/{paymentId}")]
        public async Task<ResOnvoPayment> GetPayment(string paymentId)
        {
            try
            {
                if (string.IsNullOrEmpty(paymentId))
                {
                    return new ResOnvoPayment
                    {
                        resultado = false,
                        error = new List<Error>
                        {
                            new Error
                            {
                                codigo = "INVALID_REQUEST",
                                mensaje = "Payment ID is required"
                            }
                        }
                    };
                }

                var token = Request.Headers.Authorization?.Parameter;

                if (string.IsNullOrEmpty(token))
                {
                    return new ResOnvoPayment
                    {
                        resultado = false,
                        error = new List<Error>
                        {
                            new Error
                            {
                                codigo = "UNAUTHORIZED",
                                mensaje = "Authorization token is required"
                            }
                        }
                    };
                }

                // Si LogOnvoPayment tiene un método para obtener pagos, úsalo
                // Si no, puedes usar el servicio directamente si está disponible
                if (_onvoPaymentService != null)
                {
                    var result = await _onvoPaymentService.GetPaymentAsync(paymentId);

                    // Convertir el resultado al formato de tu respuesta
                    return new ResOnvoPayment
                    {
                        resultado = true,
                        PaymentUrl = result.PaymentUrl, // Asumiendo que ResOnvo tiene PaymentUrl
                        error = null
                    };
                }
                else
                {
                    // Fallback: usar LogOnvoPayment si tiene un método para obtener pagos
                    // Asumiendo que tienes este método, si no lo tienes, elimina esta parte
                    return _logOnvo.ObtenerPagoAsync(paymentId, token);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetPayment: {ex.Message}");

                return new ResOnvoPayment
                {
                    resultado = false,
                    error = new List<Error>
                    {
                        new Error
                        {
                            codigo = "INTERNAL_ERROR",
                            mensaje = "An error occurred while retrieving the payment"
                        }
                    }
                };
            }
        }

        [HttpPost]
        [Route("api/Onvo/Webhook")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> ProcessWebhook()
        {
            try
            {
                var payload = await Request.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(payload))
                {
                    return BadRequest("Empty payload");
                }

                // Verificar la firma del webhook si tienes el servicio disponible
                if (_onvoPaymentService != null)
                {
                    var signatureHeader = Request.Headers.GetValues("X-Onvo-Signature")?.FirstOrDefault();

                    if (!string.IsNullOrEmpty(signatureHeader))
                    {
                        var isValid = await _onvoPaymentService.VerifyWebhookSignatureAsync(payload, signatureHeader);
                        if (!isValid)
                        {
                            return Unauthorized();
                        }
                    }

                    var webhookEvent = _onvoPaymentService.ParseWebhookEvent(payload);

                    // Procesar el evento según tu lógica de negocio
                    // Ejemplo: actualizar estado del pago en la base de datos
                }
                else
                {
                    // Usar LogOnvoPayment para procesar el webhook si tiene ese método
                    _logOnvo.ProcesarWebhook(payload);
                }

                return Ok(new
                {
                    received = true,
                    timestamp = DateTime.UtcNow,
                    resultado = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Webhook error: {ex.Message}");
                return InternalServerError(new Exception("Error processing webhook"));
            }
        }

        [HttpPost]
        [Route("api/Onvo/VerifyPayment")]
        public ResOnvoPayment VerifyPayment([FromBody] ReqVerifyPayment req)
        {
            try
            {
                if (req == null || string.IsNullOrEmpty(req.PaymentId))
                {
                    return new ResOnvoPayment
                    {
                        resultado = false,
                        error = new List<Error>
                        {
                            new Error
                            {
                                ErrorCode = "INVALID_REQUEST",
                                Message = "Payment ID is required for verification"
                            }
                        }
                    };
                }

                var token = Request.Headers.Authorization?.Parameter;

                if (string.IsNullOrEmpty(token))
                {
                    return new ResOnvoPayment
                    {
                        resultado = false,
                        error = new List<Error>
                        {
                            new Error
                            {
                                ErrorCode= 401,
                                Message = "Authorization token is required"
                            }
                        }
                    };
                }

                // Usar LogOnvoPayment para verificar el pago
                // Asumiendo que tienes este método, si no lo tienes, puedes implementarlo
                return _logOnvo.VerificarPagoAsync(req, token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in VerifyPayment: {ex.Message}");

                return new ResOnvoPayment
                {
                    resultado = false,
                    error = new List<Error>
                    {
                        new Error
                        {
                            codigo = "INTERNAL_ERROR",
                            mensaje = "An error occurred while verifying the payment"
                        }
                    }
                };
            }
        }

        [HttpGet]
        [Route("api/Onvo/Health")]
        [AllowAnonymous]
        public IHttpActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Onvo Payment API"
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                
                (_onvoPaymentService as IDisposable)?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Clase auxiliar para la verificación de pagos
    public class ReqVerifyPayment
    {
        public string PaymentId { get; set; }
        public string ReferenceId { get; set; }
    }
    */

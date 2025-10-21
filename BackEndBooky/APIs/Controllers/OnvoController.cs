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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Constructor para Unity (CON parámetros)
        public OnvoController(LogOnvoPayment logOnvo)
        {
            _logOnvo = logOnvo ?? throw new ArgumentNullException(nameof(logOnvo));
            log.Info("OnvoController created with dependency injection");
            System.Diagnostics.Debug.WriteLine("OnvoController created with dependency injection");
        }

        public OnvoController()
        {
            _logOnvo = new LogOnvoPayment();
            log.Info("OnvoController created with default constructor (fallback)");
            System.Diagnostics.Debug.WriteLine("OnvoController created with default constructor (fallback)");
        }

        [HttpPost]
        [Route("api/Onvo/CreatePayment")]
        public ResOnvoPayment CreatePayment([FromBody] ReqOnvoPayment req)
        {
            log.Info("=== INICIO CreatePayment ===");
            try
            {
                log.Info($"Request recibido: {Newtonsoft.Json.JsonConvert.SerializeObject(req)}");

                // Validar que _logOnvo no sea null
                if (_logOnvo == null)
                {
                    log.Error("ERROR CRÍTICO: _logOnvo is null in CreatePayment");
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
                    log.Warn("Request es null");
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
                log.Info($"Token presente: {!string.IsNullOrEmpty(token)}");

                // Validar token
                if (string.IsNullOrEmpty(token))
                {
                    log.Warn("Token de autorización faltante");
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

                log.Info($"Processing payment for amount: {req.Amount}");
                var resultado = _logOnvo.CrearPagoAsync(req, token);
                log.Info($"Resultado obtenido: {resultado?.resultado}");
                log.Info("=== FIN CreatePayment (exitoso) ===");

                return resultado;
            }
            catch (Exception ex)
            {
                log.Error($"ERROR en CreatePayment: {ex.Message}", ex);
                log.Error($"Stack trace: {ex.StackTrace}");
                log.Error($"Inner exception: {ex.InnerException?.Message}");
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
            log.Info("=== INICIO CrearCliente ===");
            log.Info($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}");

            try
            {
                log.Info("PASO 1: Request recibido en CrearCliente");
                log.Info($"PASO 1: Request es null: {request == null}");

                if (request != null)
                {
                    try
                    {
                        var jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                        log.Info($"PASO 2: Datos del request: {jsonRequest}");
                    }
                    catch (Exception serEx)
                    {
                        log.Warn($"PASO 2: No se pudo serializar request: {serEx.Message}");
                    }
                }

                log.Info("PASO 3: Obteniendo token de autorización");
                string token = Request.Headers.Authorization?.Parameter;
                log.Info($"PASO 4: Token obtenido - Presente: {!string.IsNullOrEmpty(token)}, Length: {token?.Length ?? 0}");

                if (string.IsNullOrEmpty(token))
                {
                    log.Warn("PASO 5: Token vacío, retornando Unauthorized");
                    return Unauthorized();
                }

                log.Info("PASO 6: Verificando _logOnvo");
                if (_logOnvo == null)
                {
                    log.Error("PASO 6 ERROR CRÍTICO: _logOnvo es null en CrearCliente");
                    return InternalServerError(new Exception("Servicio de pago no inicializado"));
                }

                log.Info("PASO 7: Llamando a _logOnvo.CrearClienteAsync");
                var resultado = _logOnvo.CrearClienteAsync(request, token);

                log.Info($"PASO 8: Resultado obtenido de CrearClienteAsync");
                log.Info($"PASO 8: resultado es null: {resultado == null}");
                if (resultado != null)
                {
                    log.Info($"PASO 8: resultado.resultado: {resultado.resultado}");
                    log.Info($"PASO 8: resultado.mensaje: {resultado.mensaje}");
                }

                if (resultado.resultado)
                {
                    log.Info("PASO 9: Cliente creado exitosamente, creando respuesta Ok");
                    try
                    {
                        var response = Ok(resultado);
                        log.Info("PASO 10: Respuesta Ok creada exitosamente");
                        log.Info("=== FIN CrearCliente (exitoso) ===");
                        return response;
                    }
                    catch (Exception okEx)
                    {
                        log.Error($"PASO 10 ERROR: Error al crear respuesta Ok: {okEx.Message}");
                        log.Error($"PASO 10 Stack: {okEx.StackTrace}");
                        throw;
                    }
                }
                else
                {
                    log.Warn($"PASO 9: Creación fallida: {resultado.mensaje}");
                    log.Info("=== FIN CrearCliente (BadRequest) ===");
                    return BadRequest(resultado.mensaje);
                }
            }
            catch (Exception ex)
            {
                log.Error("=== ERROR CRÍTICO en CrearCliente ===");
                log.Error($"Tipo de excepción: {ex.GetType().Name}");
                log.Error($"Mensaje: {ex.Message}");
                log.Error($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    log.Error($"Inner Exception: {ex.InnerException.Message}");
                    log.Error($"Inner Stack trace: {ex.InnerException.StackTrace}");
                }

                log.Info("=== FIN CrearCliente (con error) ===");
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("api/Onvo/metodos-pago")]
        public IHttpActionResult GuardarTarjeta([FromBody] ReqOnvoPaymentMethod request)
        {
            log.Info("=== INICIO GuardarTarjeta ===");
            try
            {
                log.Info($"PASO 1: Request recibido: {request != null}");

                string token = Request.Headers.Authorization?.Parameter;
                log.Info($"PASO 2: Token presente: {!string.IsNullOrEmpty(token)}");

                if (string.IsNullOrEmpty(token))
                {
                    log.Warn("PASO 3: Token faltante en GuardarTarjeta");
                    return Unauthorized();
                }

                if (_logOnvo == null)
                {
                    log.Error("PASO 4 ERROR: _logOnvo es null en GuardarTarjeta");
                    return InternalServerError(new Exception("Servicio no inicializado"));
                }

                log.Info("PASO 5: Llamando a GuardarTarjetaAsync");
                var resultado = _logOnvo.GuardarTarjetaAsync(request, token);
                log.Info($"PASO 6: Resultado: {resultado?.resultado}");

                if (resultado.resultado)
                {
                    log.Info("PASO 7: Tarjeta guardada exitosamente");
                    log.Info("=== FIN GuardarTarjeta (exitoso) ===");
                    return Ok(resultado);
                }
                else
                {
                    log.Warn($"PASO 7: GuardarTarjeta falló: {resultado.mensaje}");
                    log.Info("=== FIN GuardarTarjeta (BadRequest) ===");
                    return BadRequest(resultado.mensaje);
                }
            }
            catch (Exception ex)
            {
                log.Error($"ERROR en GuardarTarjeta: {ex.Message}", ex);
                log.Error($"Stack trace: {ex.StackTrace}");
                log.Info("=== FIN GuardarTarjeta (con error) ===");
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("api/Onvo/payment-links")]
        public IHttpActionResult GenerarUrlPago([FromBody] ReqOnvoPaymentLink request)
        {
            log.Info("=== INICIO GenerarUrlPago ===");
            try
            {
                log.Info($"PASO 1: Request recibido: {request != null}");

                string token = Request.Headers.Authorization?.Parameter;
                log.Info($"PASO 2: Token presente: {!string.IsNullOrEmpty(token)}");

                if (string.IsNullOrEmpty(token))
                {
                    log.Warn("PASO 3: Token faltante en GenerarUrlPago");
                    return Unauthorized();
                }

                if (_logOnvo == null)
                {
                    log.Error("PASO 4 ERROR: _logOnvo es null en GenerarUrlPago");
                    return InternalServerError(new Exception("Servicio no inicializado"));
                }

                log.Info("PASO 5: Llamando a GenerarUrlPagoAsync");
                var resultado = _logOnvo.GenerarUrlPagoAsync(request, token);
                log.Info($"PASO 6: Resultado: {resultado?.resultado}");

                if (resultado.resultado)
                {
                    log.Info("PASO 7: Generando respuesta exitosa");
                    var response = Ok(new
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
                    log.Info("PASO 8: Respuesta creada exitosamente");
                    log.Info("=== FIN GenerarUrlPago (exitoso) ===");
                    return response;
                }
                else
                {
                    log.Warn($"PASO 7: GenerarUrlPago falló: {resultado.mensaje}");
                    log.Info("=== FIN GenerarUrlPago (BadRequest) ===");
                    return BadRequest(resultado.mensaje);
                }
            }
            catch (Exception ex)
            {
                log.Error($"ERROR en GenerarUrlPago: {ex.Message}", ex);
                log.Error($"Stack trace: {ex.StackTrace}");
                log.Info("=== FIN GenerarUrlPago (con error) ===");
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
            log.Info("=== INICIO GetPaymentsByCustomer ===");
            try
            {
                log.Info($"PASO 1: CustomerId recibido: {customerId}");

                string token = Request.Headers.Authorization?.Parameter;
                log.Info($"PASO 2: Token presente: {!string.IsNullOrEmpty(token)}");

                if (string.IsNullOrEmpty(token))
                {
                    log.Warn("PASO 3: Token faltante");
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(customerId))
                {
                    log.Warn("PASO 3: CustomerId vacío");
                    return BadRequest("El parámetro customerId es requerido");
                }

                log.Info("PASO 4: Llamando a ObtenerPagosPorClienteAsync");
                var resultado = _logOnvo.ObtenerPagosPorClienteAsync(customerId, token);
                log.Info($"PASO 5: Resultado obtenido: {resultado?.resultado}");

                if (resultado.resultado)
                {
                    log.Info($"PASO 6: Pagos obtenidos exitosamente. Count: {resultado.pagos.Count}");
                    log.Info("=== FIN GetPaymentsByCustomer (exitoso) ===");
                    return Ok(new
                    {
                        success = true,
                        count = resultado.pagos.Count,
                        pagos = resultado.pagos
                    });
                }
                else
                {
                    log.Warn($"PASO 6: No se encontraron pagos");
                    log.Info("=== FIN GetPaymentsByCustomer (NotFound) ===");
                    return Content(System.Net.HttpStatusCode.NotFound, new
                    {
                        success = false,
                        errores = resultado.error.Select(e => e.Message)
                    });
                }
            }
            catch (Exception ex)
            {
                log.Error($"ERROR en GetPaymentsByCustomer: {ex.Message}", ex);
                log.Error($"Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Error in GetPaymentsByCustomer: {ex.Message}");
                log.Info("=== FIN GetPaymentsByCustomer (con error) ===");
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
            log.Info("=== INICIO ObtenerHistorialPagos ===");
            try
            {
                log.Info($"PASO 1: CustomerId: {customerId}, Status: {status ?? "null"}");

                string token = Request.Headers.Authorization?.Parameter;
                log.Info($"PASO 2: Token presente: {!string.IsNullOrEmpty(token)}");

                if (string.IsNullOrEmpty(token))
                {
                    log.Warn("PASO 3: Token faltante");
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(customerId))
                {
                    log.Warn("PASO 3: CustomerId vacío");
                    return BadRequest("El parámetro customerId es requerido");
                }

                log.Info("PASO 4: Llamando a ObtenerHistorialPagosClienteAsync");
                var resultado = _logOnvo.ObtenerHistorialPagosClienteAsync(customerId, status, token);
                log.Info($"PASO 5: Resultado obtenido: {resultado?.resultado}");

                if (resultado.resultado)
                {
                    log.Info($"PASO 6: Historial obtenido. Total registros: {resultado.totalRegistros}");
                    log.Info("=== FIN ObtenerHistorialPagos (exitoso) ===");
                    return Ok(new
                    {
                        success = true,
                        total = resultado.totalRegistros,
                        data = resultado.pagos
                    });
                }
                else
                {
                    log.Warn("PASO 6: No se encontró historial");
                    log.Info("=== FIN ObtenerHistorialPagos (NotFound) ===");
                    return Content(System.Net.HttpStatusCode.NotFound, new
                    {
                        success = false,
                        errores = resultado.error.Select(e => e.Message)
                    });
                }
            }
            catch (Exception ex)
            {
                log.Error($"ERROR en ObtenerHistorialPagos: {ex.Message}", ex);
                log.Error($"Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Error in ObtenerHistorialPagos: {ex.Message}");
                log.Info("=== FIN ObtenerHistorialPagos (con error) ===");
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
            log.Info("=== INICIO ObtenerResumenPagos ===");
            try
            {
                log.Info($"PASO 1: CustomerId: {customerId}");

                string token = Request.Headers.Authorization?.Parameter;
                log.Info($"PASO 2: Token presente: {!string.IsNullOrEmpty(token)}");

                if (string.IsNullOrEmpty(token))
                {
                    log.Warn("PASO 3: Token faltante");
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(customerId))
                {
                    log.Warn("PASO 3: CustomerId vacío");
                    return BadRequest("El parámetro customerId es requerido");
                }

                log.Info("PASO 4: Llamando a ObtenerResumenPagosClienteAsync");
                var resultado = _logOnvo.ObtenerResumenPagosClienteAsync(customerId, token);
                log.Info($"PASO 5: Resultado obtenido: {resultado?.resultado}");

                if (resultado.resultado)
                {
                    log.Info($"PASO 6: Resumen obtenido. Total pagos: {resultado.totalPagos}");
                    log.Info("=== FIN ObtenerResumenPagos (exitoso) ===");
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
                }
                else
                {
                    log.Warn("PASO 6: No se encontró resumen");
                    log.Info("=== FIN ObtenerResumenPagos (NotFound) ===");
                    return Content(System.Net.HttpStatusCode.NotFound, new
                    {
                        success = false,
                        errores = resultado.error.Select(e => e.Message)
                    });
                }
            }
            catch (Exception ex)
            {
                log.Error($"ERROR en ObtenerResumenPagos: {ex.Message}", ex);
                log.Error($"Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Error in ObtenerResumenPagos: {ex.Message}");
                log.Info("=== FIN ObtenerResumenPagos (con error) ===");
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
            log.Info("=== INICIO ObtenerPagosPendientes ===");
            try
            {
                log.Info($"PASO 1: CustomerId: {customerId}");

                string token = Request.Headers.Authorization?.Parameter;
                log.Info($"PASO 2: Token presente: {!string.IsNullOrEmpty(token)}");

                if (string.IsNullOrEmpty(token))
                {
                    log.Warn("PASO 3: Token faltante");
                    return Unauthorized();
                }

                log.Info("PASO 4: Llamando a ObtenerPagosPendientesAsync");
                var resultado = _logOnvo.ObtenerPagosPendientesAsync(customerId, token);
                log.Info($"PASO 5: Resultado obtenido: {resultado?.resultado}");

                if (resultado.resultado)
                {
                    log.Info($"PASO 6: Pagos pendientes obtenidos: {resultado.pagos?.Count ?? 0}");
                    log.Info("=== FIN ObtenerPagosPendientes (exitoso) ===");
                    return Ok(new { success = true, data = resultado.pagos });
                }
                else
                {
                    log.Warn("PASO 6: No se encontraron pagos pendientes");
                    log.Info("=== FIN ObtenerPagosPendientes (NotFound) ===");
                    return Content(System.Net.HttpStatusCode.NotFound, new
                    {
                        success = false,
                        errores = resultado.error.Select(e => e.Message)
                    });
                }
            }
            catch (Exception ex)
            {
                log.Error($"ERROR en ObtenerPagosPendientes: {ex.Message}", ex);
                log.Error($"Stack trace: {ex.StackTrace}");
                log.Info("=== FIN ObtenerPagosPendientes (con error) ===");
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
            log.Info("=== INICIO ObtenerPagosCompletados ===");
            try
            {
                log.Info($"PASO 1: CustomerId: {customerId}");

                string token = Request.Headers.Authorization?.Parameter;
                log.Info($"PASO 2: Token presente: {!string.IsNullOrEmpty(token)}");

                if (string.IsNullOrEmpty(token))
                {
                    log.Warn("PASO 3: Token faltante");
                    return Unauthorized();
                }

                log.Info("PASO 4: Llamando a ObtenerPagosCompletadosAsync");
                var resultado = _logOnvo.ObtenerPagosCompletadosAsync(customerId, token);
                log.Info($"PASO 5: Resultado obtenido: {resultado?.resultado}");

                if (resultado.resultado)
                {
                    log.Info($"PASO 6: Pagos completados obtenidos: {resultado.pagos?.Count ?? 0}");
                    log.Info("=== FIN ObtenerPagosCompletados (exitoso) ===");
                    return Ok(new { success = true, data = resultado.pagos });
                }
                else
                {
                    log.Warn("PASO 6: No se encontraron pagos completados");
                    log.Info("=== FIN ObtenerPagosCompletados (NotFound) ===");
                    return Content(System.Net.HttpStatusCode.NotFound, new
                    {
                        success = false,
                        errores = resultado.error.Select(e => e.Message)
                    });
                }
            }
            catch (Exception ex)
            {
                log.Error($"ERROR en ObtenerPagosCompletados: {ex.Message}", ex);
                log.Error($"Stack trace: {ex.StackTrace}");
                log.Info("=== FIN ObtenerPagosCompletados (con error) ===");
                return InternalServerError(ex);
            }
        }
    }
}
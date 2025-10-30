using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic;
using System;
using System.Collections.Generic;

namespace Logica
{
    /// <summary>
    /// Servicio de pagos completamente simulado - Un solo método hace todo
    /// </summary>
    public class LogSimulatedPayment
    {
        private readonly Random _random;

        public LogSimulatedPayment()
        {
            _random = new Random();
            System.Diagnostics.Debug.WriteLine("LogSimulatedPayment inicializado");
        }

        #region UTILIDADES PRIVADAS

        private bool SimulateSuccess(double rate) => _random.NextDouble() < rate;

        private string GetRandomErrorCode()
        {
            var errors = new[] { "insufficient_funds", "card_declined", "expired_card",
                "incorrect_cvc", "processing_error", "card_not_supported",
                "issuer_not_available", "exceeded_limit" };
            return errors[_random.Next(errors.Length)];
        }

        private string GetErrorMessage(string code)
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
            return messages.ContainsKey(code) ? messages[code] : "Error desconocido";
        }

        private string GenerateId(string prefix) =>
            $"{prefix}_{Guid.NewGuid().ToString("N").Substring(0, 16)}";

        private void SimulateDelay() =>
            System.Threading.Thread.Sleep(_random.Next(500, 1500));

        private string DetectCardBrand(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber)) return "unknown";
            if (cardNumber.StartsWith("4")) return "visa";
            if (cardNumber.StartsWith("5")) return "mastercard";
            if (cardNumber.StartsWith("3")) return "amex";
            return "unknown";
        }

        /// <summary>
        /// Genera el estado del pago de forma aleatoria (igual que LogPlanes)
        /// </summary>
        private Tuple<string, string> GenerarResultadoPagoSimulado()
        {
            int random = _random.Next(1, 101); // 1-100

            if (random <= 50)
                return Tuple.Create("Completado", "Pago exitoso procesado correctamente");
            else if (random <= 65)
                return Tuple.Create("Pendiente", "Pago en proceso, aún no confirmado por la entidad emisora");
            else if (random <= 85)
                return Tuple.Create("Fallido", "Transacción declinada: fondos insuficientes o error en el banco");
            else if (random <= 95)
                return Tuple.Create("Cancelado", "Pago cancelado por el usuario antes de finalizar");
            else
                return Tuple.Create("Fallido", "Error desconocido en la transacción");
        }

        #endregion

        /// <summary>
        /// MÉTODO ÚNICO: Crea cliente, guarda tarjeta y registra el pago en una sola operación
        /// Simula errores aleatorios en cada paso
        /// </summary>
        public ResOnvoFlowCompleto ProcesarPagoCompletoSimulado(ReqOnvoFlowCompleto req, string token)
        {
            ResOnvoFlowCompleto res = new ResOnvoFlowCompleto();
            res.error = new List<Error>();

            try
            {
                // Validar token
                int? idUsuarioToken = JwtService.GetUserIdFromToken(token);
                if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 40000, Message = "Sesión vencida o token inválido" });
                    return res;
                }

                // Validar monto
                if (req.Amount <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 40002, Message = "El monto debe ser mayor a 0" });
                    return res;
                }

                // =====================================================
                // PASO 1: CREAR CLIENTE (95% éxito)
                // =====================================================
                SimulateDelay();

              
                string customerId = GenerateId("cust");
                bool? resultadoBdCliente = false;
                int? errorIdCliente = 0;

                try
                {
                    using (var linq = new DataClasses1DataContext())
                    {
                        linq.SP_REGISTRAR_CLIENTE_ONVO(
                            idUsuarioToken,
                            customerId,
                            req.Email,
                            req.Name,
                            req.Phone,
                            ref resultadoBdCliente,
                            ref errorIdCliente
                        );

                        if (!resultadoBdCliente.HasValue || !resultadoBdCliente.Value)
                        {
                            string mensajeErrorCliente = TraducirErrorBD(errorIdCliente, "cliente");
                            res.resultado = false;
                            res.error.Add(new Error
                            {
                                ErrorCode = errorIdCliente ?? 50002,
                                Message = mensajeErrorCliente
                            });
                            return res;
                        }
                    }
                }
                catch (System.Data.SqlClient.SqlException sqlEx)
                {
                    string mensajeErrorSql = TraducirErrorSql(sqlEx);
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = sqlEx.Number,
                        Message = mensajeErrorSql
                    });
                    return res;
                }
                catch (Exception ex)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 50099,
                        Message = $"Error al registrar cliente: {ex.Message}"
                    });
                    return res;
                }

                res.customerId = customerId;

                // =====================================================
                // PASO 2: VALIDAR Y GUARDAR TARJETA (70% éxito)
                // =====================================================
                SimulateDelay();

                // Validar número de tarjeta
                if (string.IsNullOrWhiteSpace(req.CardNumber?.ToString()))
                {
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 40001, Message = "Número de tarjeta requerido" });
                    return res;
                }

                // Simular rechazo de tarjeta (30% de probabilidad)
                if (!SimulateSuccess(0.70))
                {
                    var errorCode = GetRandomErrorCode();
                    var errorMessage = GetErrorMessage(errorCode);
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 40002, Message = errorMessage });
                    return res;
                }

                string paymentMethodId = GenerateId("pm");
                string cardNumber = req.CardNumber.ToString();
                string last4 = cardNumber.Substring(cardNumber.Length - 4);
                string brand = DetectCardBrand(cardNumber);

                bool? resultadoBdTarjeta = false;
                int? errorIdTarjeta = 0;

                try
                {
                    using (var linq = new DataClasses1DataContext())
                    {
                        linq.SP_REGISTRAR_METODO_PAGO_ONVO(
                            idUsuarioToken,
                            customerId,
                            paymentMethodId,
                            last4,
                            brand,
                            req.ExpMonth.ToString(),
                            req.ExpYear.ToString(),
                            ref resultadoBdTarjeta,
                            ref errorIdTarjeta
                        );

                        if (!resultadoBdTarjeta.HasValue || !resultadoBdTarjeta.Value)
                        {
                            string mensajeErrorTarjeta = TraducirErrorBD(errorIdTarjeta, "tarjeta");
                            res.resultado = false;
                            res.error.Add(new Error
                            {
                                ErrorCode = errorIdTarjeta ?? 50003,
                                Message = mensajeErrorTarjeta
                            });
                            return res;
                        }
                    }
                }
                catch (System.Data.SqlClient.SqlException sqlEx)
                {
                    string mensajeErrorSql = TraducirErrorSql(sqlEx);
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = sqlEx.Number,
                        Message = mensajeErrorSql
                    });
                    return res;
                }
                catch (Exception ex)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 50098,
                        Message = $"Error al registrar método de pago: {ex.Message}"
                    });
                    return res;
                }

                res.paymentMethodId = paymentMethodId;
                res.last4 = last4;
                res.brand = brand;

                // =====================================================
                // PASO 3: REGISTRAR PAGO (con estados aleatorios)
                // =====================================================
                SimulateDelay();

                var resultadoSimulado = GenerarResultadoPagoSimulado();
                string estadoPago = resultadoSimulado.Item1;
                string mensajeEstado = resultadoSimulado.Item2;

                bool? resultadoBdPago = false;
                int? errorIdPago = 0;
                int? idPagoOnvo = 0;

                try
                {
                    using (var linq = new DataClasses1DataContext())
                    {
                        linq.SP_REGISTRAR_PAGO_ONVO(
                            idUsuarioToken,
                            req.Amount,
                            req.Currency ?? "CRC",
                            req.Description,
                            estadoPago,
                            ref idPagoOnvo,
                            ref resultadoBdPago,
                            ref errorIdPago
                        );

                        if (!resultadoBdPago.HasValue || !resultadoBdPago.Value)
                        {
                            string mensajeErrorPago = TraducirErrorBD(errorIdPago, "pago");
                            res.resultado = false;
                            res.error.Add(new Error
                            {
                                ErrorCode = errorIdPago ?? 50004,
                                Message = mensajeErrorPago
                            });
                            return res;
                        }
                    }
                }
                catch (System.Data.SqlClient.SqlException sqlEx)
                {
                    string mensajeErrorSql = TraducirErrorSql(sqlEx);
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = sqlEx.Number,
                        Message = mensajeErrorSql
                    });
                    return res;
                }
                catch (Exception ex)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 50097,
                        Message = $"Error al registrar pago: {ex.Message}"
                    });
                    return res;
                }

                // =====================================================
                // RESULTADO FINAL
                // =====================================================
                res.idPagoOnvo = idPagoOnvo ?? 0;
                res.estadoPago = estadoPago;
                res.mensajeEstadoPago = mensajeEstado;
                res.amount = req.Amount;
                res.currency = req.Currency ?? "CRC";
                res.fechaCreacion = DateTime.Now;

                if (estadoPago == "Completado")
                {
                    res.resultado = true;
                    res.mensaje = "¡Pago procesado exitosamente!";
                }
                else
                {
                    res.resultado = false;
                    res.mensaje = $"Pago registrado con estado: {estadoPago}";
                }
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50000,
                    Message = $"Error inesperado en el proceso de pago: {ex.Message}"
                });
            }

            return res;
        }

        // =====================================================
        // MÉTODOS AUXILIARES PARA TRADUCIR ERRORES
        // =====================================================

        private string TraducirErrorBD(int? errorId, string contexto)
        {
            if (!errorId.HasValue)
                return $"Error desconocido al procesar {contexto}";

            switch (errorId.Value)
            {
                // Errores de validación (40xxx)
                case 40001:
                    return contexto == "pago" ? "El ID de usuario es inválido" : "Parámetro inválido";

                case 40002:
                    return "El monto debe ser mayor o igual a cero";

                case 40004:
                    return "El estado del pago es inválido. Estados permitidos: Pendiente, Completado, Fallido, Cancelado";

                case 40005:
                    return "El usuario no existe o está inactivo en el sistema";

                case 40007:
                    return "No tiene métodos de pago activos. Por favor registre una tarjeta válida";

                // Errores de duplicados (50015-50017)
                case 50015:
                    return "Este cliente ya está registrado en el sistema";

                case 50016:
                    return "No se encontró el cliente. Por favor registre el cliente primero";

                case 50017:
                    return "Este método de pago ya está registrado";

                // Errores de base de datos (50xxx)
                case 50001:
                    return "Error al crear el cliente en el sistema de pagos";

                case 50002:
                    return "Error al guardar la información del cliente en la base de datos";

                case 50003:
                    return "Error al guardar el método de pago en la base de datos";

                case 50004:
                    return "Error al registrar el pago en la base de datos";

                // Error genérico
                default:
                    return $"Error al procesar {contexto} (Código: {errorId})";
            }
        }

        private string TraducirErrorSql(System.Data.SqlClient.SqlException sqlEx)
        {
            switch (sqlEx.Number)
            {
                // Errores comunes de SQL Server
                case -1:
                case -2:
                    return "No se pudo conectar con la base de datos. Por favor intente más tarde";

                case 2:
                case 53:
                    return "Error de conexión con el servidor de base de datos";

                case 208:
                    return "No se encontró la tabla en la base de datos. Contacte al administrador";

                case 229:
                    return "No tiene permisos suficientes para realizar esta operación";

                case 515:
                    return "Falta un dato requerido en el registro";

                case 547:
                    return "No se puede completar la operación. Hay información relacionada que depende de este registro";

                case 2627:
                case 2601:
                    return "Ya existe un registro con estos datos. No se permiten duplicados";

                case 8152:
                    return "El texto ingresado es demasiado largo para el campo";

                case 18456:
                    return "Error de autenticación con la base de datos";

                default:
                    return $"Error de base de datos: {sqlEx.Message}";
            }
        }
    }
}
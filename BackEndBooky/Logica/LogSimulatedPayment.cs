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

                System.Diagnostics.Debug.WriteLine($"[INICIO] Procesando pago completo para: {req.Email}");
                System.Diagnostics.Debug.WriteLine($"[INICIO] Monto: {req.Amount} {req.Currency ?? "CRC"}");

                // =====================================================
                // PASO 1: CREAR CLIENTE (95% éxito)
                // =====================================================
                System.Diagnostics.Debug.WriteLine("[PASO 1/3] Creando cliente...");
                SimulateDelay();

                if (!SimulateSuccess(0.95))
                {
                    System.Diagnostics.Debug.WriteLine("❌ [PASO 1/3] Error al crear cliente");
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 50001, Message = "Error al crear cliente en el sistema de pagos" });
                    return res;
                }

                string customerId = GenerateId("cust");
                bool? resultadoBdCliente = false;
                int? errorIdCliente = 0;

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
                        System.Diagnostics.Debug.WriteLine("❌ [PASO 1/3] Error al guardar cliente en BD");
                        res.resultado = false;
                        res.error.Add(new Error { ErrorCode = errorIdCliente ?? 50002, Message = "Error al guardar cliente en BD local" });
                        return res;
                    }
                }

                res.customerId = customerId;
                System.Diagnostics.Debug.WriteLine($"✅ [PASO 1/3] Cliente creado: {customerId}");

                // =====================================================
                // PASO 2: VALIDAR Y GUARDAR TARJETA (70% éxito)
                // =====================================================
                System.Diagnostics.Debug.WriteLine("[PASO 2/3] Validando tarjeta...");
                SimulateDelay();

                // Validar número de tarjeta
                if (string.IsNullOrWhiteSpace(req.CardNumber?.ToString()))
                {
                    System.Diagnostics.Debug.WriteLine("❌ [PASO 2/3] Número de tarjeta inválido");
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 40001, Message = "Número de tarjeta requerido" });
                    return res;
                }

                // Simular rechazo de tarjeta (30% de probabilidad)
                if (!SimulateSuccess(0.70))
                {
                    var errorCode = GetRandomErrorCode();
                    var errorMessage = GetErrorMessage(errorCode);

                    System.Diagnostics.Debug.WriteLine($"❌ [PASO 2/3] Tarjeta rechazada: {errorCode}");
                    System.Diagnostics.Debug.WriteLine($"❌ Razón: {errorMessage}");

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
                        System.Diagnostics.Debug.WriteLine("❌ [PASO 2/3] Error al guardar tarjeta en BD");
                        res.resultado = false;
                        res.error.Add(new Error { ErrorCode = errorIdTarjeta ?? 50003, Message = "Error al guardar tarjeta en BD local" });
                        return res;
                    }
                }

                res.paymentMethodId = paymentMethodId;
                res.last4 = last4;
                res.brand = brand;
                System.Diagnostics.Debug.WriteLine($"✅ [PASO 2/3] Tarjeta guardada: {brand} **** {last4}");

                // =====================================================
                // PASO 3: REGISTRAR PAGO (con estados aleatorios)
                // =====================================================
                System.Diagnostics.Debug.WriteLine("[PASO 3/3] Procesando pago...");
                SimulateDelay();

                // Generar resultado aleatorio del pago (igual que LogPlanes)
                var resultadoSimulado = GenerarResultadoPagoSimulado();
                string estadoPago = resultadoSimulado.Item1;
                string mensajeEstado = resultadoSimulado.Item2;

                bool? resultadoBdPago = false;
                int? errorIdPago = 0;
                int? idPagoOnvo = 0;

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
                        System.Diagnostics.Debug.WriteLine("❌ [PASO 3/3] Error al registrar pago en BD");

                        string mensajeError = "Error al registrar pago en BD local";
                        switch (errorIdPago)
                        {
                            case 40007:
                                mensajeError = "No tiene métodos de pago activos";
                                break;
                            case 40002:
                                mensajeError = "Monto inválido";
                                break;
                        }

                        res.resultado = false;
                        res.error.Add(new Error { ErrorCode = errorIdPago ?? 50004, Message = mensajeError });
                        return res;
                    }
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

                // Si el pago fue exitoso
                if (estadoPago == "Completado")
                {
                    res.resultado = true;
                    res.mensaje = "¡Pago procesado exitosamente!";

                    System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════╗");
                    System.Diagnostics.Debug.WriteLine("║   ✅ PAGO COMPLETADO EXITOSAMENTE             ║");
                    System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════╝");
                }
                else
                {
                    res.resultado = false;
                    res.mensaje = $"Pago registrado con estado: {estadoPago}";

                    System.Diagnostics.Debug.WriteLine("╔════════════════════════════════════════════════╗");
                    System.Diagnostics.Debug.WriteLine($"║   ⚠️  PAGO {estadoPago.ToUpper()}                    ║");
                    System.Diagnostics.Debug.WriteLine("╚════════════════════════════════════════════════╝");
                }

                System.Diagnostics.Debug.WriteLine($"ID Pago: {idPagoOnvo}");
                System.Diagnostics.Debug.WriteLine($"Cliente ID: {customerId}");
                System.Diagnostics.Debug.WriteLine($"Tarjeta: {brand} **** {last4}");
                System.Diagnostics.Debug.WriteLine($"Monto: {req.Amount} {req.Currency ?? "CRC"}");
                System.Diagnostics.Debug.WriteLine($"Estado: {estadoPago}");
                System.Diagnostics.Debug.WriteLine($"Mensaje: {mensajeEstado}");
                System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════════");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 EXCEPCIÓN: {ex.Message}");
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50000, Message = $"Error inesperado: {ex.Message}" });
            }

            return res;
        }
    }
}
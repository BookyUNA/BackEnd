using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Logica
{
    public class LogPlanes
    {
        private readonly Random _random = new Random();

        // ================================================================
        // MÉTODO: RegistrarPagoOnvo
        // ================================================================
        public ResRegistrarPagoOnvo RegistrarPagoOnvo(ReqRegistrarPagoOnvo req, string token)
        {
            ResRegistrarPagoOnvo res = new ResRegistrarPagoOnvo
            {
                error = new List<Error>()
            };

            bool? resultadoBd = false;
            int? errorID = 0;
            int? idUsuarioToken = 0;
            int? idPagoOnvo = 0;

            try
            {
                idUsuarioToken = JwtService.GetUserIdFromToken(token);

                // Validación de sesión
                if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
                {
                    return CrearErrorPago(res, 40001, "Sesión vencida o token inválido");
                }


                if (req.Amount <= 0)
                {
                    return CrearErrorPago(res, 40002, "El monto debe ser mayor a 0");
                }

                // Simulación de resultado del pago
                var resultadoSimulado = GenerarResultadoPagoSimulado(); // Tuple<string,string>

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_REGISTRAR_PAGO_ONVO(
                        idUsuarioToken,
                        req.Amount,
                        req.Currency ?? "USD",
                        req.Description,
                        resultadoSimulado.Item1, // Estado
                        ref idPagoOnvo,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.IdPagoOnvo = idPagoOnvo ?? 0;
                        res.Estado = resultadoSimulado.Item1;
                        res.MensajeEstado = resultadoSimulado.Item2;
                       
                        res.Amount = req.Amount;
                        res.Currency = req.Currency ?? "USD";
                        res.FechaCreacion = DateTime.Now;

               
                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 40007:
                                res.error.Add(new Error { ErrorCode = 40007, Message = "No tiene metodos de pagos activos" });
                                break;
                            case 40002:
                                res.error.Add(new Error { ErrorCode = 40002, Message = "Monto invalido" });
                                break;
                          
                            default:
                                res.error.Add(new Error { ErrorCode = errorID ?? 99999, Message = "Error inesperado en la base de datos" });
                                break;
                        }
                        res.resultado = false;
                        res.error.Add(MapearErrorPago(errorID));
                    }
                }
            }
            catch (SqlException ex)
            {
                return CrearErrorPago(res, 50001, string.Format("Error de conexión a la base de datos: {0}", ex.Message));
            }
            catch (Exception ex)
            {
                return CrearErrorPago(res, 50002, string.Format("Error en la lógica al registrar el pago: {0}", ex.Message));
            }

            return res;
        }

        // ================================================================
        // MÉTODO: GenerarResultadoPagoSimulado
        // ================================================================
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


        // ================================================================
        // LOG del sistema
        // ================================================================
        private void RegistrarLogSistema(string mensaje)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("[{0}] {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), mensaje));
        }

        // ================================================================
        // Métodos auxiliares de error para pagos
        // ================================================================
        private ResRegistrarPagoOnvo CrearErrorPago(ResRegistrarPagoOnvo res, int codigo, string mensaje)
        {
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = codigo, Message = mensaje });
            RegistrarLogSistema(string.Format("[ERROR] {0}", mensaje));
            return res;
        }

        private Error MapearErrorPago(int? errorID)
        {
            if (!errorID.HasValue)
                return new Error { ErrorCode = 99999, Message = "Error inesperado en la base de datos" };

            switch (errorID.Value)
            {
                case 40001: return new Error { ErrorCode = 40001, Message = "IdUsuario inválido" };
                case 40002: return new Error { ErrorCode = 40002, Message = "Monto inválido" };
                case 40003: return new Error { ErrorCode = 40003, Message = "ReferenceId vacío" };
                case 40004: return new Error { ErrorCode = 40004, Message = "Estado inválido" };
                case 40005: return new Error { ErrorCode = 40005, Message = "Usuario no existe o está inactivo" };
                case 40006: return new Error { ErrorCode = 40006, Message = "ReferenceId duplicado" };
                default: return new Error { ErrorCode = errorID.Value, Message = "Error inesperado en la base de datos" };
            }
        }

        // ================================================================
        // MÉTODO: AsignarPlan
        // ================================================================
        public ResAsignarPlan AsignarPlan(ReqAsignarPlan req, string token)
        {
            ResAsignarPlan res = new ResAsignarPlan { error = new List<Error>() };

            bool? resultadoBd = false;
            int? errorID = 0;
            int? idUsuarioToken = 0;

            try
            {
                idUsuarioToken = JwtService.GetUserIdFromToken(token);
                if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
                    return CrearErrorPlan(res, 20001, "Sesión vencida o token inválido");

                if (req.NumeroPlan <= 0 || req.NumeroPlan > 3)
                    return CrearErrorPlan(res, 20005, "El número de plan debe ser 1 (Gratuito), 2 (Básico) o 3 (Premium)");

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_ASIGNAR_PLAN_USUARIO(
                        idUsuarioToken,
                        req.NumeroPlan,
                        req.IdPagoOnvo,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                       
                    }
                    else
                    {
                        res.resultado = false;
                        res.error.Add(MapearErrorPlan(errorID));
                    }
                }
            }
            catch (SqlException ex)
            {
                return CrearErrorPlan(res, 50001, string.Format("Error de conexión a la base de datos: {0}", ex.Message));
            }
            catch (Exception ex)
            {
                return CrearErrorPlan(res, 50002, string.Format("Error en la lógica al asignar el plan: {0}", ex.Message));
            }

            return res;
        }

        private string ObtenerNombrePlanPorNumero(int numeroPlan)
        {
            if (numeroPlan == 1) return "Gratuito";
            if (numeroPlan == 2) return "Básico";
            if (numeroPlan == 3) return "Premium";
            return "Desconocido";
        }

        private ResAsignarPlan CrearErrorPlan(ResAsignarPlan res, int codigo, string mensaje)
        {
            res.resultado = false;
            res.error.Add(new Error { ErrorCode = codigo, Message = mensaje });
            RegistrarLogSistema(string.Format("[ERROR PLAN] {0}", mensaje));
            return res;
        }

        private Error MapearErrorPlan(int? errorID)
        {
            if (!errorID.HasValue)
                return new Error { ErrorCode = 99999, Message = "Error inesperado en la base de datos" };

            switch (errorID.Value)
            {
                case 20001: return new Error { ErrorCode = 20001, Message = "El perfil profesional no existe" };
                case 20002: return new Error { ErrorCode = 20002, Message = "El plan no existe o no está activo" };
                case 20003: return new Error { ErrorCode = 20003, Message = "Error al asignar el plan" };
                case 20005: return new Error { ErrorCode = 20005, Message = "Número de plan inválido (debe ser 1, 2 o 3)" };
                default: return new Error { ErrorCode = errorID.Value, Message = "Error inesperado en la base de datos" };
            }
        }

        // ================================================================
        // MÉTODO: ObtenerPlan (igual a tu versión original)
        // ================================================================

        /// <summary>
        /// Obtiene el plan actual de un perfil profesional
        /// </summary>
        public ResObtenerPlan ObtenerPlan(string token)
        {
            ResObtenerPlan res = new ResObtenerPlan();
            res.error = new List<Error>();

            bool? resultadoBd = false;
            int? errorID = 0;
            int? idUsuarioToken = 0;

            try
            {
                idUsuarioToken = JwtService.GetUserIdFromToken(token);

                // Validación de sesión
                if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 20001,
                        Message = "Sesión vencida o token inválido"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    var resultado = linq.SP_ObtenerPlanUsuario(
                        idUsuarioToken,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        var planInfo = resultado.FirstOrDefault();

                        if (planInfo != null)
                        {
                            res.resultado = true;
                            res.plan = new Plan
                            {
                                IdPlan = planInfo.IdPlan,
                                Nombre = planInfo.NombrePlan,
                                Descripcion = planInfo.Descripcion,
                                PrecioMensual = planInfo.PrecioMensual,
                                PrecioAnual = planInfo.PrecioAnual,
                                MaxServicios = planInfo.MaxServicios,
                                MaxClientes = planInfo.MaxClientes,
                                MaxListaEspera = planInfo.MaxListaEspera,
                                MaxPoliticaCancelacion = planInfo.MaxPoliticaCancelacion,
                                IncluyeEstadisticas = planInfo.IncluyeEstadisticas,
                                IncluyeAnuncios = planInfo.IncluyeAnuncios,
                                Estado = planInfo.Estado
                            };
                        }
                        else
                        {
                            res.resultado = false;
                            res.error.Add(new Error
                            {
                                ErrorCode = 20004,
                                Message = "No se encontró información del plan"
                            });
                        }
                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 20001:
                                res.error.Add(new Error { ErrorCode = 20001, Message = "El perfil profesional no existe" });
                                break;
                            case 20004:
                                res.error.Add(new Error { ErrorCode = 20004, Message = "No se encontró información del plan" });
                                break;
                            default:
                                res.error.Add(new Error { ErrorCode = errorID ?? 99999, Message = "Error inesperado en la base de datos" });
                                break;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50001,
                    Message = $"Error de conexión a la base de datos: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = $"Error en la lógica al obtener el plan: {ex.Message}"
                });
            }

            return res;
        }
    }
}

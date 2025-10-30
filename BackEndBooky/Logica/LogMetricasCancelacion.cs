using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica
{
    public class LogMetricasCancelacion
    {
        /// <summary>
        /// Obtiene las métricas de cancelación de un cliente basado en una cita específica
        /// </summary>
        /// <param name="req">Objeto con IdCita para identificar al cliente</param>
        /// <param name="token">Token JWT del usuario profesional</param>
        /// <returns>Métricas de cancelación del cliente</returns>
        public ResMetricasCancelacion ObtenerMetricasCancelacion(ReqMetricasCancelacion req, string token)
        {
            ResMetricasCancelacion res = new ResMetricasCancelacion();
            res.error = new List<Error>();

            try
            {
                // =====================================================
                // VALIDACIÓN 1: Token y usuario profesional
                // =====================================================
                int? idUsuarioProfesional = JwtService.GetUserIdFromToken(token);

                if (!idUsuarioProfesional.HasValue || idUsuarioProfesional.Value <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40000,
                        Message = "Sesión vencida o token inválido"
                    });
                    return res;
                }

                // =====================================================
                // VALIDACIÓN 2: IdCita requerido
                // =====================================================
                if (req == null || req.IdCita <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40008,
                        Message = "El ID de la cita es requerido y debe ser mayor a 0"
                    });
                    return res;
                }

                // =====================================================
                // EJECUTAR STORED PROCEDURE
                // =====================================================
                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    // Variables de salida del SP
                    decimal? porcentajeCancelacion = 0;
                    string categoriaRiesgo = string.Empty;
                    int? totalCitas = 0;
                    int? citasCanceladas = 0;
                    bool? resultado = false;
                    int? errorId = 0;

                    try
                    {
                        // Ejecutar el SP mejorado
                        linq.SP_CALCULAR_PORCENTAJE_CANCELACION(
                            idUsuarioProfesional.Value,
                            req.IdCita,
                            ref porcentajeCancelacion,
                            ref categoriaRiesgo,
                            ref totalCitas,
                            ref citasCanceladas,
                            ref resultado,
                            ref errorId
                        );

                        // =====================================================
                        // EVALUAR RESULTADO DEL SP
                        // =====================================================
                        if (!resultado.HasValue || !resultado.Value)
                        {
                            string mensajeError = TraducirErrorMetricas(errorId);
                            res.resultado = false;
                            res.error.Add(new Error
                            {
                                ErrorCode = errorId ?? 50004,
                                Message = mensajeError
                            });
                            return res;
                        }

                        // =====================================================
                        // ESCENARIO 1: Sin citas (cliente nuevo)
                        // =====================================================
                        if (!totalCitas.HasValue || totalCitas.Value == 0)
                        {
                            res.resultado = true;
                            res.TotalCitas = 0;
                            res.CitasCanceladas = 0;
                         
                            res.PorcentajeCancelacion = 0.00m;
                            res.CategoriaRiesgo = "MUY BAJA";
                            res.FechaCalculo = DateTime.Now;
                            res.Mensaje = "Cliente sin historial de citas. Riesgo muy bajo";
                            return res;
                        }

                        // =====================================================
                        // ESCENARIO 2: Sin cancelaciones
                        // =====================================================
                        if (!citasCanceladas.HasValue || citasCanceladas.Value == 0)
                        {
                            res.resultado = true;
                            res.TotalCitas = totalCitas.Value;
                            res.CitasCanceladas = 0;
                      
                            res.PorcentajeCancelacion = 0.00m;
                            res.CategoriaRiesgo = "MUY BAJA";
                            res.FechaCalculo = DateTime.Now;
                            res.Mensaje = "Cliente sin cancelaciones. Riesgo muy bajo";
                            return res;
                        }

                        // =====================================================
                        // ESCENARIO 3: Con historial de cancelaciones
                        // =====================================================
                        res.resultado = true;
                        res.TotalCitas = totalCitas.Value;
                        res.CitasCanceladas = citasCanceladas.Value;
                    
                        res.PorcentajeCancelacion = porcentajeCancelacion ?? 0.00m;
                        res.CategoriaRiesgo = categoriaRiesgo ?? "NO DEFINIDA";
                        res.FechaCalculo = DateTime.Now;
                        res.Mensaje = ObtenerMensajePorCategoria(res.CategoriaRiesgo, res.PorcentajeCancelacion);
                    }
                    catch (SqlException sqlEx)
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
                }
            }
            catch (SqlException sqlEx)
            {
                string mensajeErrorSql = TraducirErrorSql(sqlEx);
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = sqlEx.Number,
                    Message = mensajeErrorSql
                });
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50000,
                    Message = $"Error inesperado al calcular métricas: {ex.Message}"
                });
            }

            return res;
        }

        // =====================================================
        // MÉTODOS AUXILIARES PARA TRADUCIR ERRORES
        // =====================================================

        /// <summary>
        /// Traduce códigos de error del SP de métricas a mensajes claros
        /// </summary>
        private string TraducirErrorMetricas(int? errorId)
        {
            if (!errorId.HasValue)
                return "Error desconocido al calcular métricas de cancelación";

            switch (errorId.Value)
            {
                case 40001:
                    return "El ID de usuario es inválido";

                case 40005:
                    return "El usuario no existe o está inactivo en el sistema";

                case 40006:
                    return "El usuario no tiene un perfil profesional activo";

                case 40008:
                    return "El ID de la cita es inválido";

                case 40009:
                    return "La cita no existe o no pertenece a este profesional";

                case 40010:
                    return "El cliente de la cita no existe o está inactivo";

                case 50004:
                    return "Error al calcular las métricas de cancelación en la base de datos";

                default:
                    return $"Error al procesar métricas (Código: {errorId})";
            }
        }

        /// <summary>
        /// Traduce excepciones SQL a mensajes comprensibles
        /// </summary>
        private string TraducirErrorSql(SqlException sqlEx)
        {
            switch (sqlEx.Number)
            {
                case -1:
                case -2:
                    return "No se pudo conectar con la base de datos. Por favor intente más tarde";

                case 2:
                case 53:
                    return "Error de conexión con el servidor de base de datos";

                case 208:
                    return "No se encontró la tabla de métricas en la base de datos. Contacte al administrador";

                case 229:
                    return "No tiene permisos suficientes para consultar métricas";

                case 515:
                    return "Falta un dato requerido para calcular las métricas";

                case 547:
                    return "Error de integridad en los datos de métricas";

                case 18456:
                    return "Error de autenticación con la base de datos";

                default:
                    return $"Error de base de datos: {sqlEx.Message}";
            }
        }

        /// <summary>
        /// Genera un mensaje descriptivo según la categoría de riesgo
        /// </summary>
        private string ObtenerMensajePorCategoria(string categoria, decimal porcentaje)
        {
            switch (categoria)
            {
                case "MUY BAJA":
                    return $"Cliente confiable con {porcentaje:F2}% de cancelaciones";

                case "BAJA":
                    return $"Cliente con bajo riesgo, {porcentaje:F2}% de cancelaciones";

                case "MEDIA":
                    return $"Cliente con riesgo moderado, {porcentaje:F2}% de cancelaciones";

                case "ALTA":
                    return $"Cliente con alto riesgo, {porcentaje:F2}% de cancelaciones";

                case "MUY ALTA":
                    return $"Cliente con muy alto riesgo, {porcentaje:F2}% de cancelaciones";

                default:
                    return $"Categoría de riesgo: {categoria}, {porcentaje:F2}% de cancelaciones";
            }
        }
    }
}
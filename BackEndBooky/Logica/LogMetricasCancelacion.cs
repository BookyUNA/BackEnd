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

        public ResMetricasCancelacion ObtenerMetricasCancelacion(string token)
        {
            ResMetricasCancelacion res = new ResMetricasCancelacion();
            res.error = new List<Error>();
            int? idUsuarioToken = 0;

            try
            {
                // Obtener el IdUsuario del token
                idUsuarioToken = JwtService.GetUserIdFromToken(token);

                // Validar que el IdUsuario sea válido
                if (!idUsuarioToken.HasValue || idUsuarioToken.Value <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 1,
                        Message = "Token inválido o usuario no identificado."
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    // Variables para recibir los datos del SP
                    decimal? porcentajeCancelacion = 0;
                    string categoriaRiesgo = string.Empty;
                    int? totalCitas = 0;
                    int? citasCanceladas = 0;
                    int? citasCompletadas = 0;
                    int? citasRechazadas = 0;

                    // Ejecutar el SP con todos los parámetros OUTPUT
                    linq.SP_CalcularPorcentajeCancelacion(
                        idUsuarioToken.Value,
                        ref porcentajeCancelacion,
                        ref categoriaRiesgo,
                        ref totalCitas,
                        ref citasCanceladas


                    );

                    // Verificar que se obtuvieron datos válidos
                    if (porcentajeCancelacion.HasValue && !string.IsNullOrEmpty(categoriaRiesgo))
                    {
                        res.resultado = true;
                        res.TotalCitas = totalCitas ?? 0;
                        res.CitasCanceladas = citasCanceladas ?? 0;
                        res.CitasCompletadas = citasCompletadas ?? 0;
                        res.CitasRechazadas = citasRechazadas ?? 0;
                        res.PorcentajeCancelacion = porcentajeCancelacion.Value;
                        res.CategoriaRiesgo = categoriaRiesgo;
                        res.FechaCalculo = DateTime.Now;
                    }
                    else
                    {
                        res.resultado = false;
                        res.error.Add(new Error
                        {
                            ErrorCode = 40001,
                            Message = "No se pudo calcular las métricas de cancelación."
                        });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50001,
                    Message = "Error de base de datos al obtener métricas de cancelación: " + sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica al obtener métricas: " + ex.Message
                });
            }

            return res;
        }
    }
}
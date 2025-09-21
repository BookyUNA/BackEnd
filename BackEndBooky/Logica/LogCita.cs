using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Logica
{
    public class LogCita
    {
        public ResActualizarEstadoCita ActualizarEstadoCita(ReqActualizarEstadoCita req, string token)
        {
            ResActualizarEstadoCita res = new ResActualizarEstadoCita();
            res.error = new List<Error>();
            bool? resultadoBd = false;
            int? errorID = 0;

            try
            {
                int? idUsuarioToken = JwtService.GetUserIdFromToken(token);

                // Validación del token
                if (!idUsuarioToken.HasValue || idUsuarioToken <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40000,
                        Message = "Sesión vencida o token inválido"
                    });
                    return res;
                }

                // Validación de parámetros
                if (req.IdCita <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40009,
                        Message = "El ID de la cita es obligatorio y debe ser válido"
                    });
                    return res;
                }

                if (!req.Aprobada && string.IsNullOrWhiteSpace(req.MotivoRechazo))
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40010,
                        Message = "El motivo de rechazo es obligatorio si la cita no se aprueba"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_ACTUALIZAR_ESTADO_CITA(
                        req.IdCita,
                        req.Aprobada,   // BIT en SQL
                        req.MotivoRechazo,
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
                        switch (errorID)
                        {
                            case 30001:
                                res.error.Add(new Error { ErrorCode = 30001, Message = "La cita no fue encontrada" });
                                break;
                            case 30002:
                                res.error.Add(new Error { ErrorCode = 30002, Message = "El motivo de rechazo es obligatorio" });
                                break;
                            default:
                                res.error.Add(new Error { ErrorCode = errorID ?? 99999, Message = "Error inesperado en la base de datos" });
                                break;
                        }
                    }
                }
            }
            catch (SqlException)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50001,
                    Message = "Error de base de datos al actualizar el estado de la cita"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de actualización de cita"
                });
            }

            return res;
        }
    }
}

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
    public class LogReprogramarCita
    {
        public ResReprogramarCita ReprogramarCita(ReqReprogramarCita req, string token)
        {
            ResReprogramarCita res = new ResReprogramarCita();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuarioToken = 0;

            try
            {
                idUsuarioToken = JwtService.GetUserIdFromToken(token);

                // Validación de token
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

                if (req.IdCita <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40002,
                        Message = "El ID de la cita es obligatorio y debe ser válido"
                    });
                    return res;
                }

                if (req.NuevaFechaCita <= DateTime.Now)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40003,
                        Message = "La fecha de la cita debe ser futura"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    /*linq.SP_REPROGRAMAR_CITA_PROFESIONAL(
                        idUsuarioToken,
                        req.IdCita,
                        req.NuevaFechaCita,
                        ref resultadoBd,
                        ref errorID
                    );
                    */

                    // Evaluar respuesta del SP
                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 40001:
                                res.error.Add(new Error { ErrorCode = 40001, Message = "ID de usuario inválido" });
                                break;
                            case 40004:
                                res.error.Add(new Error { ErrorCode = 40004, Message = "Usuario no encontrado o inactivo" });
                                break;
                            case 40005:
                                res.error.Add(new Error { ErrorCode = 40005, Message = "Cita no encontrado o inactivo" });
                                break;
                            case 40006:
                                res.error.Add(new Error { ErrorCode = 40006, Message = "Solo puedo reprogramar sus propias citas" });
                                break;
                            case 40007:
                                res.error.Add(new Error { ErrorCode = 40007, Message = "Ya hay una cita programada en esa fecha/hora" });
                                break;
                            default:
                                res.error.Add(new Error { ErrorCode = errorID ?? 99999, Message = "Error inesperado en la base de datos" });
                                break;
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50001,
                    Message = "Error de base de datos al solicitar la cita"
                });
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de solicitud de cita"
                });
            }
            return res;
        }
    }
}

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
    public class LogPerfilProfesional
    {
        public ResCalificacionPromedio ObtenerCalificacionPromedio(ReqCalificacionPromedio req, string token)
        {
            ResCalificacionPromedio res = new ResCalificacionPromedio();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuarioToken = 0;

            try
            {
                idUsuarioToken = JwtService.GetUserIdFromToken(token);

                // Validar token
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

                if (req.IdProfesional <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40002,
                        Message = "El Id del profesional es obligatorio y debe ser válido"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    var resultado = linq.SP_OBTENER_CALIFICACION_PROMEDIO_PROFESIONAL(
                        idUsuarioToken,
                        req.IdProfesional,
                        ref resultadoBd,
                        ref errorID
                    );

                    // Evaluar respuesta del SP
                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        var data = resultado.FirstOrDefault();
                        res.CalificacionPromedio = data.CalificacionPromedio;
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
                            case 40003:
                                res.error.Add(new Error { ErrorCode = 40003, Message = "Usuario no encontrado o inactivo" });
                                break;
                            case 40004:
                                res.error.Add(new Error { ErrorCode = 40004, Message = "Profesional no encontrado o inactivo" });
                                break;
                            case 40005:
                                res.error.Add(new Error { ErrorCode = 40005, Message = "No hay un perfil activo para este profesional" });
                                break;
                            case 40006:
                                res.error.Add(new Error { ErrorCode = 40006, Message = "No hay calificaciones registradas" });
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
                    Message = "Error de base de datos al obtener la calificación"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de obtención de calificación"
                });
            }
            return res;
        }

        public ResCalificarProfesional CalificarProfesional(ReqCalificarProfesional req, string token)
        {
            ResCalificarProfesional res = new ResCalificarProfesional();
            res.error = new List<Error>();
            bool? resultadoBd = false;
            int? errorID = 0;
            int? idUsuarioToken = 0;

            try
            {
                idUsuarioToken = JwtService.GetUserIdFromToken(token);

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

                if (req == null || req.IdCita <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40002,
                        Message = "El Id de la cita es obligatorio y debe ser válido"
                    });
                    return res;
                }

                if (req.Calificacion <= 0 || req.Calificacion > 5)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40003,
                        Message = "La calificación es obligatoria y debe estar entre 1 y 5"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_CALIFICAR_PROFESIONAL(
                        idUsuarioToken,
                        req.IdCita,
                        req.Calificacion,
                        ref resultadoBd,
                        ref errorID
                    );
                }

                if (resultadoBd == true)
                {
                    res.resultado = true;
                }
                else
                {
                    res.resultado = false;
                    switch (errorID)
                    {
                        case 40001:
                            res.error.Add(new Error { ErrorCode = 40001, Message = "IdUsuario inválido" });
                            break;
                        case 40004:
                            res.error.Add(new Error { ErrorCode = 40004, Message = "Cliente no encontrado o inactivo" });
                            break;
                        case 40005:
                            res.error.Add(new Error { ErrorCode = 40005, Message = "La cita no pertenece al cliente o no está completada" });
                            break;
                        default:
                            res.error.Add(new Error { ErrorCode = errorID ?? 99999, Message = "Error inesperado en la base de datos" });
                            break;
                    }
                }
            }
            catch (SqlException)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50001,
                    Message = "Error de base de datos al calificar al profesional"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de calificación del profesional"
                });
            }

            return res;
        }
    }
}

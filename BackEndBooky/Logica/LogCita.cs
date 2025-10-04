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
                        req.Aprobada,   
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

        public ResListarCitasCliente ListarCitasCliente(ReqListarCitasCliente req, string token)
        {
            ResListarCitasCliente res = new ResListarCitasCliente();
            res.error = new List<Error>();
            res.Citas = new List<CitaCliente>();

            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuarioToken = 0;

            try
            {
                idUsuarioToken = JwtService.GetUserIdFromToken(token);

                // Validación de sesión
                if (idUsuarioToken <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 20001,
                        Message = "Sesión vencida"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    var citas = linq.SP_LISTAR_CITAS_CLIENTE(
                        idUsuarioToken,
                        ref resultadoBd,
                        ref errorID
                    ).ToList();

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.Citas = citas.Select(c => new CitaCliente
                        {
                            IdCita = c.IdCita,
                            FechaCita = c.FechaCita,
                            DuracionMinutos = c.DuracionMinutos,
                            PrecioAcordado = (double)c.PrecioAcordado,
                            Estado = c.Estado,
                            MensajeSolicitud = c.MensajeSolicitud,
                            MotivoRechazo = c.MotivoRechazo,
                            MotivoCancelacion = c.MotivoCancelacion,
                            FechaSolicitud = c.FechaSolicitud,
                            FechaRespuesta = c.FechaRespuesta,

                            CedulaUsuario = c.CedulaUsuario,
                            NombreUsuario = c.NombreUsuario,
                            EmailUsuario = c.EmailUsuario,
                            TelefonoUsuario = c.TelefonoUsuario,

                            Profesion = c.Profesion,
                            DescripcionPerfil = c.DescripcionPerfil,
                            Direccion = c.Direccion,
                            NombreProfesional = c.NombreProfesional,
                            EmailProfesional = c.EmailProfesional,
                            TelefonoProfesional = c.TelefonoProfesional,
                            NombreServicio = c.NombreServicio
                        }).ToList();
                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 40001:
                                res.error.Add(new Error { ErrorCode = 40001, Message = "El IdUsuario es obligatorio" });
                                break;
                            case 40002:
                                res.error.Add(new Error { ErrorCode = 40002, Message = "Usuario no encontrado o inactivo" });
                                break;
                            case 40003:
                                res.error.Add(new Error { ErrorCode = 40003, Message = "No hay citas para este cliente" });
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
                    Message = "Error de conexión a la base de datos"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica al listar las citas"
                });
            }

            return res;
        }

        public ResListarCitasProfesional ListarCitasProfesional(ReqListarCitasProfesional req, string token)
        {
            ResListarCitasProfesional res = new ResListarCitasProfesional();
            res.error = new List<Error>();
            res.Citas = new List<CitaProfesional>();

            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuarioToken = 0;

            try
            {
                idUsuarioToken = JwtService.GetUserIdFromToken(token);

                // Validación de sesión
                if (idUsuarioToken <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 20001,
                        Message = "Sesión vencida"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    var citas = linq.SP_LISTAR_CITAS_PROFESIONAL(
                        idUsuarioToken,
                        ref resultadoBd,
                        ref errorID
                    ).ToList();

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.Citas = citas.Select(c => new CitaProfesional
                        {
                            IdCita = c.IdCita,
                            FechaCita = c.FechaCita,
                            DuracionMinutos = c.DuracionMinutos,
                            PrecioAcordado = (double)c.PrecioAcordado,
                            Estado = c.Estado,
                            MensajeSolicitud = c.MensajeSolicitud,
                            MotivoRechazo = c.MotivoRechazo,
                            MotivoCancelacion = c.MotivoCancelacion,
                            FechaSolicitud = c.FechaSolicitud,
                            FechaRespuesta = c.FechaRespuesta,

                            CedulaUsuario = c.CedulaUsuario,
                            NombreUsuario = c.NombreUsuario,
                            EmailUsuario = c.EmailUsuario,
                            TelefonoUsuario = c.TelefonoUsuario,

                            Profesion = c.Profesion,
                            DescripcionPerfil = c.DescripcionPerfil,
                            Direccion = c.Direccion,
                            NombreProfesional = c.NombreProfesional,
                            EmailProfesional = c.EmailProfesional,
                            TelefonoProfesional = c.TelefonoProfesional,
                            NombreServicio = c.NombreServicio
                        }).ToList();
                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 40001:
                                res.error.Add(new Error { ErrorCode = 40001, Message = "El IdUsuario es obligatorio" });
                                break;
                            case 40002:
                                res.error.Add(new Error { ErrorCode = 40002, Message = "Usuario no encontrado o inactivo" });
                                break;
                            case 40003:
                                res.error.Add(new Error { ErrorCode = 40003, Message = "Profesional no encontrado o inactivo" });
                                break;
                            case 40004:
                                res.error.Add(new Error { ErrorCode = 40004, Message = "No hay citas para este cliente" });
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
                    Message = "Error de conexión a la base de datos"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica al listar las citas"
                });
            }

            return res;
        }
    }
}

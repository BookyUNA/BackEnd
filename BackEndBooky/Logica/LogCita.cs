using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic;
using Logic.Helpers;
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
            string nombreCliente = null;
            string correoCliente = null;
            string nombreProfesional = null;
            string nombreServicio = null;
            DateTime? fechaCita = null;

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
                        ref errorID,
                        ref nombreCliente,
                        ref correoCliente,
                        ref nombreProfesional,
                        ref nombreServicio,
                        ref fechaCita
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;

                        // Envío de correo al cliente
                        try
                        {
                            string fechaFormateada = fechaCita?.ToString("dd/MM/yyyy HH:mm");

                            if (req.Aprobada)
                            {
                                // Cita aprobada
                                EmailService.EnviarCorreo(
                                    correoCliente,
                                    "Cita confirmada - Booky",
                                    $"Hola {nombreCliente},<br><br>" +
                                    $"Tu cita con el profesional <b>{nombreProfesional}</b> ha sido <b>confirmada</b>. ¡Te esperamos!<br>" +
                                    $"<b>Servicio:</b> {nombreServicio}<br>" +
                                    $"<b>Fecha:</b> {fechaFormateada}<br><br>" +
                                    "Gracias por usar <b>Booky</b>."
                                );
                            }
                            else
                            {
                                // Cita rechazada
                                EmailService.EnviarCorreo(
                                    correoCliente,
                                    "Cita rechazada - Booky",
                                    $"Hola {nombreCliente},<br><br>" +
                                    $"Tu cita con el profesional <b>{nombreProfesional}</b> ha sido <b>rechazada</b>.<br>" +
                                    $"<b>Servicio:</b> {nombreServicio}<br>" +
                                    $"<b>Fecha:</b> {fechaFormateada}<br>" +
                                    $"<b>Motivo:</b> {req.MotivoRechazo}<br><br>" +
                                    "Puedes agendar otra cita en el momento que prefieras.<br><br>" +
                                    "Gracias por usar <b>Booky</b>."
                                );
                            }
                        }
                        catch (Exception)
                        {
                            res.error.Add(new Error
                            {
                                ErrorCode = 60001,
                                Message = "El estado fue actualizado, pero ocurrió un error al enviar el correo de notificación."
                            });
                        }
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

                            IdProfesional = c.IdProfesional,
                            Profesion = c.Profesion,
                            CalificacionPromedio = c.CalificacionPromedio,
                            EstadoCalificacion = c.EstadoCalificacion,
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

                            IdProfesional = c.IdProfesional,
                            Profesion = c.Profesion,
                            CalificacionPromedio = c.CalificacionPromedio,
                            EstadoCalificacion = c.EstadoCalificacion,
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

        public ResCancelarCita CancelarCita(ReqCancelarCita req, string token)
        {
            ResCancelarCita res = new ResCancelarCita();
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

                if (string.IsNullOrWhiteSpace(req.MotivoCancelacion))
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40003,
                        Message = "El motivo de cancelación es obligatorio"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_CANCELAR_CITA_CLIENTE(
                        idUsuarioToken,
                        req.IdCita,
                        req.MotivoCancelacion,
                        ref resultadoBd,
                        ref errorID
                    );

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
                                res.error.Add(new Error { ErrorCode = 40005, Message = "Cita no encontrada" });
                                break;
                            case 40006:
                                res.error.Add(new Error { ErrorCode = 40006, Message = "Solo puede cancelar sus propias citas" });
                                break;
                            case 40007:
                                res.error.Add(new Error { ErrorCode = 40007, Message = "Solo puede cancelar citas en estado Pendiente o Confirmada" });
                                break;
                            case 40008:
                                res.error.Add(new Error { ErrorCode = 40008, Message = "La cita solo se puede cancelar con al menos 24 horas de anticipación" });
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
                    Message = "Error de base de datos al cancelar la cita"
                });
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de cancelación de cita"
                });
            }
            return res;
        }
    }
}

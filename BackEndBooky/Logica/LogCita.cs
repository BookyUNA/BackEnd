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
                    // Ejecutar SP para actualizar el estado
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

                        // === OBTENER DATOS PARA EL CORREO ===
                        var datosCita = linq.SP_OBTENER_DATOS_CITA_CORREO(req.IdCita).FirstOrDefault();

                        if (datosCita != null)
                        {
                            try
                            {
                                // === CORREO PARA EL CLIENTE ===
                                string asuntoCliente, cuerpoCliente;
                                if (req.Aprobada)
                                {
                                    asuntoCliente = "Confirmación de cita aprobada";
                                                                    cuerpoCliente = $@"
                                Estimado/a {datosCita.NombreCliente},

                                Su cita con el profesional {datosCita.NombreProfesional} ha sido **aprobada** exitosamente.

                                🗓 Fecha de la cita: {datosCita.FechaCita:dd/MM/yyyy HH:mm}
                                ⏱ Duración: {datosCita.DuracionMinutos} minutos
                                💰 Precio acordado: {datosCita.PrecioAcordado:C}
                                📍 Dirección: {datosCita.Direccion}

                                Por favor, asegúrese de asistir puntualmente.

                                Gracias por utilizar nuestro sistema.";
                                                                }
                                                                else
                                                                {
                                                                    asuntoCliente = "Notificación de cita rechazada";
                                                                    cuerpoCliente = $@"
                                Estimado/a {datosCita.NombreCliente},

                                Su cita con el profesional {datosCita.NombreProfesional} ha sido **rechazada**.

                                🗓 Fecha solicitada: {datosCita.FechaCita:dd/MM/yyyy HH:mm}
                                💼 Profesional: {datosCita.NombreProfesional} ({datosCita.Profesion})

                                Motivo del rechazo:
                                ➡ {req.MotivoRechazo}

                                Le invitamos a reagendar una nueva cita si lo desea.";
                                }

                                EmailService.EnviarCorreo(datosCita.EmailCliente, asuntoCliente, cuerpoCliente);

                                // === CORREO PARA EL PROFESIONAL ===
                                string asuntoProfesional, cuerpoProfesional;
                                if (req.Aprobada)
                                {
                                    asuntoProfesional = "Cita confirmada con un cliente";
                                    cuerpoProfesional = $@"
                                Estimado/a {datosCita.NombreProfesional},

                                Usted ha aprobado una cita con el cliente {datosCita.NombreCliente}.

                                🗓 Fecha de la cita: {datosCita.FechaCita:dd/MM/yyyy HH:mm}
                                ⏱ Duración: {datosCita.DuracionMinutos} minutos
                                💰 Precio acordado: {datosCita.PrecioAcordado:C}
                                📞 Teléfono del cliente: {datosCita.TelefonoCliente}

                                Por favor, prepárese para la atención.";
                                                                }
                                                                else
                                                                {
                                                                    asuntoProfesional = "Cita rechazada registrada";
                                                                    cuerpoProfesional = $@"
                                Estimado/a {datosCita.NombreProfesional},

                                Usted ha rechazado una cita con el cliente {datosCita.NombreCliente}.

                                🗓 Fecha solicitada: {datosCita.FechaCita:dd/MM/yyyy HH:mm}
                                Motivo de rechazo:
                                ➡ {req.MotivoRechazo}

                                La información ha sido registrada correctamente.";
                                                                }

                                EmailService.EnviarCorreo(datosCita.EmailProfesional, asuntoProfesional, cuerpoProfesional);
                            }
                            catch (Exception exCorreo)
                            {
                                res.error.Add(new Error
                                {
                                    ErrorCode = 60001,
                                    Message = "Estado actualizado, pero ocurrió un error al enviar los correos"
                                });
                            }
                        }
                        else
                        {
                            res.error.Add(new Error
                            {
                                ErrorCode = 60002,
                                Message = "Estado actualizado, pero no se pudieron obtener los datos para el correo"
                            });
                        }
                    }
                    else
                    {
                        // Manejo de errores del SP de actualización
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

                // Validaciones de entrada
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
                    // Llamada al procedimiento para cancelar la cita
                    linq.SP_CANCELAR_CITA_CLIENTE(
                        idUsuarioToken,
                        req.IdCita,
                        req.MotivoCancelacion,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;

                        // === OBTENER DATOS DE LA CITA PARA CORREO ===
                        var datosCita = linq.SP_OBTENER_DATOS_CITA_CORREO(req.IdCita).FirstOrDefault();

                        if (datosCita != null)
                        {
                            try
                            {
                                // === Correo para el Cliente ===
                                string asuntoCliente = "Confirmación de cancelación de cita";
                                string cuerpoCliente = $@"
Estimado/a {datosCita.NombreCliente},

Su cita con el profesional {datosCita.NombreProfesional} ha sido cancelada exitosamente.

🗓 Fecha original: {datosCita.FechaCita:dd/MM/yyyy HH:mm}
💼 Profesional: {datosCita.NombreProfesional} ({datosCita.Profesion})
📍 Dirección: {datosCita.Direccion}

Motivo de cancelación: {req.MotivoCancelacion}

Gracias por utilizar nuestro servicio.";

                                EmailService.EnviarCorreo(datosCita.EmailCliente, asuntoCliente, cuerpoCliente);

                                // === Correo para el Profesional ===
                                string asuntoProfesional = "Notificación de cita cancelada por el cliente";
                                string cuerpoProfesional = $@"
Estimado/a {datosCita.NombreProfesional},

El cliente {datosCita.NombreCliente} ha cancelado la cita programada.

🗓 Fecha original: {datosCita.FechaCita:dd/MM/yyyy HH:mm}
⏱ Duración: {datosCita.DuracionMinutos} minutos
💰 Precio acordado: {datosCita.PrecioAcordado:C}

Motivo de cancelación: {req.MotivoCancelacion}

Por favor, actualice su agenda según corresponda.";

                                EmailService.EnviarCorreo(datosCita.EmailProfesional, asuntoProfesional, cuerpoProfesional);
                            }
                            catch (Exception exCorreo)
                            {
                                res.error.Add(new Error
                                {
                                    ErrorCode = 60001,
                                    Message = "Cita cancelada, pero ocurrió un error al enviar los correos"
                                });
                            }
                        }
                        else
                        {
                            res.error.Add(new Error
                            {
                                ErrorCode = 60002,
                                Message = "Cita cancelada, pero no se pudieron obtener los datos para enviar el correo"
                            });
                        }
                    }
                    else
                    {
                        // Manejo de errores del SP de cancelación
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
            catch (SqlException)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50001,
                    Message = "Error de base de datos al cancelar la cita"
                });
            }
            catch (Exception)
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

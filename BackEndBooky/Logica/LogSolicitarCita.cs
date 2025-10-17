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
using System.Text;
using System.Threading.Tasks;

namespace Logica
{
    public class LogSolicitarCita
    {
        public ResSolicitarCita SolicitarCita(ReqSolicitarCita req, string token)
        {
            ResSolicitarCita res = new ResSolicitarCita();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;
            int? idCita = null;
            int? idUsuarioToken = 0;
            string nombreCliente = null, correoCliente = null;
            string nombreProfesional = null, correoProfesional = null;
            string nombreServicio = null;

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

                if (req.IdServicio <= 0)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40003,
                        Message = "El ID del servicio es obligatorio y debe ser válido"
                    });
                    return res;
                }

                if (req.FechaCita <= DateTime.Now)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40004,
                        Message = "La fecha de la cita debe ser futura"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_SOLICITAR_CITA_PROFESIONAL(
                        idUsuarioToken,
                        req.IdServicio,
                        req.FechaCita,
                        req.MensajeSolicitud,
                        ref idCita,
                        ref resultadoBd,
                        ref errorID,
                        ref nombreCliente,
                        ref correoCliente,
                        ref nombreProfesional,
                        ref correoProfesional,
                        ref nombreServicio
                    );

                    // Evaluar respuesta del SP
                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.IdCita = idCita;

                        try
                        {
                            string fechaFormateada = req.FechaCita.ToString("dd/MM/yyyy HH:mm");

                            // Correo para el cliente
                            EmailService.EnviarCorreo(
                                correoCliente,
                                "Nueva solicitud de cita - Booky",
                                $"Hola {nombreCliente},<br><br>" +
                                $"Tu cita con <b>{nombreProfesional}</b> ha sido registrada exitosamente.<br>" +
                                $"<b>Servicio:</b> {nombreServicio}<br>" +
                                $"<b>Fecha:</b> {fechaFormateada}<br><br>" +
                                "Gracias por usar <b>Booky</b>."
                            );

                            // Correo para el profesional
                            EmailService.EnviarCorreo(
                                correoProfesional,
                                "Nueva solicitud de cita - Booky",
                                $"Hola {nombreProfesional},<br><br>" +
                                $"Has recibido una nueva solicitud de cita de <b>{nombreCliente}</b>.<br>" +
                                $"<b>Servicio:</b> {nombreServicio}<br>" +
                                $"<b>Fecha solicitada:</b> {fechaFormateada}<br>" +
                                $"<b>Mensaje del cliente:</b> {req.MensajeSolicitud ?? "(sin mensaje)"}<br><br>" +
                                "Por favor revisa tu panel para confirmar o rechazar la cita.<br><br>" +
                                "Gracias por usar <b>Booky</b>."
                            );
                        }
                        catch (Exception)
                        {
                            res.error.Add(new Error
                            {
                                ErrorCode = 60001,
                                Message = "La cita fue creada, pero ocurrió un error al enviar las notificaciones por correo."
                            });
                        }

                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 40001:
                                res.error.Add(new Error { ErrorCode = 40001, Message = "ID de usuario inválido" });
                                break;
                            case 40005:
                                res.error.Add(new Error { ErrorCode = 40005, Message = "Usuario no encontrado o inactivo" });
                                break;
                            case 40006:
                                res.error.Add(new Error { ErrorCode = 40006, Message = "Servicio no encontrado o inactivo" });
                                break;
                            case 40007:
                                res.error.Add(new Error { ErrorCode = 40007, Message = "Perfil profesional no encontrado o inactivo" });
                                break;
                            case 40008:
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
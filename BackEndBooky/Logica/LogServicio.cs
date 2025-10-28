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
    public class LogServicio
    {
        public ResListarServicio ListarServicios(ReqListarServicio req, string token)
        {
            ResListarServicio res = new ResListarServicio();
            res.error = new List<Error>();
            res.servicios = new List<Servicio>();

            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuarioToken = 0;
            idUsuarioToken = JwtService.GetUserIdFromToken(token);
            try
            {
                // Validación de campos obligatorios
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
                    var servicios = linq.SP_LISTAR_SERVICIOS_PROFESIONAL(
                        idUsuarioToken,
                        req.nombre,
                        ref resultadoBd,
                        ref errorID
                    ).ToList();

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.servicios = servicios.Select(s => new Servicio
                        {
                            IdServicio = s.IdServicio,
                            IdProfesional = s.IdPerfil,
                            Nombre = s.Nombre,
                            Descripcion = s.Descripcion,
                            DuracionMinutos = s.DuracionMinutos,
                            Precio = s.Precio,
                            PermiteDescuento = s.PermiteDescuento,
                            PorcentajeDescuento = s.PorcentajeDescuento,
                            FechaCreacion = s.FechaCreacion,
                            Estado = s.Estado
                        }).ToList();
                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 20001:
                                res.error.Add(new Error { ErrorCode = 20001, Message = "El IdUsuario es obligatorio" });
                                break;
                            case 20002:
                                res.error.Add(new Error { ErrorCode = 20002, Message = "Perfil profesional no encontrado" });
                                break;
                            case 20003:
                                res.error.Add(new Error { ErrorCode = 20003, Message = "No hay servicios asociados a este usuario" });
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
                    Message = "Error en la lógica al listar los servicios"
                });
            }

            return res;
        }
        public ResCrearServicio CrearServicio(ReqCrearServicio req, string token)
        {
            ResCrearServicio res = new ResCrearServicio();
            res.error = new List<Error>();

            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);

            try
            {
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
                    int? idServicioCreado = 0;

                    linq.SP_CREAR_SERVICIO(
                        idUsuarioToken,
                        req.nombre,
                        req.descripcion,
                        req.duracionMinutos,
                        req.precio,
                        req.permiteDescuento,
                        req.porcentajeDescuento,
                        ref idServicioCreado,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.idServicio = idServicioCreado ?? 0;
                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 20001:
                                res.error.Add(new Error { ErrorCode = 20001, Message = "El IdUsuario es obligatorio" });
                                break;
                            case 20002:
                                res.error.Add(new Error { ErrorCode = 20002, Message = "Perfil profesional no encontrado" });
                                break;
                            case 20003:
                                res.error.Add(new Error { ErrorCode = 20003, Message = "Error al crear el servicio" });
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
                    Message = "Error en la lógica al crear el servicio"
                });
            }

            return res;
        }

        public ResActualizarServicio ActualizarServicio(ReqActualizarServicio req, string token)
        {
            ResActualizarServicio res = new ResActualizarServicio();
            res.error = new List<Error>();

            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuarioToken = JwtService.GetUserIdFromToken(token);

            try
            {
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
                    linq.SP_ACTUALIZAR_SERVICIO(
                        idUsuarioToken,
                        req.idServicio,
                        req.nombre,
                        req.descripcion,
                        req.duracionMinutos,
                        req.precio,
                        req.permiteDescuento,
                        req.porcentajeDescuento,
                        req.estado,
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
                            case 20001:
                                res.error.Add(new Error { ErrorCode = 20001, Message = "El IdUsuario es obligatorio" });
                                break;
                            case 20002:
                                res.error.Add(new Error { ErrorCode = 20002, Message = "Perfil profesional no encontrado" });
                                break;
                            case 20003:
                                res.error.Add(new Error { ErrorCode = 20003, Message = "El servicio no existe" });
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
                    Message = "Error en la lógica al actualizar el servicio"
                });
            }

            return res;
        }


        public ResListarServiciosParaClientes ListarServiciosParaClientes(ReqListarServiciosParaClientes req, string token)
        {
            ResListarServiciosParaClientes res = new ResListarServiciosParaClientes();
            res.error = new List<Error>();
            res.servicios = new List<ServicioParaClientes>();

            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuarioToken = 0;
            idUsuarioToken = JwtService.GetUserIdFromToken(token);

            try
            {
                // Validación de campos obligatorios
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
                    var servicios = linq.SP_LISTAR_SERVICIOS_DISPONIBLES(
                        req.nombreServicio,
                        req.nombreProfesional,
                        req.profesion,
                        ref resultadoBd,
                        ref errorID
                    ).ToList();

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.servicios = servicios.Select(s => new ServicioParaClientes
                        {
                            idServicio = s.IdServicio,
                            nombreServicio = s.NombreServicio,
                            descripcion = s.Descripcion,
                            duracionMinutos = (int)s.DuracionMinutos,
                            precio =(double) s.Precio,
                            permiteDescuento = s.PermiteDescuento,
                            porcentajeDescuento =(double) s.PorcentajeDescuento,
                            fechaCreacion = s.FechaCreacion,
                           
                            nombreProfesional = s.NombreProfesional,
                            profesion = s.Profesion
                        }).ToList();
                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 20003:
                                res.error.Add(new Error { ErrorCode = 20003, Message = "No hay servicios disponibles" });
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
                    Message = "Error en la lógica al listar los servicios disponibles"
                });
            }

            return res;
        }






    }
}

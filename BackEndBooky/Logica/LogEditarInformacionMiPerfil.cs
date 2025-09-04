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
    public class LogEditarInformacionMiPerfil
    {
        public ResEditarInformacionMiPerfil EditarMiPerfil (ReqEditarInformacionMiPerfil req, string token)
        {
            ResEditarInformacionMiPerfil res = new ResEditarInformacionMiPerfil();
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

                // Validar campos si vienen con valor
                if (req.Nombre != null && string.IsNullOrWhiteSpace(req.Nombre))
                {
                    res.resultado = false;
                    res.error.Add(new Error 
                    { ErrorCode = 40003, 
                      Message = "El nombre no puede estar vacío" 
                    });
                    return res;
                }

                if (req.Telefono != null && string.IsNullOrWhiteSpace(req.Telefono))
                {
                    res.resultado = false;
                    res.error.Add(new Error 
                    { ErrorCode = 40004, 
                      Message = "El teléfono no puede estar vacío" 
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_EDITAR_INFORMACION_MI_PERFIL(
                        idUsuarioToken,
                        req.Nombre,
                        req.Telefono,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.Nombre = req.Nombre;
                        res.Telefono = req.Telefono;
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
                    Message = "Error en la lógica al obtener el perfil"
                });
            }

            return res;
        }
    }
}

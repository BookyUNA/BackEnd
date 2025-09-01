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
    public class LogMostrarInformacionMiPerfil
    {
        public ResMostrarInformacionMiPerfil InformacionMiPerfil(ReqMostrarInformacionMiPerfil req, string token)
        {
            ResMostrarInformacionMiPerfil res = new ResMostrarInformacionMiPerfil();
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

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    var perfil = linq.SP_OBTENER_INFORMACION_MI_PERFIL(
                        idUsuarioToken,
                        ref resultadoBd,
                        ref errorID
                    ).FirstOrDefault();

                    if (resultadoBd.HasValue && resultadoBd.Value && perfil != null)
                    {
                        res.resultado = true;
                        res.Nombre = perfil.Nombre;
                        res.Correo = perfil.Email;
                        res.Cedula = perfil.Cedula;
                        res.Telefono = perfil.Telefono;
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

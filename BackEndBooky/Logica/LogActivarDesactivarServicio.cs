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
    public class LogActivarDesactivarServicio
    {
        public ResActivarDesactivarServicio ActivarDesactivarServicio(ReqActivarDesactivarServicio req, string token)
        {
            ResActivarDesactivarServicio res = new ResActivarDesactivarServicio();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;
            bool? estadoServicio = null;
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
                    var query = linq.SP_ACTIVAR_DESACTIVAR_SERVICIO(
                        req.IdServicio,
                        idUsuarioToken,
                        ref estadoServicio,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        res.EstadoServicio = estadoServicio;
                    }
                    else
                    {
                        res.resultado = false;
                        switch (errorID)
                        {
                            case 40002:
                                res.error.Add(new Error { ErrorCode = 40002, Message = "El IdServicio es obligatorio" });
                                break;
                            case 40003:
                                res.error.Add(new Error { ErrorCode = 40003, Message = "Servicio no encontrado" });
                                break;
                            case 40004:
                                res.error.Add(new Error { ErrorCode = 40004, Message = "El servicio tiene citas pendientes o confirmadas y no se puede desactivar" });
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
                    Message = "Error en la lógica al activar/desactivar el servicio"
                });
            }
            return res;
        }
    }
}

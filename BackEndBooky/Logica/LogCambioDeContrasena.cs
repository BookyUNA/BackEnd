using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica
{
    public class LogCambioDeContrasena
    {
        public ResCambioDeContrasena CambioDeContrasena(ReqCambioDeContrasena req)
        {
            ResCambioDeContrasena res = new ResCambioDeContrasena();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;

            try
            {
                if (string.IsNullOrEmpty(req.CodigoRecuperacion) ||
                    string.IsNullOrEmpty(req.NuevaContrasenaHash) ||
                    string.IsNullOrEmpty(req.ConfirmacionContrasenaHash))
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 1,
                        Message = "Todos los campos son obligatorios."
                    });
                    return res;
                }

                if (req.NuevaContrasenaHash != req.ConfirmacionContrasenaHash)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 2,
                        Message = "Las contraseñas no coinciden."
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_CAMBIAR_CONTRASENA_CON_CODIGO(
                        req.CodigoRecuperacion,
                        req.NuevaContrasenaHash,
                        req.ConfirmacionContrasenaHash,
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
                                res.error.Add(new Error { ErrorCode = 30001, Message = "El código de recuperación es obligatorio." });
                                break;
                            case 30002:
                                res.error.Add(new Error { ErrorCode = 30002, Message = "La contraseña es obligatoria." });
                                break;
                            case 30003:
                                res.error.Add(new Error { ErrorCode = 30003, Message = "La confirmación de contraseña es obligatoria." });
                                break;
                            case 30004:
                                res.error.Add(new Error { ErrorCode = 30004, Message = "Las contraseñas no coinciden." });
                                break;
                            case 30005:
                                res.error.Add(new Error { ErrorCode = 30005, Message = "Código inválido o expirado." });
                                break;
                            case 30006:
                                res.error.Add(new Error { ErrorCode = 30006, Message = "El usuario está inactivo." });
                                break;
                            default:
                                res.error.Add(new Error { ErrorCode = errorID ?? 99999, Message = "Error al cambiar la contraseña." });
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
                    Message = "Error de base de datos al cambiar la contraseña."
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de cambio de contraseña."
                });
            }

            return res;
        }
    }
}

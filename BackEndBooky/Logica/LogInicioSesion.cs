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
    public class LogInicioSesion
    {
        public ResInicioSesion LoginUsuario(ReqInicioSesion req)
        {
            ResInicioSesion res = new ResInicioSesion();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuario = null;
            string rol = null;

            try
            {
                // Validación simple de campos obligatorios
                if (string.IsNullOrWhiteSpace(req.email) || string.IsNullOrWhiteSpace(req.password))
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 1,
                        Message = "El correo electrónico y la contraseña son obligatorios"
                    });
                    return res;
                }

                // Llamada al procedimiento almacenado
                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_LOGIN_USUARIO(
                        req.email,
                        req.password, // Ya viene hasheada
                        ref idUsuario,
                        ref rol,
                        ref resultadoBd,
                        ref errorID
                    );
                }

                // Evaluar respuesta
                if (resultadoBd.HasValue && resultadoBd.Value)
                {
                    res.resultado = true;

                    res.token = JwtService.GenerateToken(idUsuario.Value, rol);



                }
                else
                {
                    res.resultado = false;
                    switch (errorID)
                    {
                        case 20001:
                            res.error.Add(new Error { ErrorCode = 20001, Message = "El correo electrónico es obligatorio" });
                            break;
                        case 20002:
                            res.error.Add(new Error { ErrorCode = 20002, Message = "La contraseña es obligatoria" });
                            break;
                        case 20003:
                            res.error.Add(new Error { ErrorCode = 20003, Message = "Usuario o contraseña incorrectos" });
                            break;
                        case 10004:
                            res.error.Add(new Error { ErrorCode = 20003, Message = "Cuenta bloqueada" });
                            break;
                        default:
                            res.error.Add(new Error { ErrorCode = errorID ?? 99999, Message = "Error al iniciar sesión en la base de datos" });
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
                    Message = "Error de base de datos al iniciar sesión"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de inicio de sesión"
                });
            }

            return res;
        }

    }

}

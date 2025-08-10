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
    public class LogCierreSesion
    {
        public ResCierreSesion LogoutUsuario(ReqCierreSesion req)
        {
            ResCierreSesion res = new ResCierreSesion();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;

            try
            {
                // Validación simple de campos obligatorios
                if (req.IdSesion == Guid.Empty)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 1,
                        Message = "El Id de sesión es obligatorio"
                    });
                    return res;
                }

                // Llamada al procedimiento almacenado
                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq./*PONER AQUI NOMBRE DEL SP*/(
                        /*PONER AQUI LO QUE SE OCUPE SEGUN EL SP CREADO*/

                    );
                }

                // Evaluar respuesta
                if (resultadoBd.HasValue && resultadoBd.Value)
                {
                    res.resultado = true;
                }
                else
                {
                    /*"PERSONALIZAR LOS CASES SEGUN EL SP*/
                    res.resultado = false;
                    switch (errorID)
                    {
                        case 30001:
                            res.error.Add(new Error { ErrorCode = 30001, Message = "La sesión no existe o ya fue cerrada" });
                            break;
                        default:
                            res.error.Add(new Error { ErrorCode = errorID ?? 99999, Message = "Error al cerrar sesión en la base de datos" });
                            break;
                    }
                }
            }
            /*PERSONALIZAR ESTOS ERROR CODES TAMBIEN*/
            catch (SqlException)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50001,
                    Message = "Error de base de datos al cerrar sesión"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de cierre de sesión"
                });
            }

            return res;
        }
    }
}

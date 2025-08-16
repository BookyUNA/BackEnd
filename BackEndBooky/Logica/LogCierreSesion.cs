using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica
{
    public class LogCierreSesion
    {
        public ResCierreSesion LogoutUsuario(string token)
        {
            ResCierreSesion res = new ResCierreSesion();
            res.error = new List<Error>();

            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 1, Message = "Token es obligatorio" });
                    return res;
                }

               // JwtService.InvalidateToken(token);

                res.resultado = true;
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error { ErrorCode = 50002, Message = "Error al cerrar sesión" });
            }

            return res;
        }

    }
}

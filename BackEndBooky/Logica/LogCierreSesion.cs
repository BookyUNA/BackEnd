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

            try
            {
                if (string.IsNullOrEmpty(req.Token))
                {
                    res.resultado = false;
                    res.error.Add(new Error { ErrorCode = 1, Message = "Token es obligatorio" });
                    return res;
                }

                JwtService.InvalidateToken(req.Token);

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

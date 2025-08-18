using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica
{
    public class LogGenerarNuevoCodigo
    {
        public ResGenerarNuevoCodigo GenerarNuevoCodigo(ReqGenerarNuevoCodigo req)
        {
            ResGenerarNuevoCodigo res = new ResGenerarNuevoCodigo();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;
            string codigoRecuperacion = null;

            try
            {
                if (string.IsNullOrEmpty(req.email))
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 1,
                        Message = "El correo electrónico es obligatorio"
                    });
                    return res;
                }


                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_GENERAR_CODIGO_RECUPERACION(
                        req.email,
                        ref codigoRecuperacion,
                        ref resultadoBd,
                        ref errorID
                    );

                    // Evaluar respuesta
                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        // Envío del correo electrónico
                        try
                        {
                            if (!string.IsNullOrEmpty(codigoRecuperacion))
                            {
                                EmailService.EnviarCorreo(
                                    req.email,
                                    "Código de recuperación - Booky",
                                    $"Su código de recuperación es: {codigoRecuperacion}. Este código expirará en 5 minutos."
                                );

                                res.resultado = true;
                            }
                            else
                            {
                                res.resultado = false;
                                res.error.Add(new Error
                                {
                                    ErrorCode = 60002,
                                    Message = "No se generó el código correctamente."
                                });
                            }
                        }
                        catch (Exception emailEx)
                        {
                            res.resultado = false;
                            res.error.Add(new Error
                            {
                                ErrorCode = 60001,
                                Message = "Ocurrió un error al enviar el correo electrónico"
                            });
                        }
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
                                res.error.Add(new Error { ErrorCode = 20002, Message = "Correo electrónico no encontrado" });
                                break;
                            default:
                                res.error.Add(new Error { ErrorCode = errorID ?? 99999, Message = "Error al generar código de recuperación" });
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
                    Message = "Error de base de datos al generar código de recuperación"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de generación de código de recuperación"
                });
            }
            return res;
        }
    }
}

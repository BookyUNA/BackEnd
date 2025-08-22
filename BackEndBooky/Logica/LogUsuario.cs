using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Logica
{
    public class LogUsuario
    {
        // ================================================
        // 1. GENERAR CÓDIGO DE VERIFICACIÓN POR EMAIL
        // ================================================
        public bool GenerarCodigoVerificacion(ReqGenerarNuevoCodigo req)
        {
            ResGenerarNuevoCodigo res = new ResGenerarNuevoCodigo();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;
            string codigo = null;

            try
            {
                if (string.IsNullOrEmpty(req.email))
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 60001,
                        Message = "El correo electrónico es obligatorio"
                    });
                    return res.resultado;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                  

                  

                    // Generar código de verificación
                    linq.SP_GENERAR_CODIGO_VERIFICACION(
                        req.email,
                        ref codigo,
                        ref resultadoBd,
                        ref errorID
                    );

                    // Evaluar respuesta
                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(codigo))
                            {
                                EmailService.EnviarCorreo(
                                    req.email,
                                    "Código de verificación - Booky",
                                    $"Su código de verificación es: {codigo}. Este código expirará en 15 minutos."
                                );

                                res.resultado = true;
                            }
                            else
                            {
                                res.resultado = false;
                                res.error.Add(new Error
                                {
                                    ErrorCode = 60003,
                                    Message = "No se generó el código correctamente."
                                });
                            }
                        }
                        catch (Exception)
                        {
                            res.resultado = false;
                            res.error.Add(new Error
                            {
                                ErrorCode = 60004,
                                Message = "Ocurrió un error al enviar el correo electrónico"
                            });
                        }
                    }
                    else
                    {
                        res.resultado = false;
                        res.error.Add(new Error
                        {
                            ErrorCode = errorID ?? 99999,
                            Message = "Error al generar el código de verificación"
                        });
                    }
                }
            }
            catch (SqlException)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50001,
                    Message = "Error de base de datos al generar código de verificación"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica de generación de código de verificación"
                });
            }
            return res.resultado;
        }

        // ================================================
        // 2. REGISTRAR USUARIO CON ROL VALIDADO
        // ================================================
        public ResAgregarUsuario RegistrarUsuario(ReqAgregarUsuario req)
        {
            ResAgregarUsuario res = new ResAgregarUsuario();
            res.error = new List<Error>();
            bool? resultadoBd = true;
            int? errorID = 0;

            try
            {
                // Validaciones básicas
                if (string.IsNullOrEmpty(req.rol) ||
                    string.IsNullOrEmpty(req.cedula) ||
                    string.IsNullOrEmpty(req.nombreCompleto) ||
                    string.IsNullOrEmpty(req.email) ||
                    string.IsNullOrEmpty(req.password))
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 62002,
                        Message = "Todos los campos obligatorios deben completarse"
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    linq.SP_AGREGAR_USUARIO(
                        req.rol,
                        req.cedula,
                        req.nombreCompleto,
                        req.email,
                        req.password,
                        req.telefono,
                        ref resultadoBd,
                        ref errorID
                    );

                    if (resultadoBd.HasValue && resultadoBd.Value)
                    {
                        res.resultado = true;
                        bool email = GenerarCodigoVerificacion(new ReqGenerarNuevoCodigo
                        {
                            email = req.email
                        });
                        if (!email)
                        {
                            res.resultado = false;
                            res.error.Add(new Error
                            {
                                ErrorCode = 60005,
                                Message = "Error al enviar el código de verificación por correo electrónico"
                            });
                        }
                        else
                        {
                            res.resultado = true;
                            
                        }
                    }
                    else
                    {
                        res.resultado = false;
                        res.error.Add(new Error
                        {
                            ErrorCode = errorID ?? 99999,
                            Message = "Error al registrar usuario"
                        });
                    }
                }
            }
            catch (SqlException)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50003,
                    Message = "Error de base de datos al registrar usuario"
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50004,
                    Message = "Error en la lógica de registro de usuario"
                });
            }

            return res;
        }



    }
}

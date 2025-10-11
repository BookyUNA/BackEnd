using DAL;
using Entities.Entity;
using Entities.Request;
using Entities.Response;
using Logic;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Logica
{
    public class LogHorarioProfesional
    {
        public ResAgregarHorarioProfesional AgregarHorarios(ReqAgregarHorarioProfesional req, string token)
        {
            ResAgregarHorarioProfesional res = new ResAgregarHorarioProfesional();
            res.error = new List<Error>();
            res.HorariosFallidos = new List<string>();
            bool? resultadoBd = true;
            int? errorID = 0;
            int? idUsuarioToken = 0;

            try
            {
                // Obtener IdUsuario desde el token
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

                // Validar lista de horarios
                if (req.horarios == null || !req.horarios.Any())
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40001,
                        Message = "Debe proporcionar al menos un horario."
                    });
                    return res;
                }

                // Evitar duplicados en la misma solicitud
                var duplicados = req.horarios
                    .GroupBy(h => new { h.FechaDiaSemana, h.HoraInicio, h.HoraFin })
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    foreach (var horario in req.horarios)
                    {
                        resultadoBd = true;
                        errorID = 0;

                        // Validar fecha no pasada
                        if (horario.FechaDiaSemana < DateTime.Today)
                        {
                            res.HorariosFallidos.Add($"Horario en fecha {horario.FechaDiaSemana:yyyy-MM-dd} no se pudo agregar (no se permiten fechas pasadas).");
                            continue;
                        }

                        // Validar duplicado
                        bool esDuplicado = duplicados.Any(d =>
                            d.FechaDiaSemana == horario.FechaDiaSemana &&
                            d.HoraInicio == horario.HoraInicio &&
                            d.HoraFin == horario.HoraFin);

                        if (esDuplicado)
                        {
                            res.HorariosFallidos.Add($"Horario duplicado en fecha {horario.FechaDiaSemana:yyyy-MM-dd} con misma hora de inicio y fin.");
                            continue;
                        }

                        try
                        {
                            // Si no se envía estado, asignar "Activa" por defecto
                            if (string.IsNullOrWhiteSpace(horario.Estado))
                                horario.Estado = "Activa";

                            // Llamada al procedimiento almacenado
                            linq.SP_AGREGAR_HORARIO_PROFESIONAL(
                                idUsuarioToken,
                                horario.HoraInicio,
                                horario.HoraFin,
                                horario.FechaDiaSemana,
                                horario.Estado,
                                ref resultadoBd,
                                ref errorID
                            );

                            // Si falla, registrar la fecha como no agregada
                            if (!resultadoBd.HasValue || !resultadoBd.Value)
                            {
                                string mensajeError = $"Horario en fecha {horario.FechaDiaSemana:yyyy-MM-dd} no se pudo agregar.";
                                res.HorariosFallidos.Add(mensajeError);
                            }
                        }
                        catch (Exception)
                        {
                            string mensajeError = $"Horario en fecha {horario.FechaDiaSemana:yyyy-MM-dd} no se pudo agregar.";
                            res.HorariosFallidos.Add(mensajeError);
                        }
                    }
                }

                // Evaluar resultado final
                if (res.HorariosFallidos.Count == 0)
                {
                    res.resultado = true;
                }
                else
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40002,
                        Message = "Algunos horarios no se pudieron agregar."
                    });
                }
            }
            catch (SqlException)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50001,
                    Message = "Error de conexión a la base de datos."
                });
            }
            catch (Exception)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica al agregar los horarios."
                });
            }

            return res;
        }

        public ResCrearEvento CrearEvento(ReqCrearEvento req, string token)
        {
            ResCrearEvento res = new ResCrearEvento();
            res.error = new List<Error>();
            bool? resultadoBd = false;
            int? errorID = 0;
            int? idUsuarioToken = 0;
            int? idEventoCreado = 0;
            int? horariosAfectados = 0;

            try
            {
                // Obtener IdUsuario desde el token
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

                // Validar parámetros requeridos
                if (string.IsNullOrWhiteSpace(req.NombreEvento))
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40001,
                        Message = "El nombre del evento es requerido."
                    });
                    return res;
                }

                if (!req.FechaHoraInicio.HasValue || !req.FechaHoraFin.HasValue)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40001,
                        Message = "Las fechas de inicio y fin son requeridas."
                    });
                    return res;
                }

                // Validar que fecha fin sea mayor a fecha inicio
                if (req.FechaHoraFin <= req.FechaHoraInicio)
                {
                    res.resultado = false;
                    res.error.Add(new Error
                    {
                        ErrorCode = 40002,
                        Message = "La fecha de fin debe ser mayor a la fecha de inicio."
                    });
                    return res;
                }

                using (DataClasses1DataContext linq = new DataClasses1DataContext())
                {
                    try
                    {
                        // Llamada al procedimiento almacenado
                        linq.SP_CREAR_EVENTO_PROFESIONAL(
                            idUsuarioToken,
                            req.NombreEvento,
                            req.Descripcion,
                            req.FechaHoraInicio,
                            req.FechaHoraFin,
                            ref idEventoCreado,
                            ref horariosAfectados,
                            ref resultadoBd,
                            ref errorID
                        );

                        // Verificar resultado
                        if (resultadoBd.HasValue && resultadoBd.Value)
                        {
                            res.resultado = true;
                            res.IdEvento = idEventoCreado.HasValue ? idEventoCreado.Value : 0;
                            res.HorariosDesactivados = horariosAfectados.HasValue ? horariosAfectados.Value : 0;
                        }
                        else
                        {

                            res.resultado = false;

                            // Manejar códigos de error del SP
                            switch (errorID)
                            {
                                case 40001:
                                    res.error.Add(new Error
                                    {
                                        ErrorCode = 40001,
                                        Message = "Parámetros incompletos."
                                    });
                                    break;
                                case 40002:
                                    res.error.Add(new Error
                                    {
                                        ErrorCode = 40002,
                                        Message = "La fecha de fin debe ser mayor a la fecha de inicio."
                                    });
                                    break;
                                case 40003:
                                    res.error.Add(new Error
                                    {
                                        ErrorCode = 40003,
                                        Message = "Perfil profesional no encontrado."
                                    });
                                    break;
                                case 40004:
                                    res.error.Add(new Error
                                    {
                                        ErrorCode = 40004,
                                        Message = "No se puede crear el evento porque existen citas confirmadas en ese rango de fechas."
                                    });
                                    break;
                                default:
                                    res.error.Add(new Error
                                    {
                                        ErrorCode = errorID ?? 50000,
                                        Message = "Error al crear el evento."
                                    });
                                    break;
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        res.resultado = false;
                        res.error.Add(new Error
                        {
                            ErrorCode = 50001,
                            Message = "Error de conexión a la base de datos."
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                res.resultado = false;
                res.error.Add(new Error
                {
                    ErrorCode = 50002,
                    Message = "Error en la lógica al crear el evento."
                });
            }

            return res;
        }
    }
}
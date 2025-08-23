
using Entities.Request;
using Entities.Response;
using Logic;
using Logica;
using Microsoft.AspNetCore.Authorization;
using System.Web.Http;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace APIs.Controllers
{
    public class UsuarioController : ApiController
    {

        [Authorize]

        [HttpGet]
        [Route("api/debug-token")]
        
        public IHttpActionResult DebugToken()
        {
            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || authHeader.Scheme != "Bearer")
                return BadRequest("Token no proporcionado o esquema incorrecto");

            var token = authHeader.Parameter;

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            return Ok(new
            {
                TokenValido = true,
                Issuer = jwtToken.Issuer,
                Audience = jwtToken.Audiences.FirstOrDefault() ?? "null",
                Expiration = jwtToken.ValidTo,
                Claims = jwtToken.Claims.Select(c => new { c.Type, c.Value }),
                Algorithm = jwtToken.Header.Alg
            });
        }

        [Authorize]
        [HttpPost] 
        [Route("api/cerrarsesion")]
        public ResCierreSesion CerrarSesion()
        {
            try
            {
                var authHeader = Request.Headers.Authorization;

                if (authHeader == null || authHeader.Scheme != "Bearer" || string.IsNullOrEmpty(authHeader.Parameter))
                {
                    return new ResCierreSesion
                    {
                        resultado = false,
                        error = new System.Collections.Generic.List<Entities.Entity.Error>
                {
                    new Entities.Entity.Error { ErrorCode = 1, Message = "Token no proporcionado o esquema incorrecto" }
                }
                    };
                }

                // Agregar token a blacklist
                Logic.JwtService.BlacklistToken(authHeader.Parameter);

                // Procesar logout
                return new LogCierreSesion().LogoutUsuario(authHeader.Parameter);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CerrarSesion: {ex.Message}");
                return new ResCierreSesion
                {
                    resultado = false,
                    error = new System.Collections.Generic.List<Entities.Entity.Error>
            {
                new Entities.Entity.Error { ErrorCode = 500, Message = "Error interno del servidor" }
            }
                };
            }
        }

        [HttpGet]
        [Route("api/debug-generated-token")]
        public IHttpActionResult DebugGeneratedToken()
        {
            try
            {
                // Generar token de prueba usando JwtService
                string token = JwtService.GenerateToken(1, "Profesional");

                // Leer el token
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Devolver toda la info para revisar
                var claims = jwtToken.Claims.Select(c => new {
                    Type = c.Type,
                    Value = c.Value
                }).ToList();

                return Ok(new
                {
                    Token = token,
                    Issuer = jwtToken.Issuer,
                    Audience = jwtToken.Audiences.FirstOrDefault() ?? "null",
                    Expiration = jwtToken.ValidTo,
                    Claims = claims,
                    Algorithm = jwtToken.Header.Alg
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generando token: {ex.Message}");
            }
        }

    }

}
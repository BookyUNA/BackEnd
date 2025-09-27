using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public int IdRol { get; set; }
        public string Cedula { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Telefono { get; set; }
        public bool EmailVerificado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Estado { get; set; }
        public bool Bloqueado { get; set; }
        public int IntentosLoginFallidos { get; set; }
        public DateTime? FechaUltimoIntentoFallido { get; set; }
    }
}

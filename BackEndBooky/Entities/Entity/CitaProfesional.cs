using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
    public class CitaProfesional
    {
        public DateTime FechaCita { get; set; }
        public int? DuracionMinutos { get; set; }
        public double PrecioAcordado { get; set; }
        public string Estado { get; set; }
        public string MensajeSolicitud { get; set; }
        public string MotivoRechazo { get; set; }
        public string MotivoCancelacion { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime? FechaRespuesta { get; set; }

        // Cliente
        public string CedulaUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public string EmailUsuario { get; set; }
        public string TelefonoUsuario { get; set; }

        // Profesional
        public string Profesion { get; set; }
        public string DescripcionPerfil { get; set; }
        public string Direccion { get; set; }
        public string NombreProfesional { get; set; }
        public string EmailProfesional { get; set; }
        public string TelefonoProfesional { get; set; }
        public string NombreServicio { get; set; }
    }
}

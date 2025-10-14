using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
    public class CitaCliente
    {
        public int IdCita { get; set; }
        public DateTime FechaCita { get; set; }
        public int? DuracionMinutos { get; set; }
        public double PrecioAcordado { get; set; }
        public string Estado { get; set; }
        public string MensajeSolicitud { get; set; }
        public string MotivoRechazo { get; set; }
        public string MotivoCancelacion { get; set; }
        public DateTime? FechaSolicitud { get; set; }
        public DateTime? FechaRespuesta { get; set; }

        // Información del cliente
        public string CedulaUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public string EmailUsuario { get; set; }
        public string TelefonoUsuario { get; set; }

        // Información del profesional
        public string Profesion { get; set; }
        public int IdProfesional { get; set; }
        public string DescripcionPerfil { get; set; }
        public decimal? CalificacionPromedio { get; set; } 
        public string EstadoCalificacion { get; set; }
        public string Direccion { get; set; }
        public string NombreProfesional { get; set; }
        public string EmailProfesional { get; set; }
        public string TelefonoProfesional { get; set; }
        public string NombreServicio { get; set; }
    }
}

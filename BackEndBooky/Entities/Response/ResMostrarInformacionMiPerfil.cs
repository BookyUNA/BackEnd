using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResMostrarInformacionMiPerfil : ResBase
    {
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Cedula { get; set; }
        public string Telefono { get; set; }
        public string Profesion { get; set; }  
        public string Descripcon { get; set; }
        public string Direccion { get; set; }  
        public decimal? CalificacionPromedio { get; set; }
        public int? TotalCalificaciones { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
    public class Servicio
    {

        public int IdServicio { get; set; }
        public int IdProfesional { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int? DuracionMinutos { get; set; }
        public decimal Precio { get; set; }
        public bool PermiteDescuento { get; set; }
        public decimal? PorcentajeDescuento { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Estado { get; set; }
    }
}

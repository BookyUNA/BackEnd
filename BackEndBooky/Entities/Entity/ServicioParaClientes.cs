using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
    public class ServicioParaClientes
    {
       public int idServicio { get; set; } 
        public string nombreServicio { get; set; }
        public string descripcion { get; set; }
        public int duracionMinutos { get; set; }
        public double precio { get; set; }
        public bool permiteDescuento { get; set; }
        public double porcentajeDescuento { get; set; }
        public DateTime fechaCreacion { get; set; }
        public string nombreProfesional { get; set; }
        public string profesion { get; set; }
       
    }
}

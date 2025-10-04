using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqActualizarPerfilProfesional
    {
       
        public string profesion { get; set; }
        public string descripcion { get; set; }
        public string direccion { get; set; }
        public decimal? latitud { get; set; }
        public decimal? longitud { get; set; }
        public bool? estado { get; set; }
    }
}

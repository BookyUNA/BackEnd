using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqCalificarProfesional
    {
        public int IdCita { get; set; }
        public decimal Calificacion { get; set; }
    }
}

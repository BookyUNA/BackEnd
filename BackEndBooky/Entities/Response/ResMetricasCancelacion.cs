using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public  class ResMetricasCancelacion
    {
        public bool resultado { get; set; }
        public List<Error> error { get; set; }

        public string Mensaje { get; set; }

        public int TotalCitas { get; set; }
        public int CitasCanceladas { get; set; }
        public int CitasCompletadas { get; set; }
        public int CitasRechazadas { get; set; }
        public decimal PorcentajeCancelacion { get; set; }
        public string CategoriaRiesgo { get; set; }
        public DateTime FechaCalculo { get; set; }
    }
}

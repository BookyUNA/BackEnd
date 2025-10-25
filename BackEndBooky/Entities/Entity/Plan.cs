using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
    public class Plan
    {
        public int IdPlan { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioMensual { get; set; }
        public decimal? PrecioAnual { get; set; }
        public int MaxServicios { get; set; }
        public int MaxClientes { get; set; }
        public int MaxListaEspera { get; set; }
        public int MaxPoliticaCancelacion { get; set; }
        public bool IncluyeEstadisticas { get; set; }
        public bool IncluyeAnuncios { get; set; }
        public bool Estado { get; set; }
    }
}

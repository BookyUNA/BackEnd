using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResListarCitasCliente : ResBase
    {
        public List<CitaCliente> Citas { get; set; }
    }
}

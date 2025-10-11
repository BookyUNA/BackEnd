using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResCrearEvento : ResBase
    {
        public int IdEvento { get; set; }
        public int HorariosDesactivados { get; set; }
    }
}

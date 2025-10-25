using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
   public class ResVerificarLimite : ResBase
    {
        public bool permitido { get; set; }
        public int cantidadActual { get; set; }
        public int limiteMaximo { get; set; }
        public int disponibleRestante { get; set; }

    }
}

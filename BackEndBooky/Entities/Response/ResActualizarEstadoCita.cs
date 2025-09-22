using Entities.Entity;
using System.Collections.Generic;

namespace Entities.Response
{
    public class ResActualizarEstadoCita
    {
        public bool resultado { get; set; }
        public List<Error> error { get; set; }
    }
}

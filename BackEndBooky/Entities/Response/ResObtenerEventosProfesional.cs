using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResObtenerEventosProfesional : ResBase
    {
        public List<Evento> Eventos { get; set; }
    }
}

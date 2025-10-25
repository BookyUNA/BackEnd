using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResListarPlanes : ResBase
    {
        public List<Plan> planes { get; set; }
    }
}

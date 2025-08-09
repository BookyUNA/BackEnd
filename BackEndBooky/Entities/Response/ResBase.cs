using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResBase
    {
        public List<Error> error { get; set; }

        public bool resultado { get; set; }
    }
}

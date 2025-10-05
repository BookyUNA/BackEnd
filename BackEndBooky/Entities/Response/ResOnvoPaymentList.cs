using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{
    public class ResOnvoPaymentList 
        : ResBase
    {
     
        public List<ResOnvo> pagos { get; set; }
    }
}

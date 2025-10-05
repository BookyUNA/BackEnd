using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{

    public class ResOnvoCustomerPayment : ResBase
    {
        public bool resultado { get; set; }
        public string mensaje { get; set; }
        public string customerId { get; set; }
        public string email { get; set; }
        public string name { get; set; }
        public List<Error> error { get; set; }
    }
}

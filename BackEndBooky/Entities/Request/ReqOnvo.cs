using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Request
{
    public class ReqOnvo
    {
        public decimal amount { get; set; }
        public string currency { get; set; } = "USD"; // moneda por defecto
        public string description { get; set; }
       

    }
}

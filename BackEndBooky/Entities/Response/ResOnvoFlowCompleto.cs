using Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Response
{

    public class ResOnvoFlowCompleto : ResBase
    {
      
        public string mensaje { get; set; }
     

        // Datos del Cliente creado
        public string customerId { get; set; }

        // Datos del Método de Pago creado
        public string paymentMethodId { get; set; }
        public string last4 { get; set; }
        public string brand { get; set; }

        // Datos del Pago registrado
        public int idPagoOnvo { get; set; }
        public string estadoPago { get; set; }
        public string mensajeEstadoPago { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public DateTime fechaCreacion { get; set; }


    }

}

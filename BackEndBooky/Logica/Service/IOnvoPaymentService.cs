using Entities.Entity;
using Entities.Request;
using Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Service
{
    public interface IOnvoPaymentService
    {    // ===== Gestión de Clientes =====
        Task<ResOnvoCustomer> CreateCustomerAsync(ReqOnvoCustomer request);
        Task<ResOnvoCustomer> GetCustomerAsync(string customerId);

        // ===== Gestión de Métodos de Pago (Tarjetas) =====
        Task<ResOnvoPaymentMethod> CreatePaymentMethodAsync(ReqOnvoPaymentMethod request);

        // ===== Generación de URLs de Pago =====
        Task<ResOnvoPaymentLink> CreatePaymentLinkAsync(ReqOnvoPaymentLink request);
        Task<ResOnvoPaymentLink> GetPaymentLinkAsync(string linkId);

        // ===== Payment Intents (Original) =====
        ResOnvo CreatePaymentSync(ReqOnvoPayment request);
        Task<ResOnvo> CreatePaymentAsync(ReqOnvoPayment request);
        Task<ResOnvo> GetPaymentAsync(string paymentId);

        Task<List<ResOnvo>> GetPaymentsByCustomerAsync(string customerId);
        // ===== Webhooks =====
        Task<bool> VerifyWebhookSignatureAsync(string payload, string signature);
        OnvoWebhookEvent ParseWebhookEvent(string payload);
    }
}

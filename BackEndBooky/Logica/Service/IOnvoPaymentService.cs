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
    {
        Task<ResOnvo> CreatePaymentAsync(ReqOnvoPayment request);
        Task<ResOnvo> GetPaymentAsync(string paymentId);
       
        Task<bool> VerifyWebhookSignatureAsync(string payload, string signature);
        OnvoWebhookEvent ParseWebhookEvent(string payload);
    }
}

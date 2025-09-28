using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
    public class OnvoApiException : Exception
    {
        public int StatusCode { get; }
        public string ErrorCode { get; }

        public OnvoApiException(int statusCode, string errorCode, string message) : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }
}

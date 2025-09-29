using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Service
{
    public class NullLoger : ILogger
    {
        public void LogInformation(string message) { }
        public void LogError(string message) { }
        public void LogWarning(string message) { }
    }
}

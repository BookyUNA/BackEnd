using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Entity
{
    public class OnvoConfiguration
    {
        public string SecretKey { get; set; } = "onvo_test_secret_key_2d6dYlKDwPn_sjIuu3ucgZ1o6ncCBm2kicZD-B03k9bwY3IySLRI3iMcLIqKFVXACBSyJzvA9uXcUTQ6XUpIyg";
        public string PublishableKey { get; set; } = "onvo_test_publishable_key_Da68cgdF_k3KuBkpAisFVXx0NM0OYByNnBecScMTcoFiQt869tm0IFFwL1YgvPUg3r7dE2fCe32f4vwAWqoA0Q";
        public string BaseUrl { get; set; } = "https://api.onvopay.com/v1/";
        public bool IsTestMode { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
    }
}

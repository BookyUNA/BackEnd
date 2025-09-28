using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri("https://api.onvopay.com/v1/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "onvo_test_secret_key_2d6dYlKDwPn_sjIuu3ucgZ1o6ncCBm2kicZD-B03k9bwY3IySLRI3iMcLIqKFVXACBSyJzvA9uXcUTQ6XUpIyg");
       
        var body = new
        {
            amount = 200, // en centavos (2.00 USD)
            currency = "USD",
            description = "Prueba .NET",
            paymentMethodId= "cmg1o80e3v1fok62egwdibfzd"


        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // 👇 endpoint correcto para crear un intent de pago
        var response = await http.PostAsync("payment-intents", content);

        var respText = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine("Respuesta:");
        Console.WriteLine(respText);
    }
}

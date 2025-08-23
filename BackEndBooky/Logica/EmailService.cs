using System;
using System.Net;
using System.Net.Mail;
namespace Logic.Helpers
{
    public static class EmailService
    {
        public static bool EnviarCorreo(string destinatario, string asunto, string cuerpo)
        {
            try
            {
                SmtpClient client = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("sistemabooky@gmail.com", "aqgq qygz emiq iiry"),
                    EnableSsl = true,// prueba commit
                    Timeout = 30000 // Aumentar el tiempo de espera a 30 segundos
                };
                MailMessage mail = new MailMessage("sistemabooky@gmail.com", destinatario, asunto, cuerpo)
                {
                    IsBodyHtml = true
                };
                client.Send(mail);
                return true;
            }
            catch (Exception ex)
            {
                // Mejorar el registro de errores
                Console.WriteLine($"Error al enviar correo: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }
    }
}
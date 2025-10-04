using System.ComponentModel.DataAnnotations;

namespace Entities.Request
{
    public class ReqActualizarEstadoCita
    {
        [Required(ErrorMessage = "El Id de la cita es obligatorio")]
        public int IdCita { get; set; }

        [Required(ErrorMessage = "El estado de aprobación es obligatorio")]
        public bool Aprobada { get; set; }

      
        public string MotivoRechazo { get; set; }
    }
}

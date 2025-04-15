using System.ComponentModel.DataAnnotations;

namespace ComicCollector.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome utente è obbligatorio.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "La password è obbligatoria.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "L'email è obbligatoria.")]
        [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido.")]
        public string Email { get; set; }

        public string Role { get; set; }
    }
}
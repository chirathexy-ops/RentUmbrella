using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmbrellaRentalSystem.Models
{
    [Table("Accounts")]
    public class Account
    {
        [Key]
        public int AccountId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? EasyCard { get; set; } = string.Empty;

        public string Role { get; set; } = "User";
    }
}
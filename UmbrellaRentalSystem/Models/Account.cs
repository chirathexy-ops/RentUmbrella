using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmbrellaRentalSystem.Models
{
    [Table("Accounts")]
    public class Account
    {
        [Key]
        public int Account_ID { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // 給預設值

        [Required]
        public string Password { get; set; } = string.Empty; // 給預設值

        public string? Email { get; set; } // Email 可能是選填，所以加問號
        public string? Phone { get; set; }
        public string? EasyCard { get; set; } = string.Empty; // 給預設值

        public string Role { get; set; } = "User"; // 給預設值為一般使用者
    }
}
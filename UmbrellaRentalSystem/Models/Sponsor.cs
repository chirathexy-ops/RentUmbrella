using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmbrellaRentalSystem.Models
{
    [Table("Sponsors")] // 確保連結到資料庫的複數表
    public class Sponsor
    {
        [Key]
        public int SponsorId { get; set; } // 注意：必須是 int，且名稱要加底線

        [Required]
        public string SponsorName { get; set; } = string.Empty; // 解決 image_720787 的 CS0117 錯誤
    }
}
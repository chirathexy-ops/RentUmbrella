using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmbrellaRentalSystem.Models
{
    public class Umbrella
    {
        [Key]
        [Display(Name = "雨傘編號")]
        public required string UmbrellaId { get; set; } // 存放 "U001" 等格式

        [Required]
        [Display(Name = "雨傘狀態")]
        public string Status { get; set; } = "在庫";

        // --- 地點關聯 ---
        [Display(Name = "地點編號")]
        public int LocationId{ get; set; }

        [ForeignKey("LocationId")]
        public virtual Location? Location { get; set; }

        // --- 贊助商關聯 ---
        [Display(Name = "贊助商編號")]
        public int SponsorId { get; set; }

        [ForeignKey("SponsorId")]
        public virtual Sponsor? Sponsor { get; set; }
    }
}
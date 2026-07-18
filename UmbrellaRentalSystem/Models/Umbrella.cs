using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmbrellaRentalSystem.Models
{
    public class Umbrella
    {
        [Key]
        [Display(Name = "資料庫編號")]
        public int UmbrellaId { get; set; }

        [Required]
        [Display(Name = "雨傘編號")]
        public string UmbrellaCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "雨傘狀態")]
        public string Status { get; set; } = "Available";

        [Display(Name = "地點編號")]
        public int LocationId { get; set; }

        [ForeignKey("LocationId")]
        public virtual Location? Location { get; set; }

        [Display(Name = "贊助商編號")]
        public int SponsorId { get; set; }

        [ForeignKey("SponsorId")]
        public virtual Sponsor? Sponsor { get; set; }
    }
}
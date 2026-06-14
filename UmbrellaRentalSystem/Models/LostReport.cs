using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmbrellaRentalSystem.Models
{
    [Table("LostReports")]
    public class LostReport
    {
        [Key]
        public int Report_ID { get; set; }

        // 外鍵：對應租借紀錄
        public int Transaction_ID { get; set; }

        public DateTime Report_Date { get; set; } = DateTime.Now;

        public string? Description { get; set; }

        public bool IsProcessed { get; set; } = false; // 預設未處理
    }
}
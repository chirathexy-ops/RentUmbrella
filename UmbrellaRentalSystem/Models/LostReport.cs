using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmbrellaRentalSystem.Models
{
    [Table("LostReports")]
    public class LostReport
    {
        [Key]
        public int ReportId { get; set; }

        // 外鍵：對應租借紀錄
        public int TransactionId { get; set; }

        public DateTime ReportDate { get; set; } = DateTime.Now;

        public string? Description { get; set; }

        public bool IsProcessed { get; set; } = false; // 預設未處理

        // ✨ 建議補上這個：讓系統可以直接透過遺失紀錄關聯到該次交易
        [ForeignKey("TransactionId")]
        public virtual Transaction? Transaction { get; set; }
    }
}
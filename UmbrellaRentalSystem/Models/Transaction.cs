using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UmbrellaRentalSystem.Models
{
    [Table("Transactions")]
    public class Transaction
    {
        [Key]
        public int Transaction_ID { get; set; }

        // 外鍵：對應使用者
        public int Account_ID { get; set; }

        // 外鍵：對應雨傘 (NVARCHAR)
        public string Umbrella_ID { get; set; } = string.Empty;

        public DateTime LendDate { get; set; } = DateTime.Now;

        public DateTime? ReturnDate { get; set; } // 可為 Null，代表尚未歸還

        // 外鍵：對應借出地點
        public int LendLocation_ID { get; set; }

        // 外鍵：對應歸還地點 (可為 Null)
        public int? ReturnLocation_ID { get; set; }

        public string Status { get; set; } = "Active"; // Active, Completed, Lost
    }
}
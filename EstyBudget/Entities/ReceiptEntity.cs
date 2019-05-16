using System.Collections.Generic;

namespace EstyBudget.Entities
{
    public class ReceiptEntity
    {
        public long receipt_id { get; set; }
        public double creation_tsz { get; set; }
        public string payment_method { get; set; }
        public float? adjusted_grandtotal { get; set; }
        public List<TransactionEntity> Transactions { get; set; }
    }
}

namespace EstyBudget.Entities
{
    public class PaymentEntity
    {
        public long payment_id { get; set; }
        public long receipt_id { get; set; }
        public float? amount_gross { get; set; }
        public float? amount_fees { get; set; }
        public float? adjusted_gross { get; set; }
        public float? adjusted_fees { get; set; }
    }
}

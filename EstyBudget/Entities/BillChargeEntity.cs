namespace EstyBudget.Entities
{
    public class BillChargeEntity
    {
        public long bill_charge_id { get; set; }
        public string type { get; set; }
        public long type_id { get; set; }
        public float? amount { get; set; }
    }
}

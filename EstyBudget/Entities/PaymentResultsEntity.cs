using System.Collections.Generic;

namespace EstyBudget.Entities
{
    public class PaymentResultsEntity
    {
        public int count { get; set; }
        public List<PaymentEntity> results { get; set; }
    }
}

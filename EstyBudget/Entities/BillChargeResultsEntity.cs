using System.Collections.Generic;

namespace EstyBudget.Entities
{
    public class BillChargeResultsEntity
    {
        public int count { get; set; }
        public List<BillChargeEntity> results { get; set; }
    }
}

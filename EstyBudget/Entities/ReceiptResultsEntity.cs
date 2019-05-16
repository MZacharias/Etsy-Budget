using System.Collections.Generic;

namespace EstyBudget.Entities
{
    public class ReceiptResultsEntity
    {
        public int count { get; set; }
        public List<ReceiptEntity> results { get; set; }
    }
}

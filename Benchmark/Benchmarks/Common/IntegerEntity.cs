using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Orleans.Benchmarks.Common
{
    [Serializable]
    public class IntegerEntity : TableEntity
    {
        public int value { get; set; }

        public IntegerEntity()
        {
            this.value = 0;
        }
        public IntegerEntity(int value, string partitionKey, string rowKey)
        {
            this.value = value;
            this.RowKey = rowKey;
            this.PartitionKey = partitionKey;
        }
    }
}

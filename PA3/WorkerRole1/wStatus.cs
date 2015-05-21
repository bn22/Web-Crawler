using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class wStatus : TableEntity
    {
        private String workerStatus;
      
        public wStatus(String firstStatus)
        {
            this.PartitionKey = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.RowKey = firstStatus;
            this.workerStatus = firstStatus;
        }

        public wStatus() { }

        public String getStatus { get; set; }
    }
}

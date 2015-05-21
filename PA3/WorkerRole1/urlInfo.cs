using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class urlInfo : TableEntity
    {
        private String url;
        private String title;
        private String memoryAvailable;
        private String cpuUsage;
        private String lastModifiedDate;
        private String checksPerformed;
        private String checksPassed;

        public urlInfo(String url, String title)
        {
            this.PartitionKey = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.RowKey = Uri.EscapeDataString(url);
            this.title = title;
            this.url = url;
            this.memoryAvailable = null;
            this.cpuUsage = null;
            this.lastModifiedDate = DateTime.Now.ToString();
        }

        public urlInfo() { }

        public String getUrl { get; set; }
        public String getTitle { get; set; }
        public String getMemoryAvailable { get; set; }
        public String getCpuUsage { get; set; }
        public String getlastModifiedDate { get; set; }
        public String getChecksPerformed { get; set; }
        public String getChecksPassed { get; set; }

    }
}

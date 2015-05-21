using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class errorMessage : TableEntity
    {
        private String url;
        private String errorMessageContent;

        public errorMessage(String url, String error)
        {
            this.PartitionKey = Uri.EscapeDataString(url);
            this.RowKey = Uri.EscapeDataString(error);
            this.errorMessageContent = error;
            this.url = url;
        }

        public errorMessage() { }

        public String getUrl { get; set; }
        public String getErrorMessageContent { get; set; }
    }
}

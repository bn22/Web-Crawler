using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
              ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue urlQueue = queueClient.GetQueueReference("myurls");
        private static CloudQueue commandQueue = queueClient.GetQueueReference("command");
        private static CloudStorageAccount storageAccount2 = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static CloudTableClient tableClient = storageAccount2.CreateCloudTableClient();
        private static CloudTable table = tableClient.GetTableReference("crawledtable");
        private static CloudTable error = tableClient.GetTableReference("errors");
        private static CloudTable workerTable = tableClient.GetTableReference("worker");

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void beginCrawler(String rootUrl)
        {
            workerTable.CreateIfNotExists();
            urlQueue.CreateIfNotExists();
            commandQueue.CreateIfNotExists();
            String robotText = "http://" + rootUrl + "/robots.txt";
            CloudQueueMessage message1 = new CloudQueueMessage("Start");
            commandQueue.AddMessage(message1);
            CloudQueueMessage message2 = new CloudQueueMessage(robotText);
            urlQueue.AddMessage(message2);
            if (robotText.Contains("http://www.cnn.com/robots.txt"))
            {
                CloudQueueMessage message3 = new CloudQueueMessage("http://bleacherreport.com/robots.txt");
                urlQueue.AddMessage(message3);
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void endCrawler()
        {
            commandQueue.Clear();
            CloudQueueMessage message3 = new CloudQueueMessage("End");
            commandQueue.AddMessage(message3);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public void clearCrawler()
        {
            commandQueue.Clear();
            urlQueue.Clear();
            table.DeleteIfExists();
            error.DeleteIfExists();
            workerTable.DeleteIfExists();
            System.Threading.Thread.Sleep(2000);       
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public String readTable(String urlInput)
        {
            List<String> results = new List<String>();
            urlInput = Uri.EscapeDataString(urlInput);
            TableQuery<urlInfo> findTitleName = new TableQuery<urlInfo>()
                .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, urlInput));
            foreach (urlInfo entity in table.ExecuteQuery(findTitleName))
            {
                results.Add(entity.getTitle.ToString());
            }
            return new JavaScriptSerializer().Serialize(results);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public String cpu()
        {
            List<String> results = new List<string>();
            TableQuery<urlInfo> lastestQuery = new TableQuery<urlInfo>().Take(1);
            foreach (urlInfo entity in table.ExecuteQuery(lastestQuery))
            {
                results.Add(entity.getCpuUsage.ToString());
            }
            return new JavaScriptSerializer().Serialize(results);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public String memUsage()
        {
            List<String> results = new List<string>();
            TableQuery<urlInfo> lastestQuery = new TableQuery<urlInfo>().Take(1);
            foreach (urlInfo entity in table.ExecuteQuery(lastestQuery))
            {
                results.Add(entity.getMemoryAvailable.ToString());
            }
            return new JavaScriptSerializer().Serialize(results);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public String checksPerformed()
        {
            List<String> results = new List<string>();
            TableQuery<urlInfo> lastestQuery = new TableQuery<urlInfo>().Take(1);
            foreach (urlInfo entity in table.ExecuteQuery(lastestQuery))
            {
                results.Add(entity.getChecksPerformed.ToString());
            }
            return new JavaScriptSerializer().Serialize(results);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public String checksPassed()
        {
            List<String> results = new List<string>();
            TableQuery<urlInfo> lastestQuery = new TableQuery<urlInfo>().Take(1);
            foreach (urlInfo entity in table.ExecuteQuery(lastestQuery))
            {
                results.Add(entity.getChecksPassed.ToString());
            }
            return new JavaScriptSerializer().Serialize(results);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public String lastTenUrl()
        {
            List<String> results = new List<string>();
            TableQuery<urlInfo> lastestQuery = new TableQuery<urlInfo>().Take(10);
            foreach (urlInfo entity in table.ExecuteQuery(lastestQuery))
            {
                results.Add(entity.getUrl.ToString());
            }
            return new JavaScriptSerializer().Serialize(results);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public String queueCount()
        {
            urlQueue.FetchAttributes();
            int? numberInQueue = urlQueue.ApproximateMessageCount;
            String results = numberInQueue.ToString();
            return new JavaScriptSerializer().Serialize(results);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public String workerState()
        {
            List<String> result = new List<String>();
            TableQuery<wStatus> newQuery = new TableQuery<wStatus>().Take(1);
            foreach (wStatus entity in workerTable.ExecuteQuery(newQuery))
            {
                result.Add(entity.getStatus.ToString());
            }
            return new JavaScriptSerializer().Serialize(result);
        }
    }
}

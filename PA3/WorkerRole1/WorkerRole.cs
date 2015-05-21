using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.IO;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private static vistedUrlCheck checker = new vistedUrlCheck();
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
        private static CloudQueue urlQueue = queueClient.GetQueueReference("myurls");
        private static CloudQueue commandQueue = queueClient.GetQueueReference("command");
        private static CloudStorageAccount storageAccount2 = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static CloudTableClient tableClient = storageAccount2.CreateCloudTableClient();
        private static CloudTable table = tableClient.GetTableReference("crawledtable");
        private static CloudTable errorTable = tableClient.GetTableReference("errors");
        private static CloudTable workerTable = tableClient.GetTableReference("worker");
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");
            while (true)
            {
                try
                {
                    wStatus newState = new wStatus("Idle");
                    newState.getStatus = "Idle";
                    TableOperation insertOperation = TableOperation.Insert(newState);
                    workerTable.Execute(insertOperation);
                    try
                    {
                        CloudQueueMessage message = commandQueue.GetMessage();
                        String dashBoardCommand = message.AsString;
                        checker.getCommand = dashBoardCommand;
                        while (checker.getCommand == "Start")
                        {

                            CloudQueueMessage message1 = urlQueue.GetMessage();
                            String url = message1.AsString;
                            if (url.Contains("http://bleacherreport.com/robots.txt"))
                            {
                                wStatus newState1 = new wStatus("Loading CNN");
                                newState1.getStatus = "Loading CNN";
                                TableOperation insertOperation1 = TableOperation.Insert(newState);
                                workerTable.Execute(insertOperation1);
                                readXML("http://bleacherreport.com/sitemap/nba.xml");
                            }
                            else if (url.Contains("/robots.txt"))
                            {
                                wStatus newState2 = new wStatus("Loading BleacherReport");
                                newState2.getStatus = "Loading BleacherReport";
                                TableOperation insertOperation2 = TableOperation.Insert(newState);
                                workerTable.Execute(insertOperation2);
                                WebClient client = new WebClient();
                                Stream readWebRequest = client.OpenRead(url);
                                using (StreamReader sr = new StreamReader(readWebRequest))
                                {
                                    try
                                    {
                                        String line;
                                        while ((line = sr.ReadLine()) != null)
                                        {
                                            if (line.Contains("Disallow: "))
                                            {
                                                String[] disallowedUrl = line.Split(' ');
                                                checker.adddisallowedUrl(disallowedUrl[1] + "/");
                                            }
                                            else if (line.Contains("Sitemap: "))
                                            {
                                                String[] sitemapUrl = line.Split(' ');
                                                readXML(sitemapUrl[1]);
                                            }
                                        }
                                        sr.Close();
                                    }
                                    catch (WebException e)
                                    {
                                        Debug.WriteLine(e);
                                    }
                                }
                            }
                            else //url doesn't contain robot.txt
                            {
                                wStatus newState3 = new wStatus("Crawling");
                                newState3.getStatus = "Crawling";
                                TableOperation insertOperation3 = TableOperation.Insert(newState);
                                workerTable.Execute(insertOperation3);
                                errorTable.CreateIfNotExists();
                                table.CreateIfNotExists();
                                readHTML(url);
                            }
                        }
                    }
                    catch (NullReferenceException e)
                    {
                        Debug.WriteLine(e);
                    }
                }
                catch (StorageException e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private void readXML(String givenURL)
        {
            WebClient client = new WebClient();
            Stream readWebRequest = client.OpenRead(givenURL);
            using (StreamReader sr = new StreamReader(readWebRequest))
            {
                try
                {
                    String line;
                    line = sr.ReadToEnd();
                    sr.Close();
                    var filteredResults = line.Split(new String[] { "<sitemap" }, StringSplitOptions.None);
                    if (filteredResults.Count() == 1 || filteredResults.Count() == 0)
                    {
                        filteredResults = line.Split(new String[] { "<url" }, StringSplitOptions.None);
                    }
                    var httpResult = filteredResults
                        .Where(x => x.Contains("<loc>"))
                        .Select(x => x.ToString()).ToList();
                    foreach (String s in httpResult)
                    {
                        Boolean pass = true;
                        String[] splitLine = s.Split(new char[] { '>', '<' });
                        if (splitLine[7].StartsWith("2015"))
                        {
                            if (checker.checkDate(splitLine[7]) == false)
                            {
                                pass = false;
                            }
                        }
                        if (s.Contains(".xml"))
                        {
                            if (pass)
                            {
                                readXML(splitLine[3]);
                            }
                        }
                        else
                        {
                            if (pass)
                            {
                                checker.addvisitedUrl(splitLine[3]);
                                CloudQueueMessage m = new CloudQueueMessage(splitLine[3]);
                                urlQueue.AddMessage(m);
                            }   
                        }
                    }
                }
                catch (WebException e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
        }

        private Boolean uniqueCheck(String givenUrl)
        {
            checker.getCheckPerformed = checker.getCheckPerformed + 1;
            List<String> disallowedURLS = checker.getDisallowed();
            foreach (String s in disallowedURLS)
            {
                checker.checkDisallowedUrl(s);
            }
            List<String> visited = checker.getVisited();
            foreach (String st in visited)
            {
                checker.checkVisitedUrl(st);
            }
            return true;
        }

        private void readHTML(String givenUrl)
        {
            Boolean uniqueUrl = uniqueCheck(givenUrl);
            if (uniqueUrl)
            {
                try
                {
                    WebClient client = new WebClient();
                    Stream readWebRequest = client.OpenRead(givenUrl);
                    using (StreamReader sr = new StreamReader(readWebRequest))
                    {

                        String line;
                        line = sr.ReadToEnd();
                        sr.Close();
                        var filteredResults = line.Split(new char[] { '>' }, StringSplitOptions.None);
                        var httpResult = filteredResults
                            .Where(x => x.Contains("/title") || x.Contains("a href") || x.Contains("lastmod"))
                            .Select(x => x.ToString()).ToList();
                        String title = "";
                        String date = "";
                        foreach (String s in httpResult)
                        {

                            if (s.Contains("/title"))
                            {
                                String[] splitTitle = s.Split(new char[] { '<' });
                                title = splitTitle[0];
                            }
                            else if (s.Contains("a href=\"http:"))
                            {
                                String[] splitLink = s.Split(new char[] { '\"' });
                                CloudQueueMessage newLink = new CloudQueueMessage(splitLink[1]);
                                urlQueue.AddMessage(newLink);
                            }
                            else if (s.Contains("lastmod"))
                            {
                                String[] splitDate = s.Split(new String[] { "\"" }, StringSplitOptions.None);
                                date = splitDate[1];
                            }
                        }
                        checker.getCheckPassed = checker.getCheckPassed + 1;
                        float memUsage = memProcess.NextValue();
                        String memAvailable = memUsage.ToString();
                        float cpuPercent = cpuCounter.NextValue();
                        String cpuPercentage = cpuPercent.ToString();
                        urlInfo addItem = new urlInfo(givenUrl, title);
                        addItem.getUrl = givenUrl;
                        addItem.getTitle = title;
                        addItem.getMemoryAvailable = memAvailable;
                        addItem.getCpuUsage = cpuPercentage;
                        addItem.getChecksPassed = checker.getCheckPassed.ToString();
                        addItem.getChecksPerformed = checker.getCheckPerformed.ToString();
                        if (date != null)
                        {
                            addItem.getlastModifiedDate = date;
                        }
                        else
                        {
                            addItem.getlastModifiedDate = DateTime.Now.ToString();
                        }
                        TableOperation insertOperation = TableOperation.Insert(addItem);
                        table.Execute(insertOperation);
                    }
                }
                catch (WebException e)
                {
                    String error = e.ToString();
                    errorMessage currentError = new errorMessage(givenUrl, error);
                    currentError.getUrl = givenUrl;
                    currentError.getErrorMessageContent = error;
                    TableOperation insertOperation = TableOperation.Insert(currentError);
                    errorTable.Execute(insertOperation);
                }
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}

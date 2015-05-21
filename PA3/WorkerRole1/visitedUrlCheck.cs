using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class vistedUrlCheck
    {
        private List<String> visitedURL;
        private List<String> disallowedURL;
        private DateTime cutoff;
        private String command;
        private String status;
        private int checkPerformed;
        private int checkPassed;

        public vistedUrlCheck()
        {
            this.visitedURL = new List<String>();
            this.disallowedURL = new List<String>();
            this.cutoff = new DateTime(2015, 4, 1);
            this.command = null;
            this.checkPassed = 0;
            this.checkPerformed = 0;
            this.status = null;
        }

        public String getStatus { get; set; }

        public int getCheckPassed { get; set;}

        public int getCheckPerformed { get; set; }

        public String getCommand { get; set; }

        public List<String> addvisitedUrl(String url)
        {
            visitedURL.Add(url);
            return visitedURL;
        }

        public List<String> adddisallowedUrl(String url)
        {
            disallowedURL.Add(url);
            return disallowedURL;
        }

        public List<String> getVisited()
        {
            return visitedURL;
        }

        public List<String> getDisallowed()
        {
            return disallowedURL;
        }

        public Boolean checkVisitedUrl(String url)
        {
            if (visitedURL.Contains(url))
            {
                return false;
            }
            return true;
        }

        public Boolean checkDisallowedUrl(String url)
        {
            foreach (String s in disallowedURL)
            {
                if (url.Contains(s))
                {
                    return false;
                }
            }
            return true;
        }

        public Boolean checkDate(String url)
        {
            DateTime lastModified = Convert.ToDateTime(url);
            if (lastModified > cutoff)
            {
                return true;
            }
            return false;
        }
    }
}

using System;
using System.Security.AccessControl;

namespace Customization.Tasks
{
    public class WCFDetail
    {
        public WCFDetail(string url, DateTime lastCheckIn, bool isResponsive)
        {
            this.url = url;
            LastCheckIn = lastCheckIn;
            IsResponsive = isResponsive;
        }

        public WCFDetail()
        {
            
        }

        public string url { get; set; }
        public DateTime LastCheckIn { get; set; }
        public bool IsResponsive { get; set; }
    }
}


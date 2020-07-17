using System;
using System.Collections.Generic;
using System.Text;

namespace NAVZRAIntegration
{
    class CustomConfigurations
    {
        public string ReadNavisionURL()
        {
            return System.Configuration.ConfigurationManager.AppSettings.Get("NavisionURL");
        }public string ReadESDIPAddress()
        {
            return System.Configuration.ConfigurationManager.AppSettings.Get("NavisionURL");
        }
    }
}

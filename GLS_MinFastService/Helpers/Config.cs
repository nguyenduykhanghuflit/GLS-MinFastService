using GLS_MinFastService.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLS_MinFastService.Helpers
{
    internal class Config
    {

        private IConfiguration _configuration;
        public Config(IConfiguration configuration)
        {

            _configuration = configuration;
        }

        public string? ConnectionD1COFFEE()
        {
            return _configuration.GetConnectionString("ConnectionD1COFFEE");
        }

        public string? Connection_KAT_GOLDEN_BI()
        {
            string? env = _configuration.GetValue<string>("Env");
            string? result = "";
            if (env == "dev")
            {
                result = _configuration.GetConnectionString("ConnectionDev");
            }
            else result = _configuration.GetConnectionString("Connection_KAT_GOLDEN_BI");
            return result;
        }

        public string? Connection_KATINAT_CENTER_1()
        {
            return _configuration.GetConnectionString("Connection_KATINAT_CENTER_1");
        }
        public string? Connection_KATINAT_CENTER_2()
        {
            return _configuration.GetConnectionString("Connection_KATINAT_CENTER_2");
        }
        public string? Connection_KATINAT_CENTER_3()
        {
            return _configuration.GetConnectionString("Connection_KATINAT_CENTER_3");
        }
        public string? MentorURL()
        {
            return _configuration.GetSection("GatewayApi")!.GetValue<string>("MentorURL");
        }
        public string? ApiKey()
        {
            return _configuration.GetSection("GatewayApi")!.GetValue<string>("ApiKey");
        }
    }
}

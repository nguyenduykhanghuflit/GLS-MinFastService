using GLS_MinFastService.Helpers;
using GLS_MinFastService.Workers;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GLS_MinFastService.Models
{
    public class InitRunService
    {
        private readonly ILogger<Worker> _logger;

        private readonly SqlHelper _sqlHelpers;

        private IConfiguration _configuration;


        public List<KeyCenterByBranch>? KeyCenterByBranch { get; set; }
        public Dictionary<Int64, EINVOICE_CONFIG> SignOfBranch { get; set; }
        public decimal StandardDeviation { get; set; }
        public MentorModel? DataMentor { get; set; }
        public List<MentorResponse>? DataResult { get; set; }
        public List<BranchModel>? ListBranchSuccess { get; set; }
        public List<BranchModel>? ListBranchFailure { get; set; }
        public int TotalSuccess { get; set; }
        public int TotalFailure { get; set; }
        public List<string>? ListSignEodFail { get; set; }

        public InitRunService(ILogger<Worker> logger, SqlHelper sqlHelpers, IConfiguration configuration)
        {
            _logger = logger;
            _sqlHelpers = sqlHelpers;
            _configuration = configuration;

            KeyCenterByBranch = new List<KeyCenterByBranch>();
            SignOfBranch = new Dictionary<Int64, EINVOICE_CONFIG>();
            DataMentor = new MentorModel();
            DataResult = new List<MentorResponse>();
            ListBranchSuccess = new List<BranchModel>();
            ListBranchFailure = new List<BranchModel>();
            TotalSuccess = 0;
            TotalFailure = 0;
            ListSignEodFail = new List<string>();

        }

        public async Task Start()
        {


            string? connectionString = _configuration.GetConnectionString("ConnectionD1COFFEE");
            string? connection_KAT_GOLDEN_BI = _configuration?.GetConnectionString("Connection_KAT_GOLDEN_BI");


            DataTable getKeyCenterByBranch = _sqlHelpers.QueryNotParamAsDatatable("dbo.proc_BI_GATEWAY_GetList", connection_KAT_GOLDEN_BI);
            this.KeyCenterByBranch = getKeyCenterByBranch != null ? Helper.DataTableToList<KeyCenterByBranch>(getKeyCenterByBranch) : null;



            DataTable getEinvoicConfig = _sqlHelpers.QueryNotParamAsDatatable("proc_MinFast_Get_EINVOICE_CONFIG", connection_KAT_GOLDEN_BI);
            this.SignOfBranch = Helper.ConvertDataTableToDictionary<Int64, EINVOICE_CONFIG>(getEinvoicConfig, "OBJ_AUTOID");

            Hashtable hashtable = new Hashtable
            {
                { "@KEYCODE", "MINFAST_StandardDeviation" }
            };
            DataTable queryStandardDeviation = _sqlHelpers.QueryAsDataTable(hashtable, "sp_RES_GetConfigValue_CODE", connectionString);
            this.StandardDeviation = decimal.Parse(Helper.DataTableClass<ConfigValue>(queryStandardDeviation).Value!);

            int retryCount = 0;
            const int maxRetryCount = 4;




            #region Tính thời gian call api
            var stopwatchApi = new Stopwatch();
            stopwatchApi.Start();
            _logger.LogInformation("Bắt đầu Call Api Gateway: " + DateTime.Now.ToString());
            #endregion
            while (retryCount < maxRetryCount)
            {
                try
                {
                    this.DataMentor = await GetRevenueFromMentor(-1);
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetryCount)
                    {
                        throw new Exception("Đã gọi api gateway 4 lần nhưng không thành công: " + ex.ToString());
                    }
                }
            }

            #region Tính thời gian Call Api Gateway
            stopwatchApi.Stop();
            var executionTime = stopwatchApi.ElapsedMilliseconds / 1000.0;
            _logger.LogInformation($":::::Đã gọi api gateway, thời gian: {executionTime} s");
            #endregion


            this.DataResult = this.DataMentor?.DataResult;
        }


        /// <summary>
        /// Lấy doanh thu của tất cả các chi nhánh từ mentor api
        /// </summary>
        /// <param name="t">Số ngày lùi, mặc định lùi 1 ngày t=-1</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        async Task<MentorModel> GetRevenueFromMentor(int t = -1)
        {
            try
            {


                string gatewayApi = _configuration.GetSection("GatewayApi")!.GetValue<string>("MentorURL")!;
                string apiKey = _configuration.GetSection("GatewayApi")!.GetValue<string>("ApiKey")!;
                var options = new RestClientOptions(gatewayApi)
                {
                    MaxTimeout = -1,
                };
                var client = new RestClient(options);
                var request = new RestRequest("/api/gateway/mentorsystem/getdatabill", Method.Post);
                request.AddHeader("Authorization", apiKey);
                request.AddHeader("Content-Type", "application/json");

                var body = new
                {
                    date = DateTime.Now.AddDays(t),
                    keyStore = "ALL"
                };
                request.AddJsonBody(body);

                RestResponse response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    string jsonResponse = response.Content!;
                    var res = JsonConvert.DeserializeObject<MentorModel>(jsonResponse!);
                    if (res?.DataResult?.Count > 0)
                        return res;
                }

                throw new Exception(":::Đã xảy ra lỗi trong lúc gọi gateway api: " + response.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception(":::Không gọi được api gateway: " + ex.ToString());
            }
        }

    }

}

using GLS_MinFastService.Helpers;
using GLS_MinFastService.Models;
using System.Xml.Linq;
using RestSharp;
using Newtonsoft.Json;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace GLS_MinFastService.Workers
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SqlHelper _sqlHelpers;
        private IConfiguration _configuration;
        private readonly int t = -1; //lùi mấy ngày
        public Worker(ILogger<Worker> logger, SqlHelper sqlHelpers, IConfiguration configuration)
        {

            _logger = logger;
            _sqlHelpers = sqlHelpers;
            _configuration = configuration;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                var now = DateTime.Now;
                Hashtable hashtable = new()
                {
                    { "@KEYCODE", "MINFAST_TimeRunServiceCheckDataMin" }
                };

                string? connectionString = _configuration.GetConnectionString("ConnectionD1COFFEE");
                var queryTimeRunService = _sqlHelpers.QueryAsDataTable(hashtable, "sp_RES_GetConfigValue_CODE", connectionString);
                int timeRunService = int.Parse(Helper.DataTableClass<ConfigValue>(queryTimeRunService).Value!);

                #region Log thời gian service quét
                _logger.LogInformation($"_______________________________________________________________________________________________________________");
                _logger.LogInformation($"_______________________________________________________________________________________________________________");
                _logger.LogInformation($"_______________________________________________________________________________________________________________");
                _logger.LogInformation($"Service quét lúc: {now}");
                _logger.LogInformation($"Giờ hiện tại: {now.Hour}h");
                _logger.LogInformation($"Giờ service sẽ kiểm tra dữ liệu: {timeRunService}h");
                _logger.LogInformation($"_______________________________________________________________________________________________________________");
                #endregion

                Console.WriteLine(now.Hour);
                Console.WriteLine(timeRunService);
                if (now.Hour == timeRunService)
                {
                    #region Log thời gian kiểm tra dữ liệu
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    _logger.LogInformation("Service bắt đầu thực hiện kiểm tra dữ liệu: " + now.ToString());
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    #endregion

                    await RunService();

                    #region Log thời gian kiểm tra dữ liệu
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    stopwatch.Stop();
                    var executionTime = stopwatch.ElapsedMilliseconds / 1000.0;
                    _logger.LogInformation($"Service đã kiểm tra dữ liệu xong vào lúc: {DateTime.Now}, tổng thời gian kiểm tra và insert dữ liệu vào db: {executionTime}");
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    #endregion


                }
                else
                {

                    #region Log thời gian kiểm tra lại
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    _logger.LogInformation("Service bắt đầu kiểm tra lại dữ liệu của những chi nhánh có trạng thái thất bại: " + now.ToString());
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    #endregion

                    await RecheckUseApiInKat_BI(timeRunService);


                    #region Log thời gian kiểm tra lại
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    stopwatch.Stop();
                    var executionTime = stopwatch.ElapsedMilliseconds / 1000.0;
                    _logger.LogInformation($"Service đã kiểm tra lại dữ liệu của những chi nhánh thất bại thành công : {DateTime.Now}, tổng thời gian kiểm tra và update dữ liệu vào db: {executionTime}");
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    #endregion
                }

                _logger.LogInformation($"_______________________________________________________________________________________________________________");
                _logger.LogInformation($"_______________________________________________________________________________________________________________");
                _logger.LogInformation($"_______________________________________________________________________________________________________________");

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }



        #region Core proccess
        async Task RunService()
        {
            try
            {

                InitRunService initRunService = new InitRunService(_logger, _sqlHelpers, _configuration);
                try
                {
                    await initRunService.Start();

                    _logger.LogInformation(" ~ Khởi tạo các biến thành công:" + DateTime.Now.ToString());
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(" ~ Khởi tạo các biến thất bại:" + DateTime.Now.ToString());
                    _logger.LogInformation($" ~ Lỗi: {ex}");
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    throw;
                }

                /* Các bước kiểm tra, ví dụ hôm nay là ngày 24/01
                * Kiểm tra EOD của 2 ngày trước (22/01) xem có chi nhánh nào chưa EOD hay không nếu có => lấy ra kí hiệu lưu vào danh sách, bật cờ checkeod2
                * Kiểm tra EOD của 1 ngày trước (23/01) nếu  PASS, bật cờ checkeodyesterday, kiểm tra tiếp nếu kí hiệu của nó nằm trong danh sách kí hiệu checkeod2 -> bật cờ FAIL, nếu không =>PASS                 
                * Tóm lại: checkeod t-2 -> checkeod t-1, trong khi check t-1 nếu tồn tại kí hiệu FAIL ở t-2=>>>FAIL
                */

                XElement xmlRequest = new("Root");


                for (int i = 0; i < initRunService?.DataResult?.Count; i++)
                {

                    MentorResponse item = initRunService.DataResult[i];
                    MentorModel? dataMentor = initRunService.DataMentor;

                    BranchModel branch = new()
                    {
                        BranchId = item.BranchId,
                        BranchName = item.BranchName
                    };

                    //cờ check doanh thu
                    int checkPosAndCenter = 1;
                    string msgCheckPosAndCenter = "";

                    //cờ check eod 2 ngày trước
                    string msgCheckEod2 = "";
                    int checkEod2 = 1;

                    //cờ check eod ngày hôm qua
                    string msgCheckEod = "";
                    int checkEod = 1;

                    //lỗi code
                    string msgLotus = "";

                    //cờ check cuối cùng
                    int finalStatus = 0;

                    if (item.BranchId > 0 && initRunService?.SignOfBranch[item.BranchId].AllowPushInvoice == 1)
                    {


                        //alias lại tên của BBD WB
                        string wb = item.StoreID.Replace("Lotus", "");
                        if (wb == "WB") item.BranchName = item.BranchName.Replace("TDT", "") + " - " + wb;

                        _logger.LogInformation($"_______________________________________________________________________________________________________________");
                        _logger.LogInformation($"Check doanh thu: {item.BranchName}");
                        ExecuteCheckRevenue(ref msgLotus,
                                            ref msgCheckPosAndCenter,
                                            ref checkPosAndCenter,
                                            ref branch,
                                            item,
                                            initRunService,
                                            dataMentor);

                        _logger.LogInformation($"_______________________________________________________________________________________________________________");
                        _logger.LogInformation($"Check EOD 2 ngày: {item.BranchName}");
                        ExecuteCheckEOD2Day(item,
                                            initRunService,
                                            ref checkEod2,
                                            ref msgCheckEod2,
                                            ref msgLotus,
                                            ref branch);

                        _logger.LogInformation($"_______________________________________________________________________________________________________________");
                        _logger.LogInformation($"Check EOD 1 ngày: {item.BranchName}");
                        ExecuteCheckEOD1Day(item,
                                            initRunService,
                                            checkEod2,
                                            checkPosAndCenter,
                                            ref msgLotus,
                                            ref checkEod,
                                            ref msgCheckEod,
                                            ref branch,
                                            ref finalStatus);



                        XElement xElement = new("Item",
                                new XElement("BranchId", item.BranchId),
                                new XElement("BranchName", item.BranchName),
                                new XElement("KeyStore", item.StoreID),
                                new XElement("CenterData", item.PaymentSynced),
                                new XElement("StandardDeviation", initRunService?.StandardDeviation),
                                new XElement("PosData", item.PaymentClient),
                                new XElement("DifferenceStatusOfPosAndCenter", checkPosAndCenter),
                                new XElement("EodStatusYesterday", checkEod),
                                new XElement("CheckStatus", finalStatus),
                                new XElement("Executed", 0),
                                new XElement("CheckDate", DateTime.Now.AddDays(t)),
                                new XElement("MsgCheckPosAndCenter", msgCheckPosAndCenter),
                                new XElement("MsgCheckEodYesterday", msgCheckEod),
                                new XElement("MsgLotus", msgLotus),
                                new XElement("EodStatus2", checkEod2),
                                new XElement("MsgCheckEod2", msgCheckEod2)
                                );

                        xmlRequest.Add(xElement);




                    }
                }

                // InsertXMLDataMinToDB(xmlRequest.ToString());

                await SendData(initRunService!.TotalSuccess, initRunService.TotalFailure, initRunService!.ListBranchFailure!, initRunService!.ListBranchSuccess!);

            }
            catch (Exception ex)
            {
                await SendMessage($"Đã xảy ra lỗi khi chạy service kiểm tra data min, vui lòng liên hệ với GLS " +
                        $"{ex}");

                _logger.LogInformation($":::Đã xảy ra lỗi khi chạy service kiểm tra data min" +
                $"{ex}");
            }
        }
        async Task RecheckUseApiInKat_BI(int timeRunService)
        {
            try
            {

                var now = DateTime.Now;

                if (now.Hour > timeRunService)
                {
                    _logger.LogInformation("Bắt đầu kiểm tra lại: " + now.AddDays(-1).ToString());
                    var dataMinv = GetDataMin();

                    if (dataMinv.Count > 0)
                    {

                        var lstFail = dataMinv.Where(i => i.CheckStatus == 0 && i.Executed == 0).ToList();

                        if (lstFail.Count <= 0)
                            _logger.LogInformation("Không có chi nhánh nào có trạng thái thất bại và chưa đẩy");

                        List<string> listBranchRecheck = new List<string>();
                        for (int i = 0; i < lstFail.Count; i++)
                        {
                            var item = lstFail[i];

                            _logger.LogInformation($"_______________________________________________________________________________________________________________");
                            _logger.LogInformation("Kiểm tra lại chi nhánh: " + item.BranchName);

                            string url = "http://katinat-dashboard.senvangpos.com/CheckMinFast";
                            var client = new RestClient($"{url}/ReCheckMin");
                            RestRequest request = new();

                            request.Method = Method.Get;

                            var param = new
                            {
                                date = now.AddDays(-9),
                                branchId = item.BranchId,
                                callFrom = "SERVICE",
                                saveActionLog = 0,
                            };

                            request.AddObject(param);

                            RestResponse response = await client.ExecuteAsync(request);

                            if (response.IsSuccessful)
                            {
                                string jsonResponse = response.Content!;
                                ApiResponse? apiResponse = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

                                Hashtable hashtable = new Hashtable
                                        {
                                            { "@AutoId", item.AutoId },
                                            { "@Log", "Service tự động kiểm tra lại" }

                                        };
                                Config config = new Config(_configuration);
                                string? connectionString = config.Connection_KAT_GOLDEN_BI();
                                var query = _sqlHelpers.QueryAsDataTable(hashtable, "proc_MinFast_ServiceWriteLogByAutoId", connectionString);

                                _logger.LogInformation($"_______________________________________________________________________________________________________________");
                                _logger.LogInformation("Đã kiểm tra lại chi nhánh: " + item.BranchName);
                                _logger.LogInformation("Response api: ~ " + jsonResponse);
                                _logger.LogInformation($"_______________________________________________________________________________________________________________");

                                listBranchRecheck.Add(item.BranchName);
                            }

                        }
                        await SendMessage("Đã kiểm tra lại tất cả các chi nhánh " + String.Join(", ", listBranchRecheck.ToArray()));
                    }

                }

            }
            catch (Exception ex)
            {
                await SendMessage($"Lỗi khi kiểm tra lại => {ex}");
            }
        }
        List<MinModel> GetDataMin()
        {
            List<MinModel> res = new List<MinModel>();
            try
            {
                Hashtable hashtable = new Hashtable
                {
                    { "@Date", DateTime.Now.AddDays(-1) }
                };
                Config config = new Config(_configuration);
                string? connectionString = config.Connection_KAT_GOLDEN_BI();
                var query = _sqlHelpers.QueryAsDataTable(hashtable, "proc_MinFast_GetDataMinStatus", connectionString);
                res = Helper.DataTableToList<MinModel>(query);

                _logger.LogInformation("Get data min thanh cong");
            }
            catch
            {
                throw;
            }
            return res;
        }
        #endregion

        #region Steps
        void ExecuteCheckRevenue(ref string msgLotus, ref string msgCheckPosAndCenter, ref int checkPosAndCenter, ref BranchModel branch, MentorResponse item, InitRunService initRunService, MentorModel? dataMentor)
        {
            try
            {
                var findErr = dataMentor?.ListErrors?.Find(i => i.StoreID == item.StoreID);
                if (findErr != null)
                {
                    msgCheckPosAndCenter = $"Chi nhánh {item.BranchName} không kết nối được api (URL API: {findErr.UrlClient})";
                    checkPosAndCenter = 0;

                    initRunService.TotalFailure++;
                    branch.ErrorCheck = msgCheckPosAndCenter;
                    initRunService?.ListBranchFailure?.Add(branch);
                }

                else if (item.PaymentSynced != item.PaymentClient)
                {
                    decimal diff = Math.Abs(item.PaymentSynced - item.PaymentClient);

                    CultureInfo cul = CultureInfo.GetCultureInfo("vi-VN");
                    string formatCurrency = diff.ToString("#,###", cul.NumberFormat);
                    string centerFormant = item.PaymentSynced <= 0 ? "0" : item.PaymentSynced.ToString("#,###", cul.NumberFormat);
                    string clientFormant = item.PaymentClient <= 0 ? "0" : item.PaymentClient.ToString("#,###", cul.NumberFormat);
                    msgCheckPosAndCenter = $"Center: {centerFormant} VND, Outlet: {clientFormant} VND, Lệch {formatCurrency} VNĐ";



                    if (diff <= initRunService.StandardDeviation)
                    {
                        //bật cờ pass doanh thu
                        checkPosAndCenter = 1;
                        initRunService.TotalSuccess++;
                        branch.ErrorCheck = "";
                        initRunService?.ListBranchSuccess?.Add(branch);

                    }
                    else
                    {
                        //bật cờ fail doanh thu
                        checkPosAndCenter = 0;
                        initRunService.TotalFailure++;
                        branch.ErrorCheck = msgCheckPosAndCenter;
                        initRunService?.ListBranchFailure?.Add(branch);
                    }

                }
                else if (item.PaymentClient == 0 && item.PaymentSynced == 0)
                {
                    msgCheckPosAndCenter = $"Center và outlet chưa có doanh thu";
                    checkPosAndCenter = 0;

                    initRunService.TotalFailure++;
                    branch.ErrorCheck = msgCheckPosAndCenter;
                    initRunService?.ListBranchFailure?.Add(branch);
                }
                else
                {
                    msgCheckPosAndCenter = "";
                    checkPosAndCenter = 1;
                    initRunService.TotalSuccess++;
                    branch.ErrorCheck = "";
                    initRunService?.ListBranchSuccess?.Add(branch);
                }
            }
            catch (Exception ex)
            {
                msgLotus = $"Lỗi server khi kiểm tra doanh thu: " + ex;
                checkPosAndCenter = 0;
                initRunService.TotalFailure++;
                branch.ErrorCheck = msgLotus;
                initRunService?.ListBranchFailure?.Add(branch);
            }
        }

        void ExecuteCheckEOD2Day(MentorResponse item, InitRunService initRunService, ref int checkEod2, ref string msgCheckEod2, ref string msgLotus, ref BranchModel branch)
        {

            try
            {

                string? executeCheckEod2 = CheckEOD(item.BranchId, item.BranchName!, initRunService!.KeyCenterByBranch, -2);

                if (!string.IsNullOrEmpty(executeCheckEod2))
                {
                    //khi check eod hôm nay nếu kí hiệu của chi nhánh đó nằm trong list này thì cho cờ là fail
                    var signOfBranch = initRunService?.SignOfBranch[item.BranchId]?.SERIAL78;

                    checkEod2 = 0;
                    msgCheckEod2 = $"{executeCheckEod2} - Kí hiệu: {signOfBranch}";


                    if (signOfBranch != null && initRunService?.ListSignEodFail?.Find(i => i == signOfBranch) == null)
                    {
                        initRunService?.ListSignEodFail?.Add(signOfBranch);
                    }

                    ChangeStateSuccessOrFailureAfterCheckEOD(initRunService!, branch, msgCheckEod2, item, 2);
                }


            }
            catch (Exception ex)
            {
                string dateString2 = DateTime.Now.AddDays(-2).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                checkEod2 = 0;
                msgCheckEod2 = $"Kiểm tra EOD ngày {dateString2} gặp lỗi";
                msgLotus =
                    string.IsNullOrEmpty(msgLotus)
                    ? $"{msgCheckEod2} => {ex}"
                    : $"{msgLotus} - {msgCheckEod2} => {ex}";

                ChangeStateSuccessOrFailureAfterCheckEOD(initRunService!, branch, msgCheckEod2, item, 2);

            }
        }

        void ExecuteCheckEOD1Day(MentorResponse item, InitRunService initRunService, int checkEod2, int checkPosAndCenter, ref string msgLotus, ref int checkEod, ref string msgCheckEod, ref BranchModel branch, ref int finalStatus)
        {

            try
            {
                string? executeCheckEod1 = CheckEOD(item.BranchId, item.BranchName!, initRunService!.KeyCenterByBranch, -1);

                if (!string.IsNullOrEmpty(executeCheckEod1))
                {
                    checkEod = 0;
                    msgCheckEod = executeCheckEod1;
                    ChangeStateSuccessOrFailureAfterCheckEOD(initRunService!, branch, msgCheckEod, item, 1);
                }
                else
                {
                    checkEod = 1;
                    var signOfBranch = initRunService?.SignOfBranch[item.BranchId]?.SERIAL78;
                    if (signOfBranch != null && initRunService?.ListSignEodFail?.Find(i => i == signOfBranch) != null)
                    {
                        string date = DateTime.Now.AddDays(-2).ToString();
                        finalStatus = 0;
                        msgCheckEod = $"Tồn tại phiếu chưa chạy EOD ngày {date}. Kí hiệu {signOfBranch}";

                        ChangeStateSuccessOrFailureAfterCheckEOD(initRunService!, branch, msgCheckEod, item, 1);
                    }
                    else
                    {
                        if (checkEod2 == 1 && checkPosAndCenter == 1) finalStatus = 1;
                    }
                }


            }
            catch (Exception ex)
            {
                string dateString1 = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                checkEod = 0;
                msgCheckEod = $"Kiểm tra EOD ngày {dateString1} gặp lỗi";
                msgLotus =
                    string.IsNullOrEmpty(msgLotus)
                    ? $"{msgCheckEod} => {ex}"
                    : $"{msgLotus} - {msgCheckEod} => {ex}";

                ChangeStateSuccessOrFailureAfterCheckEOD(initRunService!, branch, msgCheckEod, item, 1);

            }

        }
        #endregion

        #region Common function
        /// <summary>
        /// Check EOD của từng branch
        /// </summary>
        /// <param name="branchId"></param>
        /// <param name="branchName"></param>
        /// <param name="t"></param>
        /// <returns>Null: hợp lệ</returns>
        /// <exception cref="Exception"></exception>
        string? CheckEOD(int branchId, string branchName, List<KeyCenterByBranch>? _keyCenterByBranch, int t = -1)
        {
            string dateString = DateTime.Now.AddDays(t).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            try
            {

                Dictionary<string, string> KEY_CENTER = new()
                    {
                        { "POS_CENTER01", "Connection_KATINAT_CENTER_1" },
                        { "POS_CENTER02", "Connection_KATINAT_CENTER_2" },
                        { "POS_CENTER03", "Connection_KATINAT_CENTER_3" }
                    };


                if (_keyCenterByBranch == null)
                {
                    throw new Exception($"Chưa lấy được thông tin POS CENTER của chi nhánh {branchName} để check EOD, vui lòng liên hệ admin");
                }
                else
                {
                    KeyCenterByBranch? find = _keyCenterByBranch.FirstOrDefault(i => i.ORG_AUTOID == branchId);

                    if (find != null)
                    {
                        string key = find!.KEY_CENTER!;
                        string? connectionString = _configuration.GetConnectionString(KEY_CENTER[key]);

                        Hashtable hashtable = new()
                        {
                            { "@date", DateTime.Now.AddDays(t) },
                            { "@ORG_AUTOID", branchId }
                        };
                        var dtb = _sqlHelpers.QueryAsDataTable(hashtable, "proc_RESINVOICE_COUNTING", connectionString);
                        var result = Helper.DataTableClass<EDO_CheckModel>(dtb);


                        if (result.Row_Count == 0)
                        {
                            return null;
                        }

                        if (result.Row_Count == 99)
                        {
                            return $"Tồn tại phiếu có nhiều loại thuế ({dateString})";
                        }

                        if (result.Row_Count > 0 && result.Row_Count != 99)
                        {

                            return $"Tồn tại phiếu chưa chạy EOD ({result.Row_Count}) - Ngày {dateString}";
                        }
                        else
                        {
                            return null;
                        }

                    }
                    else
                    {
                        throw new Exception($"Không tìm thấy KEY_CENTER cho chi nhánh {branchName}");
                    }

                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi check EOD cho chi nhánh {branchName}, chi tiết: " + ex.ToString());
            }


        }

        void InsertXMLDataMinToDB(string xmlRequest)
        {
            try
            {

                Hashtable hashtable = new()
                {
                    { "@xmlData", xmlRequest }
                };
                Config config = new Config(_configuration);
                string? connectionString = config.Connection_KAT_GOLDEN_BI();
                var query = _sqlHelpers.QueryAsDataTable(hashtable, "proc_MinFast_InsertMinCheckXML", connectionString);

            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi insert vào db" + ex);
            }
        }

        void ChangeStateSuccessOrFailureAfterCheckEOD(InitRunService initRunService, BranchModel branch, string msg, MentorResponse item, int eodDay)
        {
            var findSuccess = initRunService?.ListBranchSuccess?.Find(i => i.BranchId == item.BranchId);
            if (findSuccess != null)
            {
                //xóa
                initRunService!.ListBranchSuccess = initRunService!.ListBranchSuccess?.Where(i => i.BranchId != item.BranchId).ToList();
                initRunService.TotalSuccess--;
                //cập nhật msg
                branch.ErrorCheckEod =
                    eodDay == 2
                    ? msg
                    : $"{branch.ErrorCheckEod}, {msg}";
                //thêm
                initRunService?.ListBranchFailure?.Add(branch);
                initRunService!.TotalFailure++;
            }
            else
            {
                //tìm và cập nhật msg
                var findFailure = initRunService!.ListBranchFailure?.Find(i => i.BranchId == item.BranchId);
                findFailure!.ErrorCheckEod =
                    eodDay == 2
                    ? msg
                    : $"{branch.ErrorCheckEod}, {msg}";

            }
        }

        async Task SendData(int _totalSuccess, int _totalFailure, List<BranchModel> _listBranchFailure, List<BranchModel> _listBranchSuccess)
        {
            try
            {
                List<TelgramInfoModel> telgramInfoModels = new List<TelgramInfoModel>();
                Config config = new Config(_configuration);
                string? connectionString = config.Connection_KAT_GOLDEN_BI();
                var query = _sqlHelpers.QueryNotParamAsDatatable("proc_MinFast_GetTelegramInfoMinFast", connectionString);
                telgramInfoModels = Helper.DataTableToList<TelgramInfoModel>(query);

                string? url = _configuration.GetValue<string>("TelegramBotApiUrl");

                var client = new RestClient($"{url}/api/min-fast/send-data");
                foreach (var item in telgramInfoModels)
                {
                    RestRequest request = new();

                    request.Method = Method.Post;
                    request.AddHeader("Accept", "application/json");
                    MinMessageModel minMessage = new()
                    {
                        ChatId = item.ChatId,
                        Title = "Thông báo MIN-FAST",
                        TotalSuccess = _totalSuccess,
                        TotalFailure = _totalFailure,
                        ListBranchFailure = _listBranchFailure,
                        ListBranchSuccess = _listBranchSuccess
                    };


                    request.AddParameter("application/json", minMessage, ParameterType.RequestBody);

                    RestResponse response = await client.ExecuteAsync(request);

                    if (response.IsSuccessful)
                    {
                        string jsonResponse = response.Content!;
                        _logger.LogInformation($"{jsonResponse}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Lỗi không gọi được bot gửi tin nhắn: {ex}");
            }
        }

        async Task SendMessage(string msg)
        {
            try
            {

                string? url = _configuration.GetValue<string>("TelegramBotApiUrl");
                var client = new RestClient($"{url}/api/send-message");
                RestRequest request = new();

                request.Method = Method.Get;

                var param = new
                {
                    msg,
                    chat_id = "5312818365",
                    bot_type = "minfast"
                };

                request.AddObject(param);

                RestResponse response = await client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    string jsonResponse = response.Content!;
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                    _logger.LogInformation($"Bot telegram: {msg},  {jsonResponse}"); ;
                    _logger.LogInformation($"_______________________________________________________________________________________________________________");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Lỗi không gọi được bot gửi tin nhắn: {ex}");
            }
        }

        #endregion


    }
}
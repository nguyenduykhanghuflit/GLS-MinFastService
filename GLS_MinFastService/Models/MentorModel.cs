using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLS_MinFastService.Models
{
    public class BranchModel
    {
        public string? BranchName { get; set; }
        public int BranchId { get; set; }
        public string KeyStore { get; set; }
        public string ErrorCheck { get; set; }
        public string ErrorCheckEod { get; set; }
    }
    public class KeyCenterByBranch
    {
        public long ORG_AUTOID { get; set; }
        public string? KEY_CENTER { get; set; }
    }
    public class ConfigValue
    {
        public string? KeyCode { get; set; }
        public string? Value { get; set; }
    }

    public class EINVOICE_CONFIG
    {
        public int OBJ_AUTOID { get; set; }
        public string OBJ_NAME { get; set; }
        public string SERIAL78 { get; set; }
        public int AllowPushInvoice { get; set; }
    }
    public class MentorModel
    {
        public bool Success { get; set; }
        public int Errors { get; set; }
        public string? MessageErrors { get; set; }
        public List<ListBranchError>? ListErrors { get; set; }
        public List<MentorResponse>? DataResult { get; set; }
    }

    public class ListBranchError
    {
        public string? StoreID { get; set; }
        public string? UrlClient { get; set; }
    }
    public class MentorResponse
    {
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? StoreID { get; set; }
        public int BillSyncing { get; set; }
        public decimal PaymentSyncing { get; set; }
        public int BillSynced { get; set; }
        public decimal PaymentSynced { get; set; }
        public int BillCancel { get; set; }
        public int BillClient { get; set; }
        public int BillCancelClient { get; set; }
        public decimal PaymentClient { get; set; }
        public int BillDifferent { get; set; }
        public decimal PayementDifferent { get; set; }
        public string? ReSyncDataUrl { get; set; }
    }

    public class EDO_CheckModel
    {
        public int Row_Count { get; set; }
    }

    public class MinMessageModel
    {
        public string ChatId { get; set; }
        public string Title { get; set; }
        public int TotalSuccess { get; set; }
        public int TotalFailure { get; set; }
        public List<BranchModel> ListBranchSuccess { get; set; }
        public List<BranchModel> ListBranchFailure { get; set; }

    }

    public class TelgramInfoModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string ChatId { get; set; }
        public string Type { get; set; }
    }

    public class StardardDeviation
    {
        public decimal STANDARD_DEVIATION { get; set; }
    }

    public class MinFastModel
    {
        public int Ord { get; set; }
        public long AutoId { get; set; }
        public string CreatedDate { get; set; } //thời gian chạy service kiểm tra dữ liệu
        public string LastUpdatedDate { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public string Sign { get; set; }
        public decimal StandardDeviation { get; set; }
        public string CheckDate { get; set; }
        public string ServiceRunDate { get; set; } //thời gian chạy service tự động đẩy sang minv
        public string ReCheckLog { get; set; }


        public int CheckStatus { get; set; }
        public string MsgLotus { get; set; }
        public int Executed { get; set; }
    }

    public class MinModel : MinFastModel
    {
        public string PosData { get; set; }
        public string CenterData { get; set; }
        public short DifferenceStatusOfPosAndCenter { get; set; }
        public string MsgCheckPosAndCenter { get; set; }
        public short EodStatusYesterday { get; set; }
        public string MsgCheckEodYesterday { get; set; }
        public short EodStatus2 { get; set; }
        public string MsgCheckEod2 { get; set; }

    }

    public class FastModel : MinFastModel
    {
        public short EODStatus { get; set; }
        public short Revenue { get; set; }
        public short DataFast { get; set; }
        public short HeaderAndDetail { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Msg { get; set; }
        public string Error { get; set; }
        public object Data { get; set; }
    }

}

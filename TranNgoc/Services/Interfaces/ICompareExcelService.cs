using TranNgoc.Services.Dto;
using TranNgoc_BE.Services.Dto.ExcelCompare;

namespace TranNgoc_BE.Services.Interfaces
{
    public interface ICompareExcelService
    {
        Task<BaseResponse<DynamicComparePreviewResultDto>> CompareReview(IFormFile file, long objectId);
        Task<BaseResponse<CompareExcelResultDto>> ExportExcel(IFormFile file, long objectId);
        Task<BaseResponse<List<CompareTemplateOptionDto>>> GetTemplateOptionsAsync();
        Task<BaseResponse<CompareExcelResultDto>> DownloadSampleFile(long objectId);
    }
}

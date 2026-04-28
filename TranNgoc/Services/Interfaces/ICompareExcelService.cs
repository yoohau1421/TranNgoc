using TranNgoc.Services.Dto;
using TranNgoc.Services.Dto.ExcelCompare;

namespace TranNgoc.Services.Interfaces
{
    public interface ICompareExcelService
    {
        Task<List<ImportCompareExcelRowDto>> ImportExcelAsync(IFormFile file);
        Task<BaseResponse<CompareExcelResultDto>> ExportExcel(IFormFile file, long objectId);
        Task<BaseResponse<ComparePreviewResultDto>> CompareReview(IFormFile file, long objectId);
    }
}

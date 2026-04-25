using TranNgoc.Services.Dto;

namespace TranNgoc.Services.Interfaces
{
    public interface ICompareExcelService
    {
        Task<List<ImportCompareExcelRowDto>> ImportExcelAsync(IFormFile file);
        Task<CompareExcelResultDto> CompareAsync(IFormFile file, long objectId);
    }
}

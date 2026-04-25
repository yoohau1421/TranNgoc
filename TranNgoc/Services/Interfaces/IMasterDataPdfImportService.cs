namespace TranNgoc.Services.Interfaces
{
    public interface IMasterDataPdfImportService
    {
        Task<int> ImportFromPdfAsync(IFormFile file, long objectId);
    }
}

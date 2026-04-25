using Microsoft.AspNetCore.Mvc;
using TranNgoc.Services.Dto;
using TranNgoc.Services.Interfaces;

namespace TranNgoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterDataController : ControllerBase
    {
        private readonly IMasterDataPdfImportService _importService;
        public MasterDataController(IMasterDataPdfImportService importService)
        {
            _importService = importService;
        }

        [HttpPost("import-pdf")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportPdf([FromForm] ImportMasterDataRequest request)
        {
            var totalRows = await _importService.ImportFromPdfAsync(
                request.File,
                request.ObjectId
            );

            return Ok(new
            {
                isSuccess = true,
                message = "Import dữ liệu master từ PDF thành công.",
                totalRows
            });
        }
    }
}

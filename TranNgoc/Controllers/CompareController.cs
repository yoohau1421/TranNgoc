using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TranNgoc.Services;
using TranNgoc.Services.Dto;
using TranNgoc.Services.Interfaces;

namespace TranNgoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompareController : ControllerBase
    {
        private readonly ICompareExcelService _compareExcelService;
        public CompareController(ICompareExcelService excelImportService)
        {
            _compareExcelService = excelImportService;
        }

        [HttpPost("import-excel")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportExcel([FromForm] ImportCompareExcelRequest request)
        {
            var rows = await _compareExcelService.ImportExcelAsync(request.File);

            return Ok(new
            {
                isSuccess = true,
                message = "Import Excel thành công.",
                totalRows = rows.Count,
                validRows = rows.Count(x => x.IsValid),
                invalidRows = rows.Count(x => !x.IsValid),
                data = rows
            });
        }

        [HttpPost("review")]
        public async Task<IActionResult> Review(IFormFile file, [FromForm] long objectId)
        {
            var result = await _compareExcelService.CompareReview(file, objectId);
            return Ok(result);
        }

        [HttpPost("export")]
        public async Task<IActionResult> Export(IFormFile file, [FromForm] long objectId)
        {
            var result = await _compareExcelService.ExportExcel(file, objectId);

            if (!result.IsSuccess || result.Data == null)
                return BadRequest(result);

            return File(
                result.Data.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.Data.FileName
            );
        }
    }
}

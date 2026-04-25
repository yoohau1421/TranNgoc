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

        [HttpPost("check-excel")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CheckExcel([FromForm] ImportCompareExcelRequest request)
        {
            var result = await _compareExcelService.CompareAsync(request.File, request.ObjectId);

            Response.Headers.Add("X-Message", Uri.EscapeDataString(result.Message));
            Response.Headers.Add("X-Total-Rows", result.TotalRows.ToString());
            Response.Headers.Add("X-Success-Rows", result.SuccessRows.ToString());
            Response.Headers.Add("X-Error-Rows", result.ErrorRows.ToString());

            return File(
                result.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName
            );
        }
    }
}

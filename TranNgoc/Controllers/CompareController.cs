using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TranNgoc.Services.Dto;
using TranNgoc_BE.Services.Dto.ExcelCompare;
using TranNgoc_BE.Services.Interfaces;

namespace TranNgoc_BE.Controllers
{
    [Route("api/compare")]
    [ApiController]
    public class CompareController : ControllerBase
    {
        private readonly ICompareExcelService _compareExcelService;

        public CompareController(ICompareExcelService compareExcelService)
        {
            _compareExcelService = compareExcelService;
        }

        [HttpPost("review")]
        public async Task<IActionResult> CompareReview([FromForm] ImportCompareExcelRequest request)
        {
            var result = await _compareExcelService.CompareReview(request.File, request.ObjectId);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("export")]
        public async Task<IActionResult> ExportExcel([FromForm] ImportCompareExcelRequest request)
        {
            var result = await _compareExcelService.ExportExcel(request.File, request.ObjectId);

            if (!result.IsSuccess)
                return BadRequest(result);

            if (result.Data == null || result.Data.FileBytes == null)
                return BadRequest(new BaseResponse<object>
                {
                    IsSuccess = false,
                    Message = "Không tạo được file kết quả."
                });

            return File(
                result.Data.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.Data.FileName
            );
        }

        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates()
        {
            var result = await _compareExcelService.GetTemplateOptionsAsync();

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("template/sample/{objectId}")]
        public async Task<IActionResult> DownloadSampleFile(long objectId)
        {
            var result = await _compareExcelService.DownloadSampleFile(objectId);

            if (!result.IsSuccess)
                return BadRequest(result);

            if (result.Data == null || result.Data.FileBytes == null)
                return BadRequest(new BaseResponse<object>
                {
                    IsSuccess = false,
                    Message = "Không tạo được file mẫu."
                });

            return File(
                result.Data.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.Data.FileName
            );
        }
    }
}

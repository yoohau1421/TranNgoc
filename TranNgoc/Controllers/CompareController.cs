using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TranNgoc.Services;
using TranNgoc.Services.Dto;
using TranNgoc.Services.Interfaces;

namespace TranNgoc.Controllers
{
    /// <summary>
    /// Module đối soát dữ liệu Excel
    /// </summary>
    /// <remarks>
    /// Cung cấp các API phục vụ quy trình:
    /// 
    /// 1. Import dữ liệu từ file Excel
    /// 2. Kiểm tra và đối soát dữ liệu với hệ thống
    /// 3. Xuất file Excel kết quả đối soát
    /// 
    /// Áp dụng cho các nghiệp vụ kiểm tra dữ liệu đầu vào trước khi ghi nhận vào hệ thống.
    /// </remarks>
    [Route("api/compare")]
    [ApiController]
    public class CompareController : ControllerBase
    {
        private readonly ICompareExcelService _compareExcelService;
        public CompareController(ICompareExcelService excelImportService)
        {
            _compareExcelService = excelImportService;
        }

        /// <summary>
        /// Import file Excel để kiểm tra dữ liệu ban đầu
        /// </summary>
        /// <remarks>
        /// API này dùng để đọc file Excel và validate dữ liệu.
        /// 
        /// - Trả về danh sách dòng hợp lệ / không hợp lệ
        /// - Không thực hiện đối soát
        /// </remarks>
        /// <param name="request">File Excel cần import</param>
        /// <returns>Danh sách dữ liệu đã đọc từ Excel</returns>
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

        /// <summary>
        /// Đối soát dữ liệu Excel với hệ thống
        /// </summary>
        /// <remarks>
        /// ### Mô tả:
        /// API thực hiện đối soát dữ liệu từ file Excel với dữ liệu trong hệ thống theo objectId.
        /// 
        /// ### Quy trình:
        /// 1. Đọc file Excel
        /// 2. Lấy dữ liệu hệ thống theo objectId
        /// 3. So sánh từng dòng:
        ///     - Khớp dữ liệu
        ///     - Không khớp (thiếu / sai)
        /// 
        /// ### Kết quả trả về:
        /// - Danh sách dòng đã đối soát
        /// - Trạng thái từng dòng:
        ///     - Match
        ///     - Not Match
        ///     - Missing
        /// 
        /// ### Lưu ý:
        /// - Không xuất file
        /// - Dùng để hiển thị kết quả trên UI trước khi export
        /// </remarks>
        /// <param name="request">
        /// Thông tin request bao gồm:
        /// - File: file Excel cần đối soát
        /// - ObjectId: Id dữ liệu hệ thống để so sánh
        /// </param>
        /// <returns>
        /// Kết quả đối soát dạng JSON:
        /// - Tổng số dòng
        /// - Số dòng khớp / không khớp
        /// - Chi tiết từng dòng
        /// </returns>
        [HttpPost("review")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Review([FromForm] ImportCompareExcelRequest request)
        {
            var result = await _compareExcelService.CompareReview(request.File, request.ObjectId);
            return Ok(result);
        }

        /// <summary>
        /// Xuất file Excel kết quả đối soát
        /// </summary>
        /// <remarks>
        /// ### Mô tả:
        /// API thực hiện đối soát dữ liệu và xuất file Excel kết quả.
        /// 
        /// ### Quy trình:
        /// 1. Đọc file Excel đầu vào
        /// 2. Thực hiện đối soát với hệ thống
        /// 3. Sinh file Excel kết quả
        /// 
        /// ### File kết quả bao gồm:
        /// - Dữ liệu gốc từ Excel
        /// - Kết quả đối soát từng dòng
        /// - Trạng thái (Match / Not Match)
        /// - Ghi chú lỗi (nếu có)
        /// 
        /// ### Lưu ý:
        /// - File trả về dạng binary (download trực tiếp)
        /// - Định dạng: .xlsx
        /// </remarks>
        /// <param name="request">
        /// Thông tin request bao gồm:
        /// - File: file Excel đầu vào
        /// - ObjectId: Id dữ liệu hệ thống để đối soát
        /// </param>
        /// <returns>
        /// File Excel kết quả đối soát
        /// </returns>
        [HttpPost("export")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Export([FromForm] ImportCompareExcelRequest request)
        {
            var result = await _compareExcelService.ExportExcel(request.File, request.ObjectId);

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

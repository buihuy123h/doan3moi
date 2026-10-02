using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using doanki3API.Constants;
using doanki3API.Data;
using doanki3API.DTOs.Admin;
using doanki3API.Models;
using doanki3API.Services.Notifications;

namespace doanki3API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    // Xem/trả lời/xoá feedback là việc của Manager (spec); riêng CreateFeedback
    // (POST) mở cho mọi tài khoản đã đăng nhập để KHÁCH HÀNG gửi phản hồi được.
    public class FeedbackController : ControllerBase
    {
        private readonly MyAppDbContext _context;
        private readonly NotificationService _notifications;

        public FeedbackController(MyAppDbContext context, NotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

    [HttpGet]
    [Authorize(Roles = EmployeeRoles.Manager)]
    public async Task<ActionResult<IEnumerable<Feedback>>> GetFeedbacks()
        {
            return await _context.Feedbacks
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Feedback>> CreateFeedback([FromBody] CreateFeedbackRequest request)
        {
            // Danh mục và trạng thái phải khớp CHECK constraint, nếu không SaveChanges sẽ nổ lỗi 500.
            var category = FeedbackCategories.All.FirstOrDefault(
                value => string.Equals(value, request.Category?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (category is null)
            {
                return BadRequest(new
                {
                    message = $"Danh mục không hợp lệ. Cho phép: {string.Join(", ", FeedbackCategories.All)}"
                });
            }

            var status = FeedbackStatuses.All.FirstOrDefault(
                value => string.Equals(value, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? FeedbackStatuses.New;

            // Ba khoá ngoại đều cho phép NULL: chỉ gán khi bản ghi thật sự tồn tại,
            // tránh việc một mã sai làm hỏng cả lần gửi phản hồi.
            // DB lưu AccountId dạng có gạch ("D064-000000000011") trong khi client
            // có thể gửi không dấu gạch — so khớp cả hai rồi giữ đúng giá trị đang lưu.
            var accountId = NormalizeAccountId(request.AccountId);
            if (accountId is not null)
            {
                var clean = accountId;
                var dashed = clean.Length == 16 && "DBT".Contains(clean[0])
                    ? $"{clean[..4]}-{clean[4..]}"
                    : clean;
                accountId = await _context.CustomerConnections
                    .Where(c => c.AccountId == clean || c.AccountId == dashed)
                    .Select(c => c.AccountId)
                    .FirstOrDefaultAsync();
            }

            var orderId = Normalize(request.OrderId);
            if (orderId is not null && !await _context.Orders.AnyAsync(o => o.OrderId == orderId))
            {
                orderId = null;
            }

            var customerId = Normalize(request.CustomerId);
            if (customerId is not null && !await _context.Customers.AnyAsync(c => c.CustomerId == customerId))
            {
                customerId = null;
            }

            var feedback = new Feedback
            {
                // Lấy 10 chữ số CUỐI của ticks: 10 chữ số đầu gần như không đổi nên sẽ trùng khoá chính.
                FeedbackId = Normalize(request.FeedbackId)
                    ?? $"FB-{DateTime.Now.Ticks.ToString()[10..]}",
                AccountId = accountId,
                OrderId = orderId,
                CustomerId = customerId,
                CustomerName = request.CustomerName.Trim(),
                Rating = Math.Clamp(request.Rating, (byte)1, (byte)5),
                Category = category,
                Message = request.Message.Trim(),
                Status = status,
                CreatedAt = DateTime.Now
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            // Phản hồi mới -> báo ngay cho Quản trị để trả lời khách.
            await _notifications.FeedbackCreatedAsync(
                feedback.FeedbackId,
                feedback.CustomerName,
                feedback.Rating,
                feedback.Message);

            await _notifications.NotifyDataChangedAsync("feedbacks", feedback.FeedbackId);

            return Ok(feedback);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = EmployeeRoles.Manager)]
        public async Task<IActionResult> UpdateFeedback(string id, [FromBody] UpdateFeedbackRequest request)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null) return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y feedback" });

            var response = Normalize(request.Response);

            // RespondedBy có khoá ngoại sang Employee: mã không có thật sẽ làm SaveChanges nổ lỗi FK.
            var respondedBy = Normalize(request.RespondedBy);
            if (respondedBy is not null &&
                !await _context.Employees.AnyAsync(e => e.EmployeeId == respondedBy))
            {
                respondedBy = null;
            }

            feedback.Response = response;
            feedback.RespondedBy = respondedBy;

            if (response is not null)
            {
                feedback.Status = FeedbackStatuses.Resolved;
                feedback.RespondedAt = DateTime.Now;
            }
            else
            {
                feedback.Status = FeedbackStatuses.All.FirstOrDefault(
                    value => string.Equals(value, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase))
                    ?? feedback.Status;
            }

            await _context.SaveChangesAsync();

            if (response is not null)
            {
                // Trả lời xong -> khách nhận được thông báo ngay trong cổng khách hàng.
                await _notifications.FeedbackAnsweredAsync(feedback.FeedbackId, feedback.AccountId, response);
            }

            await _notifications.NotifyDataChangedAsync("feedbacks", feedback.FeedbackId);

            return Ok(feedback);
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>
        /// Giao diện hiển thị "B064-000000000002" còn CSDL lưu "B064000000000002",
        /// nên phải bỏ dấu gạch trước khi so khớp khoá ngoại.
        /// </summary>
        private static string? NormalizeAccountId(string? value)
        {
            var cleaned = new string((value ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            return cleaned.Length == 0 ? null : cleaned;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = EmployeeRoles.Manager)]
        public async Task<IActionResult> DeleteFeedback(string id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null) return NotFound(new { message = "Không tìm thấy feedback" });

            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa phản hồi" });
        }
    }
}

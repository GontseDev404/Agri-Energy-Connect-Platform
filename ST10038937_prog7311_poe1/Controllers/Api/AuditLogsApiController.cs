using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;

namespace ST10038937_prog7311_poe1.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,Employee")]
    public class AuditLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(ApplicationDbContext context, ILogger<AuditLogsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/AuditLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
            [FromQuery] string? userId,
            [FromQuery] string? action,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.AuditLogs
                    .AsNoTracking()
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(a => a.UserId == userId);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(a => a.Action != null && a.Action.Contains(action));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= endDate.Value);
                }

                // Apply pagination
                var totalCount = await query.CountAsync();
                var auditLogs = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var auditLogDtos = auditLogs.Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    Action = a.Action,
                    Details = a.Details,
                    Timestamp = a.Timestamp
                });

                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(auditLogDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return StatusCode(500, new { message = "An error occurred while retrieving audit logs" });
            }
        }

        // GET: api/AuditLogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLogDto>> GetAuditLog(int id)
        {
            try
            {
                var auditLog = await _context.AuditLogs.FindAsync(id);

                if (auditLog == null)
                {
                    return NotFound(new { message = "Audit log not found" });
                }

                var auditLogDto = new AuditLogDto
                {
                    Id = auditLog.Id,
                    UserId = auditLog.UserId,
                    Action = auditLog.Action,
                    Details = auditLog.Details,
                    Timestamp = auditLog.Timestamp
                };

                return Ok(auditLogDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit log {AuditLogId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the audit log" });
            }
        }

        // GET: api/AuditLogs/actions
        [HttpGet("actions")]
        public async Task<ActionResult<IEnumerable<string>>> GetActions()
        {
            try
            {
                var actions = await _context.AuditLogs
                    .Select(a => a.Action)
                    .Where(a => a != null)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToListAsync();

                return Ok(actions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit log actions");
                return StatusCode(500, new { message = "An error occurred while retrieving audit log actions" });
            }
        }

        // GET: api/AuditLogs/users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<string>>> GetUsers()
        {
            try
            {
                var users = await _context.AuditLogs
                    .Select(a => a.UserId)
                    .Where(u => !string.IsNullOrEmpty(u))
                    .Distinct()
                    .OrderBy(u => u)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit log users");
                return StatusCode(500, new { message = "An error occurred while retrieving audit log users" });
            }
        }

        // GET: api/AuditLogs/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<AuditStatisticsDto>> GetStatistics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= endDate.Value);
                }

                var totalLogs = await query.CountAsync();
                var uniqueUsers = await query.Select(a => a.UserId).Distinct().CountAsync();
                var topActions = await query
                    .GroupBy(a => a.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                var topUsers = await query
                    .GroupBy(a => a.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                var statistics = new AuditStatisticsDto
                {
                    TotalLogs = totalLogs,
                    UniqueUsers = uniqueUsers,
                    TopActions = topActions.Select(x => new ActionCountDto { Action = x.Action, Count = x.Count }).ToList(),
                    TopUsers = topUsers.Select(x => new UserCountDto { UserId = x.UserId, Count = x.Count }).ToList()
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving audit statistics" });
            }
        }

        // DELETE: api/AuditLogs/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAuditLog(int id)
        {
            try
            {
                var auditLog = await _context.AuditLogs.FindAsync(id);
                if (auditLog == null)
                {
                    return NotFound(new { message = "Audit log not found" });
                }

                _context.AuditLogs.Remove(auditLog);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit log {AuditLogId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the audit log" });
            }
        }

        // DELETE: api/AuditLogs/bulk
        [HttpDelete("bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAuditLogsBulk(
            [FromQuery] DateTime? beforeDate,
            [FromQuery] string? userId)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (beforeDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp < beforeDate.Value);
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(a => a.UserId == userId);
                }

                var logsToDelete = await query.ToListAsync();
                var count = logsToDelete.Count;

                if (count == 0)
                {
                    return BadRequest(new { message = "No audit logs found matching the criteria" });
                }

                _context.AuditLogs.RemoveRange(logsToDelete);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Successfully deleted {count} audit logs" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit logs in bulk");
                return StatusCode(500, new { message = "An error occurred while deleting audit logs" });
            }
        }
    }

    // DTOs for API
    public class AuditLogDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class AuditStatisticsDto
    {
        public int TotalLogs { get; set; }
        public int UniqueUsers { get; set; }
        public List<ActionCountDto> TopActions { get; set; } = new List<ActionCountDto>();
        public List<UserCountDto> TopUsers { get; set; } = new List<UserCountDto>();
    }

    public class ActionCountDto
    {
        public string? Action { get; set; }
        public int Count { get; set; }
    }

    public class UserCountDto
    {
        public string UserId { get; set; } = string.Empty;
        public int Count { get; set; }
    }
} 
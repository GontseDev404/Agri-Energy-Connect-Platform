using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using Microsoft.AspNetCore.Identity;
using ST10038937_prog7311_poe1.Services;

namespace ST10038937_prog7311_poe1.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ForumController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<ForumController> _logger;

        public ForumController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IAuditService auditService,
            ILogger<ForumController> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        // GET: api/Forum
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ForumPostDto>>> GetForumPosts(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.ForumPosts
                    .Include(p => p.User)
                    .Include(p => p.Replies)
                    .AsNoTracking()
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));
                }

                // Apply pagination
                var totalCount = await query.CountAsync();
                var posts = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var postDtos = posts.Select(p => new ForumPostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content,
                    AuthorName = p.User?.UserName ?? "Anonymous",
                    CreatedAt = p.CreatedAt,
                    ReplyCount = p.Replies?.Count ?? 0
                });

                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(postDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving forum posts");
                return StatusCode(500, new { message = "An error occurred while retrieving forum posts" });
            }
        }

        // GET: api/Forum/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ForumPostDetailDto>> GetForumPost(int id)
        {
            try
            {
                var post = await _context.ForumPosts
                    .Include(p => p.User)
                    .Include(p => p.Replies)
                        .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (post == null)
                {
                    return NotFound(new { message = "Forum post not found" });
                }

                var postDetailDto = new ForumPostDetailDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    Content = post.Content,
                    AuthorName = post.User?.UserName ?? "Anonymous",
                    CreatedAt = post.CreatedAt,
                    Replies = post.Replies?.Select(r => new ReplyDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        AuthorName = r.User?.UserName ?? "Anonymous",
                        CreatedAt = r.CreatedAt
                    }).OrderBy(r => r.CreatedAt).ToList() ?? new List<ReplyDto>()
                };

                return Ok(postDetailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving forum post {PostId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the forum post" });
            }
        }

        // POST: api/Forum
        [HttpPost]
        public async Task<ActionResult<ForumPostDto>> CreateForumPost([FromBody] CreateForumPostDto createPostDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var post = new ForumPost
                {
                    Title = createPostDto.Title,
                    Content = createPostDto.Content,
                    User = user,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ForumPosts.Add(post);
                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(user.Id, "Forum Post Created", $"Post: {post.Title}, ID: {post.Id}");

                var postDto = new ForumPostDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    Content = post.Content,
                    AuthorName = user.UserName ?? "Anonymous",
                    CreatedAt = post.CreatedAt,
                    ReplyCount = 0
                };

                return CreatedAtAction(nameof(GetForumPost), new { id = post.Id }, postDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating forum post");
                return StatusCode(500, new { message = "An error occurred while creating the forum post" });
            }
        }

        // PUT: api/Forum/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateForumPost(int id, [FromBody] UpdateForumPostDto updatePostDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var post = await _context.ForumPosts.FindAsync(id);
                if (post == null)
                {
                    return NotFound(new { message = "Forum post not found" });
                }

                // Check if user has permission to edit this post
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                if (!User.IsInRole("Admin") && post.User?.Id != user.Id)
                {
                    return Forbid();
                }

                // Update post properties
                post.Title = updatePostDto.Title ?? post.Title;
                post.Content = updatePostDto.Content ?? post.Content;

                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(user.Id, "Forum Post Updated", $"Post: {post.Title}, ID: {post.Id}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating forum post {PostId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the forum post" });
            }
        }

        // DELETE: api/Forum/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteForumPost(int id)
        {
            try
            {
                var post = await _context.ForumPosts
                    .Include(p => p.Replies)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (post == null)
                {
                    return NotFound(new { message = "Forum post not found" });
                }

                // Check if user has permission to delete this post
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                if (!User.IsInRole("Admin") && post.User?.Id != user.Id)
                {
                    return Forbid();
                }

                _context.ForumPosts.Remove(post);
                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(user.Id, "Forum Post Deleted", $"Post: {post.Title}, ID: {post.Id}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting forum post {PostId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the forum post" });
            }
        }

        // POST: api/Forum/5/replies
        [HttpPost("{id}/replies")]
        public async Task<ActionResult<ReplyDto>> AddReply(int id, [FromBody] CreateReplyDto createReplyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var post = await _context.ForumPosts.FindAsync(id);
                if (post == null)
                {
                    return NotFound(new { message = "Forum post not found" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var reply = new PostReply
                {
                    Content = createReplyDto.Content,
                    User = user,
                    ForumPostId = id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PostReplies.Add(reply);
                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(user.Id, "Forum Reply Created", $"Reply to post: {post.Title}, Reply ID: {reply.Id}");

                var replyDto = new ReplyDto
                {
                    Id = reply.Id,
                    Content = reply.Content,
                    AuthorName = user.UserName ?? "Anonymous",
                    CreatedAt = reply.CreatedAt
                };

                return CreatedAtAction(nameof(GetForumPost), new { id = post.Id }, replyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reply for post {PostId}", id);
                return StatusCode(500, new { message = "An error occurred while creating the reply" });
            }
        }

        // DELETE: api/Forum/replies/5
        [HttpDelete("replies/{id}")]
        public async Task<IActionResult> DeleteReply(int id)
        {
            try
            {
                var reply = await _context.PostReplies.FindAsync(id);
                if (reply == null)
                {
                    return NotFound(new { message = "Reply not found" });
                }

                // Check if user has permission to delete this reply
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                if (!User.IsInRole("Admin") && reply.User?.Id != user.Id)
                {
                    return Forbid();
                }

                _context.PostReplies.Remove(reply);
                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(user.Id, "Forum Reply Deleted", $"Reply ID: {reply.Id}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reply {ReplyId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the reply" });
            }
        }
    }

    // DTOs for API
    public class ForumPostDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ReplyCount { get; set; }
    }

    public class ForumPostDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ReplyDto> Replies { get; set; } = new List<ReplyDto>();
    }

    public class ReplyDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateForumPostDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateForumPostDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
    }

    public class CreateReplyDto
    {
        public string Content { get; set; } = string.Empty;
    }
} 
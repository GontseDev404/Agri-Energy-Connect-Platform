using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace ST10038937_prog7311_poe1.Controllers
{
    [Authorize]
    public class ForumController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        public ForumController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        // GET: Forum
                public async Task<IActionResult> Index(string search)
        {
            string cacheKey = $"forumPosts_{search}";
            if (!_cache.TryGetValue(cacheKey, out List<ForumPost> posts))
            {
                var postsQuery = _context.ForumPosts.Include(p => p.User).AsNoTracking().OrderByDescending(p => p.CreatedAt).AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    postsQuery = postsQuery.Where(p => p.Title.Contains(search) || p.Content.Contains(search));
                }
                posts = await postsQuery.ToListAsync();
                _cache.Set(cacheKey, posts, TimeSpan.FromMinutes(1));
            }
            ViewBag.Search = search;
            return View(posts);
        }

        // GET: Forum/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Forum/Create
                [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Content")] ForumPost post)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge(); // Not logged in
                }

                post.User = user;
                post.CreatedAt = DateTime.UtcNow;

                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        // GET: Forum/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.ForumPosts
                .Include(p => p.User)
                .Include(p => p.Replies)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReply(int ForumPostId, string Content)
        {
            if (string.IsNullOrEmpty(Content))
            {
                return RedirectToAction("Details", new { id = ForumPostId });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var reply = new PostReply
            {
                Content = Content,
                CreatedAt = DateTime.UtcNow,
                UserId = user.Id,
                ForumPostId = ForumPostId
            };

            _context.PostReplies.Add(reply);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = ForumPostId });
        }

        // GET: Forum/Delete/5
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var forumPost = await _context.ForumPosts
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (forumPost == null)
            {
                return NotFound();
            }

            return View(forumPost);
        }

        // POST: Forum/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var forumPost = await _context.ForumPosts.FindAsync(id);
            if (forumPost != null)
            {
                var replies = _context.PostReplies.Where(r => r.ForumPostId == id);
                _context.PostReplies.RemoveRange(replies);
                _context.ForumPosts.Remove(forumPost);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Forum/DeleteReply/5
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> DeleteReply(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reply = await _context.PostReplies
                .Include(r => r.User)
                .Include(r => r.Post)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reply == null)
            {
                return NotFound();
            }

            return View(reply);
        }

        // POST: Forum/DeleteReply/5
        [HttpPost, ActionName("DeleteReply")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> DeleteReplyConfirmed(int id)
        {
            var reply = await _context.PostReplies.FindAsync(id);
            if (reply != null)
            {
                var forumPostId = reply.ForumPostId;
                _context.PostReplies.Remove(reply);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = forumPostId });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

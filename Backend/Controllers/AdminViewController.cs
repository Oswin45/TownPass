using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// MVC Controller for serving Admin web interface views
    /// Routes: /Admin/Index, /Admin/CacheStatus, etc.
    /// </summary>
    [Route("Admin")]
    public class AdminViewController : Controller
    {
        /// <summary>
        /// Admin dashboard main page
        /// GET /Admin or /Admin/Index
        /// </summary>
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View("~/Views/Admin/Index.cshtml");
        }

        /// <summary>
        /// Cache status detail page
        /// GET /Admin/CacheStatus
        /// </summary>
        [HttpGet("CacheStatus")]
        public IActionResult CacheStatus()
        {
            return View("~/Views/Admin/CacheStatus.cshtml");
        }

        /// <summary>
        /// Refresh cache page
        /// GET /Admin/RefreshCache
        /// </summary>
        [HttpGet("RefreshCache")]
        public IActionResult RefreshCache()
        {
            return View("~/Views/Admin/RefreshCache.cshtml");
        }

        /// <summary>
        /// Full update page
        /// GET /Admin/FullUpdate
        /// </summary>
        [HttpGet("FullUpdate")]
        public IActionResult FullUpdate()
        {
            return View("~/Views/Admin/FullUpdate.cshtml");
        }

        /// <summary>
        /// Statistics page
        /// GET /Admin/Statistics
        /// </summary>
        [HttpGet("Statistics")]
        public IActionResult Statistics()
        {
            return View("~/Views/Admin/Statistics.cshtml");
        }
    }
}

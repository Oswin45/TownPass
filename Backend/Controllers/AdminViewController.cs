using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// MVC Controller for serving Admin web interface views
    /// Routes: /Admin/Index, /Admin/TriggerDisaster
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
        /// Disaster trigger page
        /// GET /Admin/TriggerDisaster
        /// </summary>
        [HttpGet("TriggerDisaster")]
        public IActionResult TriggerDisaster()
        {
            return View("~/Views/Admin/TriggerDisaster.cshtml");
        }
    }
}

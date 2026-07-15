using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ATMRouter.Models;

namespace ATMRouter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AtmsController : ControllerBase
    {
        private readonly AtmrouterContext _context;

        public AtmsController(AtmrouterContext context)
        {
            _context = context;
        }

        // GET: api/Atms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAtms()
        {
            // Fetch ATMs and map them to a structure matching the frontend expectations
            var atms = await _context.Atms
                .Include(a => a.Status)
                .Include(a => a.Transactions)
                .Select(a => new
                {
                    id = a.Atmid,
                    name = "ATM " + a.Atmcode, // Example mapping
                    type = "atm",
                    address = "Lat: " + a.Latitude + ", Lng: " + a.Longitude, // Example mapping, needs actual address field ideally
                    lat = a.Latitude,
                    lng = a.Longitude,
                    status = a.Status.StatusName.ToLower(), // Assumes status matches "ok", "warn", "err" in DB or needs a mapping function
                    cash = 100, // Hardcoded for now, needs logic from CashInventory
                    services = a.Transactions.Select(t => t.TransactionName.ToLower()).ToList(),
                    denoms = new[] { 200, 100, 50 }, // Hardcoded, needs logic
                    tx15 = 5 // Hardcoded, needs logic
                })
                .ToListAsync();

            return Ok(atms);
        }

        // POST: api/Atms/report
        [HttpPost("report")]
        public async Task<IActionResult> ReportIssue([FromBody] ReportIssueDto report)
        {
            if (report == null)
            {
                return BadRequest();
            }

            var atm = await _context.Atms.FindAsync(report.AtmId);
            if (atm == null)
            {
                return NotFound();
            }

            var newReport = new IssueReport
            {
                Atmid = report.AtmId,
                // CategoryId = ..., // Needs logic to map string category to ID
                Description = report.Description,
                NationalId = report.NationalId,
                ReportStatus = "Pending",
                SubmittedAt = DateTime.Now
            };

            _context.IssueReports.Add(newReport);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Report submitted successfully" });
        }
    }

    public class ReportIssueDto
    {
        public int AtmId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
    }
}

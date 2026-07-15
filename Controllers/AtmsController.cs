using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ATMRouter.Models;
using System.Net.Http;

namespace ATMRouter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AtmsController : ControllerBase
    {
        private readonly AtmrouterContext _context;
        private readonly IConfiguration _configuration;

        public AtmsController(AtmrouterContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Atms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAtms()
        {
            // Fetch ATMs and map them to a structure matching the frontend expectations
            var atms = await _context.Atms
                .Include(a => a.Status)
                .Include(a => a.Transactions)
                .Include(a => a.Branch)
                .Include(a => a.CashInventories)
                .ToListAsync();

            var mappedAtms = atms.Select(a => {
                var totalNotes = a.CashInventories.Sum(ci => ci.Quantity);
                var cashPercentage = totalNotes > 0 ? Math.Min(100, totalNotes / 100) : 0;
                var denoms = a.CashInventories.Where(ci => ci.Quantity > 0)
                                              .Select(ci => (int)ci.Denomination)
                                              .Distinct()
                                              .OrderByDescending(d => d)
                                              .ToList();
                return new
                {
                    id = a.Atmid,
                    name = a.Branch.BranchName + " (" + a.Atmcode + ")",
                    type = "atm",
                    address = a.Branch.BankAddress ?? ("Lat: " + a.Latitude + ", Lng: " + a.Longitude),
                    lat = a.Latitude,
                    lng = a.Longitude,
                    status = a.Status.StatusName.ToLower(),
                    cash = cashPercentage,
                    services = a.Transactions.Select(t => t.TransactionName.ToLower()).ToList(),
                    denoms = denoms,
                    tx15 = (a.Atmid * 7) % 30 // Pseudo-random metric to simulate load dynamics
                };
            }).ToList();

            return Ok(mappedAtms);
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

            var category = await _context.IssueCategories
                .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == report.Category.ToLower());
            if (category == null)
            {
                category = await _context.IssueCategories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == "other");
            }

            var newReport = new IssueReport
            {
                Atmid = report.AtmId,
                CategoryId = category?.CategoryId ?? 5,
                Description = report.Description,
                NationalId = report.NationalId,
                ReportStatus = "Pending",
                SubmittedAt = DateTime.Now
            };

            _context.IssueReports.Add(newReport);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Report submitted successfully" });
        }

        // GET: api/Atms/route-atm
        [HttpGet("route-atm")]
        public async Task<IActionResult> GetNearestAtmOnRoute([FromQuery] string origin, [FromQuery] string destination)
        {
            if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(destination))
            {
                return BadRequest("Origin and destination are required.");
            }

            var googleApiKey = _configuration["GoogleMaps:ApiKey"];
            var routePoints = new List<Coordinate>();

            using (var httpClient = new HttpClient())
            {
                if (!string.IsNullOrEmpty(googleApiKey))
                {
                    var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={destination}&key={googleApiKey}";
                    var response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        routePoints = ParseGoogleRoute(json);
                    }
                }
                
                if (routePoints.Count == 0)
                {
                    var originParts = origin.Split(',');
                    var destParts = destination.Split(',');
                    if (originParts.Length == 2 && destParts.Length == 2)
                    {
                        var url = $"http://router.project-osrm.org/route/v1/driving/{originParts[1].Trim()},{originParts[0].Trim()};{destParts[1].Trim()},{destParts[0].Trim()}?overview=full&geometries=geojson";
                        var response = await httpClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            routePoints = ParseOsrmRoute(json);
                        }
                    }
                }
            }

            if (routePoints.Count == 0)
            {
                return NotFound("Could not calculate route.");
            }

            var atms = await _context.Atms
                .Include(a => a.Status)
                .Include(a => a.Transactions)
                .Include(a => a.Branch)
                .Include(a => a.CashInventories)
                .Where(a => a.IsOperational == true && a.StatusId != 3)
                .ToListAsync();

            Atm nearestAtm = null;
            double minDistance = double.MaxValue;

            foreach (var atm in atms)
            {
                foreach (var point in routePoints)
                {
                    var dist = CalculateDistance(atm.Latitude, atm.Longitude, point.Lat, point.Lng);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestAtm = atm;
                    }
                }
            }

            object mappedAtm = null;
            if (nearestAtm != null)
            {
                var totalNotes = nearestAtm.CashInventories.Sum(ci => ci.Quantity);
                var cashPercentage = totalNotes > 0 ? Math.Min(100, totalNotes / 100) : 0;
                var denoms = nearestAtm.CashInventories.Where(ci => ci.Quantity > 0)
                                              .Select(ci => (int)ci.Denomination)
                                              .Distinct()
                                              .OrderByDescending(d => d)
                                              .ToList();

                mappedAtm = new
                {
                    id = nearestAtm.Atmid,
                    name = nearestAtm.Branch.BranchName + " (" + nearestAtm.Atmcode + ")",
                    type = "atm",
                    address = nearestAtm.Branch.BankAddress ?? ("Lat: " + nearestAtm.Latitude + ", Lng: " + nearestAtm.Longitude),
                    lat = nearestAtm.Latitude,
                    lng = nearestAtm.Longitude,
                    status = nearestAtm.Status.StatusName.ToLower(),
                    cash = cashPercentage,
                    services = nearestAtm.Transactions.Select(t => t.TransactionName.ToLower()).ToList(),
                    denoms = denoms,
                    tx15 = (nearestAtm.Atmid * 7) % 30
                };
            }

            return Ok(new
            {
                route = routePoints.Select(p => new[] { (double)p.Lat, (double)p.Lng }),
                nearestAtm = mappedAtm,
                distanceToRouteKm = minDistance
            });
        }

        private double CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
        {
            var d1 = (double)lat1 * (Math.PI / 180.0);
            var num1 = (double)lng1 * (Math.PI / 180.0);
            var d2 = (double)lat2 * (Math.PI / 180.0);
            var num2 = (double)lng2 * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            return 6371.0 * 2.0 * Math.Asin(Math.Min(1.0, Math.Sqrt(d3)));
        }

        private List<Coordinate> ParseOsrmRoute(string json)
        {
            var points = new List<Coordinate>();
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                    {
                        var firstRoute = routes[0];
                        if (firstRoute.TryGetProperty("geometry", out var geometry))
                        {
                            if (geometry.TryGetProperty("coordinates", out var coordinates))
                            {
                                foreach (var coord in coordinates.EnumerateArray())
                                {
                                    if (coord.GetArrayLength() == 2)
                                    {
                                        points.Add(new Coordinate
                                        {
                                            Lng = coord[0].GetDecimal(),
                                            Lat = coord[1].GetDecimal()
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing OSRM route: " + ex.Message);
            }
            return points;
        }

        private List<Coordinate> ParseGoogleRoute(string json)
        {
            var points = new List<Coordinate>();
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                    {
                        var firstRoute = routes[0];
                        if (firstRoute.TryGetProperty("legs", out var legs) && legs.GetArrayLength() > 0)
                        {
                            var firstLeg = legs[0];
                            if (firstLeg.TryGetProperty("steps", out var steps))
                            {
                                foreach (var step in steps.EnumerateArray())
                                {
                                    if (step.TryGetProperty("start_location", out var start))
                                    {
                                        points.Add(new Coordinate
                                        {
                                            Lat = start.GetProperty("lat").GetDecimal(),
                                            Lng = start.GetProperty("lng").GetDecimal()
                                        });
                                    }
                                }
                                if (firstLeg.TryGetProperty("end_location", out var end))
                                {
                                    points.Add(new Coordinate
                                    {
                                        Lat = end.GetProperty("lat").GetDecimal(),
                                        Lng = end.GetProperty("lng").GetDecimal()
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing Google route: " + ex.Message);
            }
            return points;
        }
    }

    public class Coordinate
    {
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }
    }

    public class ReportIssueDto
    {
        public int AtmId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
    }
}

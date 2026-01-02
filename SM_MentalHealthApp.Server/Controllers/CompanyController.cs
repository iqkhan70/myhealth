using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(JournalDbContext context, ILogger<CompanyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all active companies (for dropdowns)
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<Company>>> GetActiveCompanies()
        {
            try
            {
                var companies = await _context.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching active companies");
                return StatusCode(500, $"Error fetching companies: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all companies (including inactive) - Admin only
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Company>>> GetAllCompanies()
        {
            try
            {
                var companies = await _context.Companies
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all companies");
                return StatusCode(500, $"Error fetching companies: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a company by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Company>> GetCompany(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound("Company not found.");
                }

                return Ok(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company {CompanyId}", id);
                return StatusCode(500, $"Error fetching company: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a new company - Admin only
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Company>> CreateCompany([FromBody] CreateCompanyRequest request)
        {
            try
            {
                // Check if company name already exists
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Name == request.Name);

                if (existingCompany != null)
                {
                    return BadRequest("A company with this name already exists.");
                }

                var company = new Company
                {
                    Name = request.Name,
                    Description = request.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Company {CompanyName} created with ID {CompanyId}", company.Name, company.Id);

                return Ok(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, $"Error creating company: {ex.Message}");
            }
        }

        /// <summary>
        /// Update a company - Admin only
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Company>> UpdateCompany(int id, [FromBody] UpdateCompanyRequest request)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound("Company not found.");
                }

                // Check if new name conflicts with existing company
                if (!string.IsNullOrWhiteSpace(request.Name) && request.Name != company.Name)
                {
                    var existingCompany = await _context.Companies
                        .FirstOrDefaultAsync(c => c.Name == request.Name && c.Id != id);

                    if (existingCompany != null)
                    {
                        return BadRequest("A company with this name already exists.");
                    }

                    company.Name = request.Name;
                }

                if (request.Description != null)
                {
                    company.Description = request.Description;
                }

                if (request.IsActive.HasValue)
                {
                    company.IsActive = request.IsActive.Value;
                }

                company.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company {CompanyId} updated", id);

                return Ok(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                return StatusCode(500, $"Error updating company: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete (deactivate) a company - Admin only
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteCompany(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound("Company not found.");
                }

                // Check if company has any active users
                var hasUsers = await _context.Users
                    .AnyAsync(u => u.CompanyId == id && u.IsActive);

                if (hasUsers)
                {
                    return BadRequest("Cannot delete company that has active users. Deactivate it instead.");
                }

                // Soft delete by deactivating
                company.IsActive = false;
                company.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Company {CompanyId} deactivated", id);

                return Ok(new { message = "Company deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company {CompanyId}", id);
                return StatusCode(500, $"Error deleting company: {ex.Message}");
            }
        }
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor,Admin")]
    public class KnowledgeBaseController : BaseController
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly ILogger<KnowledgeBaseController> _logger;

        public KnowledgeBaseController(
            IKnowledgeBaseService knowledgeBaseService,
            ILogger<KnowledgeBaseController> logger)
        {
            _knowledgeBaseService = knowledgeBaseService;
            _logger = logger;
        }

        /// <summary>
        /// Get all active categories
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<List<KnowledgeBaseCategory>>> GetCategories()
        {
            try
            {
                var categories = await _knowledgeBaseService.GetActiveCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge base categories");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all active entries, optionally filtered by category
        /// </summary>
        [HttpGet("entries")]
        public async Task<ActionResult<List<KnowledgeBaseEntry>>> GetEntries([FromQuery] int? categoryId = null)
        {
            try
            {
                var entries = await _knowledgeBaseService.GetActiveEntriesAsync(categoryId);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge base entries");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get a specific entry by ID
        /// </summary>
        [HttpGet("entries/{id}")]
        public async Task<ActionResult<KnowledgeBaseEntry>> GetEntry(int id)
        {
            try
            {
                var entry = await _knowledgeBaseService.GetEntryByIdAsync(id);
                if (entry == null)
                    return NotFound();

                return Ok(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge base entry {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a new knowledge base entry
        /// </summary>
        [HttpPost("entries")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<KnowledgeBaseEntry>> CreateEntry([FromBody] KnowledgeBaseEntry entry)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    entry.CreatedByUserId = userId.Value;
                }

                var created = await _knowledgeBaseService.CreateEntryAsync(entry);
                return CreatedAtAction(nameof(GetEntry), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating knowledge base entry");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update an existing knowledge base entry
        /// </summary>
        [HttpPut("entries/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<KnowledgeBaseEntry>> UpdateEntry(int id, [FromBody] KnowledgeBaseEntry entry)
        {
            try
            {
                if (id != entry.Id)
                    return BadRequest("Entry ID mismatch");

                var existing = await _knowledgeBaseService.GetEntryByIdAsync(id);
                if (existing == null)
                    return NotFound();

                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    entry.UpdatedByUserId = userId.Value;
                }

                var updated = await _knowledgeBaseService.UpdateEntryAsync(entry);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating knowledge base entry {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete (soft delete) a knowledge base entry
        /// </summary>
        [HttpDelete("entries/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteEntry(int id)
        {
            try
            {
                var success = await _knowledgeBaseService.DeleteEntryAsync(id);
                if (!success)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting knowledge base entry {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Create a new category
        /// </summary>
        [HttpPost("categories")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<KnowledgeBaseCategory>> CreateCategory([FromBody] KnowledgeBaseCategory category)
        {
            try
            {
                var created = await _knowledgeBaseService.CreateCategoryAsync(category);
                return CreatedAtAction(nameof(GetCategories), null, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating knowledge base category");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Update an existing category
        /// </summary>
        [HttpPut("categories/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<KnowledgeBaseCategory>> UpdateCategory(int id, [FromBody] KnowledgeBaseCategory category)
        {
            try
            {
                if (id != category.Id)
                    return BadRequest("Category ID mismatch");

                var existing = await _knowledgeBaseService.GetCategoryByIdAsync(id);
                if (existing == null)
                    return NotFound();

                var updated = await _knowledgeBaseService.UpdateCategoryAsync(category);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating knowledge base category {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete (soft delete) a category
        /// </summary>
        [HttpDelete("categories/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            try
            {
                var success = await _knowledgeBaseService.DeleteCategoryAsync(id);
                if (!success)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting knowledge base category {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}


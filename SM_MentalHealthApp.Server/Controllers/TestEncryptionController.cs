using Microsoft.AspNetCore.Mvc;
using SM_MentalHealthApp.Server.Services;

namespace SM_MentalHealthApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestEncryptionController : ControllerBase
    {
        private readonly IPiiEncryptionService _encryptionService;

        public TestEncryptionController(IPiiEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        [HttpGet("test")]
        public IActionResult TestEncryption()
        {
            var testDate = new DateTime(1990, 5, 15);
            var encrypted = _encryptionService.EncryptDateTime(testDate);
            var decrypted = _encryptionService.DecryptDateTime(encrypted);
            
            return Ok(new
            {
                Original = testDate.ToString("O"),
                Encrypted = encrypted,
                Decrypted = decrypted.ToString("O"),
                Success = decrypted == testDate,
                DecryptedDate = decrypted
            });
        }

        [HttpPost("decrypt")]
        public IActionResult DecryptTest([FromBody] DecryptRequest request)
        {
            try
            {
                var decrypted = _encryptionService.DecryptDateTime(request.EncryptedValue);
                return Ok(new
                {
                    Encrypted = request.EncryptedValue,
                    Decrypted = decrypted.ToString("O"),
                    DecryptedDate = decrypted,
                    Success = decrypted != DateTime.MinValue
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class DecryptRequest
    {
        public string EncryptedValue { get; set; } = string.Empty;
    }
}


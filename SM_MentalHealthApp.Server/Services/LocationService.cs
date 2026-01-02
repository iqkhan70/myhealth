using Microsoft.EntityFrameworkCore;
using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;

namespace SM_MentalHealthApp.Server.Services
{
    public interface ILocationService
    {
        Task<(decimal? Latitude, decimal? Longitude)?> GetLatLonFromZipCodeAsync(string zipCode);
        Task<bool> UpdateUserLocationFromZipCodeAsync(int userId, string zipCode);
    }

    public class LocationService : ILocationService
    {
        private readonly JournalDbContext _context;
        private readonly ILogger<LocationService> _logger;

        public LocationService(JournalDbContext context, ILogger<LocationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lookup latitude and longitude for a ZIP code
        /// </summary>
        public async Task<(decimal? Latitude, decimal? Longitude)?> GetLatLonFromZipCodeAsync(string zipCode)
        {
            if (string.IsNullOrWhiteSpace(zipCode))
                return null;

            try
            {
                var zipLookup = await _context.ZipCodeLookups
                    .FirstOrDefaultAsync(z => z.ZipCode == zipCode);

                if (zipLookup == null)
                {
                    _logger.LogWarning("ZIP code {ZipCode} not found in lookup table", zipCode);
                    return null;
                }

                return (zipLookup.Latitude, zipLookup.Longitude);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up ZIP code {ZipCode}", zipCode);
                return null;
            }
        }

        /// <summary>
        /// Update a user's location (lat/lon) based on their ZIP code
        /// </summary>
        public async Task<bool> UpdateUserLocationFromZipCodeAsync(int userId, string zipCode)
        {
            if (string.IsNullOrWhiteSpace(zipCode))
                return false;

            try
            {
                // Use FirstOrDefaultAsync instead of FindAsync to ensure we get the latest data
                // and handle cases where the entity might already be tracked
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);
                
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found when updating location", userId);
                    return false;
                }

                var latLon = await GetLatLonFromZipCodeAsync(zipCode);
                if (!latLon.HasValue)
                {
                    _logger.LogWarning("ZIP code {ZipCode} not found in lookup table for user {UserId}", zipCode, userId);
                    return false;
                }

                user.ZipCode = zipCode;
                user.Latitude = latLon.Value.Latitude;
                user.Longitude = latLon.Value.Longitude;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated location for user {UserId}: ZIP {ZipCode}, Lat {Latitude}, Lon {Longitude}", 
                    userId, zipCode, latLon.Value.Latitude, latLon.Value.Longitude);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user location for user {UserId} with ZIP {ZipCode}", userId, zipCode);
                return false;
            }
        }
    }
}


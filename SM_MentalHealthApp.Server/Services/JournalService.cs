using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Services
{
    public class JournalService
    {
        private readonly JournalDbContext _context;
        private readonly HuggingFaceService _huggingFaceService;

        public JournalService(JournalDbContext context, HuggingFaceService huggingFaceService)
        {
            _context = context;
            _huggingFaceService = huggingFaceService;
        }

        public async Task<JournalEntry> ProcessEntry(JournalEntry entry, int userId)
        {
            // Ensure the entry is associated with the user
            entry.UserId = userId;
            entry.EnteredByUserId = null; // Patient entered for themselves

            var (response, mood) = await _huggingFaceService.AnalyzeEntry(entry.Text);
            entry.AIResponse = response;
            entry.Mood = mood;

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<JournalEntry> ProcessDoctorEntry(JournalEntry entry, int patientId, int doctorId)
        {
            // Ensure the entry is associated with the patient and track who entered it
            entry.UserId = patientId;
            entry.EnteredByUserId = doctorId; // Doctor entered for patient

            var (response, mood) = await _huggingFaceService.AnalyzeEntry(entry.Text);
            entry.AIResponse = response;
            entry.Mood = mood;

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<List<JournalEntry>> GetEntriesForUser(int userId)
        {
            return await _context.JournalEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<JournalEntry>> GetRecentEntriesForUser(int userId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.JournalEntries
                .Where(e => e.UserId == userId && e.CreatedAt >= cutoffDate)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetMoodDistributionForUser(int userId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.JournalEntries
                .Where(e => e.UserId == userId && e.CreatedAt >= cutoffDate && !string.IsNullOrEmpty(e.Mood))
                .GroupBy(e => e.Mood)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<JournalEntry?> GetEntryById(int entryId, int userId)
        {
            return await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);
        }

        public async Task<bool> DeleteEntry(int entryId, int userId)
        {
            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

            if (entry == null)
                return false;

            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        // Legacy method for backward compatibility
        public async Task<List<JournalEntry>> GetEntries()
        {
            return await _context.JournalEntries.OrderByDescending(e => e.CreatedAt).ToListAsync();
        }
    }
}

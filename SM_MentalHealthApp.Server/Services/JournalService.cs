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

        public async Task<JournalEntry> ProcessEntry(JournalEntry entry, int patientId)
        {
            // Ensure the entry is associated with the patient
            entry.PatientId = patientId;
            
            var (response, mood) = await _huggingFaceService.AnalyzeEntry(entry.Text);
            entry.AIResponse = response;
            entry.Mood = mood;
            
            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<List<JournalEntry>> GetEntriesForPatient(int patientId)
        {
            return await _context.JournalEntries
                .Where(e => e.PatientId == patientId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<JournalEntry>> GetRecentEntriesForPatient(int patientId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.JournalEntries
                .Where(e => e.PatientId == patientId && e.CreatedAt >= cutoffDate)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetMoodDistributionForPatient(int patientId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.JournalEntries
                .Where(e => e.PatientId == patientId && e.CreatedAt >= cutoffDate && !string.IsNullOrEmpty(e.Mood))
                .GroupBy(e => e.Mood)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<JournalEntry?> GetEntryById(int entryId, int patientId)
        {
            return await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == entryId && e.PatientId == patientId);
        }

        public async Task<bool> DeleteEntry(int entryId, int patientId)
        {
            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == entryId && e.PatientId == patientId);
            
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

using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace SM_MentalHealthApp.Server.Services
{
    public class PatientService
    {
        private readonly JournalDbContext _context;

        public PatientService(JournalDbContext context)
        {
            _context = context;
        }

        public async Task<Patient> CreatePatientAsync(Patient patient)
        {
            // Check if patient with email already exists
            var existingPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Email == patient.Email);
            
            if (existingPatient != null)
            {
                throw new InvalidOperationException("A patient with this email already exists.");
            }

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<Patient?> GetPatientByIdAsync(int id)
        {
            return await _context.Patients
                .Include(p => p.JournalEntries.OrderByDescending(j => j.CreatedAt))
                .Include(p => p.ChatSessions)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }

        public async Task<Patient?> GetPatientByEmailAsync(string email)
        {
            return await _context.Patients
                .Include(p => p.JournalEntries.OrderByDescending(j => j.CreatedAt))
                .Include(p => p.ChatSessions)
                .FirstOrDefaultAsync(p => p.Email == email && p.IsActive);
        }

        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .Include(p => p.JournalEntries)
                .Include(p => p.ChatSessions)
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }

        public async Task<Patient> UpdatePatientAsync(Patient patient)
        {
            var existingPatient = await _context.Patients.FindAsync(patient.Id);
            if (existingPatient == null)
            {
                throw new InvalidOperationException("Patient not found.");
            }

            existingPatient.FirstName = patient.FirstName;
            existingPatient.LastName = patient.LastName;
            existingPatient.Email = patient.Email;
            existingPatient.DateOfBirth = patient.DateOfBirth;
            existingPatient.Gender = patient.Gender;
            existingPatient.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingPatient;
        }

        public async Task<bool> DeactivatePatientAsync(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return false;
            }

            patient.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Patient> GetOrCreateDemoPatientAsync()
        {
            // For demo purposes, create a default patient if none exists
            var demoPatient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Email == "demo@mentalhealth.app");

            if (demoPatient == null)
            {
                demoPatient = new Patient
                {
                    FirstName = "Demo",
                    LastName = "Patient",
                    Email = "demo@mentalhealth.app",
                    DateOfBirth = DateTime.UtcNow.AddYears(-30),
                    Gender = "Other",
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Patients.Add(demoPatient);
                await _context.SaveChangesAsync();
            }
            else
            {
                demoPatient.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return await GetPatientByIdAsync(demoPatient.Id);
        }

        public async Task<PatientStats> GetPatientStatsAsync(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.JournalEntries)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                throw new InvalidOperationException("Patient not found.");
            }

            var entries = patient.JournalEntries;
            var totalEntries = entries.Count;
            var recentEntries = entries.Where(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-30)).ToList();

            var moodCounts = recentEntries
                .Where(e => !string.IsNullOrEmpty(e.Mood))
                .GroupBy(e => e.Mood)
                .ToDictionary(g => g.Key, g => g.Count());

            var averageMood = moodCounts.Any() 
                ? moodCounts.OrderByDescending(kvp => kvp.Value).First().Key 
                : "Unknown";

            return new PatientStats
            {
                PatientId = patientId,
                TotalJournalEntries = totalEntries,
                EntriesLast30Days = recentEntries.Count,
                AverageMood = averageMood,
                MoodDistribution = moodCounts,
                LastEntryDate = entries.OrderByDescending(e => e.CreatedAt).FirstOrDefault()?.CreatedAt,
                MostCommonMood = moodCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "Unknown",
                TotalChatSessions = patient.ChatSessions.Count
            };
        }
    }

}

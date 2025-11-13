using SM_MentalHealthApp.Server.Data;
using SM_MentalHealthApp.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

            // Check if the journal entry contains medical data
            var medicalAnalysis = AnalyzeMedicalContentInJournal(entry.Text);

            if (medicalAnalysis.HasMedicalContent)
            {
                // Use medical-aware analysis for entries with medical data
                var (response, mood) = await _huggingFaceService.AnalyzeMedicalJournalEntry(entry.Text, medicalAnalysis);
                entry.AIResponse = response;
                entry.Mood = mood;
            }
            else
            {
                // Use standard sentiment analysis for personal entries
                var (response, mood) = await _huggingFaceService.AnalyzeEntry(entry.Text);
                entry.AIResponse = response;
                entry.Mood = mood;
            }

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<JournalEntry> ProcessDoctorEntry(JournalEntry entry, int patientId, int doctorId)
        {
            // Ensure the entry is associated with the patient and track who entered it
            entry.UserId = patientId;
            entry.EnteredByUserId = doctorId; // Doctor entered for patient

            // Check if the journal entry contains medical data
            var medicalAnalysis = AnalyzeMedicalContentInJournal(entry.Text);

            if (medicalAnalysis.HasMedicalContent)
            {
                // Use medical-aware analysis for entries with medical data
                var (response, mood) = await _huggingFaceService.AnalyzeMedicalJournalEntry(entry.Text, medicalAnalysis);
                entry.AIResponse = response;
                entry.Mood = mood;
            }
            else
            {
                // Use standard sentiment analysis for personal entries
                var (response, mood) = await _huggingFaceService.AnalyzeEntry(entry.Text);
                entry.AIResponse = response;
                entry.Mood = mood;
            }

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<List<JournalEntry>> GetEntriesForUser(int userId)
        {
            return await _context.JournalEntries
                .Include(e => e.EnteredByUser) // Include who entered the entry (doctor or patient)
                .Include(e => e.User) // Include the patient
                .Where(e => e.UserId == userId && e.IsActive)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<JournalEntry>> GetRecentEntriesForUser(int userId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.JournalEntries
                .Where(e => e.UserId == userId && e.IsActive && e.CreatedAt >= cutoffDate)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetMoodDistributionForUser(int userId, int days = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _context.JournalEntries
                .Where(e => e.UserId == userId && e.IsActive && e.CreatedAt >= cutoffDate && !string.IsNullOrEmpty(e.Mood))
                .GroupBy(e => e.Mood)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<JournalEntry?> GetEntryById(int entryId, int? userId)
        {
            if (userId.HasValue)
            {
                return await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId.Value && e.IsActive);
            }
            return await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == entryId && e.IsActive);
        }

        public async Task<bool> DeleteEntry(int entryId, int userId)
        {
            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId && e.IsActive);

            if (entry == null)
                return false;

            // Soft delete
            entry.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VerifyDoctorAccessAsync(int patientId, int doctorId)
        {
            // Check if doctor is assigned to this patient
            return await _context.UserAssignments
                .AnyAsync(ua => ua.AssignerId == doctorId && ua.AssigneeId == patientId && ua.IsActive);
        }

        public async Task ToggleIgnoreAsync(int entryId, int doctorId)
        {
            var entry = await _context.JournalEntries.FindAsync(entryId);
            if (entry == null)
            {
                throw new ArgumentException("Journal entry not found");
            }

            // Toggle ignore status
            if (entry.IsIgnoredByDoctor)
            {
                // Unignore
                entry.IsIgnoredByDoctor = false;
                entry.IgnoredByDoctorId = null;
                entry.IgnoredAt = null;
            }
            else
            {
                // Ignore
                entry.IsIgnoredByDoctor = true;
                entry.IgnoredByDoctorId = doctorId;
                entry.IgnoredAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        // Legacy method for backward compatibility
        public async Task<List<JournalEntry>> GetEntries()
        {
            return await _context.JournalEntries
                .Where(e => e.IsActive)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        private MedicalJournalAnalysis AnalyzeMedicalContentInJournal(string text)
        {
            var analysis = new MedicalJournalAnalysis();
            var lowerText = text.ToLowerInvariant();

            // Check for vital signs patterns
            var bloodPressurePattern = @"(?:blood pressure|bp|pressure)\s*:?\s*(\d{2,3})/(\d{2,3})";
            var hemoglobinPattern = @"(?:hemoglobin|hgb|hb)\s*:?\s*(\d+\.?\d*)";
            var triglyceridesPattern = @"(?:triglycerides|trig)\s*:?\s*(\d+\.?\d*)";
            var heartRatePattern = @"(?:heart rate|hr|pulse)\s*:?\s*(\d+)";
            var temperaturePattern = @"(?:temperature|temp)\s*:?\s*(\d+\.?\d*)";

            // Check for medical keywords
            var medicalKeywords = new[]
            {
                "blood pressure", "bp", "hemoglobin", "hgb", "triglycerides", "trig",
                "heart rate", "hr", "pulse", "temperature", "temp", "glucose", "sugar",
                "cholesterol", "ldl", "hdl", "creatinine", "bun", "sodium", "potassium",
                "white blood cell", "wbc", "red blood cell", "rbc", "platelet", "pt", "inr",
                "systolic", "diastolic", "vital signs", "lab results", "test results"
            };

            // Check if text contains medical keywords
            analysis.HasMedicalContent = medicalKeywords.Any(keyword => lowerText.Contains(keyword));

            if (analysis.HasMedicalContent)
            {
                // Extract blood pressure
                var bpMatch = Regex.Match(text, bloodPressurePattern, RegexOptions.IgnoreCase);
                if (bpMatch.Success)
                {
                    var systolic = int.Parse(bpMatch.Groups[1].Value);
                    var diastolic = int.Parse(bpMatch.Groups[2].Value);
                    analysis.BloodPressure = $"{systolic}/{diastolic}";

                    if (systolic >= 180 || diastolic >= 110)
                    {
                        analysis.CriticalValues.Add($"🚨 CRITICAL: Blood Pressure {systolic}/{diastolic} (Critical: ≥180/≥110)");
                        analysis.HasCriticalValues = true;
                    }
                    else if (systolic >= 140 || diastolic >= 90)
                    {
                        analysis.AbnormalValues.Add($"⚠️ HIGH: Blood Pressure {systolic}/{diastolic} (High: ≥140/≥90)");
                        analysis.HasAbnormalValues = true;
                    }
                    else
                    {
                        analysis.NormalValues.Add($"✅ NORMAL: Blood Pressure {systolic}/{diastolic} (Normal: <140/<90)");
                    }
                }

                // Extract hemoglobin
                var hgbMatch = Regex.Match(text, hemoglobinPattern, RegexOptions.IgnoreCase);
                if (hgbMatch.Success)
                {
                    var hgb = double.Parse(hgbMatch.Groups[1].Value);
                    analysis.Hemoglobin = hgb.ToString();

                    if (hgb < 7.0)
                    {
                        analysis.CriticalValues.Add($"🚨 CRITICAL: Hemoglobin {hgb} g/dL (Critical: <7.0 g/dL)");
                        analysis.HasCriticalValues = true;
                    }
                    else if (hgb < 12.0)
                    {
                        analysis.AbnormalValues.Add($"⚠️ LOW: Hemoglobin {hgb} g/dL (Low: <12.0 g/dL)");
                        analysis.HasAbnormalValues = true;
                    }
                    else
                    {
                        analysis.NormalValues.Add($"✅ NORMAL: Hemoglobin {hgb} g/dL (Normal: 12-16 g/dL)");
                    }
                }

                // Extract triglycerides
                var trigMatch = Regex.Match(text, triglyceridesPattern, RegexOptions.IgnoreCase);
                if (trigMatch.Success)
                {
                    var trig = double.Parse(trigMatch.Groups[1].Value);
                    analysis.Triglycerides = trig.ToString();

                    if (trig >= 500)
                    {
                        analysis.CriticalValues.Add($"🚨 CRITICAL: Triglycerides {trig} mg/dL (Critical: ≥500 mg/dL)");
                        analysis.HasCriticalValues = true;
                    }
                    else if (trig >= 200)
                    {
                        analysis.AbnormalValues.Add($"⚠️ HIGH: Triglycerides {trig} mg/dL (High: ≥200 mg/dL)");
                        analysis.HasAbnormalValues = true;
                    }
                    else
                    {
                        analysis.NormalValues.Add($"✅ NORMAL: Triglycerides {trig} mg/dL (Normal: <150 mg/dL)");
                    }
                }

                // Extract heart rate
                var hrMatch = Regex.Match(text, heartRatePattern, RegexOptions.IgnoreCase);
                if (hrMatch.Success)
                {
                    var hr = int.Parse(hrMatch.Groups[1].Value);
                    analysis.HeartRate = hr.ToString();

                    if (hr >= 120 || hr <= 40)
                    {
                        analysis.CriticalValues.Add($"🚨 CRITICAL: Heart Rate {hr} bpm (Critical: ≥120 or ≤40 bpm)");
                        analysis.HasCriticalValues = true;
                    }
                    else if (hr >= 100 || hr <= 60)
                    {
                        analysis.AbnormalValues.Add($"⚠️ ABNORMAL: Heart Rate {hr} bpm (Abnormal: ≥100 or ≤60 bpm)");
                        analysis.HasAbnormalValues = true;
                    }
                    else
                    {
                        analysis.NormalValues.Add($"✅ NORMAL: Heart Rate {hr} bpm (Normal: 60-100 bpm)");
                    }
                }

                // Extract temperature
                var tempMatch = Regex.Match(text, temperaturePattern, RegexOptions.IgnoreCase);
                if (tempMatch.Success)
                {
                    var temp = double.Parse(tempMatch.Groups[1].Value);
                    analysis.Temperature = temp.ToString();

                    if (temp >= 104.0 || temp <= 95.0)
                    {
                        analysis.CriticalValues.Add($"🚨 CRITICAL: Temperature {temp}°F (Critical: ≥104°F or ≤95°F)");
                        analysis.HasCriticalValues = true;
                    }
                    else if (temp >= 100.4 || temp <= 97.0)
                    {
                        analysis.AbnormalValues.Add($"⚠️ ABNORMAL: Temperature {temp}°F (Abnormal: ≥100.4°F or ≤97°F)");
                        analysis.HasAbnormalValues = true;
                    }
                    else
                    {
                        analysis.NormalValues.Add($"✅ NORMAL: Temperature {temp}°F (Normal: 97.1-100.3°F)");
                    }
                }
            }

            return analysis;
        }
    }

    public class MedicalJournalAnalysis
    {
        public bool HasMedicalContent { get; set; }
        public bool HasCriticalValues { get; set; }
        public bool HasAbnormalValues { get; set; }
        public string? BloodPressure { get; set; }
        public string? Hemoglobin { get; set; }
        public string? Triglycerides { get; set; }
        public string? HeartRate { get; set; }
        public string? Temperature { get; set; }
        public List<string> CriticalValues { get; set; } = new();
        public List<string> AbnormalValues { get; set; } = new();
        public List<string> NormalValues { get; set; } = new();
    }
}

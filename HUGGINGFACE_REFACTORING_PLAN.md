# HuggingFaceService.cs Hardcoded String Removal Plan

## Overview
This document tracks the systematic removal of ALL hardcoded strings from `HuggingFaceService.cs` and their migration to database-driven templates.

## File Statistics
- **Total Lines**: 2,877
- **Estimated Hardcoded Strings**: 500+ instances
- **Template Categories**: 15+ categories

## Refactoring Progress

### ✅ Completed
1. **Journal Fallback Responses** - Moved to templates (`journal_fallback_*`)
2. **Medical Journal Fallback Responses** - Moved to templates (`medical_journal_*`)

### 🔄 In Progress
3. **Emergency Response Templates** - Partially completed
4. **Patient Prompt Responses** - Templates created, code refactoring needed
5. **Doctor Prompt Responses** - Templates created, code refactoring needed
6. **Admin Prompt Responses** - Templates created, code refactoring needed

### ⏳ Pending
7. **BuildJournalPrompt** - Needs template-based refactoring
8. **BuildMedicalJournalPrompt** - Needs template-based refactoring
9. **GenerateEmergencyResponse** - Needs complete refactoring
10. **HandlePatientPrompt** - Needs complete refactoring
11. **HandleDoctorPrompt** - Needs complete refactoring
12. **HandleAdminPrompt** - Needs complete refactoring
13. **All fallback responses in GenerateResponse** - Needs refactoring
14. **Hardcoded patterns in IsGenericKnowledgeQuestion** - Should move to database
15. **Hardcoded medical thresholds** (BP, Hemoglobin, Triglycerides) - Should move to database
16. **All remaining hardcoded strings** throughout the file

## Template Keys Created

### Journal Templates
- `journal_fallback_crisis`
- `journal_fallback_distressed`
- `journal_fallback_sad`
- `journal_fallback_anxious`
- `journal_fallback_happy`
- `journal_fallback_neutral`
- `journal_prompt_crisis`
- `journal_prompt_distressed`
- `journal_prompt_sad`
- `journal_prompt_anxious`
- `journal_prompt_happy`
- `journal_prompt_neutral`
- `journal_prompt_base`

### Medical Journal Templates
- `medical_journal_critical`
- `medical_journal_abnormal`
- `medical_journal_normal`
- `medical_journal_generic`
- `medical_journal_prompt_base`
- `medical_journal_prompt_critical`
- `medical_journal_prompt_abnormal`
- `medical_journal_prompt_normal`

### Emergency Templates
- `emergency_unacknowledged_alert`
- `emergency_acknowledged_history`
- `emergency_all_acknowledged`
- `emergency_critical_medical`
- `emergency_medical_data`
- `emergency_fallback`

### Patient Templates
- `patient_wellness_guidelines`
- `patient_medication_disclaimer`
- `patient_anxiety_response`
- `patient_depression_response`
- `patient_generic_response`

### Doctor Templates
- `doctor_patient_not_found`
- `doctor_no_data`
- `doctor_anxiety_recommendations`
- `doctor_depression_recommendations`
- `doctor_medication_considerations`
- `doctor_generic_response`

### Admin Templates
- `admin_trend_analysis`
- `admin_system_improvements`
- `admin_generic_response`

### Fallback Templates
- `fallback_generic`
- `fallback_no_patient_selected`
- `fallback_mental_health`
- `fallback_mood_feeling`
- `fallback_anxiety`
- `fallback_sad_depressed`
- `fallback_help_support`
- `fallback_default`

### Recommendation Templates
- `recommendations_critical`
- `recommendations_abnormal`
- `recommendations_stable`

## Next Steps

1. **Continue Refactoring Critical Methods**:
   - `GenerateEmergencyResponse` - Convert to use templates
   - `HandlePatientPrompt` - Convert to use templates
   - `HandleDoctorPrompt` - Convert to use templates
   - `HandleAdminPrompt` - Convert to use templates

2. **Refactor Prompt Building**:
   - `BuildJournalPrompt` - Use `journal_prompt_base` and mood-specific templates
   - `BuildMedicalJournalPrompt` - Use `medical_journal_prompt_base` and condition-specific templates

3. **Move Patterns to Database**:
   - Generic knowledge question patterns → `CriticalValueKeywords` table
   - Medical threshold values → New `MedicalThresholds` table (if needed)

4. **Final Cleanup**:
   - Remove all remaining hardcoded strings
   - Verify all templates are used
   - Test all code paths

## SQL Scripts
- `SeedAllAITemplates.sql` - Contains all new templates (ready to run)

## Notes
- All template methods should be async and use `_templateService.FormatTemplateAsync`
- Fallback responses should use templates with minimal hardcoded text
- Patterns and thresholds should be database-driven where possible


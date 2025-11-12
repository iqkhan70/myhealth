-- Seed AI Response Templates for IntelligentContextService
-- These templates replace hardcoded responses in IntelligentContextService.cs

INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt) VALUES
-- Intelligent Context Service Templates
('intelligent_context_error', 'Intelligent Context - Error', 'I apologize, but I encountered an error processing your question. Please try rephrasing it or contact support if the issue persists.', 'Error message for intelligent context processing failures', 10, TRUE, NOW()),

('intelligent_context_no_patient_medical', 'Intelligent Context - No Patient Medical', '**General Medical Information Request**

You''re asking: "{QUESTION}"

To provide personalized medical insights, please:
1. **Select a specific patient** from the dropdown above
2. **Ask your question in the context of that patient''s care**

This will allow me to provide:
- Patient-specific medical assessments
- Personalized treatment recommendations
- Context-aware clinical guidance
- Integration with the patient''s medical history and current data

If you need general medical information without patient context, I recommend consulting medical literature or professional medical resources.', 'Response when no patient is selected for medical questions', 10, TRUE, NOW()),

('intelligent_context_no_patient_resources', 'Intelligent Context - No Patient Resources', '**Medical Resources Search**

To provide personalized medical facility recommendations, please select a specific patient first.

=== GENERAL MEDICAL RESOURCES ===
{WEB_RESULTS}', 'Response when no patient is selected for resource questions', 10, TRUE, NOW()),

('intelligent_context_no_patient_recommendations', 'Intelligent Context - No Patient Recommendations', '**General Medical Recommendations Request**

You''re asking: "{QUESTION}"

To provide personalized medical recommendations, please:
1. **Select a specific patient** from the dropdown above
2. **Ask your question in the context of that patient''s care**

This will allow me to provide:
- Patient-specific treatment recommendations
- Personalized care approaches
- Context-aware clinical guidance
- Integration with the patient''s medical history and current data

If you need general medical recommendations without patient context, I recommend consulting medical literature or professional medical resources.', 'Response when no patient is selected for recommendation questions', 10, TRUE, NOW()),

('intelligent_context_non_patient', 'Intelligent Context - Non-Patient Query', '**Query Not Applicable to Patient Care**

I understand you''re asking about: "{QUESTION}"

However, this question appears to be unrelated to patient care or medical practice. As a clinical AI assistant, I''m designed to help with:

- Patient medical assessments and status updates
- Clinical recommendations and treatment approaches  
- Medical resource identification and referrals
- Healthcare provider decision support

For questions about entertainment, celebrities, or other non-medical topics, please use a general-purpose AI assistant or search engine.

If you have a medical question related to patient care, I''d be happy to help with that instead.', 'Response for non-patient related questions', 10, TRUE, NOW()),

('intelligent_context_general_medical', 'Intelligent Context - General Medical', '**General Medical Information Request**

You''re asking: "{QUESTION}"

While I can provide general medical information, for the most accurate and personalized guidance, please:

1. **Select a specific patient** from the dropdown above
2. **Ask your question in the context of that patient''s care**

This will allow me to provide:
- Patient-specific medical assessments
- Personalized treatment recommendations
- Context-aware clinical guidance
- Integration with the patient''s medical history and current data

If you need general medical information without patient context, I recommend consulting medical literature or professional medical resources.', 'Response for general medical questions', 10, TRUE, NOW()),

('intelligent_context_patient_resources', 'Intelligent Context - Patient Resources', '**Medical Resource Information for {PATIENT_INFO}:**

{WEB_RESULTS}

---
Please note: This information is for guidance only. Always verify details with the medical facility directly.', 'Response for patient resource questions with patient context', 10, TRUE, NOW()),

('intelligent_context_web_search', 'Intelligent Context - Web Search', '**Medical Facilities Search for: {QUERY}**

{ZIP_CODE_SECTION}**Recommended Search Strategy:**
1. **Emergency Care**: Search for ''emergency room near {ZIP_CODE}'' or ''urgent care {ZIP_CODE}''
2. **Hospitals**: Search for ''hospitals near {ZIP_CODE}'' or ''medical centers {ZIP_CODE}''
3. **Specialists**: Search for ''hematologist near {ZIP_CODE}'' (for anemia treatment)
4. **Insurance**: Check which facilities accept your insurance

**Key Considerations for This Patient:**
- **Severe Anemia (Hemoglobin 6.0)**: Requires immediate blood transfusion capability
- **Critical Triglycerides (640)**: Needs cardiology/endocrinology specialists
- **Emergency Priority**: Look for Level 1 trauma centers or major hospitals

**Immediate Action Required:**
- Call 911 or go to nearest emergency room immediately
- This patient''s condition requires urgent medical attention
- Do not delay seeking emergency care', 'Web search response template', 10, TRUE, NOW()),

('intelligent_context_web_search_error', 'Intelligent Context - Web Search Error', 'Web search is currently unavailable. Please use standard search engines to find medical facilities.', 'Error message for web search failures', 10, TRUE, NOW())

ON DUPLICATE KEY UPDATE
    TemplateName = VALUES(TemplateName),
    Content = VALUES(Content),
    Description = VALUES(Description),
    Priority = VALUES(Priority),
    IsActive = VALUES(IsActive),
    UpdatedAt = NOW();


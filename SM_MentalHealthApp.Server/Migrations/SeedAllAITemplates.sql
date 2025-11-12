-- =====================================================
-- Comprehensive AI Response Templates Seed Script
-- This replaces ALL hardcoded strings in HuggingFaceService.cs
-- =====================================================

-- Journal Fallback Responses (by mood)
INSERT INTO AIResponseTemplates (TemplateKey, TemplateName, Content, Description, Priority, IsActive, CreatedAt)
VALUES 
('journal_fallback_crisis', 'Journal Fallback - Crisis', 'I can hear that you''re going through a really difficult time right now. Please know that you''re not alone, and it''s important to reach out to a mental health professional or crisis helpline. Your feelings are valid, and there are people who want to help you through this.', 'Fallback response for crisis mood in journal entries', 10, TRUE, NOW()),
('journal_fallback_distressed', 'Journal Fallback - Distressed', 'I understand you''re feeling really bad right now. These feelings are temporary, even though they might not feel that way. Please consider reaching out to someone you trust or a mental health professional. You don''t have to go through this alone.', 'Fallback response for distressed mood in journal entries', 10, TRUE, NOW()),
('journal_fallback_sad', 'Journal Fallback - Sad', 'I''m sorry you''re feeling sad. It''s okay to feel this way, and your emotions are valid. Sometimes talking to someone we trust or engaging in activities that bring us comfort can help. Remember that this feeling will pass.', 'Fallback response for sad mood in journal entries', 10, TRUE, NOW()),
('journal_fallback_anxious', 'Journal Fallback - Anxious', 'I can sense you''re feeling anxious. Try taking some deep breaths and remember that you''ve gotten through difficult times before. Consider reaching out to someone you trust or trying some relaxation techniques.', 'Fallback response for anxious mood in journal entries', 10, TRUE, NOW()),
('journal_fallback_happy', 'Journal Fallback - Happy', 'It''s wonderful to hear that you''re feeling good! I''m glad you''re taking the time to reflect on positive moments. Keep nurturing these positive feelings and remember to celebrate the good times.', 'Fallback response for happy mood in journal entries', 10, TRUE, NOW()),
('journal_fallback_neutral', 'Journal Fallback - Neutral', 'Thank you for sharing your thoughts with me. It takes courage to express your feelings, and I appreciate you trusting me with them. Remember that you''re not alone in whatever you''re experiencing.', 'Fallback response for neutral mood in journal entries', 10, TRUE, NOW()),

-- Medical Journal Fallback Responses
('medical_journal_critical', 'Medical Journal - Critical Values', '🚨 **CRITICAL MEDICAL VALUES DETECTED**

The following critical values require **immediate medical attention**:
{CRITICAL_VALUES}

**URGENT RECOMMENDATION:** Please seek immediate medical care or contact emergency services. These values indicate a serious medical condition that needs prompt evaluation by a healthcare professional.', 'Response for critical medical values in journal', 10, TRUE, NOW()),
('medical_journal_abnormal', 'Medical Journal - Abnormal Values', '⚠️ **ABNORMAL MEDICAL VALUES DETECTED**

The following values are concerning and should be monitored:
{ABNORMAL_VALUES}

**RECOMMENDATION:** Please schedule an appointment with your healthcare provider to discuss these values and determine appropriate next steps.', 'Response for abnormal medical values in journal', 10, TRUE, NOW()),
('medical_journal_normal', 'Medical Journal - Normal Values', '📊 **MEDICAL DATA RECORDED**

The following values are within normal ranges:
{NORMAL_VALUES}

Thank you for documenting this medical information. Continue to monitor these values and consult with your healthcare provider as needed.', 'Response for normal medical values in journal', 10, TRUE, NOW()),
('medical_journal_generic', 'Medical Journal - Generic', 'Thank you for your journal entry. If you have any medical concerns, please don''t hesitate to discuss them with your healthcare provider.', 'Generic response for medical journal entries', 10, TRUE, NOW()),

-- Emergency Response Templates
('emergency_unacknowledged_alert', 'Emergency - Unacknowledged Alert', '🚨 **CRITICAL EMERGENCY ALERT:** {COUNT} unacknowledged emergency incident(s) detected!

**Unacknowledged Emergencies:**
{UNACKNOWLEDGED_DETAILS}

**Immediate Actions Required:**
1. Acknowledge all emergency incidents immediately
2. Contact patient for status check
3. Conduct fall risk assessment
4. Review medications for side effects
5. Consider emergency medical intervention', 'Template for unacknowledged emergency incidents', 10, TRUE, NOW()),
('emergency_acknowledged_history', 'Emergency - Acknowledged History', '📋 **Emergency History:** {COUNT} previously acknowledged incident(s)
{ACKNOWLEDGED_DETAILS}', 'Template for acknowledged emergency history', 10, TRUE, NOW()),
('emergency_all_acknowledged', 'Emergency - All Acknowledged', '✅ **All emergencies have been acknowledged**
**Follow-up Actions:**
1. Monitor patient for any new incidents
2. Review emergency patterns for trends
3. Consider preventive measures', 'Template when all emergencies are acknowledged', 10, TRUE, NOW()),
('emergency_critical_medical', 'Emergency - Critical Medical Values', '🚨 **CRITICAL MEDICAL VALUES DETECTED:**
{CRITICAL_ALERTS}

**IMMEDIATE ACTIONS REQUIRED:**
1. Contact patient immediately for status check
2. Consider emergency medical evaluation
3. Review medications and adjust as needed
4. Monitor vital signs closely', 'Template for critical medical values in emergency context', 10, TRUE, NOW()),
('emergency_medical_data', 'Emergency - Medical Data', '**Medical Data Analysis:**

{MEDICAL_DATA}', 'Template for medical data in emergency response', 10, TRUE, NOW()),
('emergency_fallback', 'Emergency - Fallback', '🚨 **CRITICAL EMERGENCY ALERT:** Emergency incidents detected requiring immediate attention!', 'Fallback emergency response', 10, TRUE, NOW()),

-- Patient Prompt Responses
('patient_wellness_guidelines', 'Patient - Wellness Guidelines', 'Here are some general wellness guidelines that can support your mental health:

🌱 **Daily Habits:**
• Maintain a consistent sleep schedule (7-9 hours)
• Eat regular, balanced meals with plenty of fruits and vegetables
• Stay hydrated throughout the day
• Get some sunlight exposure daily

🧘 **Mental Wellness:**
• Practice deep breathing exercises for 5-10 minutes daily
• Try mindfulness or meditation (even 5 minutes helps)
• Keep a gratitude journal - write down 3 things you''re grateful for each day
• Engage in activities you enjoy

💪 **Physical Activity:**
• Aim for at least 30 minutes of moderate exercise most days
• Take short walks throughout the day
• Try gentle stretching or yoga

🤝 **Social Connection:**
• Stay connected with friends and family
• Consider joining groups or activities you''re interested in
• Don''t hesitate to reach out when you need support

Remember, these are general guidelines. For personalized advice or if you have specific health concerns, please consult with your doctor.', 'Wellness guidelines for patients', 10, TRUE, NOW()),
('patient_medication_disclaimer', 'Patient - Medication Disclaimer', 'I understand you''re asking about medications or treatments, but I''m not qualified to provide medical advice. Please consult with your doctor about any medications or treatments. I can help you with general wellness strategies like stress management, relaxation techniques, and healthy lifestyle habits.', 'Disclaimer for medication questions', 10, TRUE, NOW()),
('patient_anxiety_response', 'Patient - Anxiety Response', 'I understand you might be feeling anxious. That''s completely normal. I can suggest some relaxation techniques like deep breathing, progressive muscle relaxation, or grounding exercises. However, if your anxiety is significantly impacting your daily life, please discuss this with your doctor for proper evaluation and treatment options.', 'Response for anxiety-related questions', 10, TRUE, NOW()),
('patient_depression_response', 'Patient - Depression Response', 'I hear that you might be feeling down. These feelings are valid and it''s okay to not be okay. I can offer emotional support and suggest activities that might help, like gentle exercise, spending time in nature, or connecting with loved ones. For persistent feelings of depression, please reach out to your doctor or a mental health professional.', 'Response for depression-related questions', 10, TRUE, NOW()),
('patient_generic_response', 'Patient - Generic Response', 'I''m here to listen and support you. I can help with general wellness advice, emotional support, and relaxation techniques. For any specific medical concerns or treatment questions, please consult with your doctor. What would you like to talk about?', 'Generic patient response', 10, TRUE, NOW()),

-- Doctor Prompt Responses
('doctor_patient_not_found', 'Doctor - Patient Not Found', '**PATIENT LOOKUP:**

**Status:** ❌ **Patient not found in your assigned patients**

**Current Situation:** The person you''re asking about does not appear to be one of your assigned patients in the system.

**What This Means:**
• No patient record found with this name
• No clinical data available for analysis
• No treatment history or journal entries to review

**Possible Reasons:**
• The person is not assigned to you as a patient
• The name might be misspelled
• The person might not be registered in the system
• You might need to check with administration for patient assignments

**Next Steps:**
• Verify the correct spelling of the patient''s name
• Check your patient list to confirm assignments
• Contact administration if you believe this person should be your patient
• Use the patient selection dropdown to choose from your assigned patients', 'Response when patient is not found', 10, TRUE, NOW()),
('doctor_no_data', 'Doctor - No Patient Data', '**PATIENT STATUS OVERVIEW:**

**Data Status:** ⚠️ **No data reported yet**

**Clinical Assessment:** This patient has not yet submitted any journal entries or mood tracking data. Without baseline information, I cannot provide specific clinical insights.

**Recommendations:**
1) **Initial Assessment:** Schedule an in-person or virtual consultation to establish baseline
2) **Patient Engagement:** Encourage the patient to start using the journaling feature
3) **Data Collection:** Consider asking about recent mood, sleep, and stress levels during consultation
4) **Monitoring Setup:** Establish a regular check-in schedule once data collection begins

**Next Steps:** I recommend reaching out to the patient to encourage platform engagement and schedule an initial assessment to gather baseline clinical information.', 'Response when patient has no data', 10, TRUE, NOW()),
('doctor_anxiety_recommendations', 'Doctor - Anxiety Recommendations', 'Based on the patient''s data showing {MOOD_PATTERNS}, I''d recommend considering: 1) Assessment of anxiety severity using standardized scales, 2) Review of current stressors and triggers, 3) Consideration of CBT or other evidence-based therapies, 4) Evaluation for medication if symptoms are moderate to severe, 5) Sleep hygiene assessment. The patient''s recent entries suggest {RECENT_PATTERNS}. I recommend asking about specific anxiety symptoms, duration, and functional impact.', 'Anxiety treatment recommendations for doctors', 10, TRUE, NOW()),
('doctor_depression_recommendations', 'Doctor - Depression Recommendations', 'Given the patient''s mood patterns showing {MOOD_PATTERNS}, consider: 1) PHQ-9 or similar depression screening, 2) Assessment of suicidal ideation and safety planning, 3) Review of sleep, appetite, and energy levels, 4) Consideration of antidepressant medication if indicated, 5) Psychotherapy referral. The recent journal entries indicate {RECENT_PATTERNS}. I suggest asking about anhedonia, concentration difficulties, and any recent life stressors.', 'Depression treatment recommendations for doctors', 10, TRUE, NOW()),
('doctor_medication_considerations', 'Doctor - Medication Considerations', 'For medication considerations with this patient showing {MOOD_PATTERNS}: 1) Start with first-line treatments (SSRIs for anxiety/depression), 2) Consider patient''s age, comorbidities, and medication history, 3) Start low and go slow with dosing, 4) Monitor for side effects and efficacy, 5) Consider drug interactions. Recent patterns show {RECENT_PATTERNS}. Always verify current prescribing guidelines and contraindications.', 'Medication considerations for doctors', 10, TRUE, NOW()),
('doctor_generic_response', 'Doctor - Generic Response', 'Based on the patient''s data showing {MOOD_PATTERNS} and recent entries indicating {RECENT_PATTERNS}, I recommend a comprehensive assessment including symptom review, functional impact evaluation, and consideration of both pharmacological and non-pharmacological interventions. What specific aspect of the patient''s care would you like to explore further?', 'Generic doctor response', 10, TRUE, NOW()),

-- Admin Prompt Responses
('admin_trend_analysis', 'Admin - Trend Analysis', 'For system-wide trend analysis, I recommend: 1) Regular review of mood distribution reports, 2) Identification of high-risk patients based on patterns, 3) System alerts for concerning trends, 4) Regular staff training on recognizing warning signs, 5) Implementation of automated monitoring systems.', 'Trend analysis recommendations for admins', 10, TRUE, NOW()),
('admin_system_improvements', 'Admin - System Improvements', 'System improvement suggestions: 1) Enhanced data analytics dashboard, 2) Automated risk assessment tools, 3) Improved patient engagement features, 4) Staff training programs, 5) Integration with electronic health records, 6) Regular system performance reviews.', 'System improvement suggestions for admins', 10, TRUE, NOW()),
('admin_generic_response', 'Admin - Generic Response', 'I can help with administrative insights, system monitoring, data analysis, and operational improvements. What specific administrative aspect would you like to focus on?', 'Generic admin response', 10, TRUE, NOW()),

-- Generic Fallback Responses
('fallback_generic', 'Fallback - Generic', 'I understand. How can I help you today?', 'Generic fallback response', 5, TRUE, NOW()),
('fallback_no_patient_selected', 'Fallback - No Patient Selected', '⚠️ **No Patient Selected**

To provide personalized insights about a specific patient, please:
1. Select a patient from the dropdown above
2. Ask your question about that specific patient

Once a patient is selected, I can analyze their journal entries, medical content, and provide detailed insights about their mental health status.', 'Response when no patient is selected', 10, TRUE, NOW()),
('fallback_mental_health', 'Fallback - Mental Health', 'I''m here to support your mental health and well-being! How are you feeling today? I can help you with mood tracking, coping strategies, or just provide a listening ear. What''s on your mind?', 'Fallback for mental health queries', 10, TRUE, NOW()),
('fallback_mood_feeling', 'Fallback - Mood/Feeling', 'I''d love to help you explore your feelings and mood. You can track your emotions in the journal section, or we can talk about what you''re experiencing right now. What''s going on for you today?', 'Fallback for mood/feeling queries', 10, TRUE, NOW()),
('fallback_anxiety', 'Fallback - Anxiety', 'I understand you might be feeling anxious. That''s completely normal and you''re not alone. Would you like to try some breathing exercises or grounding techniques? I can also help you explore what might be causing these feelings.', 'Fallback for anxiety queries', 10, TRUE, NOW()),
('fallback_sad_depressed', 'Fallback - Sad/Depressed', 'I hear that you might be feeling sad or down. These feelings are valid and it''s okay to not be okay. Would you like to talk about what''s going on? I''m here to listen and support you through this.', 'Fallback for sad/depressed queries', 10, TRUE, NOW()),
('fallback_help_support', 'Fallback - Help/Support', 'I''m here to help and support you! I can assist with mood tracking, provide coping strategies, offer emotional support, or just listen. What kind of support would be most helpful for you right now?', 'Fallback for help/support queries', 10, TRUE, NOW()),
('fallback_default', 'Fallback - Default', 'I''m here as your mental health companion to listen and support you. How are you feeling today? Is there anything about your mental wellness that you''d like to talk about or explore together?', 'Default fallback response', 5, TRUE, NOW()),

-- Journal Prompt Templates
('journal_prompt_crisis', 'Journal Prompt - Crisis', 'The person is in crisis and needs immediate support. Respond with empathy and encourage seeking professional help.', 'Prompt context for crisis mood', 10, TRUE, NOW()),
('journal_prompt_distressed', 'Journal Prompt - Distressed', 'The person is experiencing emotional distress. Provide comfort and gentle encouragement.', 'Prompt context for distressed mood', 10, TRUE, NOW()),
('journal_prompt_sad', 'Journal Prompt - Sad', 'The person is feeling sad. Offer empathy and hope.', 'Prompt context for sad mood', 10, TRUE, NOW()),
('journal_prompt_anxious', 'Journal Prompt - Anxious', 'The person is feeling anxious. Provide calming reassurance and coping strategies.', 'Prompt context for anxious mood', 10, TRUE, NOW()),
('journal_prompt_happy', 'Journal Prompt - Happy', 'The person is feeling positive. Celebrate with them and encourage continued well-being.', 'Prompt context for happy mood', 10, TRUE, NOW()),
('journal_prompt_neutral', 'Journal Prompt - Neutral', 'The person is sharing their thoughts. Respond with empathy and understanding.', 'Prompt context for neutral mood', 10, TRUE, NOW()),
('journal_prompt_base', 'Journal Prompt - Base', 'You are a compassionate mental health companion. A person has written in their journal: "{JOURNAL_TEXT}"

{MOOD_CONTEXT}

Respond with a brief, empathetic message (2-3 sentences) that acknowledges their feelings and provides gentle support. Be warm and encouraging.', 'Base journal prompt template', 10, TRUE, NOW()),

-- Medical Journal Prompt Templates
('medical_journal_prompt_base', 'Medical Journal Prompt - Base', 'You are a medical AI assistant analyzing a journal entry that contains medical data.

Journal Entry: "{JOURNAL_TEXT}"

{MEDICAL_ANALYSIS}

Provide a medical assessment that:
1. Acknowledges the medical data presented
2. Provides appropriate medical context and interpretation
3. Gives clear recommendations based on the values
4. Maintains a professional, caring tone
5. Emphasizes the importance of professional medical consultation when appropriate', 'Base medical journal prompt', 10, TRUE, NOW()),
('medical_journal_prompt_critical', 'Medical Journal Prompt - Critical', '🚨 CRITICAL MEDICAL VALUES DETECTED:
{CRITICAL_VALUES}

This requires immediate medical attention. Respond with urgency and recommend immediate consultation with a healthcare provider.', 'Medical journal prompt for critical values', 10, TRUE, NOW()),
('medical_journal_prompt_abnormal', 'Medical Journal Prompt - Abnormal', '⚠️ ABNORMAL MEDICAL VALUES DETECTED:
{ABNORMAL_VALUES}

These values are concerning and should be monitored closely. Recommend follow-up with healthcare provider.', 'Medical journal prompt for abnormal values', 10, TRUE, NOW()),
('medical_journal_prompt_normal', 'Medical Journal Prompt - Normal', '✅ NORMAL MEDICAL VALUES:
{NORMAL_VALUES}', 'Medical journal prompt for normal values', 10, TRUE, NOW()),

-- Clinical Recommendations Templates
('recommendations_critical', 'Recommendations - Critical', '🚨 **IMMEDIATE ACTIONS REQUIRED:**
1. **Emergency Medical Care**: Contact emergency services immediately
2. **Hospital Admission**: Patient requires immediate hospitalization
3. **Specialist Consultation**: Refer to appropriate specialist
4. **Continuous Monitoring**: Vital signs every 15 minutes
5. **Immediate Intervention**: Consider immediate medical intervention based on critical values', 'Clinical recommendations for critical conditions', 10, TRUE, NOW()),
('recommendations_abnormal', 'Recommendations - Abnormal', '⚠️ **MEDICAL MANAGEMENT NEEDED:**
1. **Primary Care Follow-up**: Schedule appointment within 24-48 hours
2. **Laboratory Monitoring**: Repeat blood work in 1-2 weeks
3. **Lifestyle Modifications**: Dietary changes and exercise recommendations
4. **Medication Review**: Assess current medications and interactions', 'Clinical recommendations for abnormal conditions', 10, TRUE, NOW()),
('recommendations_stable', 'Recommendations - Stable', '✅ **CURRENT STATUS: STABLE**
1. **Continue Current Care**: Maintain existing treatment plan
2. **Regular Monitoring**: Schedule routine follow-up appointments
3. **Preventive Care**: Focus on maintaining current health status', 'Clinical recommendations for stable conditions', 10, TRUE, NOW()),

-- Section Headers
('section_patient_overview', 'Section - Patient Medical Overview', '**Patient Medical Overview:**', 'Header for patient medical overview section', 5, TRUE, NOW()),
('section_recent_activity', 'Section - Recent Patient Activity', '**Recent Patient Activity:**', 'Header for recent patient activity section', 5, TRUE, NOW()),
('section_clinical_assessment', 'Section - Clinical Assessment', '**Clinical Assessment:**', 'Header for clinical assessment section', 5, TRUE, NOW()),
('section_recommendations', 'Section - Recommendations', '**Recommendations:**', 'Header for recommendations section', 5, TRUE, NOW()),
('section_clinical_recommendations', 'Section - Clinical Recommendations', '**Clinical Recommendations:**', 'Header for clinical recommendations section', 5, TRUE, NOW()),
('section_areas_of_concern', 'Section - Areas of Concern', '**Areas of Concern Analysis:**', 'Header for areas of concern section', 5, TRUE, NOW()),
('section_chat_history', 'Section - Chat History', '**Chat History:** Patient has been engaging in conversations with the AI assistant.', 'Chat history section message', 5, TRUE, NOW()),
('section_clinical_notes', 'Section - Clinical Notes', '**Clinical Notes:** Recent clinical documentation is available for review.', 'Clinical notes section message', 5, TRUE, NOW()),
('section_emergency_incidents', 'Section - Emergency Incidents', '⚠️ **EMERGENCY INCIDENTS:** Emergency incidents have been recorded. Please review the emergency dashboard for details.', 'Emergency incidents section message', 5, TRUE, NOW()),
('section_previously_acknowledged', 'Section - Previously Acknowledged', '**Previously Acknowledged Emergencies:**', 'Header for previously acknowledged emergencies', 5, TRUE, NOW()),
('section_medical_data_analysis', 'Section - Medical Data Analysis', '**Medical Data Analysis:**', 'Header for medical data analysis section', 5, TRUE, NOW()),

-- Status Messages
('status_no_journal_entries', 'Status - No Journal Entries', '- No recent journal entries found.', 'Message when no journal entries are found', 5, TRUE, NOW()),
('status_patient_tracking', 'Status - Patient Tracking', 'The patient has been actively engaging with their health tracking.', 'Message about patient engagement', 5, TRUE, NOW()),
('status_medical_alerts_detected', 'Status - Medical Alerts Detected', '🚨 **MEDICAL ALERTS DETECTED:**', 'Header for medical alerts detected', 5, TRUE, NOW()),
('status_medical_monitoring_needed', 'Status - Medical Monitoring Needed', '**MEDICAL MONITORING NEEDED:** Abnormal values detected that require medical attention.', 'Message about medical monitoring needed', 5, TRUE, NOW()),
('status_continued_monitoring', 'Status - Continued Monitoring', '**CURRENT STATUS:** Patient shows normal values, but previous concerning results require continued monitoring.', 'Message about continued monitoring', 5, TRUE, NOW()),
('status_high_priority_concerns', 'Status - High Priority Concerns', '🚨 **High Priority Concerns:**', 'Header for high priority concerns', 5, TRUE, NOW()),
('status_no_concerns', 'Status - No Concerns', '✅ No immediate concerns detected in the current data.', 'Message when no concerns are detected', 5, TRUE, NOW()),

-- Assessment Messages
('assessment_critical_intervention', 'Assessment - Critical Intervention', 'The patient requires immediate medical attention due to critical values. Urgent intervention is necessary.', 'Assessment message for critical values', 5, TRUE, NOW()),
('assessment_abnormal_monitoring', 'Assessment - Abnormal Monitoring', 'The patient shows some abnormal values that require monitoring and follow-up care. Schedule a medical review.', 'Assessment message for abnormal values', 5, TRUE, NOW()),
('assessment_stable_condition', 'Assessment - Stable Condition', 'The patient appears to be in stable condition with no immediate medical concerns. Continue routine monitoring and care.', 'Assessment message for stable condition', 5, TRUE, NOW()),
('assessment_in_response', 'Assessment - In Response', 'In response to your question: "{USER_QUESTION}"', 'Assessment message prefix with user question', 5, TRUE, NOW()),

-- Action Items
('action_immediate_evaluation', 'Action - Immediate Evaluation', '- Immediate medical evaluation required', 'Action item for immediate evaluation', 5, TRUE, NOW()),
('action_emergency_department', 'Action - Emergency Department', '- Consider emergency department visit', 'Action item for emergency department', 5, TRUE, NOW()),
('action_notify_doctors', 'Action - Notify Doctors', '- Notify assigned doctors immediately', 'Action item for notifying doctors', 5, TRUE, NOW()),
('action_followup_appointment', 'Action - Follow-up Appointment', '- Schedule follow-up appointment within 1-2 weeks', 'Action item for follow-up appointment', 5, TRUE, NOW()),
('action_repeat_tests', 'Action - Repeat Tests', '- Repeat laboratory tests as indicated', 'Action item for repeating tests', 5, TRUE, NOW()),
('action_monitor_patient', 'Action - Monitor Patient', '- Monitor patient closely for any changes', 'Action item for monitoring patient', 5, TRUE, NOW()),
('action_continue_care', 'Action - Continue Care', '- Continue current care plan', 'Action item for continuing care', 5, TRUE, NOW()),
('action_maintain_schedule', 'Action - Maintain Schedule', '- Maintain routine follow-up schedule', 'Action item for maintaining schedule', 5, TRUE, NOW()),
('action_encourage_tracking', 'Action - Encourage Tracking', '- Encourage continued health tracking', 'Action item for encouraging tracking', 5, TRUE, NOW()),

-- Medical Content Messages
('medical_content_analysis', 'Medical Content - Analysis', '📊 **Medical Content Analysis:** I''ve reviewed the patient''s medical content. ', 'Message about medical content analysis', 5, TRUE, NOW()),
('medical_content_warning', 'Medical Content - Warning', '⚠️ **IMPORTANT:** While medical content was found, I was unable to detect specific critical values in the current analysis. 
Please ensure all test results are properly formatted and accessible for accurate medical assessment.', 'Warning about medical content detection', 5, TRUE, NOW()),
('medical_content_critical_care', 'Medical Content - Critical Care', 'Please ensure all critical values are properly addressed with appropriate medical care.', 'Message about critical care', 5, TRUE, NOW()),

-- Detailed Recommendation Blocks (for fallback when main template not found)
('recommendations_critical_detailed', 'Recommendations - Critical Detailed', '🚨 **IMMEDIATE ACTIONS REQUIRED:**
1. **Emergency Medical Care**: Contact emergency services immediately
2. **Hospital Admission**: Patient requires immediate hospitalization
3. **Specialist Consultation**: Refer to appropriate specialist
4. **Continuous Monitoring**: Vital signs every 15 minutes
5. **Immediate Intervention**: Consider immediate medical intervention based on critical values', 'Detailed critical recommendations fallback', 5, TRUE, NOW()),
('recommendations_abnormal_detailed', 'Recommendations - Abnormal Detailed', '⚠️ **MEDICAL MANAGEMENT NEEDED:**
1. **Primary Care Follow-up**: Schedule appointment within 24-48 hours
2. **Laboratory Monitoring**: Repeat blood work in 1-2 weeks
3. **Lifestyle Modifications**: Dietary changes and exercise recommendations
4. **Medication Review**: Assess current medications and interactions', 'Detailed abnormal recommendations fallback', 5, TRUE, NOW()),
('recommendations_stable_detailed', 'Recommendations - Stable Detailed', '✅ **CURRENT STATUS: STABLE**
1. **Continue Current Care**: Maintain existing treatment plan
2. **Regular Monitoring**: Schedule routine follow-up appointments
3. **Preventive Care**: Focus on maintaining current health status', 'Detailed stable recommendations fallback', 5, TRUE, NOW())

ON DUPLICATE KEY UPDATE 
    TemplateName = VALUES(TemplateName),
    Content = VALUES(Content),
    Description = VALUES(Description),
    UpdatedAt = NOW();


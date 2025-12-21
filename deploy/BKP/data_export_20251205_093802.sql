-- MySQL dump 10.13  Distrib 8.0.43, for macos14.7 (arm64)
--
-- Host: localhost    Database: mentalhealthdb
-- ------------------------------------------------------
-- Server version	9.4.0-commercial

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `AIInstructionCategories`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `AIInstructionCategories` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Context` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT 'HealthCheck',
  `DisplayOrder` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AIInstructionCategories_Context` (`Context`),
  KEY `IX_AIInstructionCategories_Context_IsActive_DisplayOrder` (`Context`,`IsActive`,`DisplayOrder`),
  KEY `IX_AIInstructionCategories_DisplayOrder` (`DisplayOrder`),
  KEY `IX_AIInstructionCategories_IsActive` (`IsActive`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AIInstructionCategories`
--

LOCK TABLES `AIInstructionCategories` WRITE;
/*!40000 ALTER TABLE `AIInstructionCategories` DISABLE KEYS */;
INSERT INTO `AIInstructionCategories` (`Id`, `Name`, `Description`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (1,'CRITICAL PRIORITY','Instructions for handling critical medical values and emergencies','HealthCheck',1,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructionCategories` (`Id`, `Name`, `Description`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (2,'Patient Medical Overview','Instructions for analyzing patient medical data and status','HealthCheck',2,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructionCategories` (`Id`, `Name`, `Description`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (3,'Recent Patient Activity','Instructions for reviewing patient activity and history','HealthCheck',3,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructionCategories` (`Id`, `Name`, `Description`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (4,'Emergency Incidents','Instructions for handling emergency incidents','HealthCheck',4,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructionCategories` (`Id`, `Name`, `Description`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (5,'Clinical Assessment','Instructions for providing clinical assessment','HealthCheck',5,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructionCategories` (`Id`, `Name`, `Description`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (6,'Recommendations','Instructions for providing medical recommendations','HealthCheck',6,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructionCategories` (`Id`, `Name`, `Description`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (7,'IMPORTANT','Final important instructions','HealthCheck',99,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructionCategories` (`Id`, `Name`, `Description`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (8,'Clinical Note Generation','Instructions for primary model to generate structured clinical notes from patient encounters','ClinicalNote',1,1,'2025-11-13 16:37:21.000000',NULL);
INSERT INTO `AIInstructionCategories` (`Id`, `Name`, `Description`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (9,'Clinical Analysis','Instructions for secondary model to analyze encounters and identify missed considerations','ClinicalNote',2,1,'2025-11-13 16:37:21.000000',NULL);
/*!40000 ALTER TABLE `AIInstructionCategories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AIInstructions`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `AIInstructions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CategoryId` int NOT NULL,
  `Content` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `DisplayOrder` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_AIInstructions_CategoryId` (`CategoryId`),
  KEY `IX_AIInstructions_CategoryId_IsActive_DisplayOrder` (`CategoryId`,`IsActive`,`DisplayOrder`),
  KEY `IX_AIInstructions_DisplayOrder` (`DisplayOrder`),
  KEY `IX_AIInstructions_IsActive` (`IsActive`),
  CONSTRAINT `FK_AIInstructions_AIInstructionCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `AIInstructionCategories` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=39 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AIInstructions`
--

LOCK TABLES `AIInstructions` WRITE;
/*!40000 ALTER TABLE `AIInstructions` DISABLE KEYS */;
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (1,1,'If you see \'🚨 CRITICAL MEDICAL VALUES DETECTED\' or any critical values in the medical data, you MUST:',NULL,1,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (2,1,'Start your response by highlighting these critical values IMMEDIATELY',NULL,2,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (3,1,'State that the patient\'s status is CRITICAL or CONCERNING, NOT stable',NULL,3,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (4,1,'Emphasize that immediate medical attention is required',NULL,4,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (5,1,'Do NOT say the patient is stable if critical values are present',NULL,5,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (6,2,'FIRST: Check if there are any 🚨 CRITICAL VALUES in the medical data above',NULL,1,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (7,2,'SECOND: Review RECENT CLINICAL NOTES section above - these are written by doctors and contain critical medical observations',NULL,2,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (8,2,'If clinical notes mention serious symptoms, concerns, monitoring needs, or health risks, you MUST reflect this in your assessment',NULL,3,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (9,2,'Do NOT say the patient is \'stable\' if clinical notes indicate serious symptoms or require monitoring',NULL,4,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (10,2,'Clinical notes take precedence over other data when they indicate concerns',NULL,5,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (11,2,'If critical values exist, state: \'🚨 CRITICAL STATUS: Patient has critical medical values requiring immediate attention\'',NULL,6,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (12,2,'If no critical values but abnormal values exist, state: \'⚠️ CONCERNING STATUS: Patient has abnormal values requiring monitoring\'',NULL,7,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (13,2,'Only state \'STABLE\' if ALL values are normal, no clinical notes indicate concerns, and no concerning patterns are detected',NULL,8,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (14,2,'Summarize key medical findings from test results, vital signs, medical data, AND clinical notes',NULL,9,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (15,2,'Reference specific values from the medical data (e.g., if hemoglobin is below 7 g/dL, it indicates severe anemia)',NULL,10,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (16,3,'Review journal entries and mood patterns',NULL,1,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (17,3,'Analyze chat history for concerning conversations or medical data',NULL,2,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (18,3,'Review clinical notes for doctor observations and assessments',NULL,3,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (19,4,'If emergency incidents exist, start with: \'🚨 CRITICAL EMERGENCY ALERT: [number] unacknowledged emergency incidents detected\'',NULL,1,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (20,4,'List each emergency with severity (all are unacknowledged and require immediate attention)',NULL,2,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (21,4,'Note: Only unacknowledged emergencies are included in this analysis',NULL,3,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (22,5,'Provide a professional assessment of the patient\'s overall health status',NULL,1,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (23,5,'If critical values are present, state clearly that the patient requires IMMEDIATE medical attention',NULL,2,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (24,5,'Identify any trends (improving, stable, deteriorating)',NULL,3,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (25,5,'Highlight areas requiring attention or follow-up',NULL,4,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (26,6,'If critical values are found, recommend IMMEDIATE medical evaluation and emergency department visit if necessary',NULL,1,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (27,6,'Suggest any immediate actions if critical issues are found',NULL,2,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (28,6,'Recommend follow-up care or monitoring if needed',NULL,3,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (29,7,'Be specific and reference actual data from the patient\'s records. If critical values are present in the medical data, you MUST indicate the patient is NOT stable. Only state \'stable\' if ALL medical values are normal. Keep the response comprehensive but concise (300-400 words).',NULL,1,1,'2025-11-11 16:22:10.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (30,8,'You are a medical AI assistant specialized in generating structured clinical notes. Your task is to create a well-organized, professional clinical note from patient encounter data. The note should follow standard medical documentation format with clear sections.','Primary Model Role Definition',1,1,'2025-11-13 16:37:21.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (31,8,'Structure your clinical note with the following sections:\n1. Chief Complaint (CC): Patient\'s primary reason for visit\n2. History of Present Illness (HPI): Detailed narrative of current symptoms\n3. Review of Systems (ROS): Relevant system reviews\n4. Assessment: Clinical impression and differential diagnoses\n5. Plan: Treatment plan, medications, follow-up instructions','Note Structure Requirements',2,1,'2025-11-13 16:37:21.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (32,8,'Use medical terminology appropriately. Be concise but comprehensive. Include relevant clinical details, vital signs if available, and any pertinent medical history. Maintain professional tone suitable for medical documentation.','Writing Guidelines',3,1,'2025-11-13 16:37:21.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (33,8,'If patient context is provided, incorporate relevant past medical history, medications, and previous encounters into your note. Ensure continuity of care is reflected in the documentation.','Context Integration',4,1,'2025-11-13 16:37:21.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (34,9,'You are a medical AI assistant specialized in clinical decision support. Your task is to analyze patient encounters and structured notes to identify potential missed considerations, overlooked diagnoses, or important follow-up actions.','Secondary Model Role Definition',1,1,'2025-11-13 16:37:21.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (35,9,'Analyze the encounter data and structured note carefully. Look for:\n- Potential diagnoses that may have been missed\n- Important symptoms or findings that need further investigation\n- Drug interactions or contraindications\n- Necessary follow-up tests or referrals\n- Red flags or warning signs that require immediate attention','Analysis Focus Areas',2,1,'2025-11-13 16:37:21.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (36,9,'Format your response with two clear sections:\n\n**Missed Considerations:**\nList any potential issues, diagnoses, or considerations that may have been overlooked. Be specific and evidence-based.\n\n**Follow-up Actions:**\nProvide a numbered or bulleted list of recommended follow-up actions, tests, referrals, or monitoring that should be considered.','Response Format',3,1,'2025-11-13 16:37:21.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (37,9,'Be thorough but prioritize. Focus on clinically significant findings. If patient context is provided, consider the full medical history when identifying potential issues. Always prioritize patient safety and evidence-based recommendations.','Analysis Guidelines',4,1,'2025-11-13 16:37:21.000000',NULL);
INSERT INTO `AIInstructions` (`Id`, `CategoryId`, `Content`, `Title`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (38,9,'Compare the original encounter data with the structured note. Identify any discrepancies, missing information, or areas where additional detail or investigation might be warranted.','Comparison Analysis',5,1,'2025-11-13 16:37:21.000000',NULL);
/*!40000 ALTER TABLE `AIInstructions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AIModelChains`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `AIModelChains` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ChainName` varchar(100) NOT NULL,
  `Context` varchar(50) NOT NULL DEFAULT 'ClinicalNote',
  `Description` varchar(500) DEFAULT NULL,
  `PrimaryModelId` int NOT NULL,
  `SecondaryModelId` int NOT NULL,
  `ChainOrder` int NOT NULL DEFAULT '1',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT (now(6)),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_AIModelChains_ChainName_Context` (`ChainName`,`Context`),
  KEY `IX_AIModelChains_Context` (`Context`),
  KEY `IX_AIModelChains_IsActive` (`IsActive`),
  KEY `IX_AIModelChains_PrimaryModelId` (`PrimaryModelId`),
  KEY `IX_AIModelChains_SecondaryModelId` (`SecondaryModelId`),
  CONSTRAINT `FK_AIModelChains_PrimaryModel` FOREIGN KEY (`PrimaryModelId`) REFERENCES `AIModelConfigs` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_AIModelChains_SecondaryModel` FOREIGN KEY (`SecondaryModelId`) REFERENCES `AIModelConfigs` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AIModelChains`
--

LOCK TABLES `AIModelChains` WRITE;
/*!40000 ALTER TABLE `AIModelChains` DISABLE KEYS */;
INSERT INTO `AIModelChains` (`Id`, `ChainName`, `Context`, `Description`, `PrimaryModelId`, `SecondaryModelId`, `ChainOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (1,'Qwen3-8b-Qwen3-4b Chain','ClinicalNote','Chained AI workflow: Primary model (Qwen3-8b) generates structured clinical note draft from patient encounter, then Secondary model (Qwen3-4b) analyzes the note to identify missed considerations and follow-up actions.',1,2,1,1,'2025-11-13 16:37:09.000000','2025-11-13 19:08:26.000000');
/*!40000 ALTER TABLE `AIModelChains` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AIModelConfigs`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `AIModelConfigs` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ModelName` varchar(100) NOT NULL,
  `ModelType` varchar(50) NOT NULL,
  `Provider` varchar(50) NOT NULL,
  `ApiEndpoint` varchar(500) NOT NULL,
  `ApiKeyConfigKey` varchar(100) DEFAULT NULL,
  `SystemPrompt` text,
  `Context` varchar(50) NOT NULL DEFAULT 'ClinicalNote',
  `DisplayOrder` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT (now(6)),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_AIModelConfigs_ModelName_Context` (`ModelName`,`Context`),
  KEY `IX_AIModelConfigs_ModelType` (`ModelType`),
  KEY `IX_AIModelConfigs_Context` (`Context`),
  KEY `IX_AIModelConfigs_IsActive` (`IsActive`),
  KEY `IX_AIModelConfigs_Context_IsActive_DisplayOrder` (`Context`,`IsActive`,`DisplayOrder`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AIModelConfigs`
--

LOCK TABLES `AIModelConfigs` WRITE;
/*!40000 ALTER TABLE `AIModelConfigs` DISABLE KEYS */;
INSERT INTO `AIModelConfigs` (`Id`, `ModelName`, `ModelType`, `Provider`, `ApiEndpoint`, `ApiKeyConfigKey`, `SystemPrompt`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (1,'TinyLlama-ClinicalDecisionSupport','Primary','Ollama','tinyllama:latest',NULL,'You are a specialized medical AI assistant. Your task is to generate structured clinical note drafts from patient encounter data. Analyze the provided patient information and create a well-organized, professional clinical note that includes: Chief Complaint, History of Present Illness, Review of Systems, Assessment, and Plan. Ensure the note is clear, concise, and follows standard medical documentation practices.','ClinicalNote',1,1,'2025-11-13 16:37:09.000000',NULL);
INSERT INTO `AIModelConfigs` (`Id`, `ModelName`, `ModelType`, `Provider`, `ApiEndpoint`, `ApiKeyConfigKey`, `SystemPrompt`, `Context`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (2,'TinyLlama-ClinicalDecisionSupport2','Secondary','Ollama','tinyllama:latest',NULL,'You are a medical AI assistant specialized in clinical decision support. Your task is to analyze clinical notes and patient encounters to identify: 1) Possible missed considerations or diagnoses that should be evaluated, 2) Recommended follow-up actions or tests, 3) Potential drug interactions or contraindications, 4) Red flags or warning signs that require attention. Provide a structured list of considerations and recommendations based on evidence-based medicine principles.','ClinicalNote',2,1,'2025-11-13 16:37:09.000000',NULL);
/*!40000 ALTER TABLE `AIModelConfigs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `AIResponseTemplates`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `AIResponseTemplates` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `TemplateKey` varchar(100) NOT NULL,
  `TemplateName` varchar(200) NOT NULL,
  `Content` longtext NOT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `Priority` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT (now(6)),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `CreatedByUserId` int DEFAULT NULL,
  `UpdatedByUserId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_AIResponseTemplates_TemplateKey` (`TemplateKey`),
  KEY `IX_AIResponseTemplates_Priority` (`Priority`),
  KEY `IX_AIResponseTemplates_IsActive` (`IsActive`),
  KEY `IX_AIResponseTemplates_IsActive_Priority` (`IsActive`,`Priority`),
  KEY `IX_AIResponseTemplates_CreatedByUserId` (`CreatedByUserId`),
  KEY `IX_AIResponseTemplates_UpdatedByUserId` (`UpdatedByUserId`),
  CONSTRAINT `FK_AIResponseTemplates_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_AIResponseTemplates_Users_UpdatedByUserId` FOREIGN KEY (`UpdatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=486 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `AIResponseTemplates`
--

LOCK TABLES `AIResponseTemplates` WRITE;
/*!40000 ALTER TABLE `AIResponseTemplates` DISABLE KEYS */;
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (1,'critical_alert','Critical Medical Alert','🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.\n{CRITICAL_VALUES}\n\n**IMMEDIATE MEDICAL ATTENTION REQUIRED:**\n- These values indicate a medical emergency\n- Contact emergency services if symptoms worsen\n- Patient needs immediate medical evaluation','Template for critical medical alert responses',10,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (2,'critical_alert_deterioration','Critical Alert - Deterioration','🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.\n{CRITICAL_VALUES}\n\n**IMMEDIATE MEDICAL ATTENTION REQUIRED:**\n- These values indicate a medical emergency\n- Contact emergency services if symptoms worsen\n- Patient needs immediate medical evaluation','Template for critical alert when deterioration is detected',10,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (3,'concerns_detected','Medical Concerns Detected','⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.','Template for when medical concerns are detected',8,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (4,'stable_status','Stable Status','✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','Template for stable patient status',5,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (5,'improvement_noted','Improvement Noted','✅ **IMPROVEMENT NOTED:** Previous results showed critical values, but current results show normal values.\nThis indicates positive progress, though continued monitoring is recommended.','Template for when improvement is noted from previous critical values',7,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (6,'status_review','Status Review','📊 **Status Review:** Based on available data, the patient appears to be stable with no immediate concerns detected.','Template for general status review',4,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (7,'medical_data_warning','Medical Data Warning','⚠️ **WARNING:** Medical content was found, but critical values may not have been properly detected.\nPlease review the medical data manually to ensure no critical values are missed.\n\n📊 **Status Review:** Based on available data, the patient appears to be stable with no immediate concerns detected.\nHowever, please verify the medical content manually for accuracy.','Template for warning when medical data exists but critical values may not be detected',6,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (8,'recent_patient_activity','Recent Patient Activity','**Recent Patient Activity:**\n{JOURNAL_ENTRIES}\n\nThe patient has been actively engaging with their health tracking.','Template for recent patient activity section',3,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (9,'mood_statistics','Mood Statistics','**Mood Statistics:**\n{JOURNAL_ENTRIES}\n\n- Patient actively tracking health status','Template for mood statistics section',3,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (10,'critical_recommendations','Critical Recommendations','🚨 **IMMEDIATE ACTIONS REQUIRED:**\n1. **Emergency Medical Care**: Contact emergency services immediately\n2. **Hospital Admission**: Patient requires immediate hospitalization\n3. **Specialist Consultation**: Refer to appropriate specialist\n4. **Continuous Monitoring**: Vital signs every 15 minutes\n5. **Immediate Evaluation**: Patient needs immediate medical evaluation','Template for critical medical recommendations',9,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (11,'general_recommendations','General Recommendations','📋 **General Recommendations:**\n1. **Regular Monitoring**: Schedule routine follow-up appointments\n2. **Lifestyle Modifications**: Dietary changes and exercise recommendations\n3. **Medication Review**: Assess current medications and interactions','Template for general medical recommendations',5,1,'2025-11-11 22:24:01.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (12,'journal_fallback_crisis','Journal Fallback - Crisis','I can hear that you\'re going through a really difficult time right now. Please know that you\'re not alone, and it\'s important to reach out to a mental health professional or crisis helpline. Your feelings are valid, and there are people who want to help you through this.','Fallback response for crisis mood in journal entries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (13,'journal_fallback_distressed','Journal Fallback - Distressed','I understand you\'re feeling really bad right now. These feelings are temporary, even though they might not feel that way. Please consider reaching out to someone you trust or a mental health professional. You don\'t have to go through this alone.','Fallback response for distressed mood in journal entries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (14,'journal_fallback_sad','Journal Fallback - Sad','I\'m sorry you\'re feeling sad. It\'s okay to feel this way, and your emotions are valid. Sometimes talking to someone we trust or engaging in activities that bring us comfort can help. Remember that this feeling will pass.','Fallback response for sad mood in journal entries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (15,'journal_fallback_anxious','Journal Fallback - Anxious','I can sense you\'re feeling anxious. Try taking some deep breaths and remember that you\'ve gotten through difficult times before. Consider reaching out to someone you trust or trying some relaxation techniques.','Fallback response for anxious mood in journal entries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (16,'journal_fallback_happy','Journal Fallback - Happy','It\'s wonderful to hear that you\'re feeling good! I\'m glad you\'re taking the time to reflect on positive moments. Keep nurturing these positive feelings and remember to celebrate the good times.','Fallback response for happy mood in journal entries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (17,'journal_fallback_neutral','Journal Fallback - Neutral','Thank you for sharing your thoughts with me. It takes courage to express your feelings, and I appreciate you trusting me with them. Remember that you\'re not alone in whatever you\'re experiencing.','Fallback response for neutral mood in journal entries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (18,'medical_journal_critical','Medical Journal - Critical Values','🚨 **CRITICAL MEDICAL VALUES DETECTED**\n\nThe following critical values require **immediate medical attention**:\n{CRITICAL_VALUES}\n\n**URGENT RECOMMENDATION:** Please seek immediate medical care or contact emergency services. These values indicate a serious medical condition that needs prompt evaluation by a healthcare professional.','Response for critical medical values in journal',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (19,'medical_journal_abnormal','Medical Journal - Abnormal Values','⚠️ **ABNORMAL MEDICAL VALUES DETECTED**\n\nThe following values are concerning and should be monitored:\n{ABNORMAL_VALUES}\n\n**RECOMMENDATION:** Please schedule an appointment with your healthcare provider to discuss these values and determine appropriate next steps.','Response for abnormal medical values in journal',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (20,'medical_journal_normal','Medical Journal - Normal Values','📊 **MEDICAL DATA RECORDED**\n\nThe following values are within normal ranges:\n{NORMAL_VALUES}\n\nThank you for documenting this medical information. Continue to monitor these values and consult with your healthcare provider as needed.','Response for normal medical values in journal',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (21,'medical_journal_generic','Medical Journal - Generic','Thank you for your journal entry. If you have any medical concerns, please don\'t hesitate to discuss them with your healthcare provider.','Generic response for medical journal entries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (22,'emergency_unacknowledged_alert','Emergency - Unacknowledged Alert','🚨 **CRITICAL EMERGENCY ALERT:** {COUNT} unacknowledged emergency incident(s) detected!\n\n**Unacknowledged Emergencies:**\n{UNACKNOWLEDGED_DETAILS}\n\n**Immediate Actions Required:**\n1. Acknowledge all emergency incidents immediately\n2. Contact patient for status check\n3. Conduct fall risk assessment\n4. Review medications for side effects\n5. Consider emergency medical intervention','Template for unacknowledged emergency incidents',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (23,'emergency_acknowledged_history','Emergency - Acknowledged History','📋 **Emergency History:** {COUNT} previously acknowledged incident(s)\n{ACKNOWLEDGED_DETAILS}','Template for acknowledged emergency history',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (24,'emergency_all_acknowledged','Emergency - All Acknowledged','✅ **All emergencies have been acknowledged**\n**Follow-up Actions:**\n1. Monitor patient for any new incidents\n2. Review emergency patterns for trends\n3. Consider preventive measures','Template when all emergencies are acknowledged',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (25,'emergency_critical_medical','Emergency - Critical Medical Values','🚨 **CRITICAL MEDICAL VALUES DETECTED:**\n{CRITICAL_ALERTS}\n\n**IMMEDIATE ACTIONS REQUIRED:**\n1. Contact patient immediately for status check\n2. Consider emergency medical evaluation\n3. Review medications and adjust as needed\n4. Monitor vital signs closely','Template for critical medical values in emergency context',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (26,'emergency_medical_data','Emergency - Medical Data','**Medical Data Analysis:**\n\n{MEDICAL_DATA}','Template for medical data in emergency response',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (27,'emergency_fallback','Emergency - Fallback','🚨 **CRITICAL EMERGENCY ALERT:** Emergency incidents detected requiring immediate attention!','Fallback emergency response',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (28,'patient_wellness_guidelines','Patient - Wellness Guidelines','Here are some general wellness guidelines that can support your mental health:\n\n🌱 **Daily Habits:**\n• Maintain a consistent sleep schedule (7-9 hours)\n• Eat regular, balanced meals with plenty of fruits and vegetables\n• Stay hydrated throughout the day\n• Get some sunlight exposure daily\n\n🧘 **Mental Wellness:**\n• Practice deep breathing exercises for 5-10 minutes daily\n• Try mindfulness or meditation (even 5 minutes helps)\n• Keep a gratitude journal - write down 3 things you\'re grateful for each day\n• Engage in activities you enjoy\n\n💪 **Physical Activity:**\n• Aim for at least 30 minutes of moderate exercise most days\n• Take short walks throughout the day\n• Try gentle stretching or yoga\n\n🤝 **Social Connection:**\n• Stay connected with friends and family\n• Consider joining groups or activities you\'re interested in\n• Don\'t hesitate to reach out when you need support\n\nRemember, these are general guidelines. For personalized advice or if you have specific health concerns, please consult with your doctor.','Wellness guidelines for patients',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (29,'patient_medication_disclaimer','Patient - Medication Disclaimer','I understand you\'re asking about medications or treatments, but I\'m not qualified to provide medical advice. Please consult with your doctor about any medications or treatments. I can help you with general wellness strategies like stress management, relaxation techniques, and healthy lifestyle habits.','Disclaimer for medication questions',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (30,'patient_anxiety_response','Patient - Anxiety Response','I understand you might be feeling anxious. That\'s completely normal. I can suggest some relaxation techniques like deep breathing, progressive muscle relaxation, or grounding exercises. However, if your anxiety is significantly impacting your daily life, please discuss this with your doctor for proper evaluation and treatment options.','Response for anxiety-related questions',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (31,'patient_depression_response','Patient - Depression Response','I hear that you might be feeling down. These feelings are valid and it\'s okay to not be okay. I can offer emotional support and suggest activities that might help, like gentle exercise, spending time in nature, or connecting with loved ones. For persistent feelings of depression, please reach out to your doctor or a mental health professional.','Response for depression-related questions',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (32,'patient_generic_response','Patient - Generic Response','I\'m here to listen and support you. I can help with general wellness advice, emotional support, and relaxation techniques. For any specific medical concerns or treatment questions, please consult with your doctor. What would you like to talk about?','Generic patient response',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (33,'doctor_patient_not_found','Doctor - Patient Not Found','**PATIENT LOOKUP:**\n\n**Status:** ❌ **Patient not found in your assigned patients**\n\n**Current Situation:** The person you\'re asking about does not appear to be one of your assigned patients in the system.\n\n**What This Means:**\n• No patient record found with this name\n• No clinical data available for analysis\n• No treatment history or journal entries to review\n\n**Possible Reasons:**\n• The person is not assigned to you as a patient\n• The name might be misspelled\n• The person might not be registered in the system\n• You might need to check with administration for patient assignments\n\n**Next Steps:**\n• Verify the correct spelling of the patient\'s name\n• Check your patient list to confirm assignments\n• Contact administration if you believe this person should be your patient\n• Use the patient selection dropdown to choose from your assigned patients','Response when patient is not found',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (34,'doctor_no_data','Doctor - No Patient Data','**PATIENT STATUS OVERVIEW:**\n\n**Data Status:** ⚠️ **No data reported yet**\n\n**Clinical Assessment:** This patient has not yet submitted any journal entries or mood tracking data. Without baseline information, I cannot provide specific clinical insights.\n\n**Recommendations:**\n1) **Initial Assessment:** Schedule an in-person or virtual consultation to establish baseline\n2) **Patient Engagement:** Encourage the patient to start using the journaling feature\n3) **Data Collection:** Consider asking about recent mood, sleep, and stress levels during consultation\n4) **Monitoring Setup:** Establish a regular check-in schedule once data collection begins\n\n**Next Steps:** I recommend reaching out to the patient to encourage platform engagement and schedule an initial assessment to gather baseline clinical information.','Response when patient has no data',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (35,'doctor_anxiety_recommendations','Doctor - Anxiety Recommendations','Based on the patient\'s data showing {MOOD_PATTERNS}, I\'d recommend considering: 1) Assessment of anxiety severity using standardized scales, 2) Review of current stressors and triggers, 3) Consideration of CBT or other evidence-based therapies, 4) Evaluation for medication if symptoms are moderate to severe, 5) Sleep hygiene assessment. The patient\'s recent entries suggest {RECENT_PATTERNS}. I recommend asking about specific anxiety symptoms, duration, and functional impact.','Anxiety treatment recommendations for doctors',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (36,'doctor_depression_recommendations','Doctor - Depression Recommendations','Given the patient\'s mood patterns showing {MOOD_PATTERNS}, consider: 1) PHQ-9 or similar depression screening, 2) Assessment of suicidal ideation and safety planning, 3) Review of sleep, appetite, and energy levels, 4) Consideration of antidepressant medication if indicated, 5) Psychotherapy referral. The recent journal entries indicate {RECENT_PATTERNS}. I suggest asking about anhedonia, concentration difficulties, and any recent life stressors.','Depression treatment recommendations for doctors',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (37,'doctor_medication_considerations','Doctor - Medication Considerations','For medication considerations with this patient showing {MOOD_PATTERNS}: 1) Start with first-line treatments (SSRIs for anxiety/depression), 2) Consider patient\'s age, comorbidities, and medication history, 3) Start low and go slow with dosing, 4) Monitor for side effects and efficacy, 5) Consider drug interactions. Recent patterns show {RECENT_PATTERNS}. Always verify current prescribing guidelines and contraindications.','Medication considerations for doctors',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (38,'doctor_generic_response','Doctor - Generic Response','Based on the patient\'s data showing {MOOD_PATTERNS} and recent entries indicating {RECENT_PATTERNS}, I recommend a comprehensive assessment including symptom review, functional impact evaluation, and consideration of both pharmacological and non-pharmacological interventions. What specific aspect of the patient\'s care would you like to explore further?','Generic doctor response',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (39,'admin_trend_analysis','Admin - Trend Analysis','For system-wide trend analysis, I recommend: 1) Regular review of mood distribution reports, 2) Identification of high-risk patients based on patterns, 3) System alerts for concerning trends, 4) Regular staff training on recognizing warning signs, 5) Implementation of automated monitoring systems.','Trend analysis recommendations for admins',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (40,'admin_system_improvements','Admin - System Improvements','System improvement suggestions: 1) Enhanced data analytics dashboard, 2) Automated risk assessment tools, 3) Improved patient engagement features, 4) Staff training programs, 5) Integration with electronic health records, 6) Regular system performance reviews.','System improvement suggestions for admins',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (41,'admin_generic_response','Admin - Generic Response','I can help with administrative insights, system monitoring, data analysis, and operational improvements. What specific administrative aspect would you like to focus on?','Generic admin response',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (42,'fallback_generic','Fallback - Generic','I understand. How can I help you today?','Generic fallback response',5,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (43,'fallback_no_patient_selected','Fallback - No Patient Selected','⚠️ **No Patient Selected**\n\nTo provide personalized insights about a specific patient, please:\n1. Select a patient from the dropdown above\n2. Ask your question about that specific patient\n\nOnce a patient is selected, I can analyze their journal entries, medical content, and provide detailed insights about their mental health status.','Response when no patient is selected',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (44,'fallback_mental_health','Fallback - Mental Health','I\'m here to support your mental health and well-being! How are you feeling today? I can help you with mood tracking, coping strategies, or just provide a listening ear. What\'s on your mind?','Fallback for mental health queries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (45,'fallback_mood_feeling','Fallback - Mood/Feeling','I\'d love to help you explore your feelings and mood. You can track your emotions in the journal section, or we can talk about what you\'re experiencing right now. What\'s going on for you today?','Fallback for mood/feeling queries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (46,'fallback_anxiety','Fallback - Anxiety','I understand you might be feeling anxious. That\'s completely normal and you\'re not alone. Would you like to try some breathing exercises or grounding techniques? I can also help you explore what might be causing these feelings.','Fallback for anxiety queries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (47,'fallback_sad_depressed','Fallback - Sad/Depressed','I hear that you might be feeling sad or down. These feelings are valid and it\'s okay to not be okay. Would you like to talk about what\'s going on? I\'m here to listen and support you through this.','Fallback for sad/depressed queries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (48,'fallback_help_support','Fallback - Help/Support','I\'m here to help and support you! I can assist with mood tracking, provide coping strategies, offer emotional support, or just listen. What kind of support would be most helpful for you right now?','Fallback for help/support queries',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (49,'fallback_default','Fallback - Default','I\'m here as your mental health companion to listen and support you. How are you feeling today? Is there anything about your mental wellness that you\'d like to talk about or explore together?','Default fallback response',5,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (50,'journal_prompt_crisis','Journal Prompt - Crisis','The person is in crisis and needs immediate support. Respond with empathy and encourage seeking professional help.','Prompt context for crisis mood',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (51,'journal_prompt_distressed','Journal Prompt - Distressed','The person is experiencing emotional distress. Provide comfort and gentle encouragement.','Prompt context for distressed mood',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (52,'journal_prompt_sad','Journal Prompt - Sad','The person is feeling sad. Offer empathy and hope.','Prompt context for sad mood',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (53,'journal_prompt_anxious','Journal Prompt - Anxious','The person is feeling anxious. Provide calming reassurance and coping strategies.','Prompt context for anxious mood',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (54,'journal_prompt_happy','Journal Prompt - Happy','The person is feeling positive. Celebrate with them and encourage continued well-being.','Prompt context for happy mood',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (55,'journal_prompt_neutral','Journal Prompt - Neutral','The person is sharing their thoughts. Respond with empathy and understanding.','Prompt context for neutral mood',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (56,'journal_prompt_base','Journal Prompt - Base','You are a compassionate mental health companion. A person has written in their journal: \"{JOURNAL_TEXT}\"\n\n{MOOD_CONTEXT}\n\nRespond with a brief, empathetic message (2-3 sentences) that acknowledges their feelings and provides gentle support. Be warm and encouraging.','Base journal prompt template',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (57,'medical_journal_prompt_base','Medical Journal Prompt - Base','You are a medical AI assistant analyzing a journal entry that contains medical data.\n\nJournal Entry: \"{JOURNAL_TEXT}\"\n\n{MEDICAL_ANALYSIS}\n\nProvide a medical assessment that:\n1. Acknowledges the medical data presented\n2. Provides appropriate medical context and interpretation\n3. Gives clear recommendations based on the values\n4. Maintains a professional, caring tone\n5. Emphasizes the importance of professional medical consultation when appropriate','Base medical journal prompt',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (58,'medical_journal_prompt_critical','Medical Journal Prompt - Critical','🚨 CRITICAL MEDICAL VALUES DETECTED:\n{CRITICAL_VALUES}\n\nThis requires immediate medical attention. Respond with urgency and recommend immediate consultation with a healthcare provider.','Medical journal prompt for critical values',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (59,'medical_journal_prompt_abnormal','Medical Journal Prompt - Abnormal','⚠️ ABNORMAL MEDICAL VALUES DETECTED:\n{ABNORMAL_VALUES}\n\nThese values are concerning and should be monitored closely. Recommend follow-up with healthcare provider.','Medical journal prompt for abnormal values',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (60,'medical_journal_prompt_normal','Medical Journal Prompt - Normal','✅ NORMAL MEDICAL VALUES:\n{NORMAL_VALUES}','Medical journal prompt for normal values',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (61,'recommendations_critical','Recommendations - Critical','🚨 **IMMEDIATE ACTIONS REQUIRED:**\n1. **Emergency Medical Care**: Contact emergency services immediately\n2. **Hospital Admission**: Patient requires immediate hospitalization\n3. **Specialist Consultation**: Refer to appropriate specialist\n4. **Continuous Monitoring**: Vital signs every 15 minutes\n5. **Immediate Intervention**: Consider immediate medical intervention based on critical values','Clinical recommendations for critical conditions',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (62,'recommendations_abnormal','Recommendations - Abnormal','⚠️ **MEDICAL MANAGEMENT NEEDED:**\n1. **Primary Care Follow-up**: Schedule appointment within 24-48 hours\n2. **Laboratory Monitoring**: Repeat blood work in 1-2 weeks\n3. **Lifestyle Modifications**: Dietary changes and exercise recommendations\n4. **Medication Review**: Assess current medications and interactions','Clinical recommendations for abnormal conditions',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (63,'recommendations_stable','Recommendations - Stable','✅ **CURRENT STATUS: STABLE**\n1. **Continue Current Care**: Maintain existing treatment plan\n2. **Regular Monitoring**: Schedule routine follow-up appointments\n3. **Preventive Care**: Focus on maintaining current health status','Clinical recommendations for stable conditions',10,1,'2025-11-12 08:58:29.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (64,'section_patient_overview','Section - Patient Medical Overview','**Patient Medical Overview:**','Header for patient medical overview section',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (65,'section_recent_activity','Section - Recent Patient Activity','**Recent Patient Activity:**','Header for recent patient activity section',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (66,'section_clinical_assessment','Section - Clinical Assessment','**Clinical Assessment:**','Header for clinical assessment section',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (67,'section_recommendations','Section - Recommendations','**Recommendations:**','Header for recommendations section',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (68,'section_clinical_recommendations','Section - Clinical Recommendations','**Clinical Recommendations:**','Header for clinical recommendations section',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (69,'section_areas_of_concern','Section - Areas of Concern','**Areas of Concern Analysis:**','Header for areas of concern section',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (70,'section_chat_history','Section - Chat History','**Chat History:** Patient has been engaging in conversations with the AI assistant.','Chat history section message',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (71,'section_clinical_notes','Section - Clinical Notes','**Clinical Notes:** Recent clinical documentation is available for review.','Clinical notes section message',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (72,'section_emergency_incidents','Section - Emergency Incidents','⚠️ **EMERGENCY INCIDENTS:** Emergency incidents have been recorded. Please review the emergency dashboard for details.','Emergency incidents section message',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (73,'section_previously_acknowledged','Section - Previously Acknowledged','**Previously Acknowledged Emergencies:**','Header for previously acknowledged emergencies',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (74,'section_medical_data_analysis','Section - Medical Data Analysis','**Medical Data Analysis:**','Header for medical data analysis section',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (75,'status_no_journal_entries','Status - No Journal Entries','- No recent journal entries found.','Message when no journal entries are found',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (76,'status_patient_tracking','Status - Patient Tracking','The patient has been actively engaging with their health tracking.','Message about patient engagement',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (77,'status_medical_alerts_detected','Status - Medical Alerts Detected','🚨 **MEDICAL ALERTS DETECTED:**','Header for medical alerts detected',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (78,'status_medical_monitoring_needed','Status - Medical Monitoring Needed','**MEDICAL MONITORING NEEDED:** Abnormal values detected that require medical attention.','Message about medical monitoring needed',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (79,'status_continued_monitoring','Status - Continued Monitoring','**CURRENT STATUS:** Patient shows normal values, but previous concerning results require continued monitoring.','Message about continued monitoring',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (80,'status_high_priority_concerns','Status - High Priority Concerns','🚨 **High Priority Concerns:**','Header for high priority concerns',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (81,'status_no_concerns','Status - No Concerns','✅ No immediate concerns detected in the current data.','Message when no concerns are detected',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (82,'assessment_critical_intervention','Assessment - Critical Intervention','The patient requires immediate medical attention due to critical values. Urgent intervention is necessary.','Assessment message for critical values',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (83,'assessment_abnormal_monitoring','Assessment - Abnormal Monitoring','The patient shows some abnormal values that require monitoring and follow-up care. Schedule a medical review.','Assessment message for abnormal values',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (84,'assessment_stable_condition','Assessment - Stable Condition','The patient appears to be in stable condition with no immediate medical concerns. Continue routine monitoring and care.','Assessment message for stable condition',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (85,'assessment_in_response','Assessment - In Response','In response to your question: \"{USER_QUESTION}\"','Assessment message prefix with user question',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (86,'action_immediate_evaluation','Action - Immediate Evaluation','- Immediate medical evaluation required','Action item for immediate evaluation',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (87,'action_emergency_department','Action - Emergency Department','- Consider emergency department visit','Action item for emergency department',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (88,'action_notify_doctors','Action - Notify Doctors','- Notify assigned doctors immediately','Action item for notifying doctors',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (89,'action_followup_appointment','Action - Follow-up Appointment','- Schedule follow-up appointment within 1-2 weeks','Action item for follow-up appointment',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (90,'action_repeat_tests','Action - Repeat Tests','- Repeat laboratory tests as indicated','Action item for repeating tests',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (91,'action_monitor_patient','Action - Monitor Patient','- Monitor patient closely for any changes','Action item for monitoring patient',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (92,'action_continue_care','Action - Continue Care','- Continue current care plan','Action item for continuing care',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (93,'action_maintain_schedule','Action - Maintain Schedule','- Maintain routine follow-up schedule','Action item for maintaining schedule',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (94,'action_encourage_tracking','Action - Encourage Tracking','- Encourage continued health tracking','Action item for encouraging tracking',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (95,'medical_content_analysis','Medical Content - Analysis','📊 **Medical Content Analysis:** I\'ve reviewed the patient\'s medical content. ','Message about medical content analysis',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (96,'medical_content_warning','Medical Content - Warning','⚠️ **IMPORTANT:** While medical content was found, I was unable to detect specific critical values in the current analysis. \nPlease ensure all test results are properly formatted and accessible for accurate medical assessment.','Warning about medical content detection',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (97,'medical_content_critical_care','Medical Content - Critical Care','Please ensure all critical values are properly addressed with appropriate medical care.','Message about critical care',5,1,'2025-11-12 10:33:09.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (150,'recommendations_critical_detailed','Recommendations - Critical Detailed','🚨 **IMMEDIATE ACTIONS REQUIRED:**\n1. **Emergency Medical Care**: Contact emergency services immediately\n2. **Hospital Admission**: Patient requires immediate hospitalization\n3. **Specialist Consultation**: Refer to appropriate specialist\n4. **Continuous Monitoring**: Vital signs every 15 minutes\n5. **Immediate Intervention**: Consider immediate medical intervention based on critical values','Detailed critical recommendations fallback',5,1,'2025-11-12 10:46:32.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (151,'recommendations_abnormal_detailed','Recommendations - Abnormal Detailed','⚠️ **MEDICAL MANAGEMENT NEEDED:**\n1. **Primary Care Follow-up**: Schedule appointment within 24-48 hours\n2. **Laboratory Monitoring**: Repeat blood work in 1-2 weeks\n3. **Lifestyle Modifications**: Dietary changes and exercise recommendations\n4. **Medication Review**: Assess current medications and interactions','Detailed abnormal recommendations fallback',5,1,'2025-11-12 10:46:32.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (152,'recommendations_stable_detailed','Recommendations - Stable Detailed','✅ **CURRENT STATUS: STABLE**\n1. **Continue Current Care**: Maintain existing treatment plan\n2. **Regular Monitoring**: Schedule routine follow-up appointments\n3. **Preventive Care**: Focus on maintaining current health status','Detailed stable recommendations fallback',5,1,'2025-11-12 10:46:32.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (239,'fallback_critical_alert_header','Fallback - Critical Alert Header','🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention. ','Fallback for critical alert header',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (240,'fallback_immediate_attention','Fallback - Immediate Attention','**IMMEDIATE MEDICAL ATTENTION REQUIRED:**','Fallback for immediate attention header',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (241,'fallback_emergency_action1','Fallback - Emergency Action 1','- These values indicate a medical emergency','Fallback for emergency action 1',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (242,'fallback_emergency_action2','Fallback - Emergency Action 2','- Contact emergency services if symptoms worsen','Fallback for emergency action 2',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (243,'fallback_emergency_action3','Fallback - Emergency Action 3','- Patient needs immediate medical evaluation','Fallback for emergency action 3',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (244,'fallback_medical_alerts_detected','Fallback - Medical Alerts Detected','🚨 **MEDICAL ALERTS DETECTED:**','Fallback for medical alerts detected header',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (245,'fallback_medical_monitoring_needed','Fallback - Medical Monitoring Needed','**MEDICAL MONITORING NEEDED:** Abnormal values detected that require medical attention.','Fallback for medical monitoring needed',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (246,'fallback_continued_monitoring','Fallback - Continued Monitoring','**CURRENT STATUS:** Patient shows normal values, but previous concerning results require continued monitoring.','Fallback for continued monitoring',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (247,'fallback_medical_content_analysis','Fallback - Medical Content Analysis','📊 **Medical Content Analysis:** I\'ve reviewed the patient\'s medical content. ','Fallback for medical content analysis',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (248,'fallback_medical_content_important','Fallback - Medical Content Important','⚠️ **IMPORTANT:** While medical content was found, I was unable to detect specific critical values in the current analysis. \nPlease ensure all test results are properly formatted and accessible for accurate medical assessment.','Fallback for medical content important warning',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (249,'fallback_medical_content_critical_care','Fallback - Medical Content Critical Care','Please ensure all critical values are properly addressed with appropriate medical care.','Fallback for medical content critical care',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (250,'fallback_recent_activity_header','Fallback - Recent Activity Header','📝 **Recent Patient Activity:**','Fallback for recent activity header',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (251,'fallback_patient_overview','Fallback - Patient Overview','**Patient Medical Overview:**','Fallback for patient overview header',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (252,'fallback_stable_status','Fallback - Stable Status','✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','Fallback for stable status',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (253,'fallback_abnormal_values','Fallback - Abnormal Values','⚠️ **ABNORMAL VALUES DETECTED:** Some test results are outside normal ranges and require monitoring.','Fallback for abnormal values',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (254,'fallback_recent_activity','Fallback - Recent Activity','**Recent Patient Activity:**','Fallback for recent activity',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (255,'fallback_no_journal_entries','Fallback - No Journal Entries','- No recent journal entries found.','Fallback for no journal entries',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (256,'fallback_chat_history','Fallback - Chat History','**Chat History:** Patient has been engaging in conversations with the AI assistant.','Fallback for chat history',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (257,'fallback_clinical_notes','Fallback - Clinical Notes','**Clinical Notes:** Recent clinical documentation is available for review.','Fallback for clinical notes',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (258,'fallback_emergency_incidents','Fallback - Emergency Incidents','⚠️ **EMERGENCY INCIDENTS:** Emergency incidents have been recorded. Please review the emergency dashboard for details.','Fallback for emergency incidents',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (259,'fallback_clinical_assessment','Fallback - Clinical Assessment','**Clinical Assessment:**','Fallback for clinical assessment header',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (260,'fallback_critical_intervention','Fallback - Critical Intervention','The patient requires immediate medical attention due to critical values. Urgent intervention is necessary.','Fallback for critical intervention',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (261,'fallback_abnormal_monitoring','Fallback - Abnormal Monitoring','The patient shows some abnormal values that require monitoring and follow-up care. Schedule a medical review.','Fallback for abnormal monitoring',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (262,'fallback_stable_condition','Fallback - Stable Condition','The patient appears to be in stable condition with no immediate medical concerns. Continue routine monitoring and care.','Fallback for stable condition',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (263,'fallback_recommendations','Fallback - Recommendations','**Recommendations:**','Fallback for recommendations header',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (264,'fallback_previously_acknowledged','Fallback - Previously Acknowledged','**Previously Acknowledged Emergencies:**','Fallback for previously acknowledged header',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (265,'fallback_medical_data_analysis','Fallback - Medical Data Analysis','**Medical Data Analysis:**','Fallback for medical data analysis header',1,1,'2025-11-12 11:21:36.000000','2025-11-12 12:12:15.000000',NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (355,'alert_critical_blood_pressure','Alert - Critical Blood Pressure','🚨 **CRITICAL BLOOD PRESSURE**: {SYSTOLIC}/{DIASTOLIC} - HYPERTENSIVE CRISIS! Immediate medical intervention required!','Template for critical blood pressure alerts',10,1,'2025-11-12 12:12:15.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (356,'alert_high_blood_pressure','Alert - High Blood Pressure','⚠️ **HIGH BLOOD PRESSURE**: {SYSTOLIC}/{DIASTOLIC} - Requires immediate attention','Template for high blood pressure alerts',8,1,'2025-11-12 12:12:15.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (357,'alert_critical_hemoglobin','Alert - Critical Hemoglobin','🚨 **CRITICAL HEMOGLOBIN**: {HEMOGLOBIN_VALUE} g/dL - SEVERE ANEMIA! Blood transfusion may be required!','Template for critical hemoglobin alerts',10,1,'2025-11-12 12:12:15.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (358,'alert_low_hemoglobin','Alert - Low Hemoglobin','⚠️ **LOW HEMOGLOBIN**: {HEMOGLOBIN_VALUE} g/dL - Moderate anemia, requires monitoring','Template for low hemoglobin alerts',8,1,'2025-11-12 12:12:15.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (359,'alert_critical_triglycerides','Alert - Critical Triglycerides','🚨 **CRITICAL TRIGLYCERIDES**: {TRIGLYCERIDES_VALUE} mg/dL - EXTREMELY HIGH! Risk of pancreatitis!','Template for critical triglycerides alerts',10,1,'2025-11-12 12:12:15.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (360,'alert_high_triglycerides','Alert - High Triglycerides','⚠️ **HIGH TRIGLYCERIDES**: {TRIGLYCERIDES_VALUE} mg/dL - Requires dietary intervention','Template for high triglycerides alerts',8,1,'2025-11-12 12:12:15.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (477,'intelligent_context_error','Intelligent Context - Error','I apologize, but I encountered an error processing your question. Please try rephrasing it or contact support if the issue persists.','Error message for intelligent context processing failures',10,1,'2025-11-12 17:10:25.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (478,'intelligent_context_no_patient_medical','Intelligent Context - No Patient Medical','**General Medical Information Request**\n\nYou\'re asking: \"{QUESTION}\"\n\nTo provide personalized medical insights, please:\n1. **Select a specific patient** from the dropdown above\n2. **Ask your question in the context of that patient\'s care**\n\nThis will allow me to provide:\n- Patient-specific medical assessments\n- Personalized treatment recommendations\n- Context-aware clinical guidance\n- Integration with the patient\'s medical history and current data\n\nIf you need general medical information without patient context, I recommend consulting medical literature or professional medical resources.','Response when no patient is selected for medical questions',10,1,'2025-11-12 17:10:25.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (479,'intelligent_context_no_patient_resources','Intelligent Context - No Patient Resources','**Medical Resources Search**\n\nTo provide personalized medical facility recommendations, please select a specific patient first.\n\n=== GENERAL MEDICAL RESOURCES ===\n{WEB_RESULTS}','Response when no patient is selected for resource questions',10,1,'2025-11-12 17:10:25.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (480,'intelligent_context_no_patient_recommendations','Intelligent Context - No Patient Recommendations','**General Medical Recommendations Request**\n\nYou\'re asking: \"{QUESTION}\"\n\nTo provide personalized medical recommendations, please:\n1. **Select a specific patient** from the dropdown above\n2. **Ask your question in the context of that patient\'s care**\n\nThis will allow me to provide:\n- Patient-specific treatment recommendations\n- Personalized care approaches\n- Context-aware clinical guidance\n- Integration with the patient\'s medical history and current data\n\nIf you need general medical recommendations without patient context, I recommend consulting medical literature or professional medical resources.','Response when no patient is selected for recommendation questions',10,1,'2025-11-12 17:10:25.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (481,'intelligent_context_non_patient','Intelligent Context - Non-Patient Query','**Query Not Applicable to Patient Care**\n\nI understand you\'re asking about: \"{QUESTION}\"\n\nHowever, this question appears to be unrelated to patient care or medical practice. As a clinical AI assistant, I\'m designed to help with:\n\n- Patient medical assessments and status updates\n- Clinical recommendations and treatment approaches  \n- Medical resource identification and referrals\n- Healthcare provider decision support\n\nFor questions about entertainment, celebrities, or other non-medical topics, please use a general-purpose AI assistant or search engine.\n\nIf you have a medical question related to patient care, I\'d be happy to help with that instead.','Response for non-patient related questions',10,1,'2025-11-12 17:10:25.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (482,'intelligent_context_general_medical','Intelligent Context - General Medical','**General Medical Information Request**\n\nYou\'re asking: \"{QUESTION}\"\n\nWhile I can provide general medical information, for the most accurate and personalized guidance, please:\n\n1. **Select a specific patient** from the dropdown above\n2. **Ask your question in the context of that patient\'s care**\n\nThis will allow me to provide:\n- Patient-specific medical assessments\n- Personalized treatment recommendations\n- Context-aware clinical guidance\n- Integration with the patient\'s medical history and current data\n\nIf you need general medical information without patient context, I recommend consulting medical literature or professional medical resources.','Response for general medical questions',10,1,'2025-11-12 17:10:25.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (483,'intelligent_context_patient_resources','Intelligent Context - Patient Resources','**Medical Resource Information for {PATIENT_INFO}:**\n\n{WEB_RESULTS}\n\n---\nPlease note: This information is for guidance only. Always verify details with the medical facility directly.','Response for patient resource questions with patient context',10,1,'2025-11-12 17:10:25.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (484,'intelligent_context_web_search','Intelligent Context - Web Search','**Medical Facilities Search for: {QUERY}**\n\n{ZIP_CODE_SECTION}**Recommended Search Strategy:**\n1. **Emergency Care**: Search for \'emergency room near {ZIP_CODE}\' or \'urgent care {ZIP_CODE}\'\n2. **Hospitals**: Search for \'hospitals near {ZIP_CODE}\' or \'medical centers {ZIP_CODE}\'\n3. **Specialists**: Search for \'hematologist near {ZIP_CODE}\' (for anemia treatment)\n4. **Insurance**: Check which facilities accept your insurance\n\n**Key Considerations for This Patient:**\n- **Severe Anemia (Hemoglobin 6.0)**: Requires immediate blood transfusion capability\n- **Critical Triglycerides (640)**: Needs cardiology/endocrinology specialists\n- **Emergency Priority**: Look for Level 1 trauma centers or major hospitals\n\n**Immediate Action Required:**\n- Call 911 or go to nearest emergency room immediately\n- This patient\'s condition requires urgent medical attention\n- Do not delay seeking emergency care','Web search response template',10,1,'2025-11-12 17:10:25.000000',NULL,NULL,NULL);
INSERT INTO `AIResponseTemplates` (`Id`, `TemplateKey`, `TemplateName`, `Content`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (485,'intelligent_context_web_search_error','Intelligent Context - Web Search Error','Web search is currently unavailable. Please use standard search engines to find medical facilities.','Error message for web search failures',10,1,'2025-11-12 17:10:25.000000',NULL,NULL,NULL);
/*!40000 ALTER TABLE `AIResponseTemplates` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Appointments`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `Appointments` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `DoctorId` int NOT NULL,
  `PatientId` int NOT NULL,
  `AppointmentDateTime` datetime(6) NOT NULL,
  `Duration` time(6) NOT NULL,
  `AppointmentType` int NOT NULL,
  `Status` int NOT NULL,
  `Reason` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Notes` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedByUserId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `IsBusinessHours` tinyint(1) NOT NULL,
  `DayBeforeReminderSent` tinyint(1) NOT NULL DEFAULT '0',
  `DayOfReminderSent` tinyint(1) NOT NULL DEFAULT '0',
  `TimeZoneId` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT (_utf8mb4'UTC'),
  PRIMARY KEY (`Id`),
  KEY `IX_Appointments_AppointmentDateTime` (`AppointmentDateTime`),
  KEY `IX_Appointments_AppointmentType` (`AppointmentType`),
  KEY `IX_Appointments_CreatedByUserId` (`CreatedByUserId`),
  KEY `IX_Appointments_DoctorId` (`DoctorId`),
  KEY `IX_Appointments_DoctorId_AppointmentDateTime` (`DoctorId`,`AppointmentDateTime`),
  KEY `IX_Appointments_IsActive` (`IsActive`),
  KEY `IX_Appointments_PatientId` (`PatientId`),
  KEY `IX_Appointments_Status` (`Status`),
  CONSTRAINT `FK_Appointments_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Appointments_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Appointments_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Appointments`
--

LOCK TABLES `Appointments` WRITE;
/*!40000 ALTER TABLE `Appointments` DISABLE KEYS */;
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (1,2,3,'2025-11-04 09:00:00.000000','00:30:00.000000',1,1,'General checkup',NULL,1,'2025-11-03 17:47:42.820654',NULL,1,1,1,1,'UTC');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (2,2,3,'2025-11-04 09:30:00.000000','00:30:00.000000',1,5,'test',NULL,1,'2025-11-03 17:52:51.736350','2025-11-03 17:57:22.392306',1,1,0,0,'UTC');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (3,2,3,'2025-11-06 10:30:00.000000','00:30:00.000000',1,1,'test','testing1231',1,'2025-11-03 17:58:26.528260','2025-11-05 17:43:36.856699',1,1,1,1,'UTC');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (4,2,3,'2025-11-13 15:00:00.000000','00:30:00.000000',1,1,'test','test',1,'2025-11-03 19:55:42.937839','2025-11-13 17:26:50.261620',1,1,1,1,'America/Chicago');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (5,2,3,'2025-11-05 16:00:00.000000','00:30:00.000000',1,1,'test',NULL,2,'2025-11-05 18:18:51.713918',NULL,1,1,0,0,'America/New_York');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (6,2,3,'2025-11-05 15:00:00.000000','00:30:00.000000',1,5,'test',NULL,2,'2025-11-05 18:19:29.918865','2025-11-05 18:22:28.678311',1,1,0,0,'America/Chicago');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (7,2,3,'2025-11-05 17:00:00.000000','00:30:00.000000',1,1,'test',NULL,2,'2025-11-05 18:32:27.799287',NULL,1,0,0,0,'America/Chicago');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (8,2,3,'2025-11-05 15:00:00.000000','00:30:00.000000',1,1,'test',NULL,2,'2025-11-05 18:34:28.108695',NULL,1,1,0,0,'America/Chicago');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (9,2,3,'2025-11-10 09:00:00.000000','00:30:00.000000',1,1,'Doctors Visit',NULL,2,'2025-11-09 18:38:18.131501',NULL,1,1,1,1,'America/New_York');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (10,2,3,'2025-11-13 12:00:00.000000','00:30:00.000000',1,1,'test22',NULL,2,'2025-11-13 17:38:05.438756',NULL,1,1,0,0,'America/Chicago');
INSERT INTO `Appointments` (`Id`, `DoctorId`, `PatientId`, `AppointmentDateTime`, `Duration`, `AppointmentType`, `Status`, `Reason`, `Notes`, `CreatedByUserId`, `CreatedAt`, `UpdatedAt`, `IsActive`, `IsBusinessHours`, `DayBeforeReminderSent`, `DayOfReminderSent`, `TimeZoneId`) VALUES (11,2,3,'2025-11-28 09:00:00.000000','00:30:00.000000',1,1,'General','Test',2,'2025-11-27 16:43:01.739479',NULL,1,1,1,1,'America/New_York');
/*!40000 ALTER TABLE `Appointments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ChatMessages`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `ChatMessages` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SessionId` int NOT NULL,
  `Role` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Timestamp` datetime(6) NOT NULL,
  `IsMedicalData` tinyint(1) NOT NULL,
  `MessageType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Metadata` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_ChatMessages_SessionId` (`SessionId`),
  CONSTRAINT `FK_ChatMessages_ChatSessions_SessionId` FOREIGN KEY (`SessionId`) REFERENCES `ChatSessions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=284 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ChatMessages`
--

LOCK TABLES `ChatMessages` WRITE;
/*!40000 ALTER TABLE `ChatMessages` DISABLE KEYS */;
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (202,38,'User','status?','2025-11-11 17:11:17.425641',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T17:11:17.421253Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (203,38,'Assistant','**Patient Status Assessment:**\n🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.\n- 🚨 🚨 CRITICAL: Severe Anemia - Hemoglobin 6 g/dL (Normal: 12-16 g/dL)\n- 🚨 🚨 CRITICAL: Extremely High Triglycerides - 640 mg/dL (Normal: <150 mg/dL)\n\n**IMMEDIATE MEDICAL ATTENTION REQUIRED:**\n- These values indicate a medical emergency\n- Contact emergency services if symptoms worsen\n- Patient needs immediate medical evaluation','2025-11-11 17:11:17.871963',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (204,38,'User','status?','2025-11-11 17:11:45.151873',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T17:11:45.151789Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (205,38,'Assistant','**Patient Status Assessment:**\n🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.\n- Critical medical values detected - review test results for details\n\n**IMMEDIATE MEDICAL ATTENTION REQUIRED:**\n- These values indicate a medical emergency\n- Contact emergency services if symptoms worsen\n- Patient needs immediate medical evaluation','2025-11-11 17:11:45.262050',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (206,39,'User','status?','2025-11-11 17:12:07.808037',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T17:12:07.80794Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (207,39,'Assistant','**Patient Status Assessment:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-11 17:12:07.901949',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (208,40,'User','status?','2025-11-11 17:25:10.412534',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T17:25:10.409947Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (209,40,'Assistant','**Patient Status Assessment:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-11 17:25:10.753767',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (210,40,'User','status?','2025-11-11 17:27:43.185703',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T17:27:43.185555Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (211,40,'Assistant','**Patient Status Assessment:**\n🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.\n- Critical medical values detected - review test results for details\n\n**IMMEDIATE MEDICAL ATTENTION REQUIRED:**\n- These values indicate a medical emergency\n- Contact emergency services if symptoms worsen\n- Patient needs immediate medical evaluation','2025-11-11 17:27:43.396202',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (212,39,'User','status?','2025-11-11 17:29:51.824583',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T17:29:51.824212Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (213,39,'Assistant','**Patient Status Assessment:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-11 17:29:52.041256',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (214,41,'User','status?','2025-11-11 17:30:55.569673',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T17:30:55.569313Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (215,41,'Assistant','**Patient Status Assessment:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-11 17:30:55.757796',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (216,38,'User','status?','2025-11-11 17:35:58.205323',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T17:35:58.202375Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (217,38,'Assistant','**Patient Status Assessment:**\n🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.\n- Critical medical values detected - review test results for details\n\n**IMMEDIATE MEDICAL ATTENTION REQUIRED:**\n- These values indicate a medical emergency\n- Contact emergency services if symptoms worsen\n- Patient needs immediate medical evaluation','2025-11-11 17:35:58.588386',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (218,42,'User','status?','2025-11-11 17:36:21.382612',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T17:36:21.382435Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (219,42,'Assistant','**Patient Status Assessment:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-11 17:36:21.503489',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (220,42,'User','status','2025-11-11 21:28:44.677209',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T21:28:44.674482Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (221,42,'Assistant','**Patient Status Assessment:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-11 21:28:45.078761',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (222,42,'User','status?','2025-11-11 21:34:12.010879',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T21:34:12.010783Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (223,42,'Assistant','**Patient Status Assessment:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-11 21:34:12.226016',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (224,42,'User','status?','2025-11-11 22:25:29.588975',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T22:25:29.586645Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (225,42,'Assistant','**Patient Status Assessment:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values that require attention and monitoring.','2025-11-11 22:25:29.976549',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (226,42,'User','status?','2025-11-11 22:26:40.427269',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-11T22:26:40.427156Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (227,42,'Assistant','**Patient Status Assessment:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values that require attention and monitoring.','2025-11-11 22:26:40.537340',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (228,43,'User','status?','2025-11-12 14:05:50.837274',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T14:05:50.834979Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (229,43,'Assistant','**Patient Status Assessment:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-12 14:05:50.979905',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (230,43,'User','status?','2025-11-12 14:06:46.369552',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T14:06:46.369369Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (231,43,'Assistant','**Patient Status Assessment:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.','2025-11-12 14:06:46.468267',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (232,43,'User','status?','2025-11-12 14:08:04.928692',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T14:08:04.928628Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (233,43,'Assistant','**Patient Status Assessment:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.','2025-11-12 14:08:05.035182',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (238,45,'User','normal values of glucose?','2025-11-12 14:09:39.821848',1,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":null,\"Timestamp\":\"2025-11-12T14:09:39.821798Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (239,45,'Assistant','**Patient Medical Overview:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.\n\n**Recent Patient Activity:**\n- No recent journal entries found.\n\n**Clinical Assessment:**\nThe patient appears to be in stable condition with no immediate medical concerns. Continue routine monitoring and care.\n\n**Recommendations:**\n- Continue current care plan\n- Maintain routine follow-up schedule\n- Encourage continued health tracking','2025-11-12 14:09:39.934242',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (240,45,'User','what are the normal values of glucose?','2025-11-12 14:10:16.511025',1,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":null,\"Timestamp\":\"2025-11-12T14:10:16.510671Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (241,45,'Assistant','**Patient Medical Overview:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.\n\n**Recent Patient Activity:**\n- No recent journal entries found.\n\n**Clinical Assessment:**\nThe patient appears to be in stable condition with no immediate medical concerns. Continue routine monitoring and care.\n\n**Recommendations:**\n- Continue current care plan\n- Maintain routine follow-up schedule\n- Encourage continued health tracking','2025-11-12 14:10:16.600897',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (242,45,'User','what are the normal ranges of glucose?','2025-11-12 14:17:46.301014',1,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":null,\"Timestamp\":\"2025-11-12T14:17:46.293925Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (243,45,'Assistant','Normal blood glucose (blood sugar) levels vary depending on when you last ate:\n- **Fasting (before eating)**: 70-100 mg/dL (3.9-5.6 mmol/L) is considered normal\n- **After meals (2 hours)**: Less than 140 mg/dL (7.8 mmol/L) is considered normal\n- **Random glucose**: 70-140 mg/dL (3.9-7.8 mmol/L) is typically normal\n\n**High glucose (hyperglycemia)**: Fasting levels above 126 mg/dL (7.0 mmol/L) or random levels above 200 mg/dL (11.1 mmol/L) may indicate diabetes and require medical evaluation.\n\n**Low glucose (hypoglycemia)**: Levels below 70 mg/dL (3.9 mmol/L) can cause symptoms like shakiness, sweating, confusion, and require immediate treatment.\n\nIf you have concerns about your blood glucose levels, please consult with your healthcare provider for proper evaluation and management.','2025-11-12 14:17:46.449887',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (244,44,'User','what are the normal ranges of glucose?','2025-11-12 14:18:18.212308',1,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T14:18:18.212198Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (245,44,'Assistant','**Patient Status Assessment:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-12 14:18:18.397595',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (246,45,'User','what are critical or serious values of glucose?','2025-11-12 14:19:56.804715',1,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":null,\"Timestamp\":\"2025-11-12T14:19:56.804264Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (247,45,'Assistant','Normal blood glucose (blood sugar) levels vary depending on when you last ate:\n- **Fasting (before eating)**: 70-100 mg/dL (3.9-5.6 mmol/L) is considered normal\n- **After meals (2 hours)**: Less than 140 mg/dL (7.8 mmol/L) is considered normal\n- **Random glucose**: 70-140 mg/dL (3.9-7.8 mmol/L) is typically normal\n\n**High glucose (hyperglycemia)**: Fasting levels above 126 mg/dL (7.0 mmol/L) or random levels above 200 mg/dL (11.1 mmol/L) may indicate diabetes and require medical evaluation.\n\n**Low glucose (hypoglycemia)**: Levels below 70 mg/dL (3.9 mmol/L) can cause symptoms like shakiness, sweating, confusion, and require immediate treatment.\n\nIf you have concerns about your blood glucose levels, please consult with your healthcare provider for proper evaluation and management.','2025-11-12 14:19:56.822904',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (248,44,'User','status?','2025-11-12 14:23:28.381090',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T14:23:28.380915Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (249,44,'Assistant','**Patient Status Assessment:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.','2025-11-12 14:23:28.581241',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (250,44,'User','status?','2025-11-12 14:24:07.159377',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T14:24:07.159178Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (251,44,'Assistant','**Patient Status Assessment:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.','2025-11-12 14:24:07.270449',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (252,44,'User','status?','2025-11-12 14:34:39.153012',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T14:34:39.150619Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (253,44,'Assistant','**Response to: \"AI Health Check for Patient\"**\n\n✅ The patient appears to be stable with no immediate concerns detected.','2025-11-12 14:34:39.289535',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (254,44,'User','status?','2025-11-12 14:40:06.857042',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T14:40:06.853921Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (255,44,'Assistant','**Patient Medical Overview:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.','2025-11-12 14:40:07.070410',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (256,46,'User','status?','2025-11-12 14:41:27.498763',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T14:41:27.498722Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (257,46,'Assistant','**Patient Medical Overview:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-12 14:41:27.747623',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (258,46,'User','status?','2025-11-12 16:06:54.967383',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T16:06:54.959277Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (259,46,'Assistant','**Patient Medical Overview:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.','2025-11-12 16:06:55.127006',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (260,46,'User','status?','2025-11-12 16:35:27.623253',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T16:35:27.614427Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (261,46,'Assistant','**Patient Medical Overview:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.','2025-11-12 16:35:28.613495',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (262,47,'User','status?','2025-11-12 19:19:13.772918',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T19:19:13.771236Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (263,47,'Assistant','**Patient Medical Overview:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-12 19:19:13.908795',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (264,47,'User','status?','2025-11-12 19:19:33.922581',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T19:19:33.922445Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (265,47,'Assistant','**Patient Medical Overview:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations.','2025-11-12 19:19:34.082005',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (266,47,'User','status?','2025-11-12 19:20:03.313919',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T19:20:03.313736Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (267,47,'Assistant','**Patient Medical Overview:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations.','2025-11-12 19:20:03.547043',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (268,48,'User','status?','2025-11-12 19:20:32.270704',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-12T19:20:32.270571Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (269,48,'Assistant','**Patient Medical Overview:**\n✅ **CURRENT STATUS: STABLE** - The patient shows normal values with no immediate concerns.','2025-11-12 19:20:32.363826',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (270,49,'User','status?','2025-11-13 19:51:54.421104',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-13T19:51:54.419066Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (271,49,'Assistant','**Patient Medical Overview:**\n🚨 **CRITICAL MEDICAL ALERT:** The patient has critical medical values that require immediate attention.\n- 🚨 🚨 CRITICAL: Severe Anemia - Hemoglobin 6 g/dL (Normal: 12-16 g/dL)\n- 🚨 🚨 CRITICAL: Extremely High Triglycerides - 640 mg/dL (Normal: <150 mg/dL)\n\n**IMMEDIATE MEDICAL ATTENTION REQUIRED:**\n- These values indicate a medical emergency\n- Contact emergency services if symptoms worsen\n- Patient needs immediate medical evaluation\n\n**Recent Patient Activity:**\n[11/13/2025] Mood: Happy\n[11/13/2025] Mood: Neutral\n[10/23/2025] Mood: Neutral','2025-11-13 19:51:54.611593',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (272,50,'User','normal values of glucose?','2025-11-13 20:01:11.855558',1,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":null,\"Timestamp\":\"2025-11-13T20:01:11.853501Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (273,50,'Assistant','Normal blood glucose (blood sugar) levels vary depending on when you last ate:\n- **Fasting (before eating)**: 70-100 mg/dL (3.9-5.6 mmol/L) is considered normal\n- **After meals (2 hours)**: Less than 140 mg/dL (7.8 mmol/L) is considered normal\n- **Random glucose**: 70-140 mg/dL (3.9-7.8 mmol/L) is typically normal\n\n**High glucose (hyperglycemia)**: Fasting levels above 126 mg/dL (7.0 mmol/L) or random levels above 200 mg/dL (11.1 mmol/L) may indicate diabetes and require medical evaluation.\n\n**Low glucose (hypoglycemia)**: Levels below 70 mg/dL (3.9 mmol/L) can cause symptoms like shakiness, sweating, confusion, and require immediate treatment.\n\nIf you have concerns about your blood glucose levels, please consult with your healthcare provider for proper evaluation and management.','2025-11-13 20:01:11.959187',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (274,51,'User','normal values of glucose?','2025-11-13 20:06:27.770362',1,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":null,\"Timestamp\":\"2025-11-13T20:06:27.770257Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (275,51,'Assistant','Normal blood glucose (blood sugar) levels vary depending on when you last ate:\n- **Fasting (before eating)**: 70-100 mg/dL (3.9-5.6 mmol/L) is considered normal\n- **After meals (2 hours)**: Less than 140 mg/dL (7.8 mmol/L) is considered normal\n- **Random glucose**: 70-140 mg/dL (3.9-7.8 mmol/L) is typically normal\n\n**High glucose (hyperglycemia)**: Fasting levels above 126 mg/dL (7.0 mmol/L) or random levels above 200 mg/dL (11.1 mmol/L) may indicate diabetes and require medical evaluation.\n\n**Low glucose (hypoglycemia)**: Levels below 70 mg/dL (3.9 mmol/L) can cause symptoms like shakiness, sweating, confusion, and require immediate treatment.\n\nIf you have concerns about your blood glucose levels, please consult with your healthcare provider for proper evaluation and management.','2025-11-13 20:06:27.781436',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (276,51,'User','severity of the issue?','2025-11-13 20:07:47.084955',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":null,\"Timestamp\":\"2025-11-13T20:07:47.080197Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (277,51,'Assistant','I understand your question. For specific medical information, please consult with your healthcare provider or use the patient chat feature for personalized analysis.','2025-11-13 20:07:47.348570',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (278,52,'User','severity of the issue?','2025-11-13 20:08:44.737566',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":null,\"Timestamp\":\"2025-11-13T20:08:44.737436Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (279,52,'Assistant','I understand your question. For specific medical information, please consult with your healthcare provider or use the patient chat feature for personalized analysis.','2025-11-13 20:08:44.870603',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (280,53,'User','normal glucose levels?','2025-11-14 15:33:53.440575',1,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":null,\"Timestamp\":\"2025-11-14T15:33:53.437726Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (281,53,'Assistant','Normal blood glucose (blood sugar) levels vary depending on when you last ate:\n- **Fasting (before eating)**: 70-100 mg/dL (3.9-5.6 mmol/L) is considered normal\n- **After meals (2 hours)**: Less than 140 mg/dL (7.8 mmol/L) is considered normal\n- **Random glucose**: 70-140 mg/dL (3.9-7.8 mmol/L) is typically normal\n\n**High glucose (hyperglycemia)**: Fasting levels above 126 mg/dL (7.0 mmol/L) or random levels above 200 mg/dL (11.1 mmol/L) may indicate diabetes and require medical evaluation.\n\n**Low glucose (hypoglycemia)**: Levels below 70 mg/dL (3.9 mmol/L) can cause symptoms like shakiness, sweating, confusion, and require immediate treatment.\n\nIf you have concerns about your blood glucose levels, please consult with your healthcare provider for proper evaluation and management.','2025-11-14 15:33:53.506375',0,'Response',NULL,1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (282,54,'User','status?','2025-11-21 00:04:31.251184',0,'Question','{\"UserId\":2,\"UserRoleId\":2,\"PatientId\":3,\"Timestamp\":\"2025-11-21T00:04:31.248508Z\"}',1);
INSERT INTO `ChatMessages` (`Id`, `SessionId`, `Role`, `Content`, `Timestamp`, `IsMedicalData`, `MessageType`, `Metadata`, `IsActive`) VALUES (283,54,'Assistant','**Patient Medical Overview:**\n⚠️ **MEDICAL CONCERNS DETECTED:** There are abnormal medical values or concerning clinical observations that require attention and monitoring.\n\n**Recent Patient Activity:**\n[11/13/2025] Mood: Happy\n[11/13/2025] Mood: Neutral\n[10/23/2025] Mood: Neutral','2025-11-21 00:04:31.634471',0,'Response',NULL,1);
/*!40000 ALTER TABLE `ChatMessages` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ChatSessions`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `ChatSessions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `SessionId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PatientId` int DEFAULT NULL,
  `Summary` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `LastActivityAt` datetime(6) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `PrivacyLevel` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `MessageCount` int NOT NULL,
  `IgnoredAt` datetime(6) DEFAULT NULL,
  `IgnoredByDoctorId` int DEFAULT NULL,
  `IsIgnoredByDoctor` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_ChatSessions_SessionId` (`SessionId`),
  KEY `IX_ChatSessions_PatientId` (`PatientId`),
  KEY `IX_ChatSessions_UserId` (`UserId`),
  KEY `IX_ChatSessions_IgnoredByDoctorId` (`IgnoredByDoctorId`),
  CONSTRAINT `FK_ChatSessions_Users_IgnoredByDoctorId` FOREIGN KEY (`IgnoredByDoctorId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_ChatSessions_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_ChatSessions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=55 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ChatSessions`
--

LOCK TABLES `ChatSessions` WRITE;
/*!40000 ALTER TABLE `ChatSessions` DISABLE KEYS */;
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (38,2,'a00fc348-7031-4297-a8b5-c1e072a744b8',3,'Session included 2 patient messages and 2 AI responses.','2025-11-11 17:11:17.348636','2025-11-11 17:35:58.612500',0,'Full',6,'2025-11-11 17:36:10.146902',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (39,2,'bf23d28f-7a47-4c0a-b1ba-51aaf864d474',3,'Session included 1 patient messages and 1 AI responses.','2025-11-11 17:12:07.802093','2025-11-11 17:29:52.044448',0,'Full',4,'2025-11-11 17:35:27.763830',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (40,2,'84683d63-8314-42ca-959e-c6b8b5672536',3,'Session included 2 patient messages and 2 AI responses.','2025-11-11 17:25:10.377656','2025-11-11 17:27:43.399953',0,'Full',4,'2025-11-11 17:35:36.880697',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (41,2,'6e9cbe05-46f1-4b2e-9573-a9016dddedb0',3,'Session included 1 patient messages and 1 AI responses.','2025-11-11 17:30:55.557120','2025-11-11 17:30:55.761812',0,'Full',2,'2025-11-11 17:35:22.663704',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (42,2,'5c40fe0a-16ce-463b-a54c-3b91528587a0',3,'Session included 2 patient messages and 2 AI responses.','2025-11-11 17:36:21.357225','2025-11-11 22:26:40.541626',0,'Full',10,'2025-11-11 22:27:19.964114',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (43,2,'a4aac69d-5f76-4aaa-80ef-3a4bfc772a54',3,'Session included 2 patient messages and 2 AI responses.','2025-11-12 14:05:50.730856','2025-11-12 14:08:05.040301',0,'Full',6,'2025-11-12 14:08:12.433522',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (44,2,'fbfe6a01-0650-4a64-ac17-bae53d3e8c2c',3,'Session included 2 patient messages and 2 AI responses.','2025-11-12 14:08:23.474970','2025-11-12 14:40:07.103736',0,'Full',14,'2025-11-12 16:35:58.409350',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (45,2,'a6043ed7-5840-4419-a98c-be547b2369d3',NULL,'Session included 2 patient messages and 2 AI responses.','2025-11-12 14:09:39.817756','2025-11-12 14:19:56.829529',0,'Full',8,NULL,NULL,0);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (46,2,'f34df38e-e047-4482-b438-ea3885baa325',3,'Session included 2 patient messages and 2 AI responses.','2025-11-12 14:41:27.483274','2025-11-12 16:35:28.736866',0,'Full',6,'2025-11-12 16:35:57.881596',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (47,2,'fb88aa52-711e-4ac7-9a6a-fb8550924fbc',3,'Session included 2 patient messages and 2 AI responses.','2025-11-12 19:19:13.716521','2025-11-12 19:20:03.552691',0,'Full',6,'2025-11-12 19:20:16.152499',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (48,2,'e06c111b-589e-47a0-8f07-e42c5cf95b6d',3,'Session included 1 patient messages and 1 AI responses.','2025-11-12 19:20:32.264986','2025-11-12 19:20:32.369084',0,'Full',2,NULL,NULL,0);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (49,2,'6df70b12-bb8c-4953-b84d-33e173950085',3,'Session included 1 patient messages and 1 AI responses.','2025-11-13 19:51:54.285779','2025-11-13 19:51:54.636261',0,'Full',2,NULL,NULL,0);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (50,2,'473e6163-5346-419f-ba74-aa4032b2b1ae',NULL,NULL,'2025-11-13 20:01:11.801744','2025-11-13 20:01:11.971920',0,'Full',2,NULL,NULL,0);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (51,2,'4319e10a-becf-428e-aafa-6623dee84c03',NULL,'Session included 2 patient messages and 2 AI responses.','2025-11-13 20:06:27.766976','2025-11-13 20:07:47.367870',0,'Full',4,'2025-11-13 20:08:29.715945',2,1);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (52,2,'ea091b51-3269-4ff2-88f2-0a4c7273fdb1',NULL,'Session included 1 patient messages and 1 AI responses.','2025-11-13 20:08:44.729432','2025-11-13 20:08:44.875101',0,'Full',2,NULL,NULL,0);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (53,2,'9487768b-6b60-4293-8554-6fb31a6077d9',NULL,NULL,'2025-11-14 15:33:53.338398','2025-11-14 15:33:53.513933',1,'Full',2,NULL,NULL,0);
INSERT INTO `ChatSessions` (`Id`, `UserId`, `SessionId`, `PatientId`, `Summary`, `CreatedAt`, `LastActivityAt`, `IsActive`, `PrivacyLevel`, `MessageCount`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (54,2,'87052a49-51ab-4f0e-9b91-f38b920a32d9',3,NULL,'2025-11-21 00:04:31.149753','2025-11-21 00:04:31.661518',1,'Full',2,'2025-11-27 17:17:57.533220',2,1);
/*!40000 ALTER TABLE `ChatSessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ClinicalNotes`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `ClinicalNotes` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `DoctorId` int NOT NULL,
  `PatientId` int NOT NULL,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Assessment` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `TreatmentPlan` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Recommendations` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `FollowUpNotes` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `IsArchived` tinyint(1) NOT NULL,
  `Tags` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `NoteTypeId` int NOT NULL DEFAULT '1',
  `PriorityId` int NOT NULL DEFAULT '2',
  `Priority` varchar(20) DEFAULT 'Normal',
  `NoteType` varchar(50) DEFAULT 'General',
  `IsConfidential` tinyint(1) DEFAULT '0',
  `IsActive` tinyint(1) DEFAULT '1',
  `IgnoredAt` datetime(6) DEFAULT NULL,
  `IgnoredByDoctorId` int DEFAULT NULL,
  `IsIgnoredByDoctor` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  KEY `IX_ClinicalNotes_DoctorId_CreatedAt` (`DoctorId`,`CreatedAt`),
  KEY `IX_ClinicalNotes_DoctorId_PatientId_CreatedAt` (`DoctorId`,`PatientId`,`CreatedAt`),
  KEY `IX_ClinicalNotes_IsArchived` (`IsArchived`),
  KEY `IX_ClinicalNotes_PatientId_CreatedAt` (`PatientId`,`CreatedAt`),
  KEY `IX_ClinicalNotes_NoteTypeId` (`NoteTypeId`),
  KEY `IX_ClinicalNotes_PriorityId` (`PriorityId`),
  KEY `IX_ClinicalNotes_IgnoredByDoctorId` (`IgnoredByDoctorId`),
  KEY `IX_ClinicalNotes_IsIgnoredByDoctor` (`IsIgnoredByDoctor`),
  CONSTRAINT `FK_ClinicalNotes_ClinicalNoteTypes_NoteTypeId` FOREIGN KEY (`NoteTypeId`) REFERENCES `ClinicalNoteTypes` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_ClinicalNotes_NotePriorities_PriorityId` FOREIGN KEY (`PriorityId`) REFERENCES `NotePriorities` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_ClinicalNotes_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_ClinicalNotes_Users_IgnoredByDoctorId` FOREIGN KEY (`IgnoredByDoctorId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_ClinicalNotes_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=28 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ClinicalNotes`
--

LOCK TABLES `ClinicalNotes` WRITE;
/*!40000 ALTER TABLE `ClinicalNotes` DISABLE KEYS */;
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (18,2,3,'test','test',NULL,NULL,NULL,NULL,'2025-11-11 17:46:42.913930',NULL,0,NULL,1,2,'Normal','General',0,0,NULL,NULL,0);
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (19,2,3,'test','test',NULL,NULL,NULL,NULL,'2025-11-11 17:46:42.972128',NULL,0,NULL,1,2,'Normal','General',0,0,NULL,NULL,0);
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (20,2,3,'test2','test2',NULL,NULL,NULL,NULL,'2025-11-11 17:47:11.920393',NULL,0,NULL,1,2,'Normal','General',0,0,NULL,NULL,0);
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (21,2,3,'test2','test2',NULL,NULL,NULL,NULL,'2025-11-11 17:47:11.974799',NULL,0,NULL,1,2,'Normal','General',0,0,NULL,NULL,0);
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (22,2,3,'test','test',NULL,NULL,NULL,NULL,'2025-11-11 18:02:49.139033',NULL,0,NULL,1,2,'Normal','General',0,1,'2025-11-13 18:55:27.642525',2,1);
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (23,2,3,'test2','test2',NULL,NULL,NULL,NULL,'2025-11-11 18:08:42.350267','2025-11-11 20:05:37.870368',0,NULL,1,2,'Normal','General',0,1,'2025-11-13 18:55:26.704955',2,1);
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (24,2,3,'Observation','Patient is showing some serious symptoms of anxiety and has to monitored further. Test results shows signs of high blood pressure, risk of heart problems, more test need to be performed. ',NULL,NULL,NULL,NULL,'2025-11-11 18:09:04.899113','2025-11-13 17:18:57.180817',0,NULL,1,2,'Normal','Assessment',0,1,'2025-11-13 20:31:58.614175',2,1);
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (25,2,3,'AI Clinical Report - High Blood Pressure, signs of anxiety and possible heart complication','CLINICAL DECISION SUPPORT REPORT\n===================================\nGenerated: 2025-11-13 14:31:24\nPatient ID: 3\nDiagnosis: High Blood Pressure, signs of anxiety and possible heart complication\nSeverity Assessment: Mild\n\nCLINICAL RECOMMENDATIONS\n------------------------\n1. Patient-specific consideration:\n2. He should also receive medication to lower his blood pressure if necessary.\n3. He may benefit from therapy or medications such as antidepressants.\n4. Risks factors to monitor:\n5. Contraindications to be aware of:\n6. Alternative treatment options:\n\nCLINICAL PROTOCOL\n-----------------\nDiagnostic Criteria:\n1. Based on clinical assessment\n\nTreatment Guidelines:\n1. Include diagnostic criteria confirmation, treatment guidelines (evidence-based), monitoring requirements, safety considerations, referral criteria, and emergency protocols\n\nMonitoring Requirements:\n1. Include diagnostic criteria confirmation, treatment guidelines (evidence-based), monitoring requirements, safety considerations, referral criteria, and emergency protocols\n\nSafety Considerations:\n1. Include diagnostic criteria confirmation, treatment guidelines (evidence-based), monitoring requirements, safety considerations, referral criteria, and emergency protocols\n\nINSURANCE REQUIREMENTS\n----------------------\n1. [Coding] Document diagnosis with ICD-10 code\n   Description: Ensure proper coding for insurance billing\n   Priority: High\n2. [Documentation] Treatment plan documentation\n   Description: Document evidence-based treatment approach\n   Priority: High',NULL,NULL,NULL,NULL,'2025-11-13 20:31:24.424776',NULL,0,'AI-Generated, Clinical Report, Clinical Decision Support',1,2,'Low','Clinical Report',0,1,'2025-11-14 03:06:02.779799',2,1);
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (26,2,3,'AI Treatment Plan - High Blood Pressure, signs of anxiety and possible heart complication','TREATMENT PLAN\n========================\nPatient ID: 3\nDiagnosis: High Blood Pressure, signs of anxiety and possible heart complication\nSeverity: Mild\nGenerated: 2025-11-13 14:31\n\nIMMEDIATE ACTIONS:\n1. Patient-specific consideration:\n2. He should also receive medication to lower his blood pressure if necessary.\n3. He may benefit from therapy or medications such as antidepressants.\n4. Risks factors to monitor:\n5. Contraindications to be aware of:\n6. Alternative treatment options:\n\nPATIENT-SPECIFIC NOTES:\n1. Immediate actions to take:\n2. He should be given medication to prevent further complications.\n3. He should also receive medication to lower his blood pressure if necessary.\n4. He may benefit from therapy or medications such as antidepressants.\n5. Risks factors to monitor:\n6. Contraindications to be aware of:\n7. Alternative treatment options:\n\nRISK FACTORS:\n1. Immediate actions to take:\n2. He should be given medication to prevent further complications.\n3. Patient-specific consideration:\n4. He may benefit from therapy or medications such as antidepressants.\n5. Contraindications to be aware of:\n6. Alternative treatment options:\n\nCONTRAINDICATIONS:\n1. Patient-specific consideration:\n2. He should also receive medication to lower his blood pressure if necessary.\n3. He may benefit from therapy or medications such as antidepressants.\n4. Risks factors to monitor:\n5. Alternative treatment options:\n\nALTERNATIVE TREATMENTS:\n1. Alternative treatment options:\n   - If John Doe experiences severe anemia, he should undergo a blood transfusion to restore his iron levels\n2. In conclusion, clinical recommendations for John Doe with diagnosis of High Blood Pressure, signs of anxiety, and possible heart complication include immediate actions to take, patient-specific consideration, risks factors to monitor, contraindications to be aware of, alternative treatment options, and healthy lifestyle considerations\n\nFOLLOW-UP STEPS:\n1. Schedule follow-up appointment (Medium)\n2. Monitor patient symptoms (High)',NULL,NULL,NULL,NULL,'2025-11-13 20:31:32.291086',NULL,0,'AI-Generated, Treatment Plan, Clinical Decision Support',1,2,'Low','Treatment Plan',0,1,'2025-11-14 03:06:02.190431',2,1);
INSERT INTO `ClinicalNotes` (`Id`, `DoctorId`, `PatientId`, `Title`, `Content`, `Assessment`, `TreatmentPlan`, `Recommendations`, `FollowUpNotes`, `CreatedAt`, `UpdatedAt`, `IsArchived`, `Tags`, `NoteTypeId`, `PriorityId`, `Priority`, `NoteType`, `IsConfidential`, `IsActive`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (27,2,3,'=== AI HEALTH CHECK ANALYSIS ===','=== AI HEALTH CHECK ANALYSIS ===\n\nCRITICAL PRIORITY\n\nInstructions for analyzing patient medical data and status\n- FIRST: Check if there are any 🚨 CRITICAL VALUES in the medical data above\n- SECOND: Review RECENT CLINICAL NOTES section above - these are written by doctor(s) and contain critical medical observations\n- If clinical notes mention serious symptoms, concerns, monitoring needs, or health risks, you MUST reflect this in your assessment\n- Do NOT say the patient is \'stable\' if ALL values are normal, no clinical notes indicate concerns, and no concerning patterns are detected\n- Summarize key medical finding(s) from the patient\'s records. If critical values are present in the medical data, you MUST indicate the patient\'s condition as \"not stable\" or \"stable with conditions\". Keep the response comprehensive but concise (300-400 words).',NULL,NULL,NULL,NULL,'2025-11-14 03:05:39.356646','2025-11-14 03:05:57.727550',0,NULL,1,2,'Normal','Follow-up',0,1,'2025-11-14 15:33:14.129389',2,1);
/*!40000 ALTER TABLE `ClinicalNotes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ClinicalNoteTypes`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `ClinicalNoteTypes` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Icon` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `SortOrder` int NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_ClinicalNoteTypes_Name` (`Name`),
  KEY `IX_ClinicalNoteTypes_IsActive` (`IsActive`),
  KEY `IX_ClinicalNoteTypes_SortOrder` (`SortOrder`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ClinicalNoteTypes`
--

LOCK TABLES `ClinicalNoteTypes` WRITE;
/*!40000 ALTER TABLE `ClinicalNoteTypes` DISABLE KEYS */;
INSERT INTO `ClinicalNoteTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (1,'General','General clinical notes and observations','📋',1,1,'2025-10-23 10:03:45.000000');
INSERT INTO `ClinicalNoteTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (2,'Assessment','Clinical assessments and evaluations','🔍',1,2,'2025-10-23 10:03:45.000000');
INSERT INTO `ClinicalNoteTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (3,'Treatment Plan','Treatment plans and therapeutic approaches','📝',1,3,'2025-10-23 10:03:45.000000');
INSERT INTO `ClinicalNoteTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (4,'Progress Note','Progress notes and updates','📈',1,4,'2025-10-23 10:03:45.000000');
INSERT INTO `ClinicalNoteTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (5,'Crisis Intervention','Crisis intervention and emergency notes','🚨',1,5,'2025-10-23 10:03:45.000000');
INSERT INTO `ClinicalNoteTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (6,'Medication Review','Medication reviews and adjustments','💊',1,6,'2025-10-23 10:03:45.000000');
INSERT INTO `ClinicalNoteTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (7,'Therapy Session','Therapy session notes','🛋️',1,7,'2025-10-23 10:03:45.000000');
INSERT INTO `ClinicalNoteTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (8,'Discharge Planning','Discharge planning and follow-up','🏥',1,8,'2025-10-23 10:03:45.000000');
/*!40000 ALTER TABLE `ClinicalNoteTypes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ContentAlerts`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `ContentAlerts` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ContentId` int NOT NULL,
  `PatientId` int NOT NULL,
  `AlertType` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Severity` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `IsRead` tinyint(1) NOT NULL,
  `IsResolved` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ContentAlerts_ContentId` (`ContentId`),
  KEY `IX_ContentAlerts_PatientId` (`PatientId`),
  CONSTRAINT `FK_ContentAlerts_Contents_ContentId` FOREIGN KEY (`ContentId`) REFERENCES `Contents` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ContentAlerts_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ContentAlerts`
--

LOCK TABLES `ContentAlerts` WRITE;
/*!40000 ALTER TABLE `ContentAlerts` DISABLE KEYS */;
/*!40000 ALTER TABLE `ContentAlerts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ContentAnalyses`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `ContentAnalyses` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ContentId` int NOT NULL,
  `ContentTypeName` varchar(50) NOT NULL,
  `ExtractedText` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `AnalysisResults` json NOT NULL,
  `Alerts` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProcessedAt` datetime(6) NOT NULL,
  `ProcessingStatus` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ErrorMessage` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_ContentAnalyses_ContentId` (`ContentId`),
  CONSTRAINT `FK_ContentAnalyses_Contents_ContentId` FOREIGN KEY (`ContentId`) REFERENCES `Contents` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ContentAnalyses`
--

LOCK TABLES `ContentAnalyses` WRITE;
/*!40000 ALTER TABLE `ContentAnalyses` DISABLE KEYS */;
INSERT INTO `ContentAnalyses` (`Id`, `ContentId`, `ContentTypeName`, `ExtractedText`, `AnalysisResults`, `Alerts`, `ProcessedAt`, `ProcessingStatus`, `ErrorMessage`) VALUES (13,30,'Document','HelveticaNeue; ; ; \\* ;; Blood Pressure: 120/80\\ Hemoglobin: 13.0\\ Triglycerides: 140','{\"Keywords\": {\"mood\": 0, \"pain\": 0, \"sleep\": 0, \"anxiety\": 0, \"symptom\": 0, \"depression\": 0, \"medication\": 0}, \"TestResults\": {\"Hemoglobin\": [\"13.0\"], \"Triglycerides\": [\"140\"], \"Blood Pressure\": [\"120\"]}, \"NormalValues\": [\"✅ NORMAL: Hemoglobin 13 g/dL (Normal: 12-16 g/dL)\", \"✅ NORMAL: Triglycerides 140 mg/dL (Normal: <150 mg/dL)\"]}','[]','2025-11-26 14:50:57.787830','Completed',NULL);
INSERT INTO `ContentAnalyses` (`Id`, `ContentId`, `ContentTypeName`, `ExtractedText`, `AnalysisResults`, `Alerts`, `ProcessedAt`, `ProcessingStatus`, `ErrorMessage`) VALUES (14,33,'Document','PDF text extraction failed: No data is available for encoding 10000. For information on defining a custom encoding, see the documentation for the Encoding.RegisterProvider method.','{\"Keywords\": {\"mood\": 0, \"pain\": 0, \"sleep\": 0, \"anxiety\": 0, \"symptom\": 0, \"depression\": 0, \"medication\": 0}}','[]','2025-11-26 14:57:02.875106','Completed',NULL);
INSERT INTO `ContentAnalyses` (`Id`, `ContentId`, `ContentTypeName`, `ExtractedText`, `AnalysisResults`, `Alerts`, `ProcessedAt`, `ProcessingStatus`, `ErrorMessage`) VALUES (15,37,'Document','HelveticaNeue; ; ; \\* ;; Blood Pressure: 190/100\\ Hemoglobin: 6.0\\ Triglycerides: 640','{\"Keywords\": {\"mood\": 0, \"pain\": 0, \"sleep\": 0, \"anxiety\": 0, \"symptom\": 0, \"depression\": 0, \"medication\": 0}, \"TestResults\": {\"Hemoglobin\": [\"6.0\"], \"Triglycerides\": [\"640\"], \"Blood Pressure\": [\"190\"]}, \"CriticalValues\": [\"🚨 CRITICAL: Severe Anemia - Hemoglobin 6 g/dL (Normal: 12-16 g/dL)\", \"🚨 CRITICAL: Extremely High Triglycerides - 640 mg/dL (Normal: <150 mg/dL)\"], \"MedicalSummary\": \"CRITICAL MEDICAL VALUES DETECTED: 🚨 CRITICAL: Severe Anemia - Hemoglobin 6 g/dL (Normal: 12-16 g/dL); 🚨 CRITICAL: Extremely High Triglycerides - 640 mg/dL (Normal: <150 mg/dL)\"}','[\"\\uD83D\\uDEA8 CRITICAL: Severe Anemia - Hemoglobin 6 g/dL (Normal: 12-16 g/dL)\",\"\\uD83D\\uDEA8 CRITICAL: Extremely High Triglycerides - 640 mg/dL (Normal: \\u003C150 mg/dL)\"]','2025-11-27 17:17:37.984978','Completed',NULL);
/*!40000 ALTER TABLE `ContentAnalyses` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Contents`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `Contents` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ContentGuid` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PatientId` int NOT NULL,
  `AddedByUserId` int DEFAULT NULL,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FileName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `OriginalFileName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `MimeType` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FileSizeBytes` bigint NOT NULL,
  `S3Bucket` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `S3Key` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `LastAccessedAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `ContentTypeModelId` int NOT NULL,
  `IgnoredAt` datetime(6) DEFAULT NULL,
  `IgnoredByDoctorId` int DEFAULT NULL,
  `IsIgnoredByDoctor` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Contents_ContentGuid` (`ContentGuid`),
  KEY `IX_Contents_AddedByUserId` (`AddedByUserId`),
  KEY `IX_Contents_PatientId` (`PatientId`),
  KEY `FK_Contents_ContentTypes_ContentTypeId` (`ContentTypeModelId`),
  KEY `IX_Contents_IgnoredByDoctorId` (`IgnoredByDoctorId`),
  CONSTRAINT `FK_Contents_ContentTypes_ContentTypeId` FOREIGN KEY (`ContentTypeModelId`) REFERENCES `ContentTypes` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Contents_Users_AddedByUserId` FOREIGN KEY (`AddedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Contents_Users_IgnoredByDoctorId` FOREIGN KEY (`IgnoredByDoctorId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Contents_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=41 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Contents`
--

LOCK TABLES `Contents` WRITE;
/*!40000 ALTER TABLE `Contents` DISABLE KEYS */;
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (30,'6e2e2730-9e62-48a2-9741-ce310bc788b3',3,2,'normal','normal','6e2e2730-9e62-48a2-9741-ce310bc788b3_jdoe3.rtf','jdoe3.rtf','',370,'mentalhealth-content','content/6e2e2730-9e62-48a2-9741-ce310bc788b3_6e2e2730-9e62-48a2-9741-ce310bc788b3_jdoe3.rtf','2025-11-26 14:50:57.132281','2025-12-04 16:10:42.896223',1,1,NULL,NULL,0);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (31,'c7d0206d-8140-40dc-ae4f-4bbc53246953',3,3,'IMG_3587','','IMG_3587.PNG','IMG_3587.PNG','image/png',383661,'mentalhealth-content','documents/3/c7d0206d-8140-40dc-ae4f-4bbc53246953/IMG_3587.PNG','2025-11-26 14:53:13.277728','2025-12-04 16:10:42.891377',1,2,NULL,NULL,0);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (32,'b0abda3f-f5f6-4c3f-829d-aa98e7292eba',3,3,'spelling bee words 3-5 - Google Search','','spelling bee words 3-5 - Google Search.pdf','spelling bee words 3-5 - Google Search.pdf','application/pdf',750911,'mentalhealth-content','documents/3/b0abda3f-f5f6-4c3f-829d-aa98e7292eba/spelling bee words 3-5 - Google Search.pdf','2025-11-26 14:55:35.650339','2025-12-04 16:10:42.885215',1,1,NULL,NULL,0);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (33,'c0fd72fb-a98b-41ad-9f97-eca8e6583d42',3,2,'test','test','c0fd72fb-a98b-41ad-9f97-eca8e6583d42_spelling bee words 3-5 - Google Search.pdf','spelling bee words 3-5 - Google Search.pdf','',750911,'mentalhealth-content','content/c0fd72fb-a98b-41ad-9f97-eca8e6583d42_c0fd72fb-a98b-41ad-9f97-eca8e6583d42_spelling bee words 3-5 - Google Search.pdf','2025-11-26 14:57:01.836794','2025-12-04 16:10:42.880483',1,1,NULL,NULL,0);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (34,'7821a37d-20eb-41eb-abe2-3fd6fbb5d33d',3,3,'Facebook','','Facebook.pdf','Facebook.pdf','application/pdf',20487262,'mentalhealth-content','documents/3/7821a37d-20eb-41eb-abe2-3fd6fbb5d33d/Facebook.pdf','2025-11-26 15:00:20.045282','2025-12-04 16:10:42.876830',1,1,NULL,NULL,0);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (35,'d39dd00b-9259-4380-ae45-753d66075b14',5,5,'Screen Shot 2021-04-18 at 9','','Screen Shot 2021-04-18 at 9.12.25 PM.png','Screen Shot 2021-04-18 at 9.12.25 PM.png','image/png',12426,'mentalhealth-content','documents/5/d39dd00b-9259-4380-ae45-753d66075b14/Screen Shot 2021-04-18 at 9.12.25 PM.png','2025-11-26 15:08:06.610756','2025-11-26 15:08:07.602408',1,2,NULL,NULL,0);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (36,'e20978ab-9a24-4c60-8686-dac9f7022822',3,3,'361A733C-687B-41AF-B0CA-83085DCDBDCE','','361A733C-687B-41AF-B0CA-83085DCDBDCE.jpg','361A733C-687B-41AF-B0CA-83085DCDBDCE.jpg','image/jpeg',215290,'mentalhealth-content','documents/3/e20978ab-9a24-4c60-8686-dac9f7022822/361A733C-687B-41AF-B0CA-83085DCDBDCE.jpg','2025-11-27 16:38:31.751442','2025-12-04 16:10:42.875024',1,2,NULL,NULL,0);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (37,'b0174615-2998-407c-b505-68084bb7a1e0',3,2,'not normal','not normal','b0174615-2998-407c-b505-68084bb7a1e0_jdoe4.rtf','jdoe4.rtf','',370,'mentalhealth-content','content/b0174615-2998-407c-b505-68084bb7a1e0_b0174615-2998-407c-b505-68084bb7a1e0_jdoe4.rtf','2025-11-27 17:17:37.461627','2025-12-04 16:10:42.868372',1,1,'2025-11-27 17:17:52.695988',2,1);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (38,'6109672c-bf7c-4d9f-80ca-0d2262ccdad7',29,29,'50B8988C-00A8-405C-B62A-C0B8CB58DF5F','','50B8988C-00A8-405C-B62A-C0B8CB58DF5F.jpg','50B8988C-00A8-405C-B62A-C0B8CB58DF5F.jpg','image/jpeg',309831,'mentalhealth-content','documents/29/6109672c-bf7c-4d9f-80ca-0d2262ccdad7/50B8988C-00A8-405C-B62A-C0B8CB58DF5F.jpg','2025-11-30 17:37:40.360729','2025-11-30 17:50:15.977153',1,2,NULL,NULL,0);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (39,'15d4a75c-5320-4951-b5bd-8279edb0dc93',29,29,'7FEF3B3E-529D-457C-84AA-C678F96670F2','','7FEF3B3E-529D-457C-84AA-C678F96670F2.jpg','7FEF3B3E-529D-457C-84AA-C678F96670F2.jpg','image/jpeg',270520,'mentalhealth-content','documents/29/15d4a75c-5320-4951-b5bd-8279edb0dc93/7FEF3B3E-529D-457C-84AA-C678F96670F2.jpg','2025-11-30 17:50:14.631588','2025-11-30 17:50:15.972537',1,2,NULL,NULL,0);
INSERT INTO `Contents` (`Id`, `ContentGuid`, `PatientId`, `AddedByUserId`, `Title`, `Description`, `FileName`, `OriginalFileName`, `MimeType`, `FileSizeBytes`, `S3Bucket`, `S3Key`, `CreatedAt`, `LastAccessedAt`, `IsActive`, `ContentTypeModelId`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`) VALUES (40,'75f0ad5a-f640-4015-9855-1ef47a1a83dc',3,3,'EFB4299B-CCD3-43C0-94DD-0E410F4705C3','','EFB4299B-CCD3-43C0-94DD-0E410F4705C3.jpg','EFB4299B-CCD3-43C0-94DD-0E410F4705C3.jpg','image/jpeg',534523,'mentalhealth-content','documents/3/75f0ad5a-f640-4015-9855-1ef47a1a83dc/EFB4299B-CCD3-43C0-94DD-0E410F4705C3.jpg','2025-12-04 16:10:41.338740','2025-12-04 16:10:55.202781',1,2,NULL,NULL,0);
/*!40000 ALTER TABLE `Contents` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `ContentTypes`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `ContentTypes` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Icon` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `SortOrder` int NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_ContentTypes_Name` (`Name`),
  KEY `IX_ContentTypes_IsActive` (`IsActive`),
  KEY `IX_ContentTypes_SortOrder` (`SortOrder`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ContentTypes`
--

LOCK TABLES `ContentTypes` WRITE;
/*!40000 ALTER TABLE `ContentTypes` DISABLE KEYS */;
INSERT INTO `ContentTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (1,'Document','General document files (PDF, DOC, TXT, etc.)','📄',1,1,'2025-10-22 16:17:30.000000');
INSERT INTO `ContentTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (2,'Image','Image files (JPG, PNG, GIF, etc.)','🖼️',1,2,'2025-10-22 16:17:30.000000');
INSERT INTO `ContentTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (3,'Video','Video files (MP4, AVI, MOV, etc.)','🎥',1,3,'2025-10-22 16:17:30.000000');
INSERT INTO `ContentTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (4,'Audio','Audio files (MP3, WAV, FLAC, etc.)','🎵',1,4,'2025-10-22 16:17:30.000000');
INSERT INTO `ContentTypes` (`Id`, `Name`, `Description`, `Icon`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (5,'Other','Other file types','📁',1,5,'2025-10-22 16:17:30.000000');
/*!40000 ALTER TABLE `ContentTypes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `CriticalValueCategories`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `CriticalValueCategories` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(255) NOT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT (now(6)),
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `CriticalValueCategories`
--

LOCK TABLES `CriticalValueCategories` WRITE;
/*!40000 ALTER TABLE `CriticalValueCategories` DISABLE KEYS */;
INSERT INTO `CriticalValueCategories` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (1,'Hemoglobin','Critical hemoglobin values indicating severe anemia',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValueCategories` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (2,'Blood Pressure','Critical blood pressure values indicating hypertensive crisis',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValueCategories` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (3,'Triglycerides','Critical triglyceride values indicating severe hypertriglyceridemia',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValueCategories` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (4,'Critical','Keywords indicating critical medical status',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueCategories` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (5,'Abnormal','Keywords indicating abnormal medical values',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueCategories` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (6,'Normal','Keywords indicating normal medical values',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueCategories` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (7,'High Concern','Mental health keywords indicating high concern or crisis',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueCategories` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (8,'Distress','Mental health keywords indicating distress or negative emotions',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueCategories` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (9,'Positive','Mental health keywords indicating positive emotions or well-being',1,'2025-11-10 22:10:53.000000');
/*!40000 ALTER TABLE `CriticalValueCategories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `CriticalValueKeywords`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `CriticalValueKeywords` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CategoryId` int NOT NULL,
  `Keyword` varchar(500) NOT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT (now(6)),
  PRIMARY KEY (`Id`),
  KEY `IX_CriticalValueKeywords_CategoryId` (`CategoryId`),
  KEY `IX_CriticalValueKeywords_IsActive` (`IsActive`),
  KEY `IX_CriticalValueKeywords_Keyword` (`Keyword`),
  CONSTRAINT `FK_CriticalValueKeywords_CriticalValueCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `CriticalValueCategories` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=122 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `CriticalValueKeywords`
--

LOCK TABLES `CriticalValueKeywords` WRITE;
/*!40000 ALTER TABLE `CriticalValueKeywords` DISABLE KEYS */;
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (1,4,'🚨 CRITICAL MEDICAL VALUES DETECTED','Matches critical medical values detected message',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (2,4,'CRITICAL VALUES DETECTED IN LATEST RESULTS','Matches critical values detected in latest results',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (3,4,'STATUS: CRITICAL','Matches critical status indicator',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (4,4,'Critical Values:','Matches critical values label',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (5,4,'CRITICAL MEDICAL ALERT','Matches critical medical alert message',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (6,4,'🚨 **CRITICAL VALUES DETECTED IN LATEST RESULTS:**','Matches formatted critical values detected message',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (7,4,'⚠️ **STATUS: CRITICAL','Matches formatted critical status with warning emoji',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (8,4,'🚨 CRITICAL: Severe Anemia','Matches severe anemia critical alert',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (9,4,'🚨 CRITICAL: Extremely High Triglycerides','Matches extremely high triglycerides critical alert',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (10,4,'🚨 CRITICAL: Hypertensive Crisis','Matches hypertensive crisis critical alert',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (11,4,'**CRITICAL MEDICAL VALUES DETECTED**','Matches bold critical medical values detected',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (12,4,'**CRITICAL VALUES DETECTED IN LATEST RESULTS:**','Matches bold critical values detected message',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (13,4,'CRITICAL VALUES DETECTED IN EXTRACTED TEXT:','Matches critical values in extracted text',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (14,4,'STATUS: CRITICAL - IMMEDIATE MEDICAL ATTENTION REQUIRED','Matches critical status requiring immediate attention',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (15,4,'CRITICAL MEDICAL VALUES DETECTED:','Matches critical medical values detected with colon',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (16,5,'ABNORMAL VALUES DETECTED IN LATEST RESULTS','Matches abnormal values detected in latest results',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (17,5,'STATUS: CONCERNING','Matches concerning status indicator',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (18,5,'Abnormal Values:','Matches abnormal values label',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (19,5,'ABNORMAL','Matches abnormal keyword in alerts',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (20,5,'⚠️','Matches warning emoji indicating abnormal values',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (21,6,'NORMAL VALUES IN LATEST RESULTS','Matches normal values in latest results',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (22,6,'STATUS: STABLE','Matches stable status indicator',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (23,6,'Normal Values:','Matches normal values label',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (24,6,'IMPROVEMENT NOTED','Matches improvement noted message',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (25,6,'NORMAL','Matches normal keyword in alerts',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (26,6,'✅','Matches checkmark emoji indicating normal values',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (27,6,'STATUS: STABLE - All values within normal range','Matches stable status with normal range message',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (28,6,'**NORMAL VALUES IN LATEST RESULTS:**','Matches formatted normal values message',1,'2025-11-10 22:06:07.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (29,7,'really bad','Matches really bad feeling - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (30,7,'terrible','Matches terrible feeling - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (31,7,'awful','Matches awful feeling - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (32,7,'horrible','Matches horrible feeling - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (33,7,'worst','Matches worst feeling - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (34,7,'can\'t take it','Matches inability to handle situation - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (35,7,'can\'t handle','Matches inability to handle situation - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (36,7,'suicidal','Matches suicidal ideation - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (37,7,'suicide','Matches suicide keyword - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (38,7,'kill myself','Matches self-harm intent - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (39,7,'want to die','Matches suicidal ideation - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (40,7,'end it all','Matches suicidal ideation - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (41,7,'not worth living','Matches hopelessness with suicidal ideation - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (42,7,'no point living','Matches hopelessness with suicidal ideation - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (43,7,'hopeless','Matches hopeless feeling - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (44,7,'desperate','Matches desperate feeling - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (45,7,'crisis','Matches crisis keyword - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (46,7,'emergency','Matches emergency keyword - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (47,7,'urgent','Matches urgent keyword - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (48,7,'help me','Matches cry for help - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (49,7,'can\'t cope','Matches inability to cope - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (50,7,'breaking down','Matches mental breakdown - high concern',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (51,8,'bad','Matches bad feeling keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (52,8,'not well','Matches not well keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (53,8,'struggling','Matches struggling keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (54,8,'suffering','Matches suffering keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (55,8,'pain','Matches pain keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (56,8,'hurt','Matches hurt keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (57,8,'broken','Matches broken keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (58,8,'lost','Matches lost keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (59,8,'confused','Matches confused keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (60,8,'overwhelmed','Matches overwhelmed keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (61,8,'stressed','Matches stressed keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (62,8,'anxious','Matches anxious keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (63,8,'worried','Matches worried keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (64,8,'scared','Matches scared keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (65,8,'frightened','Matches frightened keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (66,8,'depressed','Matches depressed keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (67,8,'sad','Matches sad keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (68,8,'down','Matches down keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (69,8,'low','Matches low keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (70,8,'empty','Matches empty keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (71,8,'numb','Matches numb keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (72,8,'alone','Matches alone keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (73,8,'isolated','Matches isolated keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (74,9,'good','Matches good feeling keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (75,9,'great','Matches great feeling keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (76,9,'wonderful','Matches wonderful feeling keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (77,9,'amazing','Matches amazing feeling keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (78,9,'fantastic','Matches fantastic feeling keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (79,9,'excellent','Matches excellent feeling keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (80,9,'happy','Matches happy keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (81,9,'joyful','Matches joyful keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (82,9,'grateful','Matches grateful keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (83,9,'blessed','Matches blessed keyword',1,'2025-11-10 22:10:53.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (84,9,'lucky','Matches lucky keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (85,9,'proud','Matches proud keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (86,9,'accomplished','Matches accomplished keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (87,9,'confident','Matches confident keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (88,9,'hopeful','Matches hopeful keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (89,9,'better','Matches better keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (90,9,'improving','Matches improving keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (91,9,'progress','Matches progress keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (92,9,'breakthrough','Matches breakthrough keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (93,9,'success','Matches success keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (94,9,'achievement','Matches achievement keyword',1,'2025-11-10 22:12:21.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (95,7,'serious symptoms','Matches serious symptoms mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (96,7,'serious symptom','Matches serious symptom (singular) mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (97,7,'serious concern','Matches serious concern mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (98,7,'serious condition','Matches serious condition mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (99,7,'high blood pressure','Matches high blood pressure mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (100,7,'elevated blood pressure','Matches elevated blood pressure mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (101,7,'hypertension','Matches hypertension mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (102,7,'heart problem','Matches heart problem (singular) mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (103,7,'heart problems','Matches heart problems (plural) mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (104,7,'cardiac','Matches cardiac issues mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (105,7,'cardiovascular','Matches cardiovascular issues mentioned in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (106,7,'risk of','Matches risk indicators in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (107,7,'requires monitoring','Matches monitoring requirements in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (108,7,'needs monitoring','Matches monitoring needs in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (109,7,'must monitor','Matches mandatory monitoring in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (110,7,'more test','Matches need for additional tests in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (111,7,'additional test','Matches additional test requirements in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (112,7,'further test','Matches further test requirements in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (113,7,'further evaluation','Matches further evaluation needs in clinical notes',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (114,8,'anxiety','Matches anxiety mentioned in clinical notes or patient data',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (115,8,'anxious','Matches anxious state mentioned in clinical notes or patient data',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (116,8,'panic','Matches panic mentioned in clinical notes or patient data',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (117,8,'worry','Matches worry mentioned in clinical notes or patient data',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (118,8,'concerned','Matches concerned state mentioned in clinical notes or patient data',1,'2025-11-11 16:47:03.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (119,7,'monitor','Matches monitor keyword in clinical notes',1,'2025-11-11 16:50:42.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (120,7,'monitored','Matches monitored keyword in clinical notes',1,'2025-11-11 16:50:42.000000');
INSERT INTO `CriticalValueKeywords` (`Id`, `CategoryId`, `Keyword`, `Description`, `IsActive`, `CreatedAt`) VALUES (121,7,'monitoring','Matches monitoring keyword in clinical notes',1,'2025-11-11 16:50:42.000000');
/*!40000 ALTER TABLE `CriticalValueKeywords` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `CriticalValuePatterns`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `CriticalValuePatterns` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CategoryId` int NOT NULL,
  `Pattern` varchar(500) NOT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT (now(6)),
  PRIMARY KEY (`Id`),
  KEY `FK_CriticalValuePatterns_CriticalValueCategories_CategoryId` (`CategoryId`),
  CONSTRAINT `FK_CriticalValuePatterns_CriticalValueCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `CriticalValueCategories` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `CriticalValuePatterns`
--

LOCK TABLES `CriticalValuePatterns` WRITE;
/*!40000 ALTER TABLE `CriticalValuePatterns` DISABLE KEYS */;
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (1,1,'Hemoglobin[:\\s]+(6.0(?![0-9])|6s(?![0-9])|6s*g/dL|6s*g/|<7.0|≤6.0)','Matches hemoglobin values of 6.0 or below (severe anemia)',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (2,1,'Hemoglobin[:s]+(7.0(?![0-9])|7s(?![0-9])|7s*g/dL|7s*g/)','Matches hemoglobin values of 7.0 (critical threshold)',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (3,1,'Hb[:s]+(6.0(?![0-9])|6s(?![0-9])|6s*g/dL|6s*g/|<7.0|≤6.0)','Matches Hb abbreviation for critically low values',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (4,1,'Hgb[:s]+(6.0(?![0-9])|6s(?![0-9])|6s*g/dL|6s*g/|<7.0|≤6.0)','Matches Hgb abbreviation for critically low values',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (5,2,'Bloods*Pressure[:s]+(19[0-9]|18[0-9]|180/11[0-9]|≥180)','Matches systolic BP of 180+ or diastolic 110+ (hypertensive crisis)',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (6,2,'BP[:s]+(19[0-9]|18[0-9]|180/11[0-9]|≥180)','Matches BP abbreviation for hypertensive crisis values',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (7,2,'Bloods*Pressure[:s]+(2[0-9][0-9]|≥200)','Matches extremely high systolic BP (200+)',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (8,2,'Systolic[:s]+(19[0-9]|18[0-9]|≥180)','Matches high systolic pressure specifically',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (9,2,'Diastolic[:s]+(11[0-9]|12[0-9]|≥110)','Matches high diastolic pressure specifically',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (10,3,'Triglycerides[:s]+(64[0-9]|6[5-9][0-9]|7[0-9][0-9]|≥500)','Matches triglyceride values of 500+ mg/dL (severe hypertriglyceridemia)',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (11,3,'Trig[:s]+(64[0-9]|6[5-9][0-9]|7[0-9][0-9]|≥500)','Matches Trig abbreviation for critical values',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (12,3,'TG[:s]+(64[0-9]|6[5-9][0-9]|7[0-9][0-9]|≥500)','Matches TG abbreviation for critical values',1,'2025-11-10 20:40:12.000000');
INSERT INTO `CriticalValuePatterns` (`Id`, `CategoryId`, `Pattern`, `Description`, `IsActive`, `CreatedAt`) VALUES (13,3,'Triglycerides[:s]+(5[0-9][0-9]|≥500)','Matches triglyceride values of 500-999 mg/dL',1,'2025-11-10 20:40:12.000000');
/*!40000 ALTER TABLE `CriticalValuePatterns` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `DoctorAvailabilities`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `DoctorAvailabilities` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `DoctorId` int NOT NULL,
  `Date` datetime(6) NOT NULL,
  `IsOutOfOffice` tinyint(1) NOT NULL DEFAULT '0',
  `Reason` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `StartTime` time(6) DEFAULT NULL,
  `EndTime` time(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `TimeZoneId` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT (_utf8mb4'UTC'),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_DoctorAvailabilities_DoctorId_Date` (`DoctorId`,`Date`),
  KEY `IX_DoctorAvailabilities_Date` (`Date`),
  KEY `IX_DoctorAvailabilities_DoctorId` (`DoctorId`),
  KEY `IX_DoctorAvailabilities_IsOutOfOffice` (`IsOutOfOffice`),
  CONSTRAINT `FK_DoctorAvailabilities_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `DoctorAvailabilities`
--

LOCK TABLES `DoctorAvailabilities` WRITE;
/*!40000 ALTER TABLE `DoctorAvailabilities` DISABLE KEYS */;
INSERT INTO `DoctorAvailabilities` (`Id`, `DoctorId`, `Date`, `IsOutOfOffice`, `Reason`, `StartTime`, `EndTime`, `CreatedAt`, `UpdatedAt`, `TimeZoneId`) VALUES (1,2,'2025-11-05 00:00:00.000000',0,'Personnel','15:00:00.000000','16:00:00.000000','2025-11-05 17:57:19.724948','2025-11-05 18:34:01.684519','America/Chicago');
INSERT INTO `DoctorAvailabilities` (`Id`, `DoctorId`, `Date`, `IsOutOfOffice`, `Reason`, `StartTime`, `EndTime`, `CreatedAt`, `UpdatedAt`, `TimeZoneId`) VALUES (2,2,'2025-11-13 00:00:00.000000',1,'','17:00:00.000000','18:00:00.000000','2025-11-13 21:31:32.662861','2025-11-13 21:31:54.936778','UTC');
INSERT INTO `DoctorAvailabilities` (`Id`, `DoctorId`, `Date`, `IsOutOfOffice`, `Reason`, `StartTime`, `EndTime`, `CreatedAt`, `UpdatedAt`, `TimeZoneId`) VALUES (3,2,'2025-11-14 00:00:00.000000',1,'','15:00:00.000000','16:00:00.000000','2025-11-13 21:32:52.373608',NULL,'UTC');
/*!40000 ALTER TABLE `DoctorAvailabilities` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `EmergencyIncidents`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `EmergencyIncidents` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `PatientId` int NOT NULL,
  `DoctorId` int DEFAULT NULL,
  `EmergencyType` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Severity` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Message` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Timestamp` datetime(6) NOT NULL,
  `DeviceId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DeviceToken` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IsAcknowledged` tinyint(1) NOT NULL,
  `AcknowledgedAt` datetime(6) DEFAULT NULL,
  `DoctorResponse` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ActionTaken` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Resolution` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ResolvedAt` datetime(6) DEFAULT NULL,
  `VitalSignsJson` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LocationJson` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IpAddress` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `UserAgent` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_EmergencyIncidents_DeviceToken` (`DeviceToken`),
  KEY `IX_EmergencyIncidents_DoctorId` (`DoctorId`),
  KEY `IX_EmergencyIncidents_PatientId` (`PatientId`),
  KEY `IX_EmergencyIncidents_Timestamp` (`Timestamp`),
  CONSTRAINT `FK_EmergencyIncidents_Users_DoctorId` FOREIGN KEY (`DoctorId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_EmergencyIncidents_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `EmergencyIncidents`
--

LOCK TABLES `EmergencyIncidents` WRITE;
/*!40000 ALTER TABLE `EmergencyIncidents` DISABLE KEYS */;
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (1,3,2,'PanicAttack','High','Patient experiencing severe panic attack, needs immediate assistance','2025-10-22 15:19:23.000000','device_001','token_001',1,'2025-11-12 01:17:09.479414','Incident acknowledged','Monitoring patient','Patient stabilized with breathing exercises','2025-10-22 15:49:23.000000','{\"heart_rate\": 120, \"blood_pressure\": \"140/90\"}','{\"lat\": 37.7749, \"lng\": -122.4194, \"address\": \"San Francisco, CA\"}','192.168.1.100','MentalHealthApp/1.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (2,3,2,'Cardiac','High','Emergency assistance needed','2025-11-24 00:04:04.764337','device_3_ios','PVMJc+rF7V6AOVjgTr3AQeQbNG9vIpjSqdd9BfeUzOk=',1,'2025-11-24 00:06:06.838373','Incident acknowledged','Monitoring patient',NULL,NULL,'{\"HeartRate\":93,\"BloodPressure\":\"120/86\",\"Temperature\":99.5,\"OxygenSaturation\":99,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-24T00:04:04.764337Z\"}','192.168.86.25','Expo/54.0.6 CFNetwork/3860.100.1 Darwin/25.1.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (3,3,NULL,'Other','Medium','Car accident - need immediate attention','2025-11-24 16:22:39.648851','device_3_ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=',0,NULL,NULL,NULL,NULL,NULL,'{\"HeartRate\":90,\"BloodPressure\":\"135/80\",\"Temperature\":98.4,\"OxygenSaturation\":95,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-24T16:22:39.649284Z\"}','192.168.86.24','Expo/1017756 CFNetwork/3826.600.41 Darwin/24.6.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (4,3,NULL,'Other','Critical','Car accident - need immediate attention','2025-11-24 16:23:30.359470','device_3_ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=',0,NULL,NULL,NULL,NULL,NULL,'{\"HeartRate\":90,\"BloodPressure\":\"125/87\",\"Temperature\":98.2,\"OxygenSaturation\":95,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-24T16:23:30.35947Z\"}','192.168.86.24','Expo/1017756 CFNetwork/3826.600.41 Darwin/24.6.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (5,3,NULL,'Other','Low','Car accident - need immediate attention','2025-11-24 16:32:17.977850','device_3_ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=',0,NULL,NULL,NULL,NULL,NULL,'{\"HeartRate\":82,\"BloodPressure\":\"138/89\",\"Temperature\":99.4,\"OxygenSaturation\":97,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-24T16:32:17.977852Z\"}','192.168.86.24','Expo/1017756 CFNetwork/3826.600.41 Darwin/24.6.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (6,3,NULL,'Other','Medium','Car accident - need immediate attention','2025-11-24 16:35:56.261674','device_3_ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=',0,NULL,NULL,NULL,NULL,NULL,'{\"HeartRate\":null,\"BloodPressure\":null,\"Temperature\":null,\"OxygenSaturation\":null,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-24T16:35:56.261676Z\"}','192.168.86.24','Expo/1017756 CFNetwork/3826.600.41 Darwin/24.6.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (7,3,2,'Other','High','Car accident - need immediate attention','2025-11-25 18:49:26.892315','device_3_ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=',1,'2025-11-27 17:18:01.925794','Marked as not applicable for AI analysis','Excluded from health check',NULL,NULL,'{\"HeartRate\":null,\"BloodPressure\":null,\"Temperature\":null,\"OxygenSaturation\":null,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-25T18:49:26.892507Z\"}','192.168.1.226','Expo/1017756 CFNetwork/3826.600.41 Darwin/24.6.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (8,3,NULL,'Other','High','Car accident - need immediate attention','2025-11-25 18:51:00.638506','device_3_ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=',0,NULL,NULL,NULL,NULL,NULL,'{\"HeartRate\":null,\"BloodPressure\":null,\"Temperature\":null,\"OxygenSaturation\":null,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-25T18:51:00.638508Z\"}','192.168.1.226','Expo/1017756 CFNetwork/3826.600.41 Darwin/24.6.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (9,3,NULL,'Other','High','Workplace injury - urgent follow-up needed','2025-11-25 18:52:05.319260','device_3_ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=',0,NULL,NULL,NULL,NULL,NULL,'{\"HeartRate\":null,\"BloodPressure\":null,\"Temperature\":null,\"OxygenSaturation\":null,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-25T18:52:05.319268Z\"}','192.168.1.226','Expo/1017756 CFNetwork/3826.600.41 Darwin/24.6.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (10,3,2,'Other','High','Car accident - need immediate attention','2025-11-27 16:45:16.269633','device_3_ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=',1,'2025-11-27 17:16:44.638442','Marked as not applicable for AI analysis','Excluded from health check',NULL,NULL,'{\"HeartRate\":null,\"BloodPressure\":null,\"Temperature\":null,\"OxygenSaturation\":null,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-27T16:45:16.270265Z\"}','192.168.86.24','Expo/1017756 CFNetwork/3826.600.41 Darwin/24.6.0');
INSERT INTO `EmergencyIncidents` (`Id`, `PatientId`, `DoctorId`, `EmergencyType`, `Severity`, `Message`, `Timestamp`, `DeviceId`, `DeviceToken`, `IsAcknowledged`, `AcknowledgedAt`, `DoctorResponse`, `ActionTaken`, `Resolution`, `ResolvedAt`, `VitalSignsJson`, `LocationJson`, `IpAddress`, `UserAgent`) VALUES (11,3,2,'Other','High','Car accident - need immediate attention','2025-11-27 16:45:33.115067','device_3_ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=',1,'2025-11-27 17:16:40.775991','Marked as not applicable for AI analysis','Excluded from health check',NULL,NULL,'{\"HeartRate\":null,\"BloodPressure\":null,\"Temperature\":null,\"OxygenSaturation\":null,\"Steps\":null,\"Calories\":null,\"ActivityLevel\":null}','{\"Latitude\":0,\"Longitude\":0,\"Accuracy\":10,\"Address\":\"Test Location\",\"Timestamp\":\"2025-11-27T16:45:33.11507Z\"}','192.168.86.24','Expo/1017756 CFNetwork/3826.600.41 Darwin/24.6.0');
/*!40000 ALTER TABLE `EmergencyIncidents` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `GenericQuestionPatterns`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `GenericQuestionPatterns` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Pattern` varchar(500) NOT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `Priority` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT (now(6)),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_GenericQuestionPatterns_IsActive` (`IsActive`),
  KEY `IX_GenericQuestionPatterns_Priority` (`Priority`),
  KEY `IX_GenericQuestionPatterns_IsActive_Priority` (`IsActive`,`Priority`)
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `GenericQuestionPatterns`
--

LOCK TABLES `GenericQuestionPatterns` WRITE;
/*!40000 ALTER TABLE `GenericQuestionPatterns` DISABLE KEYS */;
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (1,'what are normal','Matches questions asking about normal values',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (2,'what are the normal','Matches questions asking about normal values (with \"the\")',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (3,'what is normal','Matches questions asking what is normal',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (4,'what are critical','Matches questions asking about critical values',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (5,'what are serious','Matches questions asking about serious values',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (6,'what is a normal','Matches questions asking what is a normal value',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (7,'what are typical','Matches questions asking about typical values',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (8,'what is typical','Matches questions asking what is typical',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (9,'normal values of','Matches questions about normal values of something',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (10,'normal range of','Matches questions about normal range of something',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (11,'normal levels of','Matches questions about normal levels of something',10,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (12,'what does','Matches questions asking what something does',5,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (13,'how does','Matches questions asking how something works',5,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (14,'explain','Matches questions asking to explain something',5,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (15,'tell me about','Matches questions asking to tell about something',5,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (16,'what is','Matches questions asking what something is',5,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (17,'what are','Matches questions asking what things are',5,1,'2025-11-12 11:48:25.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (18,'what are normal','Matches questions asking about normal values',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (19,'what are the normal','Matches questions asking about normal values (with \"the\")',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (20,'what is normal','Matches questions asking what is normal',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (21,'what are critical','Matches questions asking about critical values',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (22,'what are serious','Matches questions asking about serious values',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (23,'what is a normal','Matches questions asking what is a normal value',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (24,'what are typical','Matches questions asking about typical values',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (25,'what is typical','Matches questions asking what is typical',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (26,'normal values of','Matches questions about normal values of something',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (27,'normal range of','Matches questions about normal range of something',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (28,'normal levels of','Matches questions about normal levels of something',10,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (29,'what does','Matches questions asking what something does',5,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (30,'how does','Matches questions asking how something works',5,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (31,'explain','Matches questions asking to explain something',5,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (32,'tell me about','Matches questions asking to tell about something',5,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (33,'what is','Matches questions asking what something is',5,1,'2025-11-12 11:48:58.000000',NULL);
INSERT INTO `GenericQuestionPatterns` (`Id`, `Pattern`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (34,'what are','Matches questions asking what things are',5,1,'2025-11-12 11:48:58.000000',NULL);
/*!40000 ALTER TABLE `GenericQuestionPatterns` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `JournalEntries`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `JournalEntries` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `EnteredByUserId` int DEFAULT NULL,
  `Text` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `AIResponse` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Mood` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `IgnoredAt` datetime(6) DEFAULT NULL,
  `IgnoredByDoctorId` int DEFAULT NULL,
  `IsIgnoredByDoctor` tinyint(1) NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`Id`),
  KEY `IX_JournalEntries_EnteredByUserId` (`EnteredByUserId`),
  KEY `IX_JournalEntries_UserId` (`UserId`),
  KEY `IX_JournalEntries_IgnoredByDoctorId` (`IgnoredByDoctorId`),
  CONSTRAINT `FK_JournalEntries_Users_EnteredByUserId` FOREIGN KEY (`EnteredByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_JournalEntries_Users_IgnoredByDoctorId` FOREIGN KEY (`IgnoredByDoctorId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_JournalEntries_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `JournalEntries`
--

LOCK TABLES `JournalEntries` WRITE;
/*!40000 ALTER TABLE `JournalEntries` DISABLE KEYS */;
INSERT INTO `JournalEntries` (`Id`, `UserId`, `EnteredByUserId`, `Text`, `AIResponse`, `Mood`, `CreatedAt`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`, `IsActive`) VALUES (1,3,3,'Feeling anxious about work today. Had a difficult meeting with my manager.','It sounds like you had a challenging day at work. Anxiety about work situations is very common. Consider taking some deep breaths and maybe talking to someone you trust about your concerns.','Anxious','2025-10-21 16:19:23.000000','2025-11-11 16:30:23.661170',2,1,1);
INSERT INTO `JournalEntries` (`Id`, `UserId`, `EnteredByUserId`, `Text`, `AIResponse`, `Mood`, `CreatedAt`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`, `IsActive`) VALUES (2,3,3,'Much better day today! Went for a walk in the park and felt more relaxed.','That\'s wonderful to hear! Physical activity and time in nature can be very therapeutic. Keep up the great work with self-care!','Happy','2025-10-20 16:19:23.000000','2025-11-10 20:38:34.393737',4,1,1);
INSERT INTO `JournalEntries` (`Id`, `UserId`, `EnteredByUserId`, `Text`, `AIResponse`, `Mood`, `CreatedAt`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`, `IsActive`) VALUES (3,3,2,'Patient reported improved sleep patterns this week. Discussed stress management techniques.','Great progress on sleep patterns! Stress management is crucial for mental health. Continue monitoring and provide ongoing support.','Neutral','2025-10-19 16:19:23.000000','2025-11-10 20:38:35.089363',4,1,1);
INSERT INTO `JournalEntries` (`Id`, `UserId`, `EnteredByUserId`, `Text`, `AIResponse`, `Mood`, `CreatedAt`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`, `IsActive`) VALUES (4,3,2,'I am feeling better','It\'s wonderful to hear that you\'re feeling good! I\'m glad you\'re taking the time to reflect on positive moments. Keep nurturing these positive feelings and remember to celebrate the good times.','Happy','2025-10-22 23:14:45.698000','2025-11-10 20:38:32.949000',4,1,1);
INSERT INTO `JournalEntries` (`Id`, `UserId`, `EnteredByUserId`, `Text`, `AIResponse`, `Mood`, `CreatedAt`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`, `IsActive`) VALUES (5,3,2,'Patient is fine, now','Thank you for sharing your thoughts with me. It takes courage to express your feelings, and I appreciate you trusting me with them. Remember that you\'re not alone in whatever you\'re experiencing.','Neutral','2025-10-23 14:44:01.934000','2025-11-10 20:38:30.543605',4,1,1);
INSERT INTO `JournalEntries` (`Id`, `UserId`, `EnteredByUserId`, `Text`, `AIResponse`, `Mood`, `CreatedAt`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`, `IsActive`) VALUES (6,3,2,'Again Test','Thank you for sharing your thoughts with me. It takes courage to express your feelings, and I appreciate you trusting me with them. Remember that you\'re not alone in whatever you\'re experiencing.','Neutral','2025-10-23 15:05:42.293000',NULL,NULL,0,1);
INSERT INTO `JournalEntries` (`Id`, `UserId`, `EnteredByUserId`, `Text`, `AIResponse`, `Mood`, `CreatedAt`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`, `IsActive`) VALUES (7,3,2,'Test record','Thank you for sharing your thoughts with me. It takes courage to express your feelings, and I appreciate you trusting me with them. Remember that you\'re not alone in whatever you\'re experiencing.','Neutral','2025-11-13 17:46:19.303000',NULL,NULL,0,1);
INSERT INTO `JournalEntries` (`Id`, `UserId`, `EnteredByUserId`, `Text`, `AIResponse`, `Mood`, `CreatedAt`, `IgnoredAt`, `IgnoredByDoctorId`, `IsIgnoredByDoctor`, `IsActive`) VALUES (8,3,2,'I am feeling much better.','It\'s wonderful to hear that you\'re feeling good! I\'m glad you\'re taking the time to reflect on positive moments. Keep nurturing these positive feelings and remember to celebrate the good times.','Happy','2025-11-13 17:46:58.512000',NULL,NULL,0,1);
/*!40000 ALTER TABLE `JournalEntries` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `KnowledgeBaseCategories`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `KnowledgeBaseCategories` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `DisplayOrder` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_KnowledgeBaseCategories_Name` (`Name`),
  KEY `IX_KnowledgeBaseCategories_IsActive_DisplayOrder` (`IsActive`,`DisplayOrder`)
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `KnowledgeBaseCategories`
--

LOCK TABLES `KnowledgeBaseCategories` WRITE;
/*!40000 ALTER TABLE `KnowledgeBaseCategories` DISABLE KEYS */;
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (1,'General Health','General health and wellness information',1,1,'2025-11-11 22:03:09.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (2,'Mental Health','Mental health and wellness topics',2,1,'2025-11-11 22:03:09.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (3,'Medications','Information about medications and prescriptions',3,1,'2025-11-11 22:03:09.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (4,'Symptoms','Common symptoms and their meanings',4,1,'2025-11-11 22:03:09.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (5,'Emergency','Emergency and urgent care information',5,1,'2025-11-11 22:03:09.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (6,'Appointments','Information about appointments and scheduling',6,1,'2025-11-11 22:03:09.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (7,'General Questions','General questions and answers',7,1,'2025-11-11 22:03:09.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (8,'General Health','General health and wellness information',1,1,'2025-11-12 08:16:28.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (9,'Mental Health','Mental health and wellness topics',2,1,'2025-11-12 08:16:28.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (10,'Medications','Information about medications and prescriptions',3,1,'2025-11-12 08:16:28.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (11,'Symptoms','Common symptoms and their meanings',4,1,'2025-11-12 08:16:28.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (12,'Emergency','Emergency and urgent care information',5,1,'2025-11-12 08:16:28.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (13,'Appointments','Information about appointments and scheduling',6,1,'2025-11-12 08:16:28.000000',NULL);
INSERT INTO `KnowledgeBaseCategories` (`Id`, `Name`, `Description`, `DisplayOrder`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (14,'General Questions','General questions and answers',7,1,'2025-11-12 08:16:28.000000',NULL);
/*!40000 ALTER TABLE `KnowledgeBaseCategories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `KnowledgeBaseEntries`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `KnowledgeBaseEntries` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CategoryId` int NOT NULL,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Content` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Keywords` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Priority` int NOT NULL DEFAULT '0',
  `UseAsDirectResponse` tinyint(1) NOT NULL DEFAULT '1',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `CreatedByUserId` int DEFAULT NULL,
  `UpdatedByUserId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_KnowledgeBaseEntries_CategoryId` (`CategoryId`),
  KEY `IX_KnowledgeBaseEntries_Priority` (`Priority`),
  KEY `IX_KnowledgeBaseEntries_IsActive` (`IsActive`),
  KEY `IX_KnowledgeBaseEntries_CategoryId_IsActive_Priority` (`CategoryId`,`IsActive`,`Priority`),
  KEY `IX_KnowledgeBaseEntries_CreatedByUserId` (`CreatedByUserId`),
  KEY `IX_KnowledgeBaseEntries_UpdatedByUserId` (`UpdatedByUserId`),
  CONSTRAINT `FK_KnowledgeBaseEntries_KnowledgeBaseCategories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `KnowledgeBaseCategories` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_KnowledgeBaseEntries_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_KnowledgeBaseEntries_Users_UpdatedByUserId` FOREIGN KEY (`UpdatedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `KnowledgeBaseEntries`
--

LOCK TABLES `KnowledgeBaseEntries` WRITE;
/*!40000 ALTER TABLE `KnowledgeBaseEntries` DISABLE KEYS */;
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (1,1,'Blood Pressure Information','Normal blood pressure is typically around 120/80 mmHg. High blood pressure (hypertension) is 140/90 or higher. Low blood pressure (hypotension) is below 90/60. If you experience symptoms like dizziness, fainting, or severe headaches, please contact your healthcare provider immediately.','blood pressure,hypertension,hypotension,high bp,low bp',5,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (2,1,'Heart Rate Information','A normal resting heart rate for adults is typically between 60-100 beats per minute (bpm). Factors like age, fitness level, and medications can affect heart rate. If you experience a heart rate consistently above 100 bpm at rest or below 60 bpm with symptoms like dizziness, please consult your healthcare provider.','heart rate,pulse,bpm,heartbeat,tachycardia,bradycardia',5,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (3,2,'Anxiety Information','Anxiety is a normal response to stress, but when it becomes excessive or persistent, it may indicate an anxiety disorder. Common symptoms include excessive worry, restlessness, difficulty concentrating, and physical symptoms like rapid heartbeat or sweating. If anxiety is interfering with your daily life, please speak with your healthcare provider or mental health professional.','anxiety,anxious,worry,panic,stress,anxiety disorder',8,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (4,2,'Depression Information','Depression is a common mental health condition characterized by persistent feelings of sadness, loss of interest in activities, changes in sleep or appetite, and difficulty concentrating. If you are experiencing symptoms of depression that last for more than two weeks, please reach out to your healthcare provider or a mental health professional. Remember, help is available and treatment can be effective.','depression,depressed,sadness,feeling down,mood,mental health',8,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (5,2,'Sleep and Mental Health','Sleep plays a crucial role in mental health. Most adults need 7-9 hours of sleep per night. Poor sleep can worsen anxiety and depression, while good sleep hygiene can improve mental well-being. Tips for better sleep include maintaining a regular sleep schedule, creating a relaxing bedtime routine, avoiding screens before bed, and keeping your bedroom cool and dark.','sleep,insomnia,sleeping,rest,sleep schedule,sleep hygiene',6,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (6,4,'Headache Information','Headaches can have many causes including stress, dehydration, lack of sleep, or underlying medical conditions. Most headaches are not serious, but you should seek immediate medical attention if you experience a sudden severe headache, headache with fever or stiff neck, or headache after a head injury. For mild headaches, rest, hydration, and over-the-counter pain relievers may help.','headache,head pain,migraine,head hurts',4,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (7,4,'Chest Pain Information','Chest pain can be caused by various conditions, some serious. If you experience sudden, severe chest pain, especially with shortness of breath, sweating, or pain radiating to your arm or jaw, call emergency services immediately as this could indicate a heart attack. For less severe chest pain, it is still important to consult with your healthcare provider to determine the cause.','chest pain,chest discomfort,heart pain',9,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (8,5,'When to Call Emergency Services','Call emergency services (911) immediately if you or someone else experiences: severe chest pain or pressure, difficulty breathing, severe allergic reaction, signs of stroke (sudden weakness, confusion, trouble speaking), severe injury, or thoughts of self-harm. For mental health emergencies, you can also call the National Suicide Prevention Lifeline at 988.','emergency,911,urgent,emergency services,call 911',10,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (9,6,'How to Schedule an Appointment','You can schedule an appointment through this application by navigating to the Appointments page. Select your preferred doctor, date, and time. You will receive a confirmation and reminder notifications. If you need to reschedule or cancel, you can do so through the same page. For urgent matters, please contact your doctor\'s office directly.','appointment,schedule,booking,appointment booking,make appointment',3,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (10,7,'About This Application','This is a mental health application designed to help you track your health, communicate with your healthcare providers, and access health information. You can use the journal feature to record your thoughts and moods, upload medical documents, schedule appointments, and chat with our AI assistant for health-related questions. For specific medical advice, always consult with your healthcare provider.','about,help,what is this,application,app,how to use',2,1,1,'2025-11-11 22:03:09.000000',NULL,NULL,NULL);
INSERT INTO `KnowledgeBaseEntries` (`Id`, `CategoryId`, `Title`, `Content`, `Keywords`, `Priority`, `UseAsDirectResponse`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CreatedByUserId`, `UpdatedByUserId`) VALUES (11,1,'Glucose (Blood Sugar) Information','Normal blood glucose (blood sugar) levels vary depending on when you last ate:\n- **Fasting (before eating)**: 70-100 mg/dL (3.9-5.6 mmol/L) is considered normal\n- **After meals (2 hours)**: Less than 140 mg/dL (7.8 mmol/L) is considered normal\n- **Random glucose**: 70-140 mg/dL (3.9-7.8 mmol/L) is typically normal\n\n**High glucose (hyperglycemia)**: Fasting levels above 126 mg/dL (7.0 mmol/L) or random levels above 200 mg/dL (11.1 mmol/L) may indicate diabetes and require medical evaluation.\n\n**Low glucose (hypoglycemia)**: Levels below 70 mg/dL (3.9 mmol/L) can cause symptoms like shakiness, sweating, confusion, and require immediate treatment.\n\nIf you have concerns about your blood glucose levels, please consult with your healthcare provider for proper evaluation and management.','glucose,blood sugar,blood glucose,normal glucose,glucose levels,normal values of glucose,blood sugar levels,normal blood sugar',6,1,1,'2025-11-12 08:16:28.000000',NULL,NULL,NULL);
/*!40000 ALTER TABLE `KnowledgeBaseEntries` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `MedicalThresholds`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `MedicalThresholds` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `ParameterName` varchar(100) NOT NULL,
  `Unit` varchar(50) DEFAULT NULL,
  `SeverityLevel` varchar(50) DEFAULT NULL,
  `MinValue` double DEFAULT NULL,
  `MaxValue` double DEFAULT NULL,
  `ComparisonOperator` varchar(20) DEFAULT NULL,
  `ThresholdValue` double DEFAULT NULL,
  `SecondaryParameterName` varchar(100) DEFAULT NULL,
  `SecondaryThresholdValue` double DEFAULT NULL,
  `SecondaryComparisonOperator` varchar(20) DEFAULT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `Priority` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT (now(6)),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_MedicalThresholds_ParameterName` (`ParameterName`),
  KEY `IX_MedicalThresholds_IsActive` (`IsActive`),
  KEY `IX_MedicalThresholds_Priority` (`Priority`),
  KEY `IX_MedicalThresholds_SeverityLevel` (`SeverityLevel`),
  KEY `IX_MedicalThresholds_ParameterName_IsActive` (`ParameterName`,`IsActive`),
  KEY `IX_MedicalThresholds_IsActive_Priority` (`IsActive`,`Priority`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `MedicalThresholds`
--

LOCK TABLES `MedicalThresholds` WRITE;
/*!40000 ALTER TABLE `MedicalThresholds` DISABLE KEYS */;
INSERT INTO `MedicalThresholds` (`Id`, `ParameterName`, `Unit`, `SeverityLevel`, `MinValue`, `MaxValue`, `ComparisonOperator`, `ThresholdValue`, `SecondaryParameterName`, `SecondaryThresholdValue`, `SecondaryComparisonOperator`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (1,'Blood Pressure','mmHg','Critical',NULL,NULL,'>=',180,'Blood Pressure Diastolic',110,'>=','Hypertensive crisis - immediate medical intervention required',10,1,'2025-11-12 12:20:39.000000',NULL);
INSERT INTO `MedicalThresholds` (`Id`, `ParameterName`, `Unit`, `SeverityLevel`, `MinValue`, `MaxValue`, `ComparisonOperator`, `ThresholdValue`, `SecondaryParameterName`, `SecondaryThresholdValue`, `SecondaryComparisonOperator`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (2,'Blood Pressure','mmHg','High',NULL,NULL,'>=',140,'Blood Pressure Diastolic',90,'>=','High blood pressure - requires immediate attention',8,1,'2025-11-12 12:20:39.000000',NULL);
INSERT INTO `MedicalThresholds` (`Id`, `ParameterName`, `Unit`, `SeverityLevel`, `MinValue`, `MaxValue`, `ComparisonOperator`, `ThresholdValue`, `SecondaryParameterName`, `SecondaryThresholdValue`, `SecondaryComparisonOperator`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (3,'Hemoglobin','g/dL','Critical',NULL,NULL,'<',7,NULL,NULL,NULL,'Severe anemia - blood transfusion may be required',10,1,'2025-11-12 12:20:39.000000',NULL);
INSERT INTO `MedicalThresholds` (`Id`, `ParameterName`, `Unit`, `SeverityLevel`, `MinValue`, `MaxValue`, `ComparisonOperator`, `ThresholdValue`, `SecondaryParameterName`, `SecondaryThresholdValue`, `SecondaryComparisonOperator`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (4,'Hemoglobin','g/dL','Low',NULL,NULL,'<',10,NULL,NULL,NULL,'Moderate anemia - requires monitoring',8,1,'2025-11-12 12:20:39.000000',NULL);
INSERT INTO `MedicalThresholds` (`Id`, `ParameterName`, `Unit`, `SeverityLevel`, `MinValue`, `MaxValue`, `ComparisonOperator`, `ThresholdValue`, `SecondaryParameterName`, `SecondaryThresholdValue`, `SecondaryComparisonOperator`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (5,'Triglycerides','mg/dL','Critical',NULL,NULL,'>=',500,NULL,NULL,NULL,'Extremely high - risk of pancreatitis',10,1,'2025-11-12 12:20:39.000000',NULL);
INSERT INTO `MedicalThresholds` (`Id`, `ParameterName`, `Unit`, `SeverityLevel`, `MinValue`, `MaxValue`, `ComparisonOperator`, `ThresholdValue`, `SecondaryParameterName`, `SecondaryThresholdValue`, `SecondaryComparisonOperator`, `Description`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (6,'Triglycerides','mg/dL','High',NULL,NULL,'>=',200,NULL,NULL,NULL,'High - requires dietary intervention',8,1,'2025-11-12 12:20:39.000000',NULL);
/*!40000 ALTER TABLE `MedicalThresholds` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `NotePriorities`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `NotePriorities` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Icon` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Color` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `SortOrder` int NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_NotePriorities_Name` (`Name`),
  KEY `IX_NotePriorities_IsActive` (`IsActive`),
  KEY `IX_NotePriorities_SortOrder` (`SortOrder`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `NotePriorities`
--

LOCK TABLES `NotePriorities` WRITE;
/*!40000 ALTER TABLE `NotePriorities` DISABLE KEYS */;
INSERT INTO `NotePriorities` (`Id`, `Name`, `Description`, `Icon`, `Color`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (1,'Low','Low priority notes','🟢','success',1,1,'2025-10-23 10:03:45.000000');
INSERT INTO `NotePriorities` (`Id`, `Name`, `Description`, `Icon`, `Color`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (2,'Medium','Medium priority notes','🟡','warning',1,2,'2025-10-23 10:03:45.000000');
INSERT INTO `NotePriorities` (`Id`, `Name`, `Description`, `Icon`, `Color`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (3,'High','High priority notes','🟠','danger',1,3,'2025-10-23 10:03:45.000000');
INSERT INTO `NotePriorities` (`Id`, `Name`, `Description`, `Icon`, `Color`, `IsActive`, `SortOrder`, `CreatedAt`) VALUES (4,'Critical','Critical priority notes requiring immediate attention','🔴','danger',1,4,'2025-10-23 10:03:45.000000');
/*!40000 ALTER TABLE `NotePriorities` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `RegisteredDevices`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `RegisteredDevices` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `PatientId` int NOT NULL,
  `DeviceId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DeviceName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DeviceType` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DeviceModel` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `OperatingSystem` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DeviceToken` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PublicKey` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `ExpiresAt` datetime(6) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `LastUsedAt` datetime(6) DEFAULT NULL,
  `LastKnownLocation` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_RegisteredDevices_DeviceId` (`DeviceId`),
  UNIQUE KEY `IX_RegisteredDevices_DeviceToken` (`DeviceToken`),
  KEY `IX_RegisteredDevices_PatientId` (`PatientId`),
  CONSTRAINT `FK_RegisteredDevices_Users_PatientId` FOREIGN KEY (`PatientId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `RegisteredDevices`
--

LOCK TABLES `RegisteredDevices` WRITE;
/*!40000 ALTER TABLE `RegisteredDevices` DISABLE KEYS */;
INSERT INTO `RegisteredDevices` (`Id`, `PatientId`, `DeviceId`, `DeviceName`, `DeviceType`, `DeviceModel`, `OperatingSystem`, `DeviceToken`, `PublicKey`, `CreatedAt`, `ExpiresAt`, `IsActive`, `LastUsedAt`, `LastKnownLocation`) VALUES (1,3,'device_3_ios','ios Device','mobile','ios','ios','z8IMRphkCI4XmxRIgWH8LLBRZSlBuVcxkXrpM+5DjA4=','test-key','2025-11-23 23:15:40.147553','2026-11-24 14:22:41.300926',1,'2025-11-27 16:45:33.129610',NULL);
/*!40000 ALTER TABLE `RegisteredDevices` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Roles`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `Roles` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Roles_Name` (`Name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Roles`
--

LOCK TABLES `Roles` WRITE;
/*!40000 ALTER TABLE `Roles` DISABLE KEYS */;
INSERT INTO `Roles` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (1,'Patient','Regular patients who use the app for self-care and journaling',1,'2025-10-22 16:19:23.000000');
INSERT INTO `Roles` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (2,'Doctor','Medical professionals who provide care and consultations',1,'2025-10-22 16:19:23.000000');
INSERT INTO `Roles` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (3,'Admin','System administrators who manage users and system settings',1,'2025-10-22 16:19:23.000000');
INSERT INTO `Roles` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (4,'Coordinator','Coordinators who manage appointments and patient-doctor assignments',1,'2025-11-27 07:30:10.000000');
INSERT INTO `Roles` (`Id`, `Name`, `Description`, `IsActive`, `CreatedAt`) VALUES (5,'Attorney','Attorneys who have read access to patient information and can communicate with patients',1,'2025-11-29 15:28:04.000000');
/*!40000 ALTER TABLE `Roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `SectionMarkers`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `SectionMarkers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Marker` varchar(500) NOT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `Category` varchar(100) DEFAULT NULL,
  `Priority` int NOT NULL DEFAULT '0',
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT (now(6)),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_SectionMarkers_Marker` (`Marker`),
  KEY `IX_SectionMarkers_IsActive` (`IsActive`),
  KEY `IX_SectionMarkers_Category` (`Category`),
  KEY `IX_SectionMarkers_Priority` (`Priority`),
  KEY `IX_SectionMarkers_IsActive_Priority` (`IsActive`,`Priority`)
) ENGINE=InnoDB AUTO_INCREMENT=29 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `SectionMarkers`
--

LOCK TABLES `SectionMarkers` WRITE;
/*!40000 ALTER TABLE `SectionMarkers` DISABLE KEYS */;
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (1,'=== RECENT JOURNAL ENTRIES ===','Marks the start of recent journal entries section','Patient Data',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (2,'=== MEDICAL DATA SUMMARY ===','Marks the start of medical data summary section','Patient Data',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (3,'=== CURRENT MEDICAL STATUS ===','Marks the start of current medical status section','Patient Data',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (4,'=== HISTORICAL MEDICAL CONCERNS ===','Marks the start of historical medical concerns section','Patient Data',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (5,'=== HEALTH TREND ANALYSIS ===','Marks the start of health trend analysis section','Patient Data',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (6,'=== RECENT CLINICAL NOTES ===','Marks the start of recent clinical notes section','Patient Data',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (7,'=== RECENT CHAT HISTORY ===','Marks the start of recent chat history section','Patient Data',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (8,'=== RECENT EMERGENCY INCIDENTS ===','Marks the start of recent emergency incidents section','Emergency',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (9,'=== USER QUESTION ===','Marks the start of user question section','Instructions',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (10,'=== PROGRESSION ANALYSIS ===','Marks the start of progression analysis section','Patient Data',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (11,'=== INSTRUCTIONS FOR AI HEALTH CHECK ANALYSIS ===','Marks the start of AI health check instructions','Instructions',100,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (12,'Recent Patient Activity:','Alternative marker for recent patient activity','Patient Data',50,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (13,'Current Test Results','Alternative marker for current test results','Patient Data',50,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (14,'Latest Update:','Alternative marker for latest update','Patient Data',50,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (15,'Doctor asks:','Marks doctor questions in chat','Patient Data',50,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (16,'Patient asks:','Marks patient questions in chat','Patient Data',50,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (17,'**Medical Resource Information','Marks medical resource information section','Resources',50,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (18,'**Medical Facilities Search','Marks medical facilities search section','Resources',50,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (19,'Fall','Detects fall incidents in emergency data','Emergency',30,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (20,'Session:','Marks session information','Patient Data',30,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (21,'Summary:','Marks summary sections','Patient Data',30,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (22,'Clinical Notes','Alternative marker for clinical notes','Patient Data',30,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (23,'Journal Entries','Alternative marker for journal entries','Patient Data',30,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (24,'Chat History','Alternative marker for chat history','Patient Data',30,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (25,'MOOD PATTERNS (Last 30 days):','Marks mood patterns section','Patient Data',40,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (26,'RECENT JOURNAL ENTRIES (Last 14 days):','Alternative marker for recent journal entries','Patient Data',40,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
INSERT INTO `SectionMarkers` (`Id`, `Marker`, `Description`, `Category`, `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`) VALUES (27,'AI Health Check for Patient','Default prompt for AI health check','Instructions',20,1,'2025-11-12 19:17:28.000000','2025-11-12 19:18:41.000000');
/*!40000 ALTER TABLE `SectionMarkers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `SmsMessages`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `SmsMessages` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SenderId` int NOT NULL,
  `ReceiverId` int NOT NULL,
  `Message` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SentAt` datetime(6) NOT NULL,
  `IsRead` tinyint(1) NOT NULL DEFAULT '0',
  `ReadAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_SmsMessages_ReceiverId_IsRead` (`ReceiverId`,`IsRead`),
  KEY `IX_SmsMessages_SenderId_ReceiverId_SentAt` (`SenderId`,`ReceiverId`,`SentAt`),
  CONSTRAINT `FK_SmsMessages_Users_ReceiverId` FOREIGN KEY (`ReceiverId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_SmsMessages_Users_SenderId` FOREIGN KEY (`SenderId`) REFERENCES `Users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=186 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `SmsMessages`
--

LOCK TABLES `SmsMessages` WRITE;
/*!40000 ALTER TABLE `SmsMessages` DISABLE KEYS */;
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (1,2,3,'Hi John, I wanted to check in on how you\'re feeling today. Please let me know if you need to talk.','2025-10-22 14:19:23.000000',1,'2025-10-22 15:19:23.000000');
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (2,3,2,'Thank you Dr. Sarah. I\'m feeling better today after our session yesterday.','2025-10-22 15:19:23.000000',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (3,1,2,'System notification: New patient assignment - John Doe has been assigned to you.','2025-10-21 16:19:23.000000',1,'2025-10-21 17:19:23.000000');
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (4,2,3,'Hi there','2025-10-22 23:01:05.456179',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (5,3,2,'Hello','2025-10-22 23:01:27.694725',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (6,3,2,'I need urgent help','2025-10-26 00:51:05.862918',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (7,3,2,'Having trouble sleeping','2025-10-26 00:53:29.026771',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (8,3,2,'Hello','2025-11-02 18:45:59.240187',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (9,2,3,'How are you?','2025-11-02 18:46:13.553888',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (10,3,2,'I am doing good, how about you?','2025-11-02 18:46:38.757095',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (11,2,3,'doing good','2025-11-02 18:46:47.904554',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (12,3,2,'Hello from mobile','2025-11-02 18:53:03.480553',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (13,2,3,'Hi there','2025-11-02 19:57:15.418554',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (14,2,3,'how are things going on?','2025-11-02 19:57:45.269119',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (15,3,2,'things are going good','2025-11-02 19:58:09.188818',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (16,3,2,'Hello','2025-11-02 22:13:41.734221',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (17,2,3,'Hi there','2025-11-02 22:13:53.338343',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (18,3,2,'Testing','2025-11-02 22:16:54.188565',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (19,2,3,'Testing 1','2025-11-02 22:20:28.852912',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (20,3,2,'Testing 2 Doc','2025-11-02 22:20:47.358349',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (21,2,3,'Testing 3 Patient','2025-11-02 22:20:57.774763',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (22,3,2,'Testing 4 Doc','2025-11-02 22:21:41.388728',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (23,2,3,'Testing 5 Patient','2025-11-02 22:22:10.131447',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (24,3,2,'Testing 6 Doc','2025-11-02 22:27:00.171156',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (25,2,3,'Testing 7 Patient','2025-11-02 22:27:26.766579',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (26,3,2,'Testing 8 Doc','2025-11-02 22:30:44.972534',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (27,2,3,'Testing 9 Patient','2025-11-02 22:31:11.882254',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (28,3,2,'Testing 10 Doc','2025-11-02 22:33:40.641200',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (29,2,3,'Testing 10 Patient Patient','2025-11-02 22:34:18.018532',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (30,3,2,'t4w','2025-11-16 01:00:45.417446',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (31,2,3,'hello','2025-11-16 01:00:57.454247',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (32,3,2,'hi','2025-11-16 01:01:14.480136',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (33,2,3,'Hello','2025-11-16 16:37:53.486114',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (34,3,2,'Hi there','2025-11-16 16:37:59.475967',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (35,3,2,'machine 1 - 1st message','2025-11-16 16:39:51.820214',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (36,3,2,'machine 1 - 1st message','2025-11-16 16:40:35.228012',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (37,2,3,'machine 2 - 1st message','2025-11-16 16:40:48.169355',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (38,3,2,'machine 1 - 2nd message','2025-11-16 16:41:10.676703',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (39,3,2,'1','2025-11-16 16:51:34.191312',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (40,3,2,'11','2025-11-16 16:51:59.906819',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (41,3,2,'1','2025-11-16 16:54:48.564590',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (42,2,3,'2','2025-11-16 16:55:00.530750',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (43,3,2,'3','2025-11-16 16:55:16.403904',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (44,2,3,'4','2025-11-16 16:55:23.317759',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (45,3,2,'5555555555555555','2025-11-16 16:55:36.256789',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (46,2,3,'66666666666','2025-11-16 16:55:43.443508',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (47,2,3,'hi there','2025-11-17 23:42:53.542072',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (48,3,2,'Hello','2025-11-18 00:14:28.568546',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (49,2,3,'how are things going on','2025-11-18 00:14:38.121327',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (50,2,3,'Hi there','2025-11-18 14:30:19.011107',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (51,3,2,'Hello','2025-11-18 14:30:25.501713',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (52,2,3,'Hi','2025-11-18 14:30:31.130219',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (53,2,3,'Hi there','2025-11-18 14:30:44.882446',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (54,2,3,'test','2025-11-18 15:10:03.817567',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (55,3,2,'Iqbal Khan','2025-11-23 23:14:36.832183',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (56,3,2,'hello','2025-11-24 14:23:32.820994',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (57,2,3,'hi','2025-11-24 14:23:48.784211',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (58,3,2,'test','2025-11-24 14:24:18.754515',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (59,3,2,'Hi there','2025-11-24 14:29:40.163865',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (60,2,3,'Hello','2025-11-24 14:30:21.241376',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (61,3,2,'How are you?','2025-11-24 14:30:29.825529',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (62,3,2,'i am doing good','2025-11-24 14:31:03.787411',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (63,3,2,'hi','2025-11-24 14:34:51.826080',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (64,3,2,'hello','2025-11-24 14:35:56.615678',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (65,3,2,'hi','2025-11-24 14:36:14.489697',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (66,3,2,'hello','2025-11-24 14:37:15.341545',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (67,3,2,'2','2025-11-24 14:37:40.894040',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (68,3,2,'3','2025-11-24 14:37:49.908535',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (69,3,2,'4','2025-11-24 14:38:03.829526',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (70,3,2,'5','2025-11-24 14:38:31.214680',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (71,3,2,'6','2025-11-24 14:43:41.339416',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (72,3,2,'7','2025-11-24 14:44:13.102624',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (73,3,2,'8','2025-11-24 14:44:38.684763',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (74,3,2,'9','2025-11-24 14:44:58.640129',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (75,3,2,'10','2025-11-24 14:45:23.525065',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (76,3,2,'11','2025-11-24 14:47:28.965035',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (77,3,2,'12','2025-11-24 14:47:43.037599',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (78,3,2,'13','2025-11-24 14:47:48.892780',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (79,3,2,'14','2025-11-24 14:53:06.884493',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (80,3,2,'15','2025-11-24 14:53:17.201713',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (81,3,2,'16','2025-11-24 14:55:19.752974',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (82,3,2,'17','2025-11-24 14:55:27.294731',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (83,2,3,'18','2025-11-24 14:55:35.055086',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (84,2,3,'19','2025-11-24 14:56:05.801662',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (85,3,2,'20','2025-11-24 14:59:44.526977',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (86,3,2,'21','2025-11-24 15:00:21.166767',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (87,2,3,'22','2025-11-24 15:00:29.679459',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (88,3,2,'23','2025-11-24 15:00:37.880598',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (89,3,2,'24','2025-11-24 15:05:06.403227',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (90,2,3,'25','2025-11-24 15:05:16.678760',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (91,2,3,'26','2025-11-24 15:05:37.725997',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (92,3,2,'28','2025-11-24 15:13:22.752478',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (93,2,3,'29','2025-11-24 15:13:30.684653',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (94,3,2,'30','2025-11-24 15:13:49.548759',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (95,2,3,'31','2025-11-24 15:13:57.533039',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (96,3,2,'33','2025-11-24 15:14:09.626490',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (97,2,3,'34','2025-11-24 15:14:15.772495',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (98,3,2,'35','2025-11-24 15:16:04.640867',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (99,2,3,'36','2025-11-24 15:16:10.849096',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (100,3,2,'37','2025-11-24 15:21:18.963938',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (101,2,3,'38','2025-11-24 15:21:26.867999',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (102,3,2,'39','2025-11-24 15:21:46.221684',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (103,2,3,'40','2025-11-24 15:21:51.838993',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (104,2,3,'41','2025-11-24 15:22:40.545895',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (105,3,2,'42','2025-11-24 15:22:49.917592',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (106,3,2,'43','2025-11-24 15:29:10.446151',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (107,2,3,'44','2025-11-24 15:29:14.571368',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (108,2,3,'45','2025-11-24 15:29:59.849816',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (109,3,2,'46','2025-11-24 15:33:14.169345',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (110,3,2,'47','2025-11-24 15:33:19.769477',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (111,2,3,'48','2025-11-24 15:33:25.424409',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (112,3,2,'49','2025-11-24 15:34:14.885584',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (113,2,3,'50','2025-11-24 15:34:23.897251',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (114,3,2,'51','2025-11-24 15:39:20.145776',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (115,2,3,'52','2025-11-24 15:39:26.123140',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (116,2,3,'56','2025-11-24 15:41:06.602063',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (117,3,2,'57','2025-11-24 15:41:16.739598',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (118,3,2,'58','2025-11-24 15:42:20.948947',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (119,2,3,'59','2025-11-24 15:42:36.048612',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (120,2,3,'55','2025-11-24 15:44:20.911763',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (121,3,2,'58','2025-11-24 15:44:30.595227',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (122,3,2,'59','2025-11-24 15:44:38.877153',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (123,3,2,'60','2025-11-24 15:46:05.794631',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (124,2,3,'61','2025-11-24 15:46:13.759285',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (125,3,2,'62','2025-11-24 15:49:32.880696',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (126,2,3,'63','2025-11-24 15:49:42.984205',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (127,3,2,'64','2025-11-24 15:51:00.421484',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (128,2,3,'65','2025-11-24 15:51:07.341602',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (129,3,2,'66','2025-11-24 15:51:38.000820',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (130,2,3,'67','2025-11-24 15:51:46.021674',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (131,3,2,'68','2025-11-24 15:52:20.242424',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (132,2,3,'69','2025-11-24 15:52:27.895532',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (133,2,3,'70','2025-11-24 15:53:44.262043',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (134,3,2,'71','2025-11-24 15:54:04.003492',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (135,2,3,'72','2025-11-24 15:54:11.605742',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (136,3,2,'73','2025-11-24 15:58:16.918448',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (137,2,3,'74','2025-11-24 15:58:29.092931',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (138,3,2,'tt','2025-11-24 15:59:24.048864',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (139,2,3,'aa','2025-11-24 15:59:29.852328',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (140,2,3,'hello','2025-11-24 15:59:38.536377',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (141,3,2,'awesome','2025-11-24 15:59:50.247302',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (142,3,2,'what is going on','2025-11-24 15:59:58.591959',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (143,2,3,'I am just simply excited','2025-11-24 16:00:09.905712',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (144,3,2,'again','2025-11-24 16:00:59.538501',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (145,2,3,'Hello','2025-11-24 16:01:10.850775',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (146,3,2,'whats up','2025-11-24 16:01:18.472362',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (147,3,2,'doing good?','2025-11-24 16:01:39.506093',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (148,2,3,'how are you?','2025-11-24 16:03:45.911889',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (149,3,2,'hello','2025-11-24 16:03:59.964454',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (150,3,2,'hello','2025-11-24 16:04:15.095490',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (151,2,3,'how are you','2025-11-24 16:04:24.051531',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (152,2,3,'Hello','2025-11-24 16:04:35.314655',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (153,2,3,'testing','2025-11-24 16:04:54.266432',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (154,2,3,'whats up?','2025-11-24 16:05:16.360839',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (155,3,2,'nothing much','2025-11-24 16:05:30.963221',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (156,2,3,'how are things goinng on?','2025-11-24 16:05:38.505627',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (157,3,2,'testinh','2025-11-24 16:05:54.403098',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (158,2,3,'yep, for sure','2025-11-24 16:06:06.104907',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (159,2,3,'hello','2025-11-24 16:06:35.844320',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (160,3,2,'heelo','2025-11-24 16:06:42.531402',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (161,2,3,'how are you','2025-11-24 16:06:49.210044',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (162,2,3,'tt','2025-11-24 16:07:29.741506',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (163,2,3,'yy','2025-11-24 16:07:45.565958',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (164,2,3,'test again','2025-11-24 16:08:07.880048',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (165,2,3,'test','2025-11-24 16:08:34.783764',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (166,2,3,'test','2025-11-24 16:10:43.528821',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (167,2,3,'hello','2025-11-24 16:10:51.376414',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (168,3,2,'hello','2025-11-24 16:10:57.182617',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (169,2,3,'hello','2025-11-24 16:11:04.075666',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (170,3,2,'hi','2025-11-24 16:11:18.552372',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (171,2,3,'Hello','2025-11-24 16:11:23.703556',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (172,2,3,'hi','2025-11-24 16:13:41.857902',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (173,3,2,'hello','2025-11-24 16:13:51.577373',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (174,2,3,'hooo','2025-11-24 16:14:04.426434',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (175,3,2,'hi','2025-11-24 17:27:39.205325',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (176,2,3,'hello','2025-11-24 17:27:46.031351',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (177,3,2,'wasse where the heck r u?','2025-11-25 18:55:15.704196',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (178,2,3,'kugjghjgjfjgjgjfjfhf','2025-11-25 18:55:26.242176',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (179,2,3,'jhkjhkhjkjh','2025-11-27 16:35:38.413248',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (180,3,2,'ffffff','2025-11-27 16:35:42.937052',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (181,6,21,'test','2025-11-29 00:48:53.279985',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (182,21,2,'hello','2025-11-29 00:49:02.698767',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (183,6,21,'Hello','2025-11-29 00:49:30.661460',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (184,21,6,'how r u','2025-11-29 00:49:38.492963',0,NULL);
INSERT INTO `SmsMessages` (`Id`, `SenderId`, `ReceiverId`, `Message`, `SentAt`, `IsRead`, `ReadAt`) VALUES (185,6,27,'test','2025-11-30 23:44:34.363969',0,NULL);
/*!40000 ALTER TABLE `SmsMessages` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `UserAssignments`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `UserAssignments` (
  `AssignerId` int NOT NULL,
  `AssigneeId` int NOT NULL,
  `AssignedAt` datetime(6) NOT NULL,
  `IsActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`AssignerId`,`AssigneeId`),
  KEY `IX_UserAssignments_AssigneeId` (`AssigneeId`),
  CONSTRAINT `FK_UserAssignments_Users_AssigneeId` FOREIGN KEY (`AssigneeId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserAssignments_Users_AssignerId` FOREIGN KEY (`AssignerId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `UserAssignments`
--

LOCK TABLES `UserAssignments` WRITE;
/*!40000 ALTER TABLE `UserAssignments` DISABLE KEYS */;
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (1,2,'2025-10-22 16:19:23.000000',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (2,3,'2025-10-22 16:19:23.000000',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (2,5,'2025-11-26 15:07:42.098252',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (2,7,'2025-11-27 17:22:05.668470',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (2,19,'2025-11-29 00:19:44.125825',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (2,20,'2025-11-29 00:11:00.916729',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (2,21,'2025-11-29 00:37:21.731938',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (2,22,'2025-11-29 17:10:18.777844',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (4,3,'2025-11-10 18:10:10.441981',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (6,21,'2025-11-29 00:24:52.961228',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (6,24,'2025-11-29 22:17:44.067456',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (6,26,'2025-11-30 16:11:29.682338',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (6,27,'2025-11-30 16:22:18.608178',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (6,28,'2025-11-30 16:36:15.017125',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (6,29,'2025-11-30 16:41:15.403968',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (6,33,'2025-12-02 14:11:54.257228',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (23,3,'2025-11-29 22:54:55.661962',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (23,24,'2025-11-29 22:22:32.068673',1);
INSERT INTO `UserAssignments` (`AssignerId`, `AssigneeId`, `AssignedAt`, `IsActive`) VALUES (25,3,'2025-11-29 22:53:08.566861',1);
/*!40000 ALTER TABLE `UserAssignments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `UserRequests`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `UserRequests` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FirstName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LastName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Gender` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `MobilePhoneEncrypted` varchar(500) NOT NULL DEFAULT '',
  `Status` int NOT NULL,
  `RequestedAt` datetime(6) NOT NULL,
  `ReviewedByUserId` int DEFAULT NULL,
  `ReviewedAt` datetime(6) DEFAULT NULL,
  `Notes` varchar(2000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Age` int DEFAULT NULL,
  `Race` varchar(100) DEFAULT NULL,
  `AccidentAddress` varchar(500) DEFAULT NULL,
  `AccidentDate` datetime DEFAULT NULL,
  `VehicleDetails` varchar(1000) DEFAULT NULL,
  `DateReported` datetime DEFAULT NULL,
  `PoliceCaseNumber` varchar(100) DEFAULT NULL,
  `AccidentDetails` varchar(2000) DEFAULT NULL,
  `RoadConditions` varchar(200) DEFAULT NULL,
  `DoctorsInformation` varchar(1000) DEFAULT NULL,
  `LawyersInformation` varchar(1000) DEFAULT NULL,
  `AdditionalNotes` varchar(2000) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `UpdatedAt` datetime(6) NOT NULL,
  `Reason` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT '',
  `DateOfBirthEncrypted` varchar(500) NOT NULL DEFAULT '',
  PRIMARY KEY (`Id`),
  KEY `IX_UserRequests_Email` (`Email`),
  KEY `IX_UserRequests_ReviewedByUserId` (`ReviewedByUserId`),
  CONSTRAINT `FK_UserRequests_Users_ReviewedByUserId` FOREIGN KEY (`ReviewedByUserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=34 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `UserRequests`
--

LOCK TABLES `UserRequests` WRITE;
/*!40000 ALTER TABLE `UserRequests` DISABLE KEYS */;
INSERT INTO `UserRequests` (`Id`, `FirstName`, `LastName`, `Email`, `Gender`, `MobilePhoneEncrypted`, `Status`, `RequestedAt`, `ReviewedByUserId`, `ReviewedAt`, `Notes`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `CreatedAt`, `UpdatedAt`, `Reason`, `DateOfBirthEncrypted`) VALUES (32,'Hh','Gg','iqkhan70@yahoo.com','Male','CfFKJgFHghXMnDaiN4/4tg==',1,'2025-12-03 13:23:59.674916',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'2025-12-03 13:23:59.675131','2025-12-03 13:23:59.675152','Hccycycycycychcycy','CS+/r/dI086kRvIJmvs0XQ==');
INSERT INTO `UserRequests` (`Id`, `FirstName`, `LastName`, `Email`, `Gender`, `MobilePhoneEncrypted`, `Status`, `RequestedAt`, `ReviewedByUserId`, `ReviewedAt`, `Notes`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `CreatedAt`, `UpdatedAt`, `Reason`, `DateOfBirthEncrypted`) VALUES (33,'Tyry','Frfr','gh@gmail.com','Male','LWVk7vAX2c2KaPSNjWvkmA==',1,'2025-12-03 20:52:09.176687',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'2025-12-03 20:52:09.176687','2025-12-03 20:52:09.176687','Jgxkgxixkgxihxigxkgxkgx','CS+/r/dI086kRvIJmvs0XQ==');
/*!40000 ALTER TABLE `UserRequests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `Users`
--

/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE IF NOT EXISTS `Users` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FirstName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LastName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PasswordHash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Gender` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MobilePhoneEncrypted` varchar(500) DEFAULT NULL,
  `RoleId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `LastLoginAt` datetime(6) DEFAULT NULL,
  `IsActive` tinyint(1) NOT NULL,
  `IsFirstLogin` tinyint(1) NOT NULL,
  `MustChangePassword` tinyint(1) NOT NULL,
  `Specialization` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LicenseNumber` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Age` int DEFAULT NULL,
  `Race` varchar(100) DEFAULT NULL,
  `AccidentAddress` varchar(500) DEFAULT NULL,
  `AccidentDate` datetime DEFAULT NULL,
  `VehicleDetails` varchar(1000) DEFAULT NULL,
  `DateReported` datetime DEFAULT NULL,
  `PoliceCaseNumber` varchar(100) DEFAULT NULL,
  `AccidentDetails` varchar(2000) DEFAULT NULL,
  `RoadConditions` varchar(200) DEFAULT NULL,
  `DoctorsInformation` varchar(1000) DEFAULT NULL,
  `LawyersInformation` varchar(1000) DEFAULT NULL,
  `AdditionalNotes` varchar(2000) DEFAULT NULL,
  `DateOfBirthEncrypted` varchar(500) NOT NULL DEFAULT '',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Users_Email` (`Email`),
  KEY `IX_Users_RoleId` (`RoleId`),
  CONSTRAINT `FK_Users_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=35 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `Users`
--

LOCK TABLES `Users` WRITE;
/*!40000 ALTER TABLE `Users` DISABLE KEYS */;
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (1,'Admin','User','admin@mentalhealth.com','tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==','Other','Fth4CYucYxcZ3jNsjBZYPg==',3,'2025-10-22 16:19:23.000000','2025-12-03 20:32:57.383677',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (2,'Dr. Sarah','Johnson','dr.sarah@mentalhealth.com','tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==','Female','SurIMkdbqGfouZjh/h6+dg==',2,'2025-10-22 16:19:23.000000','2025-12-04 16:10:20.734049',1,0,0,'Psychiatry','MD123456',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (3,'John','Doe','john@doe.com','tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==','Male','xNDweMAojpQ9a3gs1HK0Bw==',1,'2025-10-22 16:19:23.000000','2025-12-04 16:25:47.522279',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'5SiHrNQY/2IH2ICIDG969A==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (4,'Idris','Khan','ikhan@testhealth.com','tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==','Male','8pMbzlwjIbkfEvGJ+3k/NA==',2,'2025-11-05 17:49:10.606947','2025-11-18 14:31:03.099560',1,0,0,'General Medicine','1234567',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (5,'iqbal','khan','iqkhan@health.com','tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==','Male','DvbUu3Z+4/CCP6GZD7MboA==',1,'2025-11-05 17:49:10.606947','2025-11-26 15:07:06.482401',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (6,'Junaid','Khan','jk@mentalhealth.com','tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==','Male','q84/SF4XLFvvETibQgVgIw==',4,'2025-11-27 13:53:49.714589','2025-12-04 16:11:26.185789',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (7,'Jenny','Marshal','jm@health.com','6guR3EKEQv3Psl5VgtqLh0bwNYkntGfzhbk3ONCJmsY=','Female','iELFqTXZctI/q8wTuoUX2g==',1,'2025-11-27 16:55:13.050944',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (8,'Mark','Cuban','mc@health.com','8TE4XHo0pjoUGnn9vL9ht8D5p4KW5G3mNT01NxaIUno=','Male','uuQmkxmf0E6bvnBa4sgIvQ==',1,'2025-11-27 18:22:12.129479',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'RszOh2/JsbCggKWjKn02TBXyqqjbLQoUgJezOznQjF8=');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (9,'Rob','Mac','rm@health.com','VAgjS+m48V34Mb8iLgX+vUjGnXcbLmXpNucbu8gQ/2A=','Male','omLhEP9YU+kr+oEnxN2sDw==',1,'2025-11-27 18:27:11.179719',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (10,'Mm','Kk','mk@health.com','KeZG11JEc3oKmFjd11Af/U6qCCLzbEQgbyZ8RHwIKQY=','Male','/VKN16D7RFbUulnWrPQYSQ==',1,'2025-11-27 18:29:45.432927',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (11,'Tt','Mm','tm1@health.com','ltCocHHxl5lmkCD5bd/a5KXjEM9omyrQaK6oICucm3E=','Male','SurIMkdbqGfouZjh/h6+dg==',1,'2025-11-27 18:34:01.317372',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (12,'Kk','Jj','iqk@yahoo.com','LVqvI9ydDXeqa7YPztFyvT1KZ1Ll7hLBHpU2WzR6HyQ=','Male','Cp04bqfNKphivag1VgvQdg==',1,'2025-11-27 18:43:22.071888',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (13,'Kk','Ff','iqkhan2@yahoo.com','UTjl2zQtl+BnnNgXw24Y60UDRp6xtH+v1u7uvfprydg=','Male','oeR4C3hKGpqQ3Q5FZfsv3A==',1,'2025-11-27 18:52:01.250039',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'n91cMpxU/7NeuUs5m9whZp8oDvsxEKH6wPVxHHxmfKQ=');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (14,'Ii','Kk','iqkhan3@yahoo.com','0BbzXRZOJl+uH6e2YgC2i5AW8thLIDDa+Zwe7Rxtk3I=','Male','/agueDH9KFLVL7olZzUsOw==',1,'2025-11-28 22:05:12.546539',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (15,'Iq','Khan Sahab','iqkhan4@yahoo.com','NkAYHO51Vf6zMAEgYjp3n5LEwhXYdiS5nhOyTvLgHfY=','Male','PDUvuPaMb+iNVMWoa3xYpA==',1,'2025-11-28 22:14:55.084134',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (16,'Kh','Iq','iqkhan5@yahoo.com','bFvHTNr8P2iKX5c8/LGbkAHPZndgNQDyFUOy4f4luFQ=','Male','q88vOD7RiBjkdSVNMFIKMw==',1,'2025-11-28 22:23:20.708314',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'EKcRz934u+QG1l+fx/eK9d3CCrCTl2hW0Vbcs0l2gIE=');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (17,'Kk','Aa','iqkhan6@yahoo.com','SYIjRZRvRubLRcdLzTJ5TLi+D1H+jnFBzQ5ll3I/yJ435Go87+iiKwTlDbtUVKn5FIO14S6zq77AXdYg9baUrA==','Male','Ml5SnxkQn4isy9ZfbIjQsw==',1,'2025-11-28 22:44:10.221882','2025-11-28 22:50:26.162080',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'F38MGIMP1Q/BFzQ4qbvIAbmyoFj8N2sBmp+4m9fnPVE=');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (18,'Gh','Hh','iqkhan@yahoo.com','hDJjZVs4yCYoJ8hek5BWZKyQ3EHAbtF7341tMm7+WxTq0buY40JUHUvf9D9Zl6wI','Male','ZAzVbFwNk2cS+aLogbL5IA==',1,'2025-11-28 22:56:25.848368','2025-11-28 23:01:53.410583',1,0,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'CIPvPs5egpe28RjZWuWW8+jTWG/miTOXoNynnL3ZMW8=');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (19,'Hh','Mm','hg@gmail.com','G8207NfCUZyciMVrgzHtbaYNUadMH+G3WGZrhHFJ8ixwVBWtwuUaQNkG2FnlnlokDrm1981SjAH6DoPhfSLdAw==','Male','WrXNSNkQnbVfE5uvcZI+HA==',1,'2025-11-29 00:03:32.310289',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (20,'Gg','Jj','ii@gmail.com','mD6WdvriXcde+R9xXxuYFm7ogS/g/ZdrezihRVdDsvFtsGEfy9306lDgNUf4eFgENaKjf2sXAcaksn0IKy3/HQ==','Male','IYlJW9WEnIkVTMk7XS/A8w==',1,'2025-11-29 00:04:42.866104',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'ikJT5/yXCxC9mK96l1f9pD9uSeWl3fXaZqbPMqVETfE=');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (21,'Hh','Kk','iqmkhan71@gmail.com','+LHY8IEjn9TFeP6KDPohHte8I9ho17GcYucUtvp0YYHVdQapQgKAegzwbkJyR3BGXvKiyCSWzGHIeTFHbH5POA==','Female','VgLll+Q5jd5aeLc0+h6j7g==',1,'2025-11-29 00:24:52.896147','2025-11-29 00:42:11.796578',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (22,'Jk','Mm','jm@gmail.com','awNjdo5upfPTsPb3R3a6YEHF9Rm+0mwQA0fitGefvX+H6dbK3U3eDw+O70aLqJeIxrC/QVE00yzmmuKD860seA==','Male','S6WZDKYxVsjWSPWlVwtIkg==',1,'2025-11-29 17:09:58.056068',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'CIPvPs5egpe28RjZWuWW8+jTWG/miTOXoNynnL3ZMW8=');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (23,'test','test2','attr1@health.com','tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==','Male','UxXe0ef/9w5yImGvpRLoVw==',5,'2025-11-29 21:57:02.596793','2025-11-29 23:06:16.537149',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'x6TLCYLBeq6AE3nAbhnz+A==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (24,'Ffff','Jhhhh','vv@gmail.com','r6CMsEBhPQoWG2YhRxEckfOZ6oFP6h9hP8yPnQliWN78JbmNbUvIkxnuIweNcAr/G4I08dSPPESBAqSnwYXKOg==','Male','ExPGTLyIjLgALi2NHZTorg==',1,'2025-11-29 22:17:44.001149',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (25,'hello','khan','hk@health.com','tEDPRZp22Y6DcavESpSxgp0pTiTrDVESEXtJlJFAD3pWvrri5dY+1KXzPRuQTNuWiDBwZk8y5kIHRgTHDgmIYw==','Female','UxXe0ef/9w5yImGvpRLoVw==',5,'2025-11-29 22:51:37.165084','2025-12-02 01:19:57.424203',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'x6TLCYLBeq6AE3nAbhnz+A==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (26,'Jgjcjj','Jcjcjc','ufchf@gm.com','Ig5XvcCzE63EGwT7y117JCjCeLlKYnZP8mtByrHoWKfN7UOqkrWdZ5GOca6ULpLe/Jh5yEmPiMXzEvYLKsUozA==','Male','1Ur1XBX3iywwLdfHlj28pg==',1,'2025-11-30 16:11:29.655809',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'KLTz5W6o+sB4xzGZwqRTqg==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (27,'Khan','Iq','iqmkhan70@gmail.com','WkAhVjdEgx3lIi57AvhWgoFAY0XMyU5usYCO63YTSEzBoskS60kFJ5pLw2KPBqrW4aCrqkZYwGSMieC6mTwhlQ==','Male','aGtlI4INEDEBjeeIzX2NIg==',1,'2025-11-30 16:22:18.558411','2025-11-30 16:31:15.241609',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'KLTz5W6o+sB4xzGZwqRTqg==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (28,'Fff','Thigh','jj22@gmail.com','9hr5/plbEasXb0somA7UlO3Q3UQeKuUhMFZGyq/KkFNwO6dDoenSRVsDmtw4pAQwh44eogph7CNOWIbb9Q7vTw==','Male','819RzM0PFbeYeafZcTwlog==',1,'2025-11-30 16:36:14.987376','2025-11-30 16:37:22.497323',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'9xRE9chYCCZG1vV21qn2GQ==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (29,'Jfjggkg','Klingon','j2@gmail.com','jvmplM5dnHXX9gA9hgXPwJlWHrNXuy2tCW+Zoj3v6qfWai1VpS3NT7hRxmHxsXwGeYhotbHyx8DmFew/n9VIgQ==','Male','CpIXIoF5P7+a4jGpbbfoKg==',1,'2025-11-30 16:41:15.391039','2025-11-30 17:37:21.009140',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'kp+aWJ/YixdsnjgboCUf8A==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (30,'Wasse','Zifar','wasse@health.com','StsZsHJVAeV57Qvaj9oQ4Vb9RZgJxQJr4H14xZnQN2DxhyDQyKXPyMZWDqDNm+qPaeTgh0U5Jm7JtQuc0eUFNw==','Male','bx+angmP5G/hra4Ef73R1w==',2,'2025-11-30 19:21:27.788706','2025-11-30 20:24:24.343900',1,0,0,'Neurology','1234',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'vbPxJAMePDC/lnFiwK9GnA==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (31,'pfn1','pln1','p1@health.com','kh9DBOMwUJ6bk0XDO47pP+XBjq/aq/Jph/sut+YY3zQF7tmorvQAfuexIArY5Nviqiq+bPlhdXkjCPH4IWM8pA==','','sL1XtuV5BVYJjLnFgQ+e+A==',1,'2025-11-30 20:25:33.111282','2025-11-30 20:27:50.174233',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'Ob7qPulo94eFBS1h+zAZug==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (32,'Julia','Roberts','jr@health.com','kitY60pk+IPYXmKOBUyzmBI3fab1UhvhW7RwDs/a/lpfW2UHzwr9APZJT0Z4Y/6BRE9hVKUyGNuWLvlWRK3PeQ==','','UxXe0ef/9w5yImGvpRLoVw==',4,'2025-11-30 23:42:35.452189','2025-12-03 23:02:51.434479',1,0,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'vbPxJAMePDC/lnFiwK9GnA==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (33,'Fffffff','Ggcfff','h1@gnail.com','vYPl963YVXe6Nd9/OhkbMbzanZEt2CH46VUmtFP11z7pg1Vhl+IKmQx06EO98fsq/iP1VjHuirPV0gmO2JLDFQ==','Male','gWlFAmZcCbkAiV01UvZLog==',1,'2025-12-02 14:11:54.197940',NULL,1,1,1,NULL,NULL,29,'','OP Kansas','2025-12-01 00:00:00','Hundai','2025-12-02 00:00:00','A123456','Testing Accident','ok','Test Doctor','Not Yet','None as of now','eufGB0KqScztiX3Axir5SQ==');
INSERT INTO `Users` (`Id`, `FirstName`, `LastName`, `Email`, `PasswordHash`, `Gender`, `MobilePhoneEncrypted`, `RoleId`, `CreatedAt`, `LastLoginAt`, `IsActive`, `IsFirstLogin`, `MustChangePassword`, `Specialization`, `LicenseNumber`, `Age`, `Race`, `AccidentAddress`, `AccidentDate`, `VehicleDetails`, `DateReported`, `PoliceCaseNumber`, `AccidentDetails`, `RoadConditions`, `DoctorsInformation`, `LawyersInformation`, `AdditionalNotes`, `DateOfBirthEncrypted`) VALUES (34,'Raj','Sharma','rajs@health.com','GfTmbJbydSmCdMiY2nMitgt0Laxmo2CFCsjk70DOh/QUbDkBbQGgwnFjoNhYRtVMgpYALw7FrPee/4iZ18LSyA==','','UxXe0ef/9w5yImGvpRLoVw==',4,'2025-12-03 20:35:00.144942',NULL,1,1,1,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'guwWt3pLeAIGjEQKONrekQ==');
/*!40000 ALTER TABLE `Users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-12-05  9:38:03

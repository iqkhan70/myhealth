# Document Upload Feature

This document describes the comprehensive document upload system implemented for the Mental Health App, allowing both doctors and patients to upload, manage, and share medical documents.

## Features

### 🏥 For Doctors

- Upload documents for any assigned patient
- View all documents for assigned patients
- Categorize documents (Test Results, Prescriptions, X-Rays, etc.)
- Download and manage patient documents
- Access to AI analysis of uploaded documents

### 👤 For Patients

- Upload their own medical documents
- View their personal document library
- Share documents with their assigned doctors
- Download their documents

## Supported File Types

### Images

- JPEG, PNG, GIF, WebP
- Maximum size: 10MB

### Videos

- MP4, AVI, MOV, QuickTime
- Maximum size: 100MB

### Audio

- MP3, WAV
- Maximum size: 50MB

### Documents

- PDF, DOC, DOCX, TXT
- Maximum size: 25MB

## Document Categories

- Test Results
- Prescription
- X-Ray
- Lab Report
- Medical Record
- Insurance
- Referral
- Discharge Summary
- Consultation
- Other

## Technical Implementation

### Backend (C#/.NET)

- **Models**: `DocumentUploadModels.cs` - Request/response models
- **Service**: `DocumentUploadService.cs` - Business logic and S3 integration
- **Controller**: `DocumentUploadController.cs` - REST API endpoints
- **Database**: Uses existing `ContentItem` entity with S3 storage

### Blazor Client

- **Service**: `DocumentUploadService.cs` - HTTP client wrapper
- **Component**: `DocumentUpload.razor` - Upload and management UI
- **Page**: `DocumentManagement.razor` - Main document management page

### Mobile App (React Native)

- **Service**: `DocumentUploadService.js` - API integration
- **Component**: `DocumentUpload.js` - Mobile UI with camera/photo library support
- **Integration**: Added to main App.js with navigation

## API Endpoints

### Document Upload

- `POST /api/documentupload/initiate` - Start upload process
- `POST /api/documentupload/complete/{contentId}` - Complete upload
- `GET /api/documentupload/list` - Get documents with filtering
- `GET /api/documentupload/{contentId}` - Get specific document
- `GET /api/documentupload/{contentId}/download` - Get download URL
- `GET /api/documentupload/{contentId}/thumbnail` - Get thumbnail URL
- `DELETE /api/documentupload/{contentId}` - Delete document
- `GET /api/documentupload/categories` - Get available categories
- `GET /api/documentupload/validation-rules` - Get file validation rules

## Security Features

- **Authentication**: JWT token required for all operations
- **Authorization**: Users can only access documents for their assigned patients
- **File Validation**: Strict file type and size validation
- **S3 Security**: Pre-signed URLs for secure uploads/downloads
- **Access Control**: Role-based access (doctors vs patients)

## Usage

### Web Client

1. Navigate to `/documents` page
2. Select a patient (if you're a doctor)
3. Click "Upload Document" button
4. Fill in document details and select file
5. Upload and manage documents

### Mobile App

1. Login to the app
2. Tap "📄 Documents" button on main screen
3. Select patient (if you're a doctor)
4. Tap "+ Upload" button
5. Choose file from camera, photo library, or documents
6. Fill in details and upload

## File Storage

- **Storage**: Amazon S3 (DigitalOcean Spaces)
- **Organization**: `documents/{patientId}/{contentGuid}/{filename}`
- **Security**: Pre-signed URLs for secure access
- **Backup**: Files are stored redundantly in S3

## AI Integration

The system is designed to integrate with AI analysis services:

- Document content extraction
- Medical data analysis
- Automated categorization
- Alert generation for critical findings
- Summary generation

## Future Enhancements

- [ ] Real-time document synchronization
- [ ] Advanced search and filtering
- [ ] Document versioning
- [ ] Collaborative annotations
- [ ] Automated document processing
- [ ] Integration with EHR systems
- [ ] Mobile offline support
- [ ] Document templates

## Dependencies

### Backend

- Amazon S3 SDK
- Entity Framework Core
- JWT Authentication
- SignalR (for real-time updates)

### Frontend (Blazor)

- HttpClient
- Bootstrap (for styling)
- Font Awesome (for icons)

### Mobile (React Native)

- expo-document-picker
- expo-image-picker
- expo-file-system
- @react-native-async-storage/async-storage

## Configuration

### Environment Variables

```bash
DIGITALOCEAN_ACCESS_KEY=your_access_key
DIGITALOCEAN_SECRET_KEY=your_secret_key
```

### S3 Configuration

```json
{
  "S3": {
    "AccessKey": "your_access_key",
    "SecretKey": "your_secret_key",
    "ServiceUrl": "https://nyc3.digitaloceanspaces.com",
    "BucketName": "your-bucket-name",
    "Region": "nyc3"
  }
}
```

## Testing

The system includes comprehensive error handling and validation:

- File type validation
- File size limits
- User permission checks
- Network error handling
- S3 upload verification

## Support

For issues or questions regarding the document upload feature, please check:

1. File size and type requirements
2. Network connectivity
3. User permissions
4. S3 configuration
5. Authentication status

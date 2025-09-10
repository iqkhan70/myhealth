# 🧠 Mental Health Journal App

A comprehensive mental health journaling application built with Blazor WebAssembly and .NET Core, featuring AI-powered mood analysis and personalized chat support.

## 🚀 Features

- **Patient Management**: Create and manage patient records
- **Journal Entries**: Track daily mood and thoughts with AI analysis
- **Personalized Chat**: AI-powered mental health companion with patient-specific context
- **Mood Trends**: Visualize mood patterns over time
- **Patient-Centric**: Each patient has isolated data and personalized AI responses

## 🛠️ Technology Stack

- **Frontend**: Blazor WebAssembly
- **Backend**: .NET Core Web API
- **Database**: MySQL
- **AI Integration**: Hugging Face API
- **UI Components**: Custom CSS with Radzen services

## 📋 Prerequisites

- .NET 6.0 or later
- MySQL Server
- Visual Studio 2022 or VS Code
- Hugging Face API key

## ⚙️ Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd health
```

### 2. Database Setup

1. Install MySQL Server
2. Create a database named `mentalhealthdb`
3. Update the connection string in `appsettings.json`

### 3. Configuration

1. Copy `appsettings.example.json` to `appsettings.json`
2. Update the configuration with your actual values:

```json
{
  "ConnectionStrings": {
    "MySQL": "server=localhost;port=3306;database=mentalhealthdb;user=YOUR_USERNAME;password=YOUR_PASSWORD"
  },
  "HuggingFace": {
    "ApiKey": "YOUR_HUGGINGFACE_API_KEY_HERE"
  }
}
```

### 4. Database Migration

```bash
cd SM_MentalHealthApp.Server
dotnet ef database update
```

### 5. Run the Application

```bash
# Start the server
cd SM_MentalHealthApp.Server
dotnet run

# In another terminal, start the client (if needed)
cd SM_MentalHealthApp.Client
dotnet run
```

The application will be available at:

- **Server**: http://localhost:5262
- **Client**: http://localhost:5000 (if running separately)

## 🔐 Security Notes

- **Never commit `appsettings.json`** - it contains sensitive database credentials and API keys
- Use `appsettings.example.json` as a template for configuration
- Keep your Hugging Face API key secure
- Use environment variables for production deployments

## 📁 Project Structure

```
health/
├── SM_MentalHealthApp.Client/          # Blazor WebAssembly frontend
├── SM_MentalHealthApp.Server/          # .NET Core Web API backend
├── SM_MentalHealthApp.Shared/          # Shared models and DTOs
├── appsettings.example.json            # Configuration template
└── README.md                           # This file
```

## 🎯 Key Features

### Patient Management

- Create, read, update, and delete patient records
- Patient statistics and dashboard
- Search and filter functionality

### Journal System

- AI-powered mood analysis using Hugging Face
- Journal entry management
- Mood trend visualization

### Chat System

- Patient-aware AI chat
- Personalized responses based on journal data
- Real-time patient switching

## 🔧 API Endpoints

### Patients

- `GET /api/patient` - Get all patients
- `POST /api/patient` - Create new patient
- `GET /api/patient/{id}` - Get patient by ID
- `PUT /api/patient/{id}` - Update patient
- `DELETE /api/patient/{id}` - Delete patient
- `GET /api/patient/{id}/stats` - Get patient statistics

### Journal

- `GET /api/journal/patient/{patientId}` - Get journal entries for patient
- `POST /api/journal/patient/{patientId}` - Create journal entry
- `GET /api/journal/patient/{patientId}/mood-distribution` - Get mood distribution

### Chat

- `POST /api/chat/send` - Send chat message
- `POST /api/chat/patient/{patientId}/send` - Send patient-specific chat message

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For support and questions, please open an issue in the repository.

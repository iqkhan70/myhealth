namespace SM_MentalHealthApp.Shared
{
    public class State
    {
        public string Code { get; set; } = string.Empty; // CHAR(2) PRIMARY KEY
        public string Name { get; set; } = string.Empty; // VARCHAR(50)
    }

    public class AccidentParticipantRole
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // VARCHAR(30) UNIQUE
        public string Label { get; set; } = string.Empty; // VARCHAR(50)
    }

    public class VehicleDisposition
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // VARCHAR(30) UNIQUE
        public string Label { get; set; } = string.Empty; // VARCHAR(50)
    }

    public class TransportToCareMethod
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // VARCHAR(30) UNIQUE
        public string Label { get; set; } = string.Empty; // VARCHAR(80)
    }

    public class MedicalAttentionType
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // VARCHAR(30) UNIQUE
        public string Label { get; set; } = string.Empty; // VARCHAR(80)
    }

    public class SymptomOngoingStatus
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // VARCHAR(30) UNIQUE
        public string Label { get; set; } = string.Empty; // VARCHAR(80)
    }
}


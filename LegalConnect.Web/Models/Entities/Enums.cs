namespace LegalConnect.Web.Models.Entities
{
    public enum VerificationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public enum AppointmentStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        Cancelled = 3,
        Completed = 4
    }

    public enum ConsultationMode
    {
        Online = 0,
        InPerson = 1
    }
}

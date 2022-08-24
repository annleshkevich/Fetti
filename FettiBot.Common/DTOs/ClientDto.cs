using FettiBot.Model.DatabaseModels;

namespace FettiBot.Common.DTOs
{
    public class ClientDto
    {
        public long Num { get; set; }
        public string? Language { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; } = null;
        public string? CurrentLocation { get; set; } = null;
        public string? NextDestination { get; set; } = null;
        public string? ReasonForMoving { get; set; } = null;
        public string? TravelingWithChildren { get; set; } = null;
        public string? Interests { get; set; } = null;
        public string? Applications { get; set; } = null;
        public string? Access { get; set; } = null;
        public bool? CurrentL { get; set; } = false;
        public bool? NextD { get; set; } = false;
        public bool? Moving { get; set; } = false;
        public bool? Start { get; set; } = true;
        public string? LastMessage { get; set; } = null;
        public int? IntCount { get; set; } = 0;
        public int? AppCount { get; set; } = 0;
        public bool? IsValidEmail { get; set; } = false;
        public bool? Save { get; set; } = false;
    }
}

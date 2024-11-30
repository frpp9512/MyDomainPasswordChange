namespace MyDomainPasswordChange.Managers.Models;

public record ChallengeModel
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string Answer { get; set; }
}

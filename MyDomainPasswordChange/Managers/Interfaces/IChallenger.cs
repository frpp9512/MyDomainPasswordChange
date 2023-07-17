using System.Drawing;

namespace MyDomainPasswordChange.Managers.Interfaces;

public interface IChallenger
{
    int GetChallenge();
    Image GetChallengeImage(int challengeId);
    bool EvaluateChallengeAnswer(int challengeId, string answer);
}

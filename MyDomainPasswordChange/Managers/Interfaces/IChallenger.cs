using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace MyDomainPasswordChange
{
    public interface IChallenger
    {
        int GetChallenge();
        Image GetChallengeImage(int challengeId);
        bool EvaluateChallengeAnswer(int challengeId, string answer);
    }
}

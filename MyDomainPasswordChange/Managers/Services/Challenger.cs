using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MyDomainPasswordChange.Managers.Interfaces;
using MyDomainPasswordChange.Managers.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MyDomainPasswordChange.Managers.Services;

public class Challenger : IChallenger
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private List<ChallengeModel> _challenges;

    public Challenger(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
        LoadChallenges();
    }

    private void LoadChallenges() => _challenges = _configuration.GetSection("Challenges").Get<List<ChallengeModel>>();

    public bool EvaluateChallengeAnswer(int challengeId, string answer) => _challenges == null || _challenges.Count == 0
            ? throw new Exception("No existen challenges registrados.")
            : !_challenges.Any(c => c.Id == challengeId)
                ? throw new Exception("El challenge especificado no existe.")
                : !string.IsNullOrEmpty(answer)
&& _challenges.First(c => c.Id == challengeId).Answer.ToLower() == answer.ToLower();

    public int GetChallenge()
    {
        if (_challenges == null || _challenges.Count == 0)
        {
            throw new Exception("No existen challenges registrados.");
        }

        var challengePos = new Random().Next(1, _challenges.Count + 1);
        return _challenges[challengePos - 1].Id;
    }

    public Image GetChallengeImage(int challengeId)
    {
        if (_challenges == null || _challenges.Count == 0 || !_challenges.Any(c => c.Id == challengeId))
        {
            throw new Exception("No existen challenges registrados.");
        }

        var filename = _challenges.First(c => c.Id == challengeId).FileName;
        var path = Path.Combine(_webHostEnvironment.WebRootPath, $"img{Path.DirectorySeparatorChar}challenges{Path.DirectorySeparatorChar}{filename}");
        Console.WriteLine(path);
        return Image.FromFile(path);
    }
}
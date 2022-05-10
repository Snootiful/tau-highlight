﻿using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Tau.Difficulty.Skills;
using osu.Game.Rulesets.Tau.Mods;
using osu.Game.Scoring;

namespace osu.Game.Rulesets.Tau.Difficulty;

public class TauPerformanceCalculator : PerformanceCalculator
{
    private TauPerformanceContext context;

    public TauPerformanceCalculator()
        : base(new TauRuleset())
    {
    }

    protected override PerformanceAttributes CreatePerformanceAttributes(ScoreInfo score, DifficultyAttributes attributes)
    {
        var tauAttributes = (TauDifficultyAttributes)attributes;
        context = new TauPerformanceContext(score, tauAttributes);

        // Mod multipliers here, let's just set to default osu! value.
        const double mod_multiplier = 1.12;

        double aimValue = Aim.ComputePerformance(context);
        double accuracyValue = computeAccuracy(context);

        double totalValue = Math.Pow(
            Math.Pow(aimValue, 1.1) +
            Math.Pow(accuracyValue, 1.1),
            1.0 / 1.1
        ) * mod_multiplier;

        return new TauPerformanceAttribute
        {
            Aim = aimValue,
            Accuracy = accuracyValue,
            Total = totalValue
        };
    }

    private double computeAccuracy(TauPerformanceContext context)
    {
        if (context.Score.Mods.Any(mod => mod is TauModRelax))
            return 0.0;

        // This percentage only considers HitCircles of any value - in this part of the calculation we focus on hitting the timing hit window.
        double betterAccuracyPercentage;
        int amountHitObjectsWithAccuracy = context.DifficultyAttributes.HitCircleCount;

        if (amountHitObjectsWithAccuracy > 0)
            betterAccuracyPercentage = ((context.CountGreat - (totalHits() - amountHitObjectsWithAccuracy)) * 6 + context.CountOk * 2) / (double)(amountHitObjectsWithAccuracy * 6);
        else
            betterAccuracyPercentage = 0;

        // It is possible to reach a negative accuracy with this formula. Cap it at zero - zero points.
        if (betterAccuracyPercentage < 0)
            betterAccuracyPercentage = 0;

        // Lots of arbitrary values from testing.
        // Considering to use derivation from perfect accuracy in a probabilistic manner - assume normal distribution.
        double accuracyValue = Math.Pow(1.52163, context.DifficultyAttributes.OverallDifficulty) * Math.Pow(betterAccuracyPercentage, 24) * 2.83;

        // Bonus for many hitcircles - it's harder to keep good accuracy up for longer.
        accuracyValue *= Math.Min(1.15, Math.Pow(amountHitObjectsWithAccuracy / 1000.0, 0.3));
        return accuracyValue;

        int totalHits() => context.CountGreat + context.CountOk + context.CountMiss;
    }
}

public struct TauPerformanceContext
{
    public double Accuracy => Score.Accuracy;
    public int ScoreMaxCombo => Score.MaxCombo;
    public int CountGreat => Score.Statistics.GetValueOrDefault(HitResult.Great);
    public int CountOk => Score.Statistics.GetValueOrDefault(HitResult.Ok);
    public int CountMiss => Score.Statistics.GetValueOrDefault(HitResult.Miss);

    public ScoreInfo Score { get; set; }
    public TauDifficultyAttributes DifficultyAttributes { get; set; }

    public TauPerformanceContext(ScoreInfo score, TauDifficultyAttributes attributes)
    {
        Score = score;
        DifficultyAttributes = attributes;
    }
}

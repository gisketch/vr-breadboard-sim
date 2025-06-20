using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class ExperimentEvaluator
{
    private ExperimentDefinitions _experimentDefinitions;
    private Evaluate74138 _evaluate74138;
    private Evaluate74148 _evaluate74148;
    private EvaluateBCD _evaluateBCD;

    public ExperimentEvaluator(ExperimentDefinitions experimentDefinitions)
    {
        _experimentDefinitions = experimentDefinitions;
        _evaluate74138 = new Evaluate74138();
        _evaluate74148 = new Evaluate74148();
        _evaluateBCD = new EvaluateBCD();
    }

    public ExperimentResult EvaluateExperiment(BreadboardSimulator.SimulationResult simResult, JToken components)
    {
        var experiment = _experimentDefinitions.GetCurrentExperiment();
        if (experiment == null)
        {
            return new ExperimentResult
            {
                ExperimentId = _experimentDefinitions.CurrentExperimentId,
                Messages = new List<string> { $"Unknown experiment ID: {_experimentDefinitions.CurrentExperimentId}" },
                MainInstruction = "Error: Invalid experiment selected",
                IsSetupValid = false
            };
        }

        switch (_experimentDefinitions.CurrentExperimentId)
        {
            case 1:
                return _evaluate74138.Evaluate74138To8LED(simResult, experiment, components, _experimentDefinitions);
            case 2:
                return _evaluateBCD.EvaluateBCDTo7SegmentExperiment(simResult, experiment, components, _experimentDefinitions);
            case 3:
                return _evaluate74148.Evaluate74148To3LED(simResult, experiment, components, _experimentDefinitions);
            default:
                return new ExperimentResult
                {
                    ExperimentId = _experimentDefinitions.CurrentExperimentId,
                    Messages = new List<string> { $"No evaluation implemented for experiment {_experimentDefinitions.CurrentExperimentId}" },
                    MainInstruction = "Error: Experiment not implemented",
                    IsSetupValid = false
                };
        }
    }
}
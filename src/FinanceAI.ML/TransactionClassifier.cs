using FinanceAI.Application.Abstractions;
using FinanceAI.Application.Dtos;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace FinanceAI.ML;

public class TransactionInput
{
    [LoadColumn(0)] public string Description { get; set; } = string.Empty;
    [LoadColumn(1)] public string Category { get; set; } = string.Empty;
}

public class TransactionPrediction
{
    [ColumnName("PredictedLabel")] public string Category { get; set; } = string.Empty;

    /// <summary>Probability per category; SdcaMaximumEntropy outputs values that sum to 1.</summary>
    public float[] Score { get; set; } = [];
}

public record ClassifierOptions(string TrainingDataPath, string ModelPath);

/// <summary>
/// Classifies a bank-statement description into a category. Loads the saved model when present,
/// otherwise trains from the CSV and saves it, so the first startup bootstraps itself.
/// </summary>
public sealed class TransactionClassifier : ITransactionClassifier
{
    private readonly PredictionEngine<TransactionInput, TransactionPrediction> _engine;

    // PredictionEngine is not thread-safe; inference is fast enough that a lock beats pooling here.
    private readonly Lock _sync = new();

    public TransactionClassifier(ClassifierOptions options)
    {
        var ml = new MLContext(seed: 42);
        ITransformer model;

        if (File.Exists(options.ModelPath))
        {
            model = ml.Model.Load(options.ModelPath, out _);
        }
        else
        {
            var data = ModelTrainer.LoadData(ml, options.TrainingDataPath);
            model = ModelTrainer.BuildPipeline(ml).Fit(data);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.ModelPath))!);
            ml.Model.Save(model, data.Schema, options.ModelPath);
        }

        _engine = ml.Model.CreatePredictionEngine<TransactionInput, TransactionPrediction>(model);
    }

    public ClassificationResult Classify(string description)
    {
        TransactionPrediction prediction;
        lock (_sync)
        {
            prediction = _engine.Predict(new TransactionInput { Description = description });
        }
        return new ClassificationResult(prediction.Category, prediction.Score.Max());
    }
}

public static class ModelTrainer
{
    public static IDataView LoadData(MLContext ml, string path) =>
        ml.Data.LoadFromTextFile<TransactionInput>(path, hasHeader: true, separatorChar: ',');

    public static IEstimator<ITransformer> BuildPipeline(MLContext ml) =>
        ml.Transforms.Conversion.MapValueToKey("Label", nameof(TransactionInput.Category))
            // Word and character n-grams: char n-grams help with merchant codes like "IFD*" or "UBER *TRIP".
            .Append(ml.Transforms.Text.FeaturizeText("Features", nameof(TransactionInput.Description)))
            .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

    /// <summary>Average micro-accuracy over k-fold cross-validation, measured on unseen descriptions.</summary>
    public static double CrossValidateAccuracy(string path, int folds = 5)
    {
        var ml = new MLContext(seed: 42);
        var results = ml.MulticlassClassification.CrossValidate(
            LoadData(ml, path), BuildPipeline(ml), numberOfFolds: folds);
        return results.Average(r => r.Metrics.MicroAccuracy);
    }
}

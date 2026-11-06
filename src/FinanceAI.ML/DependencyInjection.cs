using FinanceAI.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceAI.ML;

public static class DependencyInjection
{
    public static IServiceCollection AddMachineLearning(this IServiceCollection services)
    {
        var options = new ClassifierOptions(
            TrainingDataPath: Path.Combine(AppContext.BaseDirectory, "Data", "training-data.csv"),
            ModelPath: Path.Combine(AppContext.BaseDirectory, "models", "transaction-classifier.zip"));

        services.AddSingleton(options);
        services.AddSingleton<ITransactionClassifier, TransactionClassifier>();
        return services;
    }
}

using FinanceAI.ML;
using Xunit.Abstractions;

namespace FinanceAI.UnitTests;

public class TransactionClassifierTests(ITestOutputHelper output) : IDisposable
{
    private static readonly string DataPath = Path.Combine(AppContext.BaseDirectory, "Data", "training-data.csv");
    private readonly string _modelPath = Path.Combine(Path.GetTempPath(), $"financeai-{Guid.NewGuid()}.zip");

    [Theory]
    [InlineData("UBER *TRIP HELP.UBER.COM", "Transporte")]
    [InlineData("POSTO SHELL AV BRASIL", "Transporte")]
    [InlineData("NETFLIX.COM ASSINATURA", "Assinaturas")]
    [InlineData("SUPERMERCADO ZONA SUL", "Alimentação")]
    [InlineData("RENDIMENTO POUPANCA OUTUBRO", "Rendimentos")]
    [InlineData("CREDITO SALARIO OUTUBRO", "Salário")]
    [InlineData("DROGARIA SAO PAULO", "Saúde")]
    public void Classify_PredictsExpectedCategory(string description, string expected)
    {
        var classifier = new TransactionClassifier(new ClassifierOptions(DataPath, _modelPath));

        var result = classifier.Classify(description);

        output.WriteLine($"{description} -> {result.Category} ({result.Confidence:P0})");
        Assert.Equal(expected, result.Category);
        Assert.InRange(result.Confidence, 0f, 1f);
    }

    [Fact]
    public void FirstRun_SavesModel_AndSecondRunLoadsIt()
    {
        var first = new TransactionClassifier(new ClassifierOptions(DataPath, _modelPath));
        Assert.True(File.Exists(_modelPath));

        var second = new TransactionClassifier(new ClassifierOptions("arquivo-inexistente.csv", _modelPath));

        Assert.Equal(first.Classify("IFOOD PEDIDO 123"), second.Classify("IFOOD PEDIDO 123"));
    }

    /// <summary>
    /// Regression guard. Cross-validation measures descriptions the model never saw (mostly new
    /// merchant names), so with ~12 examples per category it stays around 40%. Random guessing
    /// over 10 categories is 10%; the test fails if the model drops below 3x that.
    /// </summary>
    [Fact]
    public void CrossValidation_AccuracyOnUnseenDescriptions_BeatsRandomGuessing()
    {
        var accuracy = ModelTrainer.CrossValidateAccuracy(DataPath);

        output.WriteLine($"Acurácia média (validação cruzada 5 folds): {accuracy:P1}");
        Assert.True(accuracy >= 0.30, $"Acurácia {accuracy:P1} abaixo do mínimo esperado.");
    }

    public void Dispose()
    {
        if (File.Exists(_modelPath)) File.Delete(_modelPath);
    }
}

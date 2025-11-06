using Booksy.ServiceCatalog.IntegrationTests.Infrastructure;
using Booksy.ServiceCatalog.IntegrationTests.Support;
using Reqnroll;

namespace Booksy.ServiceCatalog.IntegrationTests.Hooks;

/// <summary>
/// Hooks for test initialization, cleanup, and context setup
/// </summary>
[Binding]
public class TestHooks
{
    private readonly ServiceCatalogIntegrationTestBase _testBase;
    private readonly ScenarioContext _scenarioContext;
    private readonly ReqnrollContext _reqnrollContext;

    public TestHooks(
        ServiceCatalogIntegrationTestBase testBase,
        ScenarioContext scenarioContext,
        ReqnrollContext reqnrollContext)
    {
        _testBase = testBase;
        _scenarioContext = scenarioContext;
        _reqnrollContext = reqnrollContext;
    }

    [BeforeScenario]
    public async Task BeforeScenario()
    {
        // Initialize test base (database, client, etc.)
        await _testBase.InitializeAsync();

        // Set up scenario context with helper
        _scenarioContext.Set(new ScenarioContextHelper(_scenarioContext), "Helper");

        // Log scenario start
        Console.WriteLine($"Starting scenario: {_scenarioContext.ScenarioInfo.Title}");
    }

    [AfterScenario]
    public async Task AfterScenario()
    {
        // Clean up test data
        await _testBase.DisposeAsync();

        // Log scenario end
        var status = _scenarioContext.TestError == null ? "PASSED" : "FAILED";
        Console.WriteLine($"Finished scenario: {_scenarioContext.ScenarioInfo.Title} - {status}");

        // Log error if test failed
        if (_scenarioContext.TestError != null)
        {
            Console.WriteLine($"Error: {_scenarioContext.TestError.Message}");
            Console.WriteLine($"Stack trace: {_scenarioContext.TestError.StackTrace}");
        }
    }

    [BeforeStep]
    public void BeforeStep()
    {
        // Log step execution
        Console.WriteLine($"  Executing: {_scenarioContext.StepContext.StepInfo.Text}");
    }

    [AfterStep]
    public void AfterStep()
    {
        // Log step completion with timing if available
        var stepInfo = _scenarioContext.StepContext.StepInfo;
        Console.WriteLine($"  Completed: {stepInfo.Text}");
    }
}

using Test.Tools;

namespace Test.Logic.Basic.SimpleGeneration;

[TestClass]
public class SimpleGenerationTest
{
    [TestMethod]
    public async Task TestMethod()
    {
        // setup
        var runner = new Runner<Mode_Simple>();
        var game = runner.GameRoom;

        // execute
        await game.StartGameAsync();
        IsNull(game.Phase);
    }
}

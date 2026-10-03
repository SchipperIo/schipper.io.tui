namespace Schipper.Io.Tui.Tests;

[TestClass]
public sealed class FakeTerminalTests
{
    [TestMethod]
    public async Task ReadAsyncKeepsUnreadTailOfALongChunk()
    {
        FakeTerminal terminal = new();
        byte[] chunk = new byte[600];
        for (int i = 0; i < chunk.Length; i++)
        {
            chunk[i] = (byte)(i % 251);
        }

        terminal.QueueInput(chunk);

        byte[] first = new byte[512];
        byte[] second = new byte[512];
        int n1 = await terminal.ReadAsync(first);
        int n2 = await terminal.ReadAsync(second);

        Assert.AreEqual(512, n1);
        Assert.AreEqual(88, n2);
        CollectionAssert.AreEqual(chunk.AsSpan(0, 512).ToArray(), first);
        CollectionAssert.AreEqual(chunk.AsSpan(512, 88).ToArray(), second.AsSpan(0, 88).ToArray());
    }
}

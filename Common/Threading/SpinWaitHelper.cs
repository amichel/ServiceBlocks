public static class SpinWaitHelper
{
    public static bool SpinWaitForTimeout(int timeout)
    {
        return SpinWaitForCondition(() => false, timeout);
    }

    public static bool SpinWaitForCondition(Func<bool> predicate, int timeout)
    {
        Thread.MemoryBarrier();
        var sw = new Stopwatch();
        var spin = new SpinWait();
        sw.Start();
        while (sw.ElapsedMilliseconds < timeout)
        {
            if (predicate())
            {
                sw.Stop();
                return true;
            }
            spin.SpinOnce();
        }
        sw.Stop();
        return false;
    }
}
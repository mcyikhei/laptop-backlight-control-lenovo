namespace LenovoLaptopBacklight.Services.Ec;

/// <summary>
/// Process-wide shared EcController so the WinRing0 driver is installed/opened once
/// and every view (Control, Wizard, ...) talks to the same instance. Avoids multiple
/// EcController instances racing to install/remove the driver service.
/// </summary>
public static class EcAccess
{
    private static EcController? _ec;
    private static readonly object Gate = new();

    public static EcController Instance
    {
        get
        {
            lock (Gate)
            {
                return _ec ??= new EcController();
            }
        }
    }

    public static void Shutdown()
    {
        lock (Gate)
        {
            _ec?.Dispose();
            _ec = null;
        }
    }
}

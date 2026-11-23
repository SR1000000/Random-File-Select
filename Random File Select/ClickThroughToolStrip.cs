namespace Random_File_Select;

/// <summary>ToolStrip that processes clicks immediately when the parent window is unfocused.</summary>
public class ClickThroughToolStrip : ToolStrip
{
    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_ACTIVATE = 1;
    private const int MA_ACTIVATEANDEAT = 2;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        // WinForms ToolStrip returns MA_ACTIVATEANDEAT on WM_MOUSEACTIVATE, which focuses the window
        // but swallows the click so toolbar buttons don't fire when the app is unfocused.
        if (m.Msg == WM_MOUSEACTIVATE && m.Result == (nint)MA_ACTIVATEANDEAT)
        {
            m.Result = (nint)MA_ACTIVATE;
        }
    }
}
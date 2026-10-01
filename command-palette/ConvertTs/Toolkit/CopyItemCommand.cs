using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ConvertTs.Toolkit;

public partial class CopyItemCommand : InvokableCommand
{
    public virtual string Text { get; set; }

    public virtual CommandResult Result { get; set; } = CommandResult.ShowToast("Copied to clipboard");

    public CopyItemCommand(string text, string label)
    {
        Text = text;
        Name = label;
        Icon = new IconInfo("\ue8c8");
    }

    public override ICommandResult Invoke()
    {
        ClipboardHelper.SetText(Text);
        return Result;
    }
}
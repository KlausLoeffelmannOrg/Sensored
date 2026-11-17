namespace Sensored;

/// <summary>Restores font-sized editors affected by the .NET 11 up/down height regression.</summary>
internal static class UpDownHeightWorkaround
{
    /// <summary>Resets every up/down editor on first show and whenever its font changes.</summary>
    internal static void Attach(Form form)
    {
        AttachEditors(form);
        form.Shown += OnShown;
    }

    private static void AttachEditors(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is UpDownBase editor)
            {
                editor.FontChanged += OnEditorFontChanged;
                ResetHeight(editor);
            }

            AttachEditors(child);
        }
    }

    private static void OnShown(object? sender, EventArgs e)
    {
        if (sender is Control root)
        {
            ResetHeights(root);
        }
    }

    private static void ResetHeights(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is UpDownBase editor)
            {
                ResetHeight(editor);
            }

            ResetHeights(child);
        }
    }

    private static void OnEditorFontChanged(object? sender, EventArgs e)
    {
        if (sender is UpDownBase editor)
        {
            ResetHeight(editor);
        }
    }

    private static void ResetHeight(UpDownBase editor)
    {
        editor.Height = editor.PreferredHeight;
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FH6Mod.ViewModels;

namespace FH6Mod;

/// <summary>
/// Resolves a View for a given ViewModel by convention:
///   FH6Mod.ViewModels.Pages.FooViewModel -> FH6Mod.Views.Pages.FooView
///   FH6Mod.ViewModels.FooViewModel       -> FH6Mod.Views.FooView
/// </summary>
[RequiresUnreferencedCode("ViewLocator uses reflection — keep View types reachable in trimming config.")]
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null) return null;

        var name = param.GetType().FullName!
            .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        var type = Type.GetType(name);
        if (type is null)
            return new TextBlock { Text = $"Missing view: {name}" };

        return (Control)Activator.CreateInstance(type)!;
    }

    public bool Match(object? data) => data is ViewModelBase;
}

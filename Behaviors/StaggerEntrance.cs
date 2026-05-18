using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using FH6Mod.Services;

namespace FH6Mod.Behaviors;

/// <summary>
/// Attached behavior — apply to a <see cref="Panel"/> (StackPanel, Grid, WrapPanel...)
/// or <see cref="ItemsControl"/> to make its children fade-and-slide-in one after
/// another (stagger). Honors <see cref="AppSettings.AnimationsEnabled"/>.
/// </summary>
public static class StaggerEntrance
{
    public static readonly AttachedProperty<bool> EnableProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Enable", typeof(StaggerEntrance));

    public static void SetEnable(Control element, bool value) => element.SetValue(EnableProperty, value);
    public static bool GetEnable(Control element) => element.GetValue(EnableProperty);

    static StaggerEntrance()
    {
        EnableProperty.Changed.AddClassHandler<Control>((ctrl, e) =>
        {
            if (e.NewValue is true) Hook(ctrl);
        });
    }

    private static void Hook(Control ctrl)
    {
        ctrl.AttachedToVisualTree += (_, _) =>
        {
            if (!AppSettings.Current.AnimationsEnabled) return;

            // SYNC: hide all children + arm transitions BEFORE first render
            // (avoids the "static visible → flicker out → animate in" glitch)
            var children = GetChildren(ctrl);
            ArmInitialState(children);

            // ASYNC: kick off the staggered fade-in next frame
            Dispatcher.UIThread.Post(() => StartStagger(children), DispatcherPriority.Render);
        };
    }

    private static List<Control> GetChildren(Control ctrl)
    {
        var list = new List<Control>();
        if (ctrl is ItemsControl ic)
        {
            foreach (var container in ic.GetRealizedContainers())
                if (container is Control c) list.Add(c);
        }
        else if (ctrl is Panel panel)
        {
            foreach (var child in panel.Children)
                if (child is Control c) list.Add(c);
        }
        return list;
    }

    private static void ArmInitialState(List<Control> children)
    {
        var duration = AppSettings.Current.AnimationDurationMs;
        foreach (var c in children)
        {
            // 1) Set hidden state FIRST, while transitions are still null/empty,
            //    so this assignment is instantaneous (no animation from 1→0).
            c.Transitions = null;
            c.Opacity = 0;
            c.RenderTransform = TransformOperations.Parse("translateY(14px)");

            // 2) Arm transitions AFTER the hide, so the upcoming 0→1 + translate→identity animates.
            c.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration = TimeSpan.FromMilliseconds(duration),
                    Easing = new CubicEaseOut(),
                },
                new TransformOperationsTransition
                {
                    Property = Visual.RenderTransformProperty,
                    Duration = TimeSpan.FromMilliseconds(duration),
                    Easing = new CubicEaseOut(),
                },
            };
        }
    }

    private static async void StartStagger(List<Control> children)
    {
        var stagger = AppSettings.Current.AnimationStaggerMs;
        var index = 0;
        foreach (var c in children)
        {
            // Capture by-value for async closure
            var control = c;
            var delay = index * stagger;
            index++;
            _ = RevealAfter(control, delay);
        }
        await System.Threading.Tasks.Task.CompletedTask;
    }

    private static async System.Threading.Tasks.Task RevealAfter(Control c, int delayMs)
    {
        try
        {
            if (delayMs > 0) await System.Threading.Tasks.Task.Delay(delayMs);
            c.Opacity = 1;
            c.RenderTransform = TransformOperations.Parse("translateY(0)");
        }
        catch { /* never break the UI for an animation */ }
    }
}

using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace CC98.Services;

public class UiEx
{
    public static void AnimateCard(TranslateTransform transform, double targetX, double targetY)
    {
        var storyboard = new Storyboard();

        var animationX = new DoubleAnimation
        {
            To = targetX,
            Duration = TimeSpan.FromSeconds(0.2),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animationX, transform);
        Storyboard.SetTargetProperty(animationX, "X");

        var animationY = new DoubleAnimation
        {
            To = targetY,
            Duration = TimeSpan.FromSeconds(0.2),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(animationY, transform);
        Storyboard.SetTargetProperty(animationY, "Y");

        storyboard.Children.Add(animationX);
        storyboard.Children.Add(animationY);
        storyboard.Begin();
    }
}
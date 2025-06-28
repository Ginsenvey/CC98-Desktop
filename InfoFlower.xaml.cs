using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App3;

public sealed partial class InfoFlower : UserControl
{
    public InfoFlower()
    {
        this.InitializeComponent();
    }
    public void PlayAnimation(string Glyph,string Text)
    {
        // ����״̬
        this.FlowIcon.Glyph = Glyph;
        this.FlowInfo.Text = Text;
        FlowerTransform.TranslateY = 0;
        Flower.Opacity = 0;

        // ��ʾ�ؼ�
        Flower.Visibility = Visibility.Visible;

        // ��������
        FlowerAnimation.Begin();

        // ��������ʱ����
        FlowerAnimation.Completed += OnAnimationCompleted;
    }

    private void OnAnimationCompleted(object sender, object e)
    {
        Flower.Visibility = Visibility.Collapsed;
        FlowerAnimation.Completed -= OnAnimationCompleted;
        FlowerAnimation.Stop();

        // ��������¼�
        AnimationCompleted?.Invoke(this, EventArgs.Empty);
    }

   
    

    
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(InfoFlower),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
            "Glyph",
            typeof(string),
            typeof(InfoFlower),
            new PropertyMetadata(string.Empty, OnGlyphChanged));

    public string Glygh
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }
    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (InfoFlower)d;
        if (control.FlowInfo != null)
        {
            control.FlowInfo.Text = (string)e.NewValue;

            // �����ı����ݵ����ɼ���
            control.FlowInfo.Visibility =
                string.IsNullOrEmpty((string)e.NewValue) ?
                Visibility.Collapsed :
                Visibility.Visible;
        }
    }
    private static void OnGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (InfoFlower)d;
        if (control.FlowIcon!= null)
        {
            control.FlowIcon.Glyph = (string)e.NewValue;
        }
    }
    // ��������¼�
    public event EventHandler AnimationCompleted;
}

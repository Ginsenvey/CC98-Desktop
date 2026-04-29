using System;
using System.Diagnostics;
using Windows.Foundation;
using CSharpMath.SkiaSharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using SkiaSharp.Views.Windows;

namespace CC98.Controls.LatexBlock;

public sealed partial class LatexBlock : UserControl
{
    private readonly SKXamlCanvas _canvas;
    private readonly MathPainter _painter = new();
    private float _contentHeight;

    // 内容尺寸（不包含Padding）
    private float _contentWidth;
    private string _latex = "";

    // 行间距
    private float _lineSpacing = 0.2f;

    // 内边距
    private Thickness _padding = new(5);

    public LatexBlock()
    {
        _canvas = new();
        //必须设置 IgnorePixelScaling 为 true，以避免 DPI 缩放问题
        //关闭将导致实际高度和宽度与测量值不一致
        _canvas.IgnorePixelScaling = true;

        var grid = new Grid();
        grid.Children.Add(_canvas);


        Content = grid;


        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Bottom;

        _canvas.PaintSurface += OnPaintSurface;
    }

    public Thickness Padding
    {
        get => _padding;
        set
        {
            _padding = value;
            InvalidateMeasure();
        }
    }

    public double LineSpacing
    {
        get => _lineSpacing;
        set
        {
            _lineSpacing = (float)value;
            InvalidateMeasure();
        }
    }

    // LaTeX公式
    public string LaTeX
    {
        get => _latex;
        set
        {
            _latex = value;
            InvalidateMeasure();
        }
    }

    // 字体大小
    public double FontSize
    {
        get => _painter.FontSize;
        set
        {
            _painter.FontSize = (float)value;
            InvalidateMeasure();
        }
    }

    // 水平对齐
    public HorizontalAlignment HorizontalContentAlignment { get; set; } = HorizontalAlignment.Center;

    // 是否显示调试边框
    public bool ShowDebugBounds { get; set; } = false;

    public string DebugInfo
    {
        get
        {
            var lineCount = _latex?.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
            return $"内容: {_contentWidth:F1}x{_contentHeight:F1}, " +
                   $"控件: {ActualWidth:F1}x{ActualHeight:F1}, " +
                   $"画布: {_canvas.ActualWidth:F1}x{_canvas.ActualHeight:F1}, " +
                   $"行数: {lineCount}";
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (string.IsNullOrEmpty(_latex))
        {
            _contentWidth = 0;
            _contentHeight = 0;
            var emptySize = new Size(
                Padding.Left + Padding.Right,
                Padding.Top + Padding.Bottom
            );

            // 测量Content
            if (Content is FrameworkElement c) c.Measure(emptySize);

            return emptySize;
        }

        try
        {
            // 使用足够大的宽度测量（公式不需要换行）
            const float measureWidth = 2000f;

            var lines = _latex.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            _contentWidth = 0;
            _contentHeight = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                _painter.LaTeX = lines[i].Trim();
                var rect = _painter.Measure(measureWidth);

                _contentWidth = Math.Max(_contentWidth, rect.Width);
                //_contentHeight += rect.Height;
                var lineHeight = rect.Height;
                var extraBottomSpace = lineHeight * 0.2f; // 增加20%的底部空间
                _contentHeight += lineHeight + extraBottomSpace;

                if (i < lines.Length - 1) _contentHeight += rect.Height * _lineSpacing;
            }

            if (_contentWidth <= 0) _contentWidth = 20;
            if (_contentHeight <= 0) _contentHeight = 20;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"测量失败: {ex.Message}");
            _contentWidth = 100;
            _contentHeight = 30;
        }

        // 计算总尺寸（内容 + Padding）
        var totalWidth = _contentWidth + Padding.Left + Padding.Right;
        var totalHeight = _contentHeight + Padding.Top + Padding.Bottom;

        var desiredSize = new Size(totalWidth, totalHeight);

        // 测量Content
        if (Content is FrameworkElement content) content.Measure(desiredSize);

        Debug.WriteLine($"MeasureOverride 返回: {totalWidth:F1}x{totalHeight:F1}");

        return desiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Debug.WriteLine($"ArrangeOverride 收到: {finalSize.Width:F1}x{finalSize.Height:F1}");

        // 排列Content
        if (Content is FrameworkElement content) content.Arrange(new(0, 0, finalSize.Width, finalSize.Height));

        return finalSize;
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // 获取画布实际尺寸
        var canvasWidth = (float)_canvas.ActualWidth;
        var canvasHeight = (float)_canvas.ActualHeight;

        // 如果没有内容或尺寸无效，直接返回
        if (string.IsNullOrEmpty(_latex) || _contentWidth <= 0 || _contentHeight <= 0 || canvasWidth <= 0 ||
            canvasHeight <= 0)
        {
            // 绘制空状态提示
            if (ShowDebugBounds && string.IsNullOrEmpty(_latex))
            {
                var textPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    TextSize = 14,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Consolas")
                };
                canvas.DrawText("无公式", 10, 30, textPaint);
            }

            return;
        }

        try
        {
            // ============ 1. 绘制控件整体边界（红色） ============
            if (ShowDebugBounds)
            {
                var controlBoundsPaint = new SKPaint
                {
                    Color = SKColors.Red,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    IsAntialias = true
                };
                canvas.DrawRect(0, 0, canvasWidth, canvasHeight, controlBoundsPaint);

                var textPaint = new SKPaint
                {
                    Color = SKColors.Red,
                    TextSize = 12,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Consolas")
                };
                canvas.DrawText($"控件边界 ({canvasWidth:F0}x{canvasHeight:F0})",
                    5, 15, textPaint);
            }

            // ============ 2. 绘制 Padding 区域（蓝色虚线） ============
            if (ShowDebugBounds)
            {
                var paddingBoundsPaint = new SKPaint
                {
                    Color = SKColors.Blue,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.5f,
                    PathEffect = SKPathEffect.CreateDash([5, 5], 0),
                    IsAntialias = true
                };

                canvas.DrawRect(
                    (float)Padding.Left,
                    (float)Padding.Top,
                    canvasWidth - (float)(Padding.Left + Padding.Right),
                    canvasHeight - (float)(Padding.Top + Padding.Bottom),
                    paddingBoundsPaint
                );

                var textPaint = new SKPaint
                {
                    Color = SKColors.Blue,
                    TextSize = 12,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Consolas")
                };
                canvas.DrawText("Padding",
                    (float)Padding.Left, (float)Padding.Top - 5, textPaint);
            }

            // ============ 3. 绘制内容区域边界（绿色） ============
            if (ShowDebugBounds)
            {
                var contentBoundsPaint = new SKPaint
                {
                    Color = SKColors.Green,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1.5f,
                    IsAntialias = true
                };

                canvas.DrawRect(
                    (float)Padding.Left,
                    (float)Padding.Top,
                    _contentWidth,
                    _contentHeight,
                    contentBoundsPaint
                );

                var textPaint = new SKPaint
                {
                    Color = SKColors.Green,
                    TextSize = 12,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Consolas")
                };
                canvas.DrawText($"内容区域 ({_contentWidth:F0}x{_contentHeight:F0})",
                    (float)Padding.Left, (float)Padding.Top + _contentHeight + 15, textPaint);
            }

            // ============ 4. 绘制公式内容 ============
            var lines = _latex.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var currentY = (float)Padding.Top;

            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                _painter.LaTeX = lines[i].Trim();
                var rect = _painter.Measure(_contentWidth);

                // 计算水平对齐
                var x = (float)Padding.Left;

                switch (HorizontalContentAlignment)
                {
                    case HorizontalAlignment.Right:
                        x = canvasWidth - rect.Width - (float)Padding.Right;
                        break;
                    case HorizontalAlignment.Center:
                        x = (float)Padding.Left + (_contentWidth - rect.Width) / 2;
                        break;
                }

                // ============ 5. 绘制每行公式的边界框（橙色） ============
                if (ShowDebugBounds)
                {
                    var lineBoundsPaint = new SKPaint
                    {
                        Color = SKColors.Orange,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 1,
                        IsAntialias = true
                    };

                    canvas.DrawRect(
                        x,
                        currentY + rect.Y,
                        rect.Width,
                        rect.Height,
                        lineBoundsPaint
                    );

                    var lineNumberPaint = new SKPaint
                    {
                        Color = SKColors.Orange,
                        TextSize = 11,
                        IsAntialias = true,
                        Typeface = SKTypeface.FromFamilyName("Consolas")
                    };
                    canvas.DrawText($"行{i + 1}",
                        x + rect.Width + 5,
                        currentY + rect.Y + rect.Height / 2,
                        lineNumberPaint);
                }


                // 绘制公式 - 给底部增加额外空间
                var baselineOffset = Math.Abs(rect.Y); // rect.Y 是负值，表示基线以上的高度
                var extraBottomSpace = rect.Height * 0.2f; // 增加20%的底部空间
                _painter.Draw(canvas, new(x, currentY + rect.Height + extraBottomSpace));

                currentY += rect.Height;

                if (i < lines.Length - 1) currentY += rect.Height * _lineSpacing;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"绘制失败: {ex.Message}");

            if (ShowDebugBounds)
            {
                var errorPaint = new SKPaint
                {
                    Color = SKColors.Red,
                    TextSize = 14,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Consolas")
                };
                canvas.DrawText($"错误: {ex.Message}", 10, 50, errorPaint);
            }
        }
    }
}
﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ListenerGui.Models;
using Microsoft.Diagnostics.Runtime;

namespace ListenerGui
{
    public partial class Region
    {
        private bool _realSize;
        private bool _showReservedMemory;

        public Region()
        {
            InitializeComponent();
        }

        public Region(Segment segment, int heap, bool realSize, bool showReservedMemory)
        {
            _realSize = realSize;
            _showReservedMemory = showReservedMemory;
            Segment = segment;

            InitializeComponent();

            ApplySegment(segment, null, heap);
        }

        public ulong Address => CustomMemoryRange?.Start ?? Segment.Start;

        public bool IsDeleted { get; private set; }

        public MemoryRange? CustomMemoryRange { get; set; }

        public Segment Segment { get; private set; }

        public void Update(Segment newSegment, int heap, bool realSize, bool showReservedMemory)
        {
            _realSize = realSize;
            _showReservedMemory = showReservedMemory;
            ApplySegment(newSegment, Segment, heap);
            Segment = newSegment;
        }

        public void Delete()
        {
            IsDeleted = true;

            var creationStoryboard = (Storyboard)Resources["AnimateCreation"]!;
            var creationAnimation = (DoubleAnimation)creationStoryboard.Children[0];

            if (creationStoryboard.GetCurrentState() != ClockState.Stopped)
            {
                return;
            }

            creationAnimation.From = Width;
            creationAnimation.To = 0;
            creationStoryboard.Completed += (_, _) => { ((Panel)Parent)?.Children.Remove(this); };

            creationStoryboard.Begin();
        }

        private void ApplySegment(Segment segment, Segment? previousSegment, int heap)
        {
            if (segment.Kind == GCSegmentKind.Ephemeral)
            {
                ApplySegmentEphemeral(segment, previousSegment, heap);
                return;
            }

            TextHeap.Text = heap.ToString();

            if (_realSize)
            {
                var size = (long)ToMB(SegmentSize(segment, _showReservedMemory));
                Width = size * 10;
            }
            else
            {
                var size = (long)ToMB(SegmentSize(segment, true));
                Width = size <= 4 ? 40 : 80;
            }

            Height = 40;
            Margin = new Thickness(Width <= 1 ? 0 : 1);

            var generation = segment.Generation;
            var color = GetColor(generation);

            if (segment.Flags.HasFlag((ClrSegmentFlags)32))
            {
                color = Colors.Red;
            }

            if (previousSegment == null)
            {
                MainColor.Color = color;
                FillRectangle.Width = Width * GetFillFactor(segment);

                var creationStoryboard = (Storyboard)Resources["AnimateCreation"]!;
                var creationAnimation = (DoubleAnimation)creationStoryboard.Children[0];

                creationAnimation.From = 0;
                creationAnimation.To = Width;

                creationStoryboard.Begin();
            }
            else
            {
                var previousGeneration = previousSegment.Generation;

                MainColor.Color = color;
                FillRectangle.Width = Width * GetFillFactor(segment);

                if (previousGeneration != generation)
                {
                    var colorStoryboard = (Storyboard)Resources["AnimateColor"]!;
                    var colorAnimation = (ColorAnimation)colorStoryboard.Children[0];

                    colorAnimation.From = GetColor(previousGeneration);
                    colorAnimation.To = color;

                    BeginStoryboard(colorStoryboard);
                }

                if (GetFillFactor(previousSegment) != GetFillFactor(segment))
                {
                    var fillStoryboard = (Storyboard)Resources["AnimateFill"]!;
                    var fillAnimation = (DoubleAnimation)fillStoryboard.Children[0];

                    fillAnimation.From = Width * GetFillFactor(previousSegment);
                    fillAnimation.To = Width * GetFillFactor(segment);

                    BeginStoryboard(fillStoryboard);
                }
            }
        }

        private void ApplySegmentEphemeral(Segment segment, Segment? previousSegment, int heap)
        {
            if (Gen0Rectangle.Visibility == Visibility.Collapsed)
            {
                Gen0Rectangle.Visibility = Visibility.Visible;
                Gen1Rectangle.Visibility = Visibility.Visible;
                Gen2Rectangle.Visibility = Visibility.Visible;

                Gen0Rectangle.Fill = new SolidColorBrush(GetColor(Generation.Generation0));
                Gen1Rectangle.Fill = new SolidColorBrush(GetColor(Generation.Generation1));
                Gen2Rectangle.Fill = new SolidColorBrush(GetColor(Generation.Generation2));
            }

            TextHeap.Text = heap.ToString();

            ulong size;

            if (_realSize)
            {
                size = SegmentSize(segment, _showReservedMemory);
                var sizeInMb = (long)ToMB(size);
                Width = sizeInMb * 10;
            }
            else
            {
                size = SegmentSize(segment, true);
                Width = 200;
                size = segment.Generation2.Length + segment.Generation1.Length + segment.Generation0.Length;
            }

            Height = 40;

            Margin = new Thickness(1);

            if (previousSegment == null)
            {
                MainColor.Color = Colors.LightGray;

                var creationStoryboard = (Storyboard)Resources["AnimateCreation"]!;
                var creationAnimation = (DoubleAnimation)creationStoryboard.Children[0];

                creationAnimation.From = 0;
                creationAnimation.To = Width;

                creationStoryboard.Begin();
            }

            Gen2Rectangle.Width = ((double)segment.Generation2.Length / size) * Width;

            Gen1Rectangle.Width = ((double)segment.Generation1.Length / size) * Width;
            Gen1Rectangle.Margin = new Thickness(Gen2Rectangle.Width, 0, 0, 0);

            Gen0Rectangle.Width = ((double)segment.Generation0.Length / size) * Width;
            Gen0Rectangle.Margin = new Thickness(Gen2Rectangle.Width + Gen1Rectangle.Width, 0, 0, 0);
        }

        private double GetFillFactor(Segment segment)
        {
            return (double)segment.ObjectRange.Length / SegmentSize(segment, _showReservedMemory);
        }

        public static ulong SegmentSize(Segment segment, bool showReservedMemory)
        {
            if (showReservedMemory)
            {
                return segment.ReservedMemory.Length + segment.CommittedMemory.Length;
            }

            return segment.CommittedMemory.Length;
        }

        public static double ToMB(ulong length)
        {
            return Math.Round(length / (1024.0 * 1024), 2);
        }

        public static Color GetColor(Generation generation)
        {
            return generation switch
            {
                Generation.Generation0 => Colors.PowderBlue,
                Generation.Generation1 => Colors.SkyBlue,
                Generation.Generation2 => Colors.CornflowerBlue,
                Generation.Large => Colors.Orange,
                Generation.Pinned => Colors.Pink,
                Generation.Frozen => Colors.Gray,
                Generation.Unknown => Colors.Red,
            };
        }
    }
}

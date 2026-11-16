using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Bloxstrap.UI.Utility;

namespace Bloxstrap.UI.Elements.Controls
{
    /// <summary>
    /// Branding layer that sits under the window content: the VidoyStrap artwork in the
    /// background plane and a persistent "VidoyStrap" wordmark. Never takes input.
    /// Visibility and animation are driven by the personalization settings, and the artwork
    /// is tinted with the chosen accent so the backdrop follows the theme too.
    /// </summary>
    public partial class BrandBackdrop : UserControl
    {
        private static readonly Uri AnimatedSource =
            new("pack://application:,,,/Resources/VidoyStrapBackdrop.gif", UriKind.Absolute);

        private static readonly ImageSource StaticSource =
            new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/VidoyStrapBackdrop.png", UriKind.Absolute));

        private bool _animating;

        public BrandBackdrop()
        {
            InitializeComponent();
            Loaded += BrandBackdrop_Loaded;
            Unloaded += BrandBackdrop_Unloaded;
        }

        private void BrandBackdrop_Loaded(object sender, RoutedEventArgs e)
        {
            Branding.Changed += Branding_Changed;
            ApplySettings();
        }

        private void BrandBackdrop_Unloaded(object sender, RoutedEventArgs e)
        {
            Branding.Changed -= Branding_Changed;
            StopAnimation();
        }

        private void Branding_Changed(object? sender, EventArgs e) => ApplySettings();

        private void ApplySettings()
        {
            bool show = App.Settings.Prop.ShowBackdrop;
            bool animate = show && App.Settings.Prop.AnimateBackdrop;

            BackdropImage.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            BackdropTint.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

            ApplyTint();

            if (!show)
            {
                StopAnimation();
                return;
            }

            if (animate)
                StartAnimation();
            else
                StopAnimation();
        }

        private void ApplyTint()
        {
            Color accent = Branding.EffectiveAccent;

            var brush = new SolidColorBrush(Color.FromArgb(0xFF, accent.R, accent.G, accent.B));
            brush.Freeze();

            BackdropTint.Fill = brush;
        }

        private void StartAnimation()
        {
            if (_animating)
                return;

            try
            {
                // keep the static frame underneath so a decoder failure costs the motion, not the image
                BackdropImage.Source = StaticSource;
                XamlAnimatedGif.AnimationBehavior.SetRepeatBehavior(BackdropImage, RepeatBehavior.Forever);
                XamlAnimatedGif.AnimationBehavior.SetSourceUri(BackdropImage, AnimatedSource);
                _animating = true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("BrandBackdrop::StartAnimation", $"Animated backdrop unavailable ({ex.GetType().Name}), keeping static frame");
                BackdropImage.Source = StaticSource;
            }
        }

        private void StopAnimation()
        {
            if (!_animating)
                return;

            try
            {
                XamlAnimatedGif.AnimationBehavior.SetSourceUri(BackdropImage, null);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("BrandBackdrop::StopAnimation", $"Failed to stop the animated backdrop: {ex.Message}");
            }

            BackdropImage.Source = StaticSource;
            _animating = false;
        }
    }
}

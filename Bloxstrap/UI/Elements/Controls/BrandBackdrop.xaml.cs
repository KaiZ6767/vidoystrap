using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Bloxstrap.UI.Elements.Controls
{
    /// <summary>
    /// Branding layer that sits under the window content: the VidoyStrap artwork in the
    /// background plane and a persistent "VidoyStrap" wordmark. Never takes input.
    /// </summary>
    public partial class BrandBackdrop : UserControl
    {
        private static readonly Uri AnimatedSource =
            new("pack://application:,,,/Resources/VidoyStrapBackdrop.gif", UriKind.Absolute);

        public BrandBackdrop()
        {
            InitializeComponent();
            Loaded += BrandBackdrop_Loaded;
        }

        private void BrandBackdrop_Loaded(object sender, RoutedEventArgs e)
        {
            // The PNG frame is already showing, so a decoder failure costs the animation, not the window.
            try
            {
                XamlAnimatedGif.AnimationBehavior.SetRepeatBehavior(BackdropImage, RepeatBehavior.Forever);
                XamlAnimatedGif.AnimationBehavior.SetSourceUri(BackdropImage, AnimatedSource);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("BrandBackdrop::Loaded", $"Animated backdrop unavailable ({ex.GetType().Name}), keeping static frame");
            }
        }
    }
}

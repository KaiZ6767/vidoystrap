using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using Bloxstrap.UI.Utility;
using CommunityToolkit.Mvvm.Input;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class PersonalizationViewModel : NotifyPropertyChangedViewModel
    {
        private readonly Page _page;

        private string _customAccentHex = "";
        private string _customAccentStatus = "";

        public PersonalizationViewModel(Page page)
        {
            _page = page;
            _customAccentHex = App.Settings.Prop.AccentColor;
        }

        public IReadOnlyList<AccentSwatch> Accents => Branding.Palette;

        public AccentSwatch SelectedAccent
        {
            get => Branding.FindSwatch(App.Settings.Prop.AccentColor);
            set
            {
                if (value is null)
                    return;

                App.Settings.Prop.AccentColor = value.Hex;
                _customAccentHex = value.Hex;

                Branding.ApplyAccent();
                Branding.NotifyChanged();

                CustomAccentStatus = "";

                OnPropertyChanged(nameof(SelectedAccent));
                OnPropertyChanged(nameof(CustomAccentHex));
                OnPropertyChanged(nameof(CustomAccentStatus));
            }
        }

        public string CustomAccentHex
        {
            get => _customAccentHex;
            set
            {
                _customAccentHex = value ?? "";
                OnPropertyChanged(nameof(CustomAccentHex));
            }
        }

        public string CustomAccentStatus
        {
            get => _customAccentStatus;
            private set
            {
                _customAccentStatus = value;
                OnPropertyChanged(nameof(CustomAccentStatus));
            }
        }

        public ICommand ApplyCustomAccentCommand => new RelayCommand(ApplyCustomAccent);

        private void ApplyCustomAccent()
        {
            string hex = _customAccentHex;

            if (!Branding.TryParseHex(hex, out _))
            {
                CustomAccentStatus = "Formato inválido. Usá 6 dígitos, por ejemplo #FF1336.";
                return;
            }

            App.Settings.Prop.AccentColor = "#" + Branding.Normalize(hex);
            Branding.ApplyAccent();
            Branding.NotifyChanged();

            CustomAccentStatus = $"Aplicado: {App.Settings.Prop.AccentColor}";

            OnPropertyChanged(nameof(SelectedAccent));
        }

        public bool ShowBackdrop
        {
            get => App.Settings.Prop.ShowBackdrop;
            set
            {
                App.Settings.Prop.ShowBackdrop = value;
                Branding.NotifyChanged();

                OnPropertyChanged(nameof(ShowBackdrop));
                OnPropertyChanged(nameof(AnimateBackdropEnabled));
            }
        }

        public bool AnimateBackdrop
        {
            get => App.Settings.Prop.AnimateBackdrop;
            set
            {
                App.Settings.Prop.AnimateBackdrop = value;
                Branding.NotifyChanged();

                OnPropertyChanged(nameof(AnimateBackdrop));
            }
        }

        public bool AnimateBackdropEnabled => App.Settings.Prop.ShowBackdrop;
    }
}

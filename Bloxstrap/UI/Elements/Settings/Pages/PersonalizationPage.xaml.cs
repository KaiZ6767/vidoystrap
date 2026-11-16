using Bloxstrap.UI.ViewModels.Settings;

namespace Bloxstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for PersonalizationPage.xaml
    /// </summary>
    public partial class PersonalizationPage
    {
        public PersonalizationPage()
        {
            DataContext = new PersonalizationViewModel(this);
            InitializeComponent();
        }
    }
}

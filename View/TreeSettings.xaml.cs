using SolutionBuilder.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SolutionBuilder.View
{
    /// <summary>
    /// Interaktionslogik für TreeSettings.xaml
    /// </summary>
    public partial class TreeSettings : Window
    {
        public TreeSettings()
        {
            InitializeComponent();

            List<Family> families = new List<Family>();

            Family family1 = new Family() { Name = "The Doe's" };
            family1.Members.Add(new FamilyMember() { Name = "John Doe", Age = 42 });
            family1.Members.Add(new FamilyMember() { Name = "Jane Doe", Age = 39 });
            family1.Members.Add(new FamilyMember() { Name = "Sammy Doe", Age = 13 });
            families.Add(family1);

            Family family2 = new Family() { Name = "The Moe's" };
            family2.Members.Add(new FamilyMember() { Name = "Mark Moe", Age = 31 });
            family2.Members.Add(new FamilyMember() { Name = "Norma Moe", Age = 28 });
            families.Add(family2);

            trvFamilies.ItemsSource = families;
        }
    }
}

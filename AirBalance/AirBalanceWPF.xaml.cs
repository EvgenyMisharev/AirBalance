using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AirBalance
{
    public partial class AirBalanceWPF : Window
    {
        public Parameter SupplySystemNamesParam;
        public string SupplySystemNamesPrefix;
        public Parameter ExhaustSystemNamesParam;
        public string ExhaustSystemNamesPrefix;

        public Parameter AirConsumptionParam;
        public Parameter EstimatedSupplyParam;
        public Parameter EstimatedExhaustParam;

        List<Parameter> SpaceStringParametersList;
        List<Parameter> SpaceDoubleParametersList;
        List<Parameter> DuctTerminalDoubleParametersList;
        public string CalculationOptionButtonName;

        AirBalanceSettings AirBalanceSettingsItem;
        public AirBalanceWPF(List<Space> spaceList, List<FamilyInstance> ductTerminalList)
        {
            List<Parameter> spaceParametersList = new List<Parameter>();
            List<Parameter> ductTerminalParametersList = new List<Parameter>();

            ParameterSet spaceParameterSet = spaceList.First().Parameters;
            foreach (Parameter spaceParameter in spaceParameterSet)
            {
                spaceParametersList.Add(spaceParameter);
            }
            SpaceStringParametersList = spaceParametersList.Where(p => p.StorageType == StorageType.String)
                .OrderBy(p => p.Definition.Name, new AlphanumComparatorFastString())
                .ToList();
            SpaceDoubleParametersList = spaceParametersList.Where(p => p.StorageType == StorageType.Double)
                .OrderBy(p => p.Definition.Name, new AlphanumComparatorFastString())
                .ToList();

            ParameterSet ductTerminalParameterSet = ductTerminalList.First().Parameters;
            foreach (Parameter ductTerminalParameter in ductTerminalParameterSet)
            {
                ductTerminalParametersList.Add(ductTerminalParameter);
            }
            DuctTerminalDoubleParametersList = ductTerminalParametersList.Where(p => p.StorageType == StorageType.Double)
                .OrderBy(p => p.Definition.Name, new AlphanumComparatorFastString())
                .ToList();

            AirBalanceSettingsItem = new AirBalanceSettings().GetSettings();
            InitializeComponent();
            comboBox_SupplySystemNamesParam.ItemsSource = SpaceStringParametersList;
            comboBox_SupplySystemNamesParam.DisplayMemberPath = "Definition.Name";

            comboBox_ExhaustSystemNamesParam.ItemsSource = SpaceStringParametersList;
            comboBox_ExhaustSystemNamesParam.DisplayMemberPath = "Definition.Name";

            comboBox_AirConsumptionParam.ItemsSource = DuctTerminalDoubleParametersList;
            comboBox_AirConsumptionParam.DisplayMemberPath = "Definition.Name";

            comboBox_EstimatedSupplyParam.ItemsSource = SpaceDoubleParametersList;
            comboBox_EstimatedSupplyParam.DisplayMemberPath = "Definition.Name";

            comboBox_EstimatedExhaustParam.ItemsSource = SpaceDoubleParametersList;
            comboBox_EstimatedExhaustParam.DisplayMemberPath = "Definition.Name";

            if (AirBalanceSettingsItem != null)
            {
                if (SpaceStringParametersList.FirstOrDefault(sp => sp.Definition.Name == AirBalanceSettingsItem.SupplySystemNamesParamName) != null)
                {
                    comboBox_SupplySystemNamesParam.SelectedItem = SpaceStringParametersList.FirstOrDefault(sp => sp.Definition.Name == AirBalanceSettingsItem.SupplySystemNamesParamName);
                }
                else
                {
                    comboBox_SupplySystemNamesParam.SelectedItem = comboBox_SupplySystemNamesParam.Items[0];
                }
                textBox_SupplySystemNamesPrefix.Text = AirBalanceSettingsItem.SupplySystemNamesPrefix;

                if (SpaceStringParametersList.FirstOrDefault(sp => sp.Definition.Name == AirBalanceSettingsItem.ExhaustSystemNamesParamName) != null)
                {
                    comboBox_ExhaustSystemNamesParam.SelectedItem = SpaceStringParametersList.FirstOrDefault(sp => sp.Definition.Name == AirBalanceSettingsItem.ExhaustSystemNamesParamName);
                }
                else
                {
                    comboBox_ExhaustSystemNamesParam.SelectedItem = comboBox_ExhaustSystemNamesParam.Items[0];
                }
                textBox_ExhaustSystemNamesPrefix.Text = AirBalanceSettingsItem.ExhaustSystemNamesPrefix;


                if (DuctTerminalDoubleParametersList.FirstOrDefault(dtp => dtp.Definition.Name == AirBalanceSettingsItem.AirConsumptionParamName) != null)
                {
                    comboBox_AirConsumptionParam.SelectedItem = DuctTerminalDoubleParametersList.FirstOrDefault(dtp => dtp.Definition.Name == AirBalanceSettingsItem.AirConsumptionParamName);
                }
                else
                {
                    comboBox_AirConsumptionParam.SelectedItem = comboBox_AirConsumptionParam.Items[0];
                }

                if (SpaceDoubleParametersList.FirstOrDefault(sp => sp.Definition.Name == AirBalanceSettingsItem.EstimatedSupplyParamName) != null)
                {
                    comboBox_EstimatedSupplyParam.SelectedItem = SpaceDoubleParametersList.FirstOrDefault(sp => sp.Definition.Name == AirBalanceSettingsItem.EstimatedSupplyParamName);
                }
                else
                {
                    comboBox_EstimatedSupplyParam.SelectedItem = comboBox_EstimatedSupplyParam.Items[0];
                }

                if (SpaceDoubleParametersList.FirstOrDefault(sp => sp.Definition.Name == AirBalanceSettingsItem.EstimatedExhaustParamName) != null)
                {
                    comboBox_EstimatedExhaustParam.SelectedItem = SpaceDoubleParametersList.FirstOrDefault(sp => sp.Definition.Name == AirBalanceSettingsItem.EstimatedExhaustParamName);
                }
                else
                {
                    comboBox_EstimatedExhaustParam.SelectedItem = comboBox_EstimatedExhaustParam.Items[0];
                }

                if (AirBalanceSettingsItem.CalculationOptionButtonName == "rbt_AllProject")
                {
                    rbt_AllProject.IsChecked = true;
                }
                else
                {
                    rbt_SelectedItems.IsChecked = true;
                }
            }
            else
            {
                comboBox_SupplySystemNamesParam.SelectedItem = comboBox_SupplySystemNamesParam.Items[0];
                textBox_SupplySystemNamesPrefix.Text = "П";
                comboBox_ExhaustSystemNamesParam.SelectedItem = comboBox_ExhaustSystemNamesParam.Items[0];
                textBox_ExhaustSystemNamesPrefix.Text = "В";
                comboBox_AirConsumptionParam.SelectedItem = comboBox_AirConsumptionParam.Items[0];
                comboBox_EstimatedSupplyParam.SelectedItem = comboBox_EstimatedSupplyParam.Items[0];
                comboBox_EstimatedExhaustParam.SelectedItem = comboBox_EstimatedExhaustParam.Items[0];
                rbt_AllProject.IsChecked = true;
            }
        }
        private void btn_Ok_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.DialogResult = true;
            this.Close();
        }
        private void AirBalanceWPF_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                SaveSettings();
                this.DialogResult = true;
                this.Close();
            }

            else if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void SaveSettings()
        {
            AirBalanceSettingsItem = new AirBalanceSettings();
            SupplySystemNamesParam = comboBox_SupplySystemNamesParam.SelectedItem as Parameter;
            AirBalanceSettingsItem.SupplySystemNamesParamName = SupplySystemNamesParam.Definition.Name;

            SupplySystemNamesPrefix = textBox_SupplySystemNamesPrefix.Text;
            AirBalanceSettingsItem.SupplySystemNamesPrefix = SupplySystemNamesPrefix;

            ExhaustSystemNamesParam = comboBox_ExhaustSystemNamesParam.SelectedItem as Parameter;
            AirBalanceSettingsItem.ExhaustSystemNamesParamName = ExhaustSystemNamesParam.Definition.Name;

            ExhaustSystemNamesPrefix = textBox_ExhaustSystemNamesPrefix.Text;
            AirBalanceSettingsItem.ExhaustSystemNamesPrefix = ExhaustSystemNamesPrefix;


            AirConsumptionParam = comboBox_AirConsumptionParam.SelectedItem as Parameter;
            AirBalanceSettingsItem.AirConsumptionParamName = AirConsumptionParam.Definition.Name;

            EstimatedSupplyParam = comboBox_EstimatedSupplyParam.SelectedItem as Parameter;
            AirBalanceSettingsItem.EstimatedSupplyParamName = EstimatedSupplyParam.Definition.Name;

            EstimatedExhaustParam = comboBox_EstimatedExhaustParam.SelectedItem as Parameter;
            AirBalanceSettingsItem.EstimatedExhaustParamName = EstimatedExhaustParam.Definition.Name;

            CalculationOptionButtonName = (groupBox_CalculationOption.Content as System.Windows.Controls.Grid)
                .Children.OfType<RadioButton>()
                .FirstOrDefault(rb => rb.IsChecked.Value == true)
                .Name;

            AirBalanceSettingsItem.CalculationOptionButtonName = CalculationOptionButtonName;

            AirBalanceSettingsItem.SaveSettings();
        }
    }
}

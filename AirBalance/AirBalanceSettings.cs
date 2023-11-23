using System.IO;
using System.Xml.Serialization;

namespace AirBalance
{
    public class AirBalanceSettings
    {
        public string SupplySystemNamesParamName { get; set; }
        public string SupplySystemNamesPrefix { get; set; }
        public string ExhaustSystemNamesParamName { get; set; }
        public string ExhaustSystemNamesPrefix { get; set; }

        public string AirConsumptionParamName { get; set; }
        public string EstimatedSupplyParamName { get; set; }
        public string EstimatedExhaustParamName { get; set; }
        public string CalculationOptionButtonName { get; set; }

        public AirBalanceSettings GetSettings()
        {
            AirBalanceSettings airBalanceSettings = null;
            string assemblyPathAll = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string fileName = "AirBalanceSettings.xml";
            string assemblyPath = assemblyPathAll.Replace("AirBalance.dll", fileName);

            if (File.Exists(assemblyPath))
            {
                using (FileStream fs = new FileStream(assemblyPath, FileMode.Open))
                {
                    XmlSerializer xSer = new XmlSerializer(typeof(AirBalanceSettings));
                    airBalanceSettings = xSer.Deserialize(fs) as AirBalanceSettings;
                    fs.Close();
                }
            }
            else
            {
                airBalanceSettings = null;
            }

            return airBalanceSettings;
        }

        public void SaveSettings()
        {
            string assemblyPathAll = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string fileName = "AirBalanceSettings.xml";
            string assemblyPath = assemblyPathAll.Replace("AirBalance.dll", fileName);

            if (File.Exists(assemblyPath))
            {
                File.Delete(assemblyPath);
            }

            using (FileStream fs = new FileStream(assemblyPath, FileMode.Create))
            {
                XmlSerializer xSer = new XmlSerializer(typeof(AirBalanceSettings));
                xSer.Serialize(fs, this);
                fs.Close();
            }
        }
    }
}

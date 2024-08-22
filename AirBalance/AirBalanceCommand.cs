using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace AirBalance
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class AirBalanceCommand : IExternalCommand
    {
        AirBalanceProgressBarWPF airBalanceProgressBarWPF;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                GetPluginStartInfo();
            }
            catch { }

            Document doc = commandData.Application.ActiveUIDocument.Document;
            Selection sel = commandData.Application.ActiveUIDocument.Selection;

            List<Space> spaceList = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .Cast<Space>()
                .Where(s => s.Volume > 0)
                .OrderBy(sp => sp.Number, new AlphanumComparatorFastString())
                .ToList();

            if (spaceList.Count == 0)
            {
                TaskDialog.Show("Revit", "Пространства отсутствуют в проекте!");
                return Result.Cancelled;
            }

            List<FamilyInstance> ductTerminalList = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_DuctTerminal)
                .Cast<FamilyInstance>()
                .ToList();

            if (ductTerminalList.Count == 0)
            {
                TaskDialog.Show("Revit", "Воздухораспределители отсутствуют в проекте!");
                return Result.Cancelled;
            }

            AirBalanceWPF airBalanceWPF = new AirBalanceWPF(spaceList, ductTerminalList);
            airBalanceWPF.ShowDialog();

            if (airBalanceWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }

            Parameter supplySystemNamesParam = airBalanceWPF.SupplySystemNamesParam;
            string supplySystemNamesPrefix = airBalanceWPF.SupplySystemNamesPrefix;
            Parameter exhaustSystemNamesParam = airBalanceWPF.ExhaustSystemNamesParam;
            string exhaustSystemNamesPrefix = airBalanceWPF.ExhaustSystemNamesPrefix;

            Parameter airConsumptionParam = airBalanceWPF.AirConsumptionParam;
            Parameter estimatedSupplyParam = airBalanceWPF.EstimatedSupplyParam;
            Parameter estimatedExhaustParam = airBalanceWPF.EstimatedExhaustParam;

            List<string> supplySystemNamesPrefixList = supplySystemNamesPrefix.Split(',').Select(str => str.Trim()).ToList();
            List<string> exhaustSystemNamesPrefixList = exhaustSystemNamesPrefix.Split(',').Select(str => str.Trim()).ToList();

            string calculationOptionButtonName = airBalanceWPF.CalculationOptionButtonName;

            if (calculationOptionButtonName == "rbt_SelectedItems")
            {
                spaceList = GetSpacesFromCurrentSelection(doc, sel);

                if (!spaceList.Any())
                {
                    try
                    {
                        IList<Reference> selSpaces = sel.PickObjects(ObjectType.Element, new SpaceSelectionFilter(), "Выберите пространства!");
                        foreach (Reference roomRef in selSpaces)
                        {
                            var space = doc.GetElement(roomRef) as Space;
                            if (space != null && space.Volume > 0)
                            {
                                spaceList.Add(space);
                            }
                        }
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                }
            }

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Баланс воздухообмена");

                // Запуск прогресс-бара в отдельном потоке
                Thread newWindowThread = new Thread(new ThreadStart(ThreadStartingPoint));
                newWindowThread.SetApartmentState(ApartmentState.STA);
                newWindowThread.IsBackground = true;
                newWindowThread.Start();

                Thread.Sleep(100);

                int step = 0;

                airBalanceProgressBarWPF.Dispatcher.Invoke(() =>
                {
                    airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Minimum = 0;
                    airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Maximum = spaceList.Count;
                });

                var terminalList = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_DuctTerminal)
                        .Cast<FamilyInstance>()
                        .Where(dt => dt.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM).AsString() != null)
                        .GroupBy(f => f.Space?.Id.IntegerValue)
                        .ToList();

                foreach (Space space in spaceList)
                {
                    step++;
                    airBalanceProgressBarWPF.Dispatcher.Invoke(() =>
                    {
                        airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Value = step;
                        airBalanceProgressBarWPF.label_ItemName.Content = space.Name;
                    });

                    var tmpDuctTerminalGroup = terminalList.SingleOrDefault(e => e.Key == space.Id.IntegerValue);
                    if (tmpDuctTerminalGroup == null) continue;

                    var tmpDuctTerminalList = tmpDuctTerminalGroup.ToList();

                    List<string> supplySystemNameList = new List<string>();
                    List<string> exhaustSystemNameList = new List<string>();
                    double estimatedSupply = 0;
                    double estimatedExhaust = 0;

                    foreach (FamilyInstance ductTerminal in tmpDuctTerminalList)
                    {
                        string systemName = ductTerminal.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM).AsString();

                        if (supplySystemNamesPrefixList.Any(prefix => systemName.StartsWith(prefix)))
                        {
                            if (!supplySystemNameList.Contains(systemName))
                            {
                                supplySystemNameList.Add(systemName);
                            }
                            estimatedSupply += ductTerminal.get_Parameter(airConsumptionParam.Definition).AsDouble();
                        }
                        else if (exhaustSystemNamesPrefixList.Any(prefix => systemName.StartsWith(prefix)))
                        {
                            if (!exhaustSystemNameList.Contains(systemName))
                            {
                                exhaustSystemNameList.Add(systemName);
                            }
                            estimatedExhaust += ductTerminal.get_Parameter(airConsumptionParam.Definition).AsDouble();
                        }
                    }

                    supplySystemNameList.Sort(new AlphanumComparatorFastString());
                    exhaustSystemNameList.Sort(new AlphanumComparatorFastString());

                    string supplySystemNames = string.Join(", ", supplySystemNameList);
                    string exhaustSystemNames = string.Join(", ", exhaustSystemNameList);

                    space.get_Parameter(supplySystemNamesParam.Definition).Set(supplySystemNames);
                    space.get_Parameter(exhaustSystemNamesParam.Definition).Set(exhaustSystemNames);
                    space.get_Parameter(estimatedSupplyParam.Definition).Set(estimatedSupply);
                    space.get_Parameter(estimatedExhaustParam.Definition).Set(estimatedExhaust);
                }

                airBalanceProgressBarWPF.Dispatcher.Invoke(() => airBalanceProgressBarWPF.Close());
                t.Commit();
            }

            return Result.Succeeded;
        }
        private void ThreadStartingPoint()
        {
            airBalanceProgressBarWPF = new AirBalanceProgressBarWPF();
            airBalanceProgressBarWPF.Show();
            System.Windows.Threading.Dispatcher.Run();
        }
        private static List<Space> GetSpacesFromCurrentSelection(Document doc, Selection sel)
        {
            ICollection<ElementId> selectedIds = sel.GetElementIds();
            List<Space> tempSpacessList = new List<Space>();

            foreach (ElementId roomId in selectedIds)
            {
                Space space = doc.GetElement(roomId) as Space;
                if (space != null && space.Category != null && space.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_MEPSpaces))
                {
                    tempSpacessList.Add(space);
                }
            }

            return tempSpacessList;
        }
        private static void GetPluginStartInfo()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string assemblyName = "AirBalance";
            string assemblyNameRus = "Фактический воздухообмен";
            string assemblyFolderPath = Path.GetDirectoryName(thisAssembly.Location);

            int lastBackslashIndex = assemblyFolderPath.LastIndexOf("\\");
            string dllPath = assemblyFolderPath.Substring(0, lastBackslashIndex + 1) + "PluginInfoCollector\\PluginInfoCollector.dll";

            Assembly assembly = Assembly.LoadFrom(dllPath);
            Type type = assembly.GetType("PluginInfoCollector.InfoCollector");

            if (type != null)
            {
                var constructor = type.GetConstructor(new Type[] { typeof(string), typeof(string) });
                if (constructor != null)
                {
                    Activator.CreateInstance(type, new object[] { assemblyName, assemblyNameRus });
                }
            }
        }

    }
}
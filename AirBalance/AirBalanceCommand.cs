﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AirBalance
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class AirBalanceCommand : IExternalCommand
    {
        AirBalanceProgressBarWPF airBalanceProgressBarWPF;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            List<Space> spaceList = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .Cast<Space>()
                .OrderBy(sp => sp.Number, new AlphanumComparatorFastString())
                .ToList();
            if(spaceList.Count == 0)
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

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Баланс воздухообмена");
                Thread newWindowThread = new Thread(new ThreadStart(ThreadStartingPoint));
                newWindowThread.SetApartmentState(ApartmentState.STA);
                newWindowThread.IsBackground = true;
                newWindowThread.Start();
                int step = 0;
                Thread.Sleep(100);
                airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Dispatcher.Invoke(() => airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Minimum = 0);
                airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Dispatcher.Invoke(() => airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Maximum = spaceList.Count);
                foreach (Space space in spaceList)
                {
                    step++;
                    airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Dispatcher.Invoke(() => airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Value = step);
                    airBalanceProgressBarWPF.pb_AirBalanceProgressBar.Dispatcher.Invoke(() => airBalanceProgressBarWPF.label_ItemName.Content = space.Name);

                    List<FamilyInstance> tmpDuctTerminalList = ductTerminalList
                        .Where(dt => dt.Space != null)
                        .Where(dt => dt.Space.Id == space.Id)
                        .Where(dt => dt.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM).AsString() != null)
                        .ToList();

                    List<string> supplySystemNameList = new List<string>();
                    List<string> exhaustSystemNameList = new List<string>();
                    double estimatedSupply = 0;
                    double estimatedExhaust = 0;
                    foreach (FamilyInstance ductTerminal in tmpDuctTerminalList)
                    {
                        if (ductTerminal.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM) != null)
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
                    }
                    supplySystemNameList.Sort(new AlphanumComparatorFastString());
                    exhaustSystemNameList.Sort(new AlphanumComparatorFastString());

                    string supplySystemNames = "";
                    foreach (string sn in supplySystemNameList)
                    {
                        if (supplySystemNames == "")
                        {
                            supplySystemNames = sn;
                        }
                        else
                        {
                            supplySystemNames += ", " + sn;
                        }
                    }
                    space.get_Parameter(supplySystemNamesParam.Definition).Set(supplySystemNames);

                    string exhaustSystemNames = "";
                    foreach (string sn in exhaustSystemNameList)
                    {
                        if (exhaustSystemNames == "")
                        {
                            exhaustSystemNames = sn;
                        }
                        else
                        {
                            exhaustSystemNames += ", " + sn;
                        }
                    }
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
    }
}
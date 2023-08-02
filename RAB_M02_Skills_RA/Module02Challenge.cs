#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

#endregion

namespace RAB_M02_Skills_RA
{
    [Transaction(TransactionMode.Manual)]
    public class Module02Challenge : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            // 1. pick elements by rectangular selection and add to list
            UIDocument uidoc = uiapp.ActiveUIDocument;
            IList<Element> pickList = uidoc.Selection.PickElementsByRectangle("Select Model Lines");

            // 2. filter selected elements for curves
            List<CurveElement> allCurves = new List<CurveElement>();
            foreach (Element elem in pickList)
            {
                if (elem is CurveElement)
                {
                    allCurves.Add(elem as CurveElement);
                }
            }

            // 3. filter selected curves for model curves
            List<CurveElement> modelCurves = new List<CurveElement>();
            foreach (Element elem in pickList)
            {
                if (elem is CurveElement)
                {
                    CurveElement curveElem = elem as CurveElement;

                    if (curveElem.CurveElementType == CurveElementType.ModelCurve)
                    {
                        modelCurves.Add(curveElem);
                    }
                }
            }


            TaskDialog.Show("Attention!", modelCurves.Count.ToString() + " Model Lines selected.");

            // 4. start of transaction to create Revit elements
            using (Transaction t1 = new Transaction(doc))
            {
                t1.Start("Revit elements from Model Lines");
           
                // 5. use custom methods to get elements by string matches
                //WallType curtainWallType = GetWallTypeByName(doc, "Storefront");
                //WallType genericWallType = GetWallTypeByName(doc, "Generic - 8\"");
                //Level levelOneInstance = GetLevelByName(doc, "Level 1");
                //MEPSystemType ductSystemType1 = GetSystemTypeByName(doc, "Supply Air");
                //MEPSystemType pipeSystemType1 = GetSystemTypeByName(doc, "Domestic Hot Water");

                // 6. get duct types
                FilteredElementCollector ductTypes = new FilteredElementCollector(doc);
                ductTypes.OfClass(typeof(DuctType));

                // 7. get pipe types
                FilteredElementCollector pipeTypes = new FilteredElementCollector(doc);
                pipeTypes.OfClass(typeof(PipeType));

                // 8. Loop thorugh curves, get they graphical styles, and create elements based on each case

                Curve boundCurve = null;
                foreach (CurveElement currentCurve in modelCurves)
                {
                        Curve curve = currentCurve.GeometryCurve;
                        if (curve.IsBound == true)
                        {
                            boundCurve = curve;
                        }

                        GraphicsStyle curStyle = currentCurve.LineStyle as GraphicsStyle;

                        switch (curStyle.Name)
                        {
                            case "A-GLAZ":
                                CreateWallByTypeAndLevelNames(doc, boundCurve, "Storefront", "Level 1", 20, 0, false, false);
                                break;
                            case "A-WALL":
                                CreateWallByTypeAndLevelNames(doc, boundCurve, "Generic - 8\"", "Level 1", 20, 0, false, false);
                                break;
                            case "M-DUCT":
                                CreateDuctByTypeAndLevelNames(doc, "Supply Air", "Level 1", boundCurve.GetEndPoint(0), boundCurve.GetEndPoint(1));
                                break;
                            case "P-PIPE":
                                CreatePipeByTypeAndLevelNames(doc, "Domestic Hot Water", "Level 1", boundCurve.GetEndPoint(0), boundCurve.GetEndPoint(1));
                                break;
                            default:
                                break;
                        }
                    
                }
                t1.Commit();
            }
            return Result.Succeeded;
        }

        // 9. create custom methods to get Revit elements with less inputs
        internal WallType GetWallTypeByName(Document doc, string typeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(WallType));

            foreach (WallType curType in collector)
            {
                if (curType.Name == typeName)
                {
                    return curType;
                }
            }
            return null;
        }

        internal MEPSystemType GetSystemTypeByName(Document doc, string typeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(MEPSystemType));

            foreach (MEPSystemType curType in collector)
            {
                if (curType.Name == typeName)
                {
                    return curType;
                }
            }
            return null;
        }

        internal Level GetLevelByName(Document doc, string Name)
        {
            FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
            levelCollector.OfCategory(BuiltInCategory.OST_Levels);
            levelCollector.WhereElementIsNotElementType();

            foreach (Level curName in levelCollector)
            {
                if (curName.Name == Name)
                {
                    return curName;
                }
            }
            return null;
        }

        internal Wall CreateWallByTypeAndLevelNames(Document doc, Curve curve, string wallTypeName, string levelName, double height, double offset, bool flip, bool structural)
        {
            WallType wallType1 = GetWallTypeByName(doc, wallTypeName);
            Level level1 = GetLevelByName(doc, levelName);
            Wall.Create(doc, curve, wallType1.Id, level1.Id, height, offset, flip, structural);

            return null;
        }

        internal Duct CreateDuctByTypeAndLevelNames(Document doc, string systemTypeName, string levelName, XYZ startPoint, XYZ endPoint)
        {
            MEPSystemType ductSystemType1 = GetSystemTypeByName(doc, systemTypeName);

            FilteredElementCollector ductTypes = new FilteredElementCollector(doc);
            ductTypes.OfClass(typeof(DuctType));

            Level level1 = GetLevelByName(doc, levelName);

            Duct.Create(doc, ductSystemType1.Id, ductTypes.FirstElementId(), level1.Id, startPoint, endPoint);

            return null;
        }

        internal Pipe CreatePipeByTypeAndLevelNames(Document doc, string systemTypeName, string levelName, XYZ startPoint, XYZ endPoint)
        {
            MEPSystemType pipeSystemType1 = GetSystemTypeByName(doc, systemTypeName);

            FilteredElementCollector pipeTypes = new FilteredElementCollector(doc);
            pipeTypes.OfClass(typeof(PipeType));

            Level level1 = GetLevelByName(doc, levelName);

            Pipe.Create(doc, pipeSystemType1.Id, pipeTypes.FirstElementId(), level1.Id, startPoint, endPoint);

            return null;
        }


        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "Button 1";

            ButtonDataClass myButtonData1 = new ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.Blue_32,
                Properties.Resources.Blue_16,
                "This is a tooltip for Button 1");

            return myButtonData1.Data;
        }
    }
}

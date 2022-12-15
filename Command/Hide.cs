using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Collections;

namespace cityjsonToRevit
{
    [TransactionAttribute(TransactionMode.Manual)]
    class Hide : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            Autodesk.Revit.DB.View actView = doc.ActiveView;


            using (Transaction trans = new Transaction(doc, "Hide Unhide CJ"))
            {
                trans.Start();
                ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_GenericModel);
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ICollection<ElementId> elemIds = collector.WherePasses(filter).WhereElementIsNotElementType().ToElementIds();
                List<ElementId> hlist = new List<ElementId>();
                List<ElementId> uhlist = new List<ElementId>();

                foreach (ElementId eid in elemIds)
                {
                    Element elem = doc.GetElement(eid);
                    Parameter para = elem.GetParameters("Object Name").First();

                    if (elem.IsHidden(actView) && para.HasValue)
                    {
                        uhlist.Add(eid);
                    }
                    else if (!elem.IsHidden(actView) && para.HasValue)
                    {
                        hlist.Add(eid);
                    }
                }

                if (hlist.Count==0 && uhlist.Count == 0)
                {
                    TaskDialog.Show("Import CityJSON file", "There is no CityJSON geometry loaded.\n");
                    trans.RollBack();
                    return Result.Failed;
                }

                ICollection<ElementId> uhcol = uhlist;
                ICollection<ElementId> hcol = hlist;

                if(hcol.Count!= 0)
                actView.HideElements(hcol);
                if (uhcol.Count != 0)
                actView.UnhideElements(uhcol);

                trans.Commit();

                return Result.Succeeded;
            }
        }
    }
}

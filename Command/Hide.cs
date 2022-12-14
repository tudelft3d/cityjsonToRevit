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
                    if (doc.GetElement(eid).IsHidden(actView))
                    {
                        uhlist.Add(eid);
                    }
                    else
                    {
                        hlist.Add(eid);
                    }
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

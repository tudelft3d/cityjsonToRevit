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
    [Transaction(TransactionMode.Manual)]
    class DbRunner : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;


            using (Transaction trans = new Transaction(doc, "Importer"))
            {
                trans.Start();

                trans.Commit();

                return Result.Succeeded;
            }
        }
    }
}

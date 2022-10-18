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
using System.Windows.Forms;
using Newtonsoft.Json;
using Autodesk.Revit.Creation;
using Document = Autodesk.Revit.DB.Document;
using System.Xml.Linq;
using Autodesk.Revit.DB.Architecture;

namespace cityjsonToRevit
{
    [Transaction(TransactionMode.Manual)]
    class DbRunner : IExternalCommand
    {
        static public bool CheckValidity(dynamic file)
        {
            if (file.CityObjects == null || file.type != "CityJSON" || file.version == null ||
                file.transform == null || file.transform.scale == null || file.transform.translate == null ||
                file.vertices == null)
            {
                return false;
            }
            else if (file.version != "1.1" && file.version != "1.0")
            {
                return false;
            }
            return true;
        }
        public void CreateTessellatedShape(Autodesk.Revit.DB.Document doc, ElementId materialId)
        {
            List<XYZ> loopVertices = new List<XYZ>(4);

            TessellatedShapeBuilder builder = new TessellatedShapeBuilder();

            builder.OpenConnectedFaceSet(true);
            // create a pyramid with a square base 4' x 4' and 5' high
            double length = 4.0;
            double height = 5.0;

            XYZ basePt1 = XYZ.Zero;
            XYZ basePt2 = new XYZ(length, 0, 0);
            XYZ basePt3 = new XYZ(length, length, 0);
            XYZ basePt4 = new XYZ(0, length, 0);
            XYZ apex = new XYZ(length / 2, length / 2, height);

            loopVertices.Add(basePt1);
            loopVertices.Add(basePt2);
            loopVertices.Add(basePt3);
            loopVertices.Add(basePt4);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(basePt1);
            loopVertices.Add(apex);
            loopVertices.Add(basePt2);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(basePt2);
            loopVertices.Add(apex);
            loopVertices.Add(basePt3);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(basePt3);
            loopVertices.Add(apex);
            loopVertices.Add(basePt4);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(basePt4);
            loopVertices.Add(apex);
            loopVertices.Add(basePt1);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            builder.CloseConnectedFaceSet();
            builder.Target = TessellatedShapeBuilderTarget.Solid;
            builder.Fallback = TessellatedShapeBuilderFallback.Abort;
            builder.Build();

            TessellatedShapeBuilderResult result = builder.GetBuildResult();

      

                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.ApplicationId = "Application id";
                ds.ApplicationDataId = "Geometry object id";

                ds.SetShape(result.GetGeometricalObjects());
            
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;


            using (Transaction trans = new Transaction(doc, "Importer"))
            {
                trans.Start();

                var fileContent = string.Empty;
                var filePath = string.Empty;

                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Open CityJSON file";
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = "JSON files (*.JSON)|*.JSON";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        //Get the path of specified file
                        filePath = openFileDialog.FileName;

                        //Read the contents of the file into a stream
                        var fileStream = openFileDialog.OpenFile();

                        using (StreamReader reader = new StreamReader(fileStream))
                        {
                            string json = reader.ReadToEnd();
                            dynamic array = JsonConvert.DeserializeObject(json);
                            foreach (var item in array)
                            {
                                Console.WriteLine("{0}", item.Name);
                            }
                        }
                    }
                }
                //Selecting Default Material for shape creation


                FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Material));

                IEnumerable<Material> materialsEnum
                  = collector.ToElements().Cast<Material>().Where(e => e.Name == "Default");
                Material materialDef = materialsEnum.First();



                CreateTessellatedShape(doc, materialDef.Id);


                //MessageBox.Show(fileContent, "File Content at path: " + filePath, MessageBoxButtons.OK);
                trans.Commit();

                return Result.Succeeded;
            }
        }
    }
}

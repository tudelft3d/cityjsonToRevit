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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace cityjsonToRevit
{

    [Transaction(TransactionMode.Manual)]
    class DbRunner : IExternalCommand
    {
        internal System.Windows.Forms.ComboBox ComboBox1;

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
        public string lodSelecter(dynamic cityJ)
        {
            string level = "";
            List<string> lods = new List<string>();
            foreach (var objects in cityJ.CityObjects)
            {
                foreach(var obj in objects)
                {
                    foreach(var boundaryGroup in obj.geometry)
                    {
                        string lod = (string)boundaryGroup.lod;
                        lods.Add(lod);
                    }
                }
            }
            lods = lods.Distinct().ToList();
            if (lods.Count == 0)
            {
                return lods.First();

            }
            else
            {
                using (lodUserSelect loder = new lodUserSelect(lods))
                {
                    loder.ShowDialog();
                    level = loder._level;
                }
                return level;
            }
                  
        }



        public void CreateTessellatedShape(Autodesk.Revit.DB.Document doc, ElementId materialId, dynamic cityObjProp, List<XYZ> verticesList, string Namer, string Lod)
        {
            List<XYZ> loopVertices = new List<XYZ>();
            TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
            foreach (var boundaryGroup in cityObjProp.geometry)
            {
                if (Lod == (string)boundaryGroup.lod) 
                {
                    builder.OpenConnectedFaceSet(false);
                    foreach (var boundary in boundaryGroup.boundaries)
                    {
                        loopVertices.Clear();
                        foreach (var facePoints in boundary)
                        {
                            bool levelCheck = false;
                            foreach (var facePoint in facePoints)
                            {

                                if (!facePoint.HasValues)
                                {
                                    int VV = unchecked((int)facePoint.Value);
                                    XYZ vertPoint = new XYZ(verticesList[VV].X, verticesList[VV].Y, verticesList[VV].Z);
                                    loopVertices.Add(vertPoint);
                                }
                                //else if ((int)boundaryGroup.lod == 1.2)
                                //{
                                //    foreach(var fp in facePoint)
                                //    {
                                //        int VV = unchecked((int)fp.Value);
                                //        XYZ vertPoint = new XYZ(verticesList[VV].X, verticesList[VV].Y, verticesList[VV].Z);
                                //        loopVertices.Add(vertPoint);
                                //    }

                                //}
                                else
                                {
                                    foreach (var fp in facePoint)
                                    {
                                        int VV = unchecked((int)fp.Value);
                                        XYZ vertPoint = new XYZ(verticesList[VV].X, verticesList[VV].Y, verticesList[VV].Z);
                                        loopVertices.Add(vertPoint);
                                    }
                                    builder.AddFace(new TessellatedFace(loopVertices, materialId));
                                    levelCheck = true;
                                    loopVertices.Clear();
                                }

                            }
                            if (!levelCheck)
                            {
                                builder.AddFace(new TessellatedFace(loopVertices, materialId));
                            }


                        }
                    }
                    builder.CloseConnectedFaceSet();
                    builder.Target = TessellatedShapeBuilderTarget.AnyGeometry;
                    builder.Fallback = TessellatedShapeBuilderFallback.Mesh;
                    builder.Build();
                    builder.Clear();
                    TessellatedShapeBuilderResult result = builder.GetBuildResult();
                    string lod = (string)boundaryGroup.lod;

                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                    ds.ApplicationId = "Application id";
                    ds.ApplicationDataId = "Geometry object id";
                    ds.Name = Namer + "-lod " + lod;
                    ds.SetShape(result.GetGeometricalObjects());
                }

            }            
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            //Selecting Default Material for shape creation


            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Material));

            IEnumerable<Material> materialsEnum
              = collector.ToElements().Cast<Material>().Where(e => e.Name == "Default");
            Material materialDef = materialsEnum.First();


            using (Transaction trans = new Transaction(doc, "Load CityJSON"))
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
                            dynamic jCity = JsonConvert.DeserializeObject(json);
                            List<XYZ> vertList = new List<XYZ>();
                            foreach (var vertex in jCity.vertices)
                            {
                                double x = vertex[0];
                                double y = vertex[1];
                                double z = vertex[2];
                                XYZ vert = new XYZ(x, y, z);
                                vertList.Add(vert);
                            }
                            string lodSpec = lodSelecter(jCity);
                            foreach (var objects in jCity.CityObjects)
                            {
                                foreach(var objProperties in  objects)
                                {
                                        string attributeName = objects.Name;
                                        CreateTessellatedShape(doc, materialDef.Id, objProperties, vertList, attributeName, lodSpec);
                                }
                                
                            }
                                TaskDialog.Show("Good!", "All set! Let's Go!\n");
                        }
                    }
                }



                //CreateTessellatedShape(doc, materialDef.Id);


                //MessageBox.Show(fileContent, "File Content at path: " + filePath, MessageBoxButtons.OK);
                trans.Commit();

                return Result.Succeeded;
            }
        }
    }
}

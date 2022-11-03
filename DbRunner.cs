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
using Newtonsoft.Json.Linq;
using DotSpatial.Projections;
using Autodesk.Revit.DB.Visual;

namespace cityjsonToRevit
{

    [Transaction(TransactionMode.Manual)]
    class DbRunner : IExternalCommand
    {
        public int espgNum(dynamic cityJ)
        {
            string espg = unchecked((string)cityJ.metadata.referenceSystem);
            int found = espg.LastIndexOf(":");
            if (found == 0)
            {
                found = espg.LastIndexOf("/");
            }
            espg = espg.Substring(found + 1);
            int espgNo = Int32.Parse(espg);
            return espgNo;
        }
        public List<double> ShowActiveProjectLocationUsage(Autodesk.Revit.DB.Document document)
        {
            List<double> coord = new List<double>();
            // Get the project location handle 
            ProjectLocation projectLocation = document.ActiveProjectLocation;

            // Show the information of current project location
            XYZ origin = new XYZ(0, 0, 0);
            ProjectPosition position = projectLocation.GetProjectPosition(origin);
            if (null == position)
            {
                throw new Exception("No project position in origin point.");
            }

            // Format the prompt string to show the message.
            String prompt = "Current project location information:\n";
            prompt += "\n\t" + "Origin point position:";
            prompt += "\n\t\t" + "Angle: " + position.Angle;
            prompt += "\n\t\t" + "East to West offset: " + position.EastWest;
            prompt += "\n\t\t" + "Elevation: " + position.Elevation;
            prompt += "\n\t\t" + "North to South offset: " + position.NorthSouth;

            // Angles are in radians when coming from Revit API, so we 
            // convert to degrees for display
            const double angleRatio = Math.PI / 180;   // angle conversion factor

            SiteLocation site = projectLocation.GetSiteLocation();
            double latDeg = site.Latitude / angleRatio;
            double lonDeg = site.Longitude / angleRatio;
            double ylatDeg = latDeg * (10000 * 1000 / 90);
            double xlonDeg = lonDeg * (10000 * 1000 / 90);

            prompt += "\n\t" + "Site location:";
            prompt += "\n\t\t" + "Latitude: " + latDeg + "°";
            prompt += "\n\t\t" + ylatDeg + " meters";
            prompt += "\n\t\t" + "Longitude: " + lonDeg + "°";
            prompt += "\n\t\t" + xlonDeg + " meters";
            prompt += "\n\t\t" + "TimeZone: " + site.TimeZone;
            coord.Add(xlonDeg);
            coord.Add(ylatDeg);
            // Give the user some information
            TaskDialog.Show("Revit", prompt);
            return coord;
        }
        public void UpdateSiteLocation(Document document, dynamic cityJ)
        {
            const double angleRatio = Math.PI / 180;
            SiteLocation site = document.ActiveProjectLocation.GetSiteLocation();
            ProjectionInfo pStart = ProjectionInfo.FromEpsgCode(espgNum(cityJ));
            ProjectionInfo pEnd = ProjectionInfo.FromEpsgCode(4326);
            double[] xy = { cityJ.transform.translate[0], cityJ.transform.translate[1] };
            double[] z = { 0 };
            Reproject.ReprojectPoints(xy, z, pStart, pEnd, 0, 1);
            site.Latitude = xy[1] * angleRatio;
            site.Longitude = xy[0] * angleRatio;

        }
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

                foreach (var obj in objects)
                {
                    if (obj.geometry == null)
                    {
                        continue;
                    }
                    foreach (var boundaryGroup in obj.geometry)
                    {
                        string lod = (string)boundaryGroup.lod;
                        lods.Add(lod);
                    }
                }
            }
            lods = lods.Distinct().ToList();
            if (lods.Count == 1)
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
            if (cityObjProp.geometry == null)
            {
                return;
            }
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
         public List<Material> matGenerator(Document doc)
         {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Material));
            IEnumerable<Material> materialsEnum
              = collector.ToElements().Cast<Material>().Where(e => e.Name == "Default");
            Material materialDef = materialsEnum.First();
            IEnumerable<Material> checkMat
              = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Building");
            List<Material> mats = new List<Material>();
            if (!checkMat.Any())
            {
                using (Transaction t = new Transaction(doc, "Set CJ Materials"))
                {
                    t.Start();
                    Material cj00 = materialDef.Duplicate("cj-Building");
                    cj00.Color = new Color(119, 136, 153);
                    mats.Add(cj00);
                    Material cj01 = materialDef.Duplicate("cj-Bridge");
                    cj01.Color = new Color(160, 82, 45);
                    mats.Add(cj01);
                    Material cj02 = materialDef.Duplicate("cj-Group");
                    cj02.Color = new Color(218, 165, 32);
                    mats.Add(cj02);
                    Material cj03 = materialDef.Duplicate("cj-Furniture");
                    cj03.Color = new Color(255, 69, 0);
                    mats.Add(cj03);
                    Material cj04 = materialDef.Duplicate("cj-Landuse");
                    cj04.Color = new Color(218, 165, 32);
                    mats.Add(cj04);
                    Material cj05 = materialDef.Duplicate("cj-Plants");
                    cj05.Color = new Color(0, 204, 0);
                    mats.Add(cj05);
                    Material cj06 = materialDef.Duplicate("cj-Railway");
                    cj06.Color = new Color(0, 0, 0);
                    mats.Add(cj06);
                    Material cj07 = materialDef.Duplicate("cj-Road");
                    cj07.Color = new Color(64, 64, 64);
                    mats.Add(cj07);
                    Material cj08 = materialDef.Duplicate("cj-Tunnel");
                    cj08.Color = new Color(51, 25, 0);
                    mats.Add(cj08);
                    Material cj09 = materialDef.Duplicate("cj-Water");
                    cj09.Color = new Color(0, 128, 255);
                    mats.Add(cj09);

                    foreach (Material m in mats)
                    {
                        ElementId appearanceAssetId = m.AppearanceAssetId;
                        AppearanceAssetElement assetElem = m.Document.GetElement(appearanceAssetId) as AppearanceAssetElement;
                        ElementId duplicateAssetElementId = ElementId.InvalidElementId;
                        AppearanceAssetElement duplicateAssetElement = assetElem.Duplicate(m.Name);
                        m.AppearanceAssetId = duplicateAssetElement.Id;
                        duplicateAssetElementId = duplicateAssetElement.Id;
                        using (AppearanceAssetEditScope editScope = new AppearanceAssetEditScope(assetElem.Document))
                        {
                            Asset editableAsset = editScope.Start(duplicateAssetElementId);
                            AssetPropertyDoubleArray4d genericDiffuseProperty = editableAsset.FindByName("generic_diffuse") as AssetPropertyDoubleArray4d;
                            genericDiffuseProperty.SetValueAsColor(m.Color);
                            editScope.Commit(true);
                        }
                    }
                    t.Commit();
                }
            }
            return mats;
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

            List<Material> materials = matGenerator(doc);
            //starting transaction
            using (Transaction trans = new Transaction(doc, "Load CityJSON"))
            {
                trans.Start();
                var fileContent = string.Empty;
                var filePath = string.Empty;
                List<double> coord = ShowActiveProjectLocationUsage(doc);
                XYZ BaseP = BasePoint.GetProjectBasePoint(doc).Position;
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
                            ProjectionInfo pStart = ProjectionInfo.FromEpsgCode(espgNum(jCity));
                            ProjectionInfo pEnd = ProjectionInfo.FromEpsgCode(4326);
                            //const double angleRatio = Math.PI / 180;
                            List<XYZ> vertList = new List<XYZ>();
                            //bool newLocation = true;
                            //if (newLocation)
                            //{
                            foreach (var vertex in jCity.vertices)
                            {
                                double x = vertex[0] * jCity.transform.scale[0];
                                //+ jCity.transform.translate[0] - coord[0];
                                double y = vertex[1] * jCity.transform.scale[1];
                                double z = vertex[2] * jCity.transform.scale[2];
                                double xx = UnitUtils.ConvertToInternalUnits(x, UnitTypeId.Meters);
                                double yy = UnitUtils.ConvertToInternalUnits(y, UnitTypeId.Meters);
                                double zz = UnitUtils.ConvertToInternalUnits(z, UnitTypeId.Meters);
                                XYZ vert = new XYZ(xx, yy, zz);
                                vertList.Add(vert);
                            }
                            UpdateSiteLocation(doc, jCity);
                            //}
                            //else
                            //{
                            //    foreach (var vertex in jCity.vertices)
                            //    {
                            //        double x = vertex[0] * jCity.transform.scale[0] + jCity.transform.translate[0];
                            //        double y = vertex[1] * jCity.transform.scale[1] + jCity.transform.translate[1];
                            //        double z = vertex[2] * jCity.transform.scale[2] + jCity.transform.translate[2];
                            //        double[] xy = { x, y };
                            //        double[] zy = { 0 };
                            //        Reproject.ReprojectPoints(xy, zy, pStart, pEnd, 0, 1);
                            //        SiteLocation site = doc.ActiveProjectLocation.GetSiteLocation();
                            //        ;
                            //        double xDeg = xy[0] - site.Longitude / angleRatio;
                            //        double yDeg = xy[1] - site.Latitude / angleRatio;

                            //        double xx = UnitUtils.ConvertToInternalUnits(xy[0] * (10000 * 1000 / 90), UnitTypeId.Meters);
                            //        double yy = UnitUtils.ConvertToInternalUnits(xy[1] * (10000 * 1000 / 90), UnitTypeId.Meters);
                            //        double zz = UnitUtils.ConvertToInternalUnits(z, UnitTypeId.Meters);
                            //        XYZ vert = new XYZ(xx, yy, zz);
                            //        vertList.Add(vert);
                            //    }
                            //}

                            string lodSpec = lodSelecter(jCity);
                            foreach (var objects in jCity.CityObjects)
                            {
                                foreach (var objProperties in objects)
                                {
                                    string attributeName = objects.Name;
                                    CreateTessellatedShape(doc, materialDef.Id, objProperties, vertList, attributeName, lodSpec);
                                }
                            }
                            TaskDialog.Show("Good!", "All set! Let's Go!\n");
                        }
                    }
                }
                trans.Commit();
            }
            return Result.Succeeded;
        }
    }
}

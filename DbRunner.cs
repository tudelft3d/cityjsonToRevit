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
        const double angleRatio = Math.PI / 180;
        public static double distanceBetweenPlaces(double lon1, double lat1, double lon2, double lat2)
        {
            double R = 6371000; // meter

            double sLat1 = Math.Sin(lat1);
            double sLat2 = Math.Sin(lat2);
            double cLat1 = Math.Cos(lat1);
            double cLat2 = Math.Cos(lat2);
            double cLon = Math.Cos(lon1 - lon2);

            double cosD = sLat1 * sLat2 + cLat1 * cLat2 * cLon;

            double d = Math.Acos(cosD);

            double dist = R * d;

            return dist;
        }

        public int epsgNum(dynamic cityJ)
        {
            string espg = unchecked((string)cityJ.metadata.referenceSystem);
            int found = espg.LastIndexOf("/");
            if (found == -1)
            {
                found = espg.LastIndexOf(":");
            }
            espg = espg.Substring(found + 1);
            int espgNo = Int32.Parse(espg);
            return espgNo;
        }

        private List<double> ShowActiveProjectLocationUsage(Autodesk.Revit.DB.Document document)
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
            SiteLocation site = projectLocation.GetSiteLocation();
            double latDeg = site.Latitude / angleRatio;
            double lonDeg = site.Longitude / angleRatio;
            double distance = distanceBetweenPlaces(site.Longitude, site.Latitude, 0, 0);
            double xdistance = distanceBetweenPlaces(site.Longitude, site.Latitude, 0, site.Latitude);
            double ydistance = distanceBetweenPlaces(site.Longitude, site.Latitude, site.Longitude, 0);

            prompt += "\n\t" + "Site location:";
            prompt += "\n\t\t" + "Latitude: " + latDeg + "°";
            prompt += "\n\t\t" + "y distance to zero zero  " + ydistance + " meters";

            prompt += "\n\t\t" + "Longitude: " + lonDeg + "°";
            prompt += "\n\t\t" + "x distance to zero zero  " + xdistance + " meters";

            prompt += "\n\t\t" +"overal distance to zero zero  "+ distance + " meters";
            prompt += "\n\t\t" + "TimeZone: " + site.TimeZone;
            coord.Add(latDeg);
            coord.Add(lonDeg);
            // Give the user some information
            TaskDialog.Show("Revit", prompt);
            return coord;
        }
        private void PointProjector(int number, double[] xy)
        {
            ProjectionInfo pStart = ProjectionInfo.FromEpsgCode(number);
            ProjectionInfo pEnd = ProjectionInfo.FromEpsgCode(4326);
            double[] z = { 0 };
            Reproject.ReprojectPoints(xy, z, pStart, pEnd, 0, 1);
            return;
        }

        private void PointProjectorRev(int number, double[] xy)
        {
            ProjectionInfo pEnd = ProjectionInfo.FromEpsgCode(number);
            ProjectionInfo pStart = ProjectionInfo.FromEpsgCode(4326);
            double[] z = { 0 };
            Reproject.ReprojectPoints(xy, z, pStart, pEnd, 0, 1);
            return;
        }

        private void UpdateSiteLocation(Document document, dynamic cityJ)
        {
            const double angleRatio = Math.PI / 180;
            SiteLocation site = document.ActiveProjectLocation.GetSiteLocation();
            int espgNo = epsgNum(cityJ);
            double[] xy = { cityJ.transform.translate[0], cityJ.transform.translate[1] };
            PointProjector(espgNo, xy);
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
        private string lodSelecter(dynamic cityJ)
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
        private void CreateTessellatedShape(Autodesk.Revit.DB.Document doc, ElementId materialId, dynamic cityObjProp, List<XYZ> verticesList, string Namer, string Lod)
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
        private List<Material> matGenerator(Document doc)
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
            else
            {
                Material m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Building").First();
                mats.Add(m);
                m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Bridge").First();
                mats.Add(m);
                m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Group").First();
                mats.Add(m);
                m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Furniture").First();
                mats.Add(m);
                m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Landuse").First();
                mats.Add(m);
                m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Plants").First();
                mats.Add(m);
                m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Railway").First();
                mats.Add(m);
                m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Road").First();
                mats.Add(m);
                m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Tunnel").First();
                mats.Add(m);
                m = collector.ToElements().Cast<Material>().Where(e => e.Name == "cj-Water").First();
                mats.Add(m);
            }
            return mats;
         }

        private Material matSelector(List<Material> materials, string type, Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Material));
            IEnumerable<Material> materialsEnum
              = collector.ToElements().Cast<Material>().Where(e => e.Name == "Default");
            Material m = materialsEnum.First();
            switch (type) 
            {
                case "Building": m = materials[0]; break;
                case "BuildingPart": m = materials[0]; break;
                case "BuildingInstallation": m = materials[0]; break;
                case "BuildingConstructiveElement": m = materials[0]; break;
                case "BuildingFurniture": m = materials[0]; break;
                case "BuildingStorey": m = materials[0]; break;
                case "BuildingRoom": m = materials[0]; break;
                case "BuildingUnit": m = materials[0]; break;
                case "Bridge": m = materials[1]; break;
                case "BridgePart": m = materials[1]; break;
                case "BridgeInstallation": m = materials[1]; break;
                case "BridgeConstructiveElement": m = materials[1]; break;
                case "BridgeRoom": m = materials[1]; break;
                case "BridgeFurniture": m = materials[1]; break;
                case "CityObjectGroup": m = materials[2]; break;
                case "CityFurniture": m = materials[3]; break;
                case "LandUse": m = materials[4]; break;
                case "PlantCover": m = materials[5]; break;
                case "SolitaryVegetationObject": m = materials[5]; break;
                case "Railway": m = materials[6]; break;
                case "Road": m = materials[7]; break;
                case "TransportSquare": m = materials[7]; break;
                case "Waterway": m = materials[9]; break;
                case "Tunnel": m = materials[8]; break;
                case "TunnelPart": m = materials[8]; break;
                case "TunnelInstallation": m = materials[8]; break;
                case "TunnelConstructiveElement": m = materials[8]; break;
                case "TunnelHollowSpace": m = materials[8]; break;
                case "TunnelFurniture": m = materials[8]; break;
                case "WaterBody": m = materials[9]; break;
                default: break;
            }
            return m;
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
                            int espgNo = epsgNum(jCity);
                            //const double angleRatio = Math.PI / 180;

                            bool newLocation = false;
                            //if (newLocation)
                            //{
                            SiteLocation site = doc.ActiveProjectLocation.GetSiteLocation();
                            double latDeg = site.Latitude / angleRatio;
                            double lonDeg = site.Longitude / angleRatio;

                            double[] xy = { jCity.transform.translate[0], jCity.transform.translate[1] };
                            PointProjector(espgNo, xy);
                            double cjLat = xy[1];
                            double cjLon = xy[0];

                            //User selects to update or choose the revit origin
                            using (mapViewer mpv = new mapViewer(latDeg, lonDeg, cjLat, cjLon))
                            {
                                mpv.ShowDialog();
                                newLocation = mpv._loc;
                            }

                            List<XYZ> vertList = new List<XYZ>();
                            switch (newLocation)
                            {
                                case true:
                                    UpdateSiteLocation(doc, jCity);
                                    foreach (var vertex in jCity.vertices)
                                    {
                                        double x = vertex[0] * jCity.transform.scale[0];
                                        double y = vertex[1] * jCity.transform.scale[1];
                                        double z = vertex[2] * jCity.transform.scale[2];
                                        double xx = UnitUtils.ConvertToInternalUnits(x, UnitTypeId.Meters);
                                        double yy = UnitUtils.ConvertToInternalUnits(y, UnitTypeId.Meters);
                                        double zz = UnitUtils.ConvertToInternalUnits(z, UnitTypeId.Meters);
                                        XYZ vert = new XYZ(xx, yy, zz);
                                        vertList.Add(vert);
                                    }
                                    break;
                                default:
                                    double[] tranC = { jCity.transform.translate[0], jCity.transform.translate[1] };
                                    double[] tranR = { lonDeg, latDeg };
                                    PointProjectorRev(espgNo, tranR);
                                    double tranx = tranC[0] - tranR[0];
                                    double trany = tranC[1] - tranR[1];
                                    foreach (var vertex in jCity.vertices)
                                    {
                                        double x = (vertex[0] * jCity.transform.scale[0]) + tranx;
                                        double y = (vertex[1] * jCity.transform.scale[1]) + trany;
                                        double z = vertex[2] * jCity.transform.scale[2];
                                        double xx = UnitUtils.ConvertToInternalUnits(x, UnitTypeId.Meters);
                                        double yy = UnitUtils.ConvertToInternalUnits(y, UnitTypeId.Meters);
                                        double zz = UnitUtils.ConvertToInternalUnits(z, UnitTypeId.Meters);
                                        XYZ vert = new XYZ(xx, yy, zz);
                                        vertList.Add(vert);
                                    }

                                    break;
                            }

                            string lodSpec = lodSelecter(jCity);
                            foreach (var objects in jCity.CityObjects)
                            {
                                foreach (var objProperties in objects)
                                {
                                    string attributeName = objects.Name;
                                    string objType = unchecked((string)objProperties.type);
                                    Material mat = matSelector(materials, objType, doc);
                                    CreateTessellatedShape(doc, mat.Id, objProperties, vertList, attributeName, lodSpec);
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

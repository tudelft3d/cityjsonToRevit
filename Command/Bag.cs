using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GMap.NET;
using GMap.NET.MapProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Document = Autodesk.Revit.DB.Document;

namespace cityjsonToRevit
{
    [Transaction(TransactionMode.Manual)]

    class Bag : IExternalCommand
    {
        public List<string> Tiles(string url)
        {
            List<string> tileNums = new List<string>();
            try
            {
                // Create an HttpClient and send the request
                WebClient client = new WebClient();
                string response = client.DownloadString(url);
                dynamic responseJson = JsonConvert.DeserializeObject(response);
                foreach (var feature in responseJson.features)
                {
                    tileNums.Add(feature.properties.tile_id.ToString());
                }
            }

            catch
            {
                TaskDialog.Show("Error", "An error occurred while trying to download the files. Please check your internet connection and try again. ");
            }
            return tileNums;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            UIApplication uiapp = commandData.Application;
            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Performing on family document", "The plugin should run on project documents.\n");
                return Result.Failed;
            }


            SiteLocation site = doc.ActiveProjectLocation.GetSiteLocation();
            double latDeg = site.Latitude / Program.angleRatio;
            double lonDeg = site.Longitude / Program.angleRatio;
            PointLatLng point = new PointLatLng(latDeg, lonDeg);
            GeoCoderStatusCode geoCoder = GeoCoderStatusCode.Unknow;
            Placemark? placemark = GMapProviders.OpenStreetMap.GetPlacemark(point, out geoCoder);
            if (placemark?.CountryName != "Nederland")
            {
                TaskDialog.Show("Site Loaction out of the Netherlands", "3D BAG service is currently available inside the Netherlands. Please update site location and run the plugin again.");
                return Result.Failed;
            }
            double boxlength = -1;
            using (Command.BagMap bm = new Command.BagMap(latDeg, lonDeg))
            {
                bm.ShowDialog();
                boxlength = bm.side;
            }
            if (boxlength == -1)
            {
                return Result.Failed;
            }
            List<string> tileNums = Tiles("https://data.3dbag.nl/api/BAG3D_v2/wfs?&request=GetFeature&typeName=AG3D_v2:bag_tiles_3k&outputFormat=json&bbox=" + boundingb(latDeg, lonDeg, boxlength));
            if (tileNums.Count == 0)
                return Result.Failed;
            string cjUrl = "https://data.3dbag.nl/cityjson/v210908_fd2cee53/3dbag_v210908_fd2cee53_";
            string lodSpec = lodBagSelecter();
            if (lodSpec == "")
            {
                return Result.Failed;
            }
            List<Material> materials = Program.matGenerator(doc);

            boxlength = UnitUtils.ConvertToInternalUnits(boxlength, UnitTypeId.Meters);

            using (Transaction tran = new Transaction(doc, "Build 3D BAG Tiles"))
            {
                tran.Start();
                FilteredElementCollector matcollector = new FilteredElementCollector(doc).OfClass(typeof(Material));
                Material matDef
                  = matcollector.ToElements().Cast<Material>().FirstOrDefault(e => e.Name == "cj-Default");

                foreach (string tileNum in tileNums)
                {
                    string cjUrlAll = cjUrl + tileNum + ".json" + ".gz";

                    string gzFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\TEMP\\" + tileNum + ".gz";
                    if (!File.Exists(gzFile))
                    {
                        try
                        {
                            using (var client2 = new WebClient())
                            {
                                client2.DownloadFile(cjUrlAll, gzFile);
                            }
                        }
                        catch
                        {
                            TaskDialog.Show("Error", "An error occurred while trying to download the files. Please check your internet connection and try again. ");
                            tran.RollBack();
                            return Result.Failed;
                        }

                    }
                    using (FileStream fileToDecompressAsStream = new FileStream(gzFile, FileMode.Open))
                    using (GZipStream decompressionStream = new GZipStream(fileToDecompressAsStream, CompressionMode.Decompress))
                    using (StreamReader sr = new StreamReader(decompressionStream))
                    {
                        string json = sr.ReadToEnd();
                        dynamic jCity = JsonConvert.DeserializeObject(json);
                        int epsgNo = Program.epsgNum(jCity);
                        double[] tranC = { jCity.transform.translate[0], jCity.transform.translate[1] };
                        double[] tranR = { lonDeg, latDeg };
                        Program.PointProjectorRev(epsgNo, tranR);
                        double tranx = tranC[0] - tranR[0];
                        double trany = tranC[1] - tranR[1];
                        List<XYZ> vertList = Program.vertBuilder(jCity, tranx, trany).Item1;
                        List<bool> tags = inBB(vertList, boxlength);

                        List<string> paramets = Program.paramFinder(jCity);
                        Program.paramMaker(uiapp, Program.paramFinder(jCity));
                        Dictionary<string, dynamic> semanticParentInfo = new Dictionary<string, dynamic>();


                        foreach (var objects in jCity.CityObjects)
                        {
                            foreach (var objProperties in objects)
                            {
                                var attributes = objProperties.attributes;
                                var children = objProperties.children;
                                if (children != null && attributes != null)
                                {
                                    foreach (string child in children)
                                    {
                                        semanticParentInfo.Add(child, attributes);
                                    }
                                }

                            }
                        }
                        ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_GenericModel);
                        FilteredElementCollector collector = new FilteredElementCollector(doc);


                        foreach (var objects in jCity.CityObjects)
                        {
                            foreach (var objProperties in objects)
                            {

                                if (tagCheck(objProperties, lodSpec, tags))
                                {
                                    bool exist = collector.OfCategory(BuiltInCategory.OST_GenericModel).WhereElementIsNotElementType().ToElements().Any(e => e.Name == objects.Name + "-lod " + lodSpec);

                                    if (!exist)
                                    {
                                        string attributeName = objects.Name;
                                        string objType = unchecked((string)objProperties.type);
                                        Material mat = Program.matSelector(matDef, materials, objType, doc);
                                        Program.CreateTessellatedShape(doc, mat.Id, objProperties, vertList, attributeName, lodSpec, paramets, semanticParentInfo);
                                    }
                                }
                                    

                            }
                        }
                    }
                }
                double bminus = boxlength * (-1);
                XYZ a1 = new XYZ(boxlength, boxlength, 0);
                XYZ a2 = new XYZ(bminus, boxlength, 0);
                XYZ a3 = new XYZ(bminus, bminus, 0);
                XYZ a4 = new XYZ(boxlength, bminus, 0);
                Line l1 = Line.CreateBound(a1, a2);
                Line l2 = Line.CreateBound(a2, a3);
                Line l3 = Line.CreateBound(a3, a4);
                Line l4 = Line.CreateBound(a4, a1);
                XYZ origin = new XYZ(0, 0, 0);
                XYZ normal = new XYZ(0, 0, 1);
                Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);
                SketchPlane sketch = SketchPlane.Create(doc, geomPlane);
                ModelLine line1 = doc.Create.NewModelCurve(l1, sketch) as ModelLine;
                ModelLine line2 = doc.Create.NewModelCurve(l2, sketch) as ModelLine;
                ModelLine line3 = doc.Create.NewModelCurve(l3, sketch) as ModelLine;
                ModelLine line4 = doc.Create.NewModelCurve(l4, sketch) as ModelLine;
                tran.Commit();
            }
            return Result.Succeeded;

        }
        private string boundingb(double lat, double lon, double a)
        {
            double[] xy = { lon, lat };
            Program.PointProjectorRev(28992, xy);
            double xmax = xy[0] + a;
            double ymax = xy[1] + a;
            double xmin = xy[0] - a;
            double ymin = xy[1] - a;
            string box = xmin.ToString() + "," + ymin.ToString() + "," + xmax.ToString() + "," + ymax.ToString();
            return box;
        }
        private string lodBagSelecter()
        {
            string level = "";
            List<string> lods = new List<string> { "0", "1.2", "1.3", "2.2" };
            using (lodUserSelect loder = new lodUserSelect(lods))
            {
                loder.ShowDialog();
                level = loder._level;
            }
            return level;
        }
        private List<bool> inBB(List<XYZ> vertList, double range)
        {
            List<bool> tags = new List<bool>();
            foreach (XYZ xyz in vertList)
            {
                if (Math.Abs(xyz.X) < range && Math.Abs(xyz.Y) < range)
                    tags.Add(true);
                else
                    tags.Add(false);
            }
            return tags;
        }
        private bool tagCheck(dynamic cityObjProp, string Lod, List<bool> tgs)
        {
            if (cityObjProp.geometry == null)
            {
                return false;
            }
            foreach (var boundaryGroup in cityObjProp.geometry)
            {

                if (Lod == (string)boundaryGroup.lod)
                {
                    foreach (var boundary in boundaryGroup.boundaries)
                    {
                        foreach (var facePoints in boundary)
                        {
                            //bool levelCheck = false;
                            foreach (var facePoint in facePoints)
                            {

                                if (!facePoint.HasValues)
                                {
                                    int VV = unchecked((int)facePoint.Value);
                                    if (tgs[VV])
                                        return true;
                                }
                                else
                                {
                                    foreach (var fp in facePoint)
                                    {
                                        int VV = unchecked((int)fp.Value);
                                        if (tgs[VV])
                                            return true;
                                    }
                                }
                            }

                        }
                    }
                }
            }
            return false;
        }
    }
}

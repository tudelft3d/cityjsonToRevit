using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Newtonsoft.Json;

namespace cityjsonToRevit
{
    public class Class1 : GH_Component
    {
        class CJObject
        {
            private string name_ = "None";
            private string lod_ = "None";
            private string parentName_ = "None";

            private string geometryType_ = "None";

            private List<string> surfaceNames_ = new List<string>();
            private List<Rhino.Geometry.Brep> brepList_ = new List<Rhino.Geometry.Brep>();

            public CJObject(string name)
            {
                name_ = name;
            }

            public string getName() { return name_; }
            public void setName(string name) { name_ = name; }
            public string getLod() { return lod_; }
            public void setLod(string lod) { lod_ = lod; }
            public string getParendName() { return parentName_; }
            public void setParendName(string parentName) { parentName_ = parentName; }
            public string getGeometryType() { return geometryType_; }
            public void setGeometryType(string geometryType) { geometryType_ = geometryType; }
            public List<string> getSurfaceNames() { return surfaceNames_; }
            public void setSurfaceNames(List<string> surfaceTypes) { surfaceNames_ = surfaceTypes; }
            //public List<Rhino.Geometry.Brep> getBrepList() { return brepList_; }
            //public void setBrepList(List<Rhino.Geometry.Brep> brepList) { brepList_ = brepList; }
            public int getBrepCount() { return brepList_.Count; }
        }

        public SimpleRhinoCityJSONReader()
          : base("SimpleRCJReader", "SReader",
              "Reads the Geometry related data stored in a CityJSON file",
              "RhinoCityJSON", "Reading")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Location of JSON file", GH_ParamAccess.list, "");
            pManager.AddBooleanParameter("Activate", "A", "Activate reader", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Settings", "S", "Settings coming from the RSettings component", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Geometry output", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<String> pathList = new List<string>();

            var settingsList = new List<Grasshopper.Kernel.Types.GH_ObjectWrapper>();
            var readSettingsList = new List<Tuple<bool, Rhino.Geometry.Point3d, bool, double, List<string>>>();

            bool boolOn = false;


            if (!DA.GetDataList(0, pathList)) return;
            DA.GetData(1, ref boolOn);
            DA.GetDataList(2, settingsList);

            if (!boolOn)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Node is offline");
                return;
            }
            else if (settingsList.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Only a single settings input allowed");
                return;
            }
            // validate the data and warn the user if invalid data is supplied.
            else if (pathList[0] == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Path is empty");
                return;
            }
            foreach (var path in pathList)
            {
                if (!System.IO.File.Exists(path))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid filepath found");
                    return;
                }
            }

            // get the settings
            List<string> loDList = new List<string>();

            Point3d worldOrigin = new Point3d(0, 0, 0);
            bool translate = false;

            double rotationAngle = 0;

            if (settingsList.Count > 0)
            {
                // extract settings
                foreach (Grasshopper.Kernel.Types.GH_ObjectWrapper objWrap in settingsList)
                {
                    readSettingsList.Add(objWrap.Value as Tuple<bool, Rhino.Geometry.Point3d, bool, double, List<string>>);
                }

                Tuple<bool, Rhino.Geometry.Point3d, bool, double, List<string>> settings = readSettingsList[0];
                translate = settings.Item1;
                rotationAngle = Math.PI * settings.Item4 / 180.0;

                if (settings.Item3) // if world origin is set
                {
                    worldOrigin = settings.Item2;
                }
                loDList = settings.Item5;
            }
            // check lod validity
            bool setLoD = false;

            foreach (string lod in loDList)
            {
                if (lod != "")
                {
                    if (lod == "0" || lod == "0.0" || lod == "0.1" || lod == "0.2" || lod == "0.3" ||
                        lod == "1" || lod == "1.0" || lod == "1.1" || lod == "1.2" || lod == "1.3" ||
                        lod == "2" || lod == "2.0" || lod == "2.1" || lod == "2.2" || lod == "2.3" ||
                        lod == "3" || lod == "3.0" || lod == "3.1" || lod == "3.2" || lod == "3.3")
                    {
                        setLoD = true;
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid lod input found");
                        return;
                    }
                }

            }

            List<Rhino.Geometry.Brep> breps = new List<Rhino.Geometry.Brep>();

            // coordinates of the first input
            double globalX = 0.0;
            double globalY = 0.0;
            double globalZ = 0.0;

            bool isFirst = true;

            double originX = worldOrigin.X;
            double originY = worldOrigin.Y;
            double originZ = worldOrigin.Z;

            foreach (var path in pathList)
            {
                // Check if valid CityJSON format
                var Jcity = JsonConvert.DeserializeObject<dynamic>(System.IO.File.ReadAllText(path));
                if (!ReaderSupport.CheckValidity(Jcity))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid CityJSON file");
                    return;
                }

                // get scalers
                double scaleX = Jcity.transform.scale[0];
                double scaleY = Jcity.transform.scale[1];
                double scaleZ = Jcity.transform.scale[2];

                // translation vectors
                double localX = 0.0;
                double localY = 0.0;
                double localZ = 0.0;

                // get location
                if (translate)
                {
                    localX = Jcity.transform.translate[0];
                    localY = Jcity.transform.translate[1];
                    localZ = Jcity.transform.translate[2];
                }
                else if (isFirst && !translate)
                {
                    isFirst = false;
                    globalX = Jcity.transform.translate[0];
                    globalY = Jcity.transform.translate[1];
                    globalZ = Jcity.transform.translate[2];
                }
                else if (!isFirst && !translate)
                {
                    localX = Jcity.transform.translate[0] - globalX;
                    localY = Jcity.transform.translate[1] - globalY;
                    localZ = Jcity.transform.translate[2] - globalZ;
                }

                // ceate vertlist
                var jsonverts = Jcity.vertices;
                List<Rhino.Geometry.Point3d> vertList = new List<Rhino.Geometry.Point3d>();
                foreach (var jsonvert in jsonverts)
                {
                    double x = jsonvert[0];
                    double y = jsonvert[1];
                    double z = jsonvert[2];

                    double tX = x * scaleX + localX - originX;
                    double tY = y * scaleY + localY - originY;
                    double tZ = z * scaleZ + localZ - originZ;

                    Rhino.Geometry.Point3d vert = new Rhino.Geometry.Point3d(
                        tX * Math.Cos(rotationAngle) - tY * Math.Sin(rotationAngle),
                        tY * Math.Cos(rotationAngle) + tX * Math.Sin(rotationAngle),
                        tZ
                        );
                    vertList.Add(vert);
                }

                // create surfaces
                foreach (var objectGroup in Jcity.CityObjects)
                {
                    foreach (var cObject in objectGroup)
                    {
                        if (cObject.geometry == null) // parents
                        {
                            continue;
                        }

                        foreach (var boundaryGroup in cObject.geometry)
                        {
                            if (setLoD && !loDList.Contains((string)boundaryGroup.lod))
                            {
                                continue;
                            }

                            if (boundaryGroup.template != null)
                            {
                                continue;
                            }
                            // this is all the geometry in one shape with info
                            else if (boundaryGroup.type == "Solid")
                            {
                                foreach (var solid in boundaryGroup.boundaries)
                                {
                                    List<Rhino.Geometry.Brep> localBreps = new List<Brep>();

                                    foreach (var surface in solid)
                                    {
                                        var readersurf = ReaderSupport.getBrepSurface(surface, vertList);
                                        if (readersurf.Item2)
                                        {
                                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                                        }
                                        foreach (var brep in readersurf.Item1)
                                        {
                                            localBreps.Add(brep);
                                        }
                                    }
                                    foreach (var brep in Brep.JoinBreps(localBreps, 0.2))
                                    {
                                        breps.Add(brep);
                                    }
                                }
                            }
                            else if (boundaryGroup.type == "CompositeSolid" || boundaryGroup.type == "MultiSolid")
                            {
                                foreach (var composit in boundaryGroup.boundaries)
                                {
                                    foreach (var solid in composit)
                                    {
                                        List<Rhino.Geometry.Brep> localBreps = new List<Brep>();
                                        foreach (var surface in solid)
                                        {
                                            var readersurf = ReaderSupport.getBrepSurface(surface, vertList);
                                            if (readersurf.Item2)
                                            {
                                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                                            }
                                            foreach (var brep in readersurf.Item1)
                                            {
                                                localBreps.Add(brep);
                                            }
                                        }
                                        foreach (var brep in Brep.JoinBreps(localBreps, 0.2))
                                        {
                                            breps.Add(brep);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                List<Rhino.Geometry.Brep> localBreps = new List<Brep>();
                                foreach (var surface in boundaryGroup.boundaries)
                                {
                                    var readersurf = ReaderSupport.getBrepSurface(surface, vertList);
                                    if (!readersurf.Item2)
                                    {
                                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not all surfaces have been correctly created");
                                    }
                                    foreach (var brep in readersurf.Item1)
                                    {
                                        localBreps.Add(brep);
                                    }
                                }
                                foreach (var brep in Brep.JoinBreps(localBreps, 0.2))
                                {
                                    breps.Add(brep);
                                }
                            }
                        }
                    }
                }
            }
            if (breps.Count > 0)
            {
                DA.SetDataList(0, breps);
            }
        }
    }

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using DotSpatial.Projections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Document = Autodesk.Revit.DB.Document;


namespace cityjsonToRevit
{

    [Transaction(TransactionMode.Manual)]
    class Program : IExternalCommand
    {
        const double angleRatio = Math.PI / 180;

        public int epsgNum(dynamic cityJ)
        {
            string epsg = unchecked((string)cityJ.metadata.referenceSystem);
            if (epsg == null)
            {
                return -1;
            }
            int found = epsg.LastIndexOf("/");
            if (found == -1)
            {
                found = epsg.LastIndexOf(":");
            }
            epsg = epsg.Substring(found + 1);
            int espgNo = Int32.Parse(epsg);
            return espgNo;
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
                        if (lod == null)
                        {
                            return "Failed";
                        }
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
        private void CreateTessellatedShape(Autodesk.Revit.DB.Document doc, ElementId materialId, dynamic cityObjProp, List<XYZ> verticesList, string Namer,
            string Lod, List<string> parameters, Dictionary<string, dynamic> ParentInfo)
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
                    dynamic parentAtt = null;

                    if (ParentInfo.ContainsKey(Namer))
                    {
                        parentAtt = ParentInfo[Namer];
                    }


                    foreach (string p in parameters)
                    {
                        Parameter para = ds.GetParameters(p).FirstOrDefault(e => e.Definition.Name == p);
                        if (p == "Object Name")
                        {
                            para.Set(Namer);
                            continue;
                        }

                        if (p == "Object Type")
                        {
                            para.Set((string)cityObjProp.type);
                            continue;
                        }

                        if (parentAtt != null)
                        {
                            foreach (var patt in parentAtt)
                            {
                                if (patt.Name == p)
                                    para.Set((string)patt);
                            }
                        }

                        if (cityObjProp.attributes == null)
                            continue;

                        foreach (var attr in cityObjProp.attributes)
                        {

                            if (attr.Name == p)
                                para.Set((string)attr);
                        }


                    }
                }

            }
        }
        private List<Material> matGenerator(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Material));

            IEnumerable<Material> existingMats
              = collector.ToElements().Cast<Material>();
            List<Material> mats = new List<Material>();
            if (!existingMats.Any(e => e.Name == "cj-Building"))
            {
                using (Transaction t = new Transaction(doc, "Set CJ Materials"))
                {
                    t.Start();

                    ElementId materialId = Material.Create(doc, "cj-Default");
                    Material materialDef = doc.GetElement(materialId) as Material;

                    Asset asset = doc.Application.GetAssets(AssetType.Appearance).FirstOrDefault(e => e.Name == "Generic");
                    AppearanceAssetElement assetElement = AppearanceAssetElement.Create(doc, "cjAsset", asset);
                    var materialProperties = new[]
                    {
                        new { name = "cj-Building", color = new Color(119, 136, 153) },
                        new { name = "cj-Bridge", color = new Color(160, 82, 45) },
                        new { name = "cj-Group", color = new Color(250, 128, 114) },
                        new { name = "cj-Furniture", color = new Color(255, 69, 0) },
                        new { name = "cj-Landuse", color = new Color(218, 165, 32) },
                        new { name = "cj-Plants", color = new Color(0, 204, 0) },
                        new { name = "cj-Railway", color = new Color(20, 20, 20) },
                        new { name = "cj-Road", color = new Color(64, 64, 64) },
                        new { name = "cj-Tunnel", color = new Color(51, 25, 0) },
                        new { name = "cj-Water", color = new Color(0, 128, 255) }
                    };
                    foreach (var materialProp in materialProperties)
                    {
                        Material newMaterial = materialDef.Duplicate(materialProp.name);
                        newMaterial.Color = materialProp.color;
                        ElementId duplicateAssetElementId = ElementId.InvalidElementId;
                        AppearanceAssetElement duplicateAssetElement = assetElement.Duplicate(newMaterial.Name);
                        newMaterial.AppearanceAssetId = duplicateAssetElement.Id;
                        duplicateAssetElementId = duplicateAssetElement.Id;
                        using (AppearanceAssetEditScope editScope = new AppearanceAssetEditScope(assetElement.Document))
                        {
                            Asset editableAsset = editScope.Start(duplicateAssetElementId);
                            AssetPropertyDoubleArray4d genericDiffuseProperty = editableAsset.FindByName("generic_diffuse") as AssetPropertyDoubleArray4d;
                            genericDiffuseProperty.SetValueAsColor(newMaterial.Color);
                            editScope.Commit(true);
                        }
                        mats.Add(newMaterial);
                    }
                    t.Commit();
                }
            }
            else
            {
                var materialNames = new[] { "cj-Building", "cj-Bridge", "cj-Group", "cj-Furniture", "cj-Landuse", "cj-Plants", "cj-Railway", "cj-Road", "cj-Tunnel", "cj-Water" };
                foreach (string name in materialNames)
                {
                    Material m = existingMats.FirstOrDefault(e => e.Name == name);
                    if (m != null)
                        mats.Add(m);
                }
            }
            return mats;
        }

        private Material matSelector(Material m, List<Material> materials, string type, Document doc)
        {
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

        private bool checkExist(string filepath, string loadedFiles)
        {
            string[] lfs = loadedFiles.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string lf in lfs)
            {
                if (lf == filepath)
                    return true;
            }
            return false;
        }

        private Tuple<List<XYZ>, XYZ, XYZ> vertBuilder(dynamic cityJ, double transX, double transY)
        {
            List<XYZ> vertList = new List<XYZ>();
            double intX = (cityJ.vertices[0][0] * cityJ.transform.scale[0]) + transX;
            double intY = (cityJ.vertices[0][1] * cityJ.transform.scale[0]) + transY;
            double intZ = (cityJ.vertices[0][2] * cityJ.transform.scale[0]);

            double minX = UnitUtils.ConvertToInternalUnits(intX, UnitTypeId.Meters);
            double maxX = minX;
            double minY = UnitUtils.ConvertToInternalUnits(intY, UnitTypeId.Meters);
            double maxY = minY;
            double minZ = UnitUtils.ConvertToInternalUnits(intZ, UnitTypeId.Meters);
            double maxZ = minZ;
            foreach (var vertex in cityJ.vertices)
            {
                double x = (vertex[0] * cityJ.transform.scale[0]) + transX;
                double y = (vertex[1] * cityJ.transform.scale[1]) + transY;
                double z = vertex[2] * cityJ.transform.scale[2];
                double xx = UnitUtils.ConvertToInternalUnits(x, UnitTypeId.Meters);
                double yy = UnitUtils.ConvertToInternalUnits(y, UnitTypeId.Meters);
                double zz = UnitUtils.ConvertToInternalUnits(z, UnitTypeId.Meters);
                XYZ vert = new XYZ(xx, yy, zz);
                vertList.Add(vert);
                minX = Math.Min(minX, xx);
                maxX = Math.Max(maxX, xx);
                minY = Math.Min(minY, yy);
                maxY = Math.Max(maxY, yy);
                minZ = Math.Min(minZ, zz);
                maxZ = Math.Max(maxZ, zz);
            }
            XYZ minPoint = new XYZ(minX, minY, minZ);
            XYZ maxPoint = new XYZ(maxX, maxY, maxZ);
            return Tuple.Create(vertList, minPoint, maxPoint);
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

            ProjectInfo projectInfo = doc.ProjectInformation;
            Parameter parLoad = projectInfo.GetParameters("loadedFiles").FirstOrDefault(e => e.Definition.Name == "loadedFiles");
            String files = string.Empty;

            if (parLoad != null)
                files = parLoad.AsString();


            var fileContent = string.Empty;
            string filePath = string.Empty;
            XYZ BaseP = BasePoint.GetProjectBasePoint(doc).Position;
            XYZ minPoint = new XYZ();
            XYZ maxPoint = new XYZ();
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open CityJSON file";
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "JSON files (*.JSON)|*.JSON";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                //Get the path of specified file
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (checkExist(filePath, files))
                    {
                        TaskDialog.Show("Loading an existing file", "The file has been loaded before.\n");
                        return Result.Failed;
                    }

                    filePath = openFileDialog.FileName;

                    //Read the contents of the file into a stream
                    var fileStream = openFileDialog.OpenFile();

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        string json = reader.ReadToEnd();
                        dynamic jCity = JsonConvert.DeserializeObject(json);

                        if (!CheckValidity(jCity))
                        {
                            TaskDialog.Show("Error!", "Invalid CityJSON file" +
                                ".");
                            return Result.Failed;
                        }

                        List<XYZ> vertList = new List<XYZ>();
                        string lodSpec = lodSelecter(jCity);

                        if (lodSpec == "")
                        {
                            return Result.Failed;
                        }
                        if (lodSpec == "Failed")
                        {
                            TaskDialog.Show("Error!", "This version does not support Templating.");
                            return Result.Failed;
                        }

                        int epsgNo = epsgNum(jCity);
                        if (epsgNo == -1)
                        {
                            TaskDialog.Show("No CRS", "There is no reference system available in CityJSON file.\r\nGeoemetries will be generated in Revit origin's point.");
                            vertList = vertBuilder(jCity, 0, 0).Item1;
                            minPoint = vertBuilder(jCity, 0, 0).Item2;
                            maxPoint = vertBuilder(jCity, 0, 0).Item3;
                        }
                        else
                        {
                            SiteLocation site = doc.ActiveProjectLocation.GetSiteLocation();
                            double latDeg = site.Latitude / angleRatio;
                            double lonDeg = site.Longitude / angleRatio;

                            double[] xy = { jCity.transform.translate[0], jCity.transform.translate[1] };
                            PointProjector(epsgNo, xy);
                            double cjLat = xy[1];
                            double cjLon = xy[0];

                            bool newLocation = false;
                            if (latDeg != cjLat || lonDeg != cjLon)
                            {
                                //User select to update or choose revit origin
                                using (mapViewer mpv = new mapViewer(latDeg, lonDeg, cjLat, cjLon))
                                {
                                    mpv.ShowDialog();
                                    newLocation = mpv._loc;
                                }
                            }
                            if (newLocation)
                            {
                                using (Transaction tran = new Transaction(doc, "Update Site Location"))
                                {
                                    tran.Start();
                                    UpdateSiteLocation(doc, jCity);
                                    vertList = vertBuilder(jCity, 0, 0).Item1;
                                    minPoint = vertBuilder(jCity, 0, 0).Item2;
                                    maxPoint = vertBuilder(jCity, 0, 0).Item3;
                                    tran.Commit();
                                }

                            }
                            else
                            {
                                double[] tranC = { jCity.transform.translate[0], jCity.transform.translate[1] };
                                double[] tranR = { lonDeg, latDeg };
                                PointProjectorRev(epsgNo, tranR);
                                double tranx = tranC[0] - tranR[0];
                                double trany = tranC[1] - tranR[1];
                                vertList = vertBuilder(jCity, tranx, trany).Item1;
                                minPoint = vertBuilder(jCity, tranx, trany).Item2;
                                maxPoint = vertBuilder(jCity, tranx, trany).Item3;
                            }
                        }

                        List<Material> materials = matGenerator(doc);

                        using (Transaction trans = new Transaction(doc, "Load CityJSON"))
                        {
                            trans.Start();

                            List<string> paramets = paramFinder(jCity);
                            Dictionary<string, dynamic> semanticParentInfo = new Dictionary<string, dynamic>();
                            foreach (string p in paramets)
                            {
                                paramMaker(uiapp, p);
                            }




                            FilteredElementCollector matcollector = new FilteredElementCollector(doc).OfClass(typeof(Material));
                            Material matDef
                              = matcollector.ToElements().Cast<Material>().FirstOrDefault(e => e.Name == "cj-Default");

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
                            foreach (var objects in jCity.CityObjects)
                            {
                                foreach (var objProperties in objects)
                                {
                                    string attributeName = objects.Name;
                                    string objType = unchecked((string)objProperties.type);
                                    Material mat = matSelector(matDef, materials, objType, doc);
                                    CreateTessellatedShape(doc, mat.Id, objProperties, vertList, attributeName, lodSpec, paramets, semanticParentInfo);
                                }
                            }



                            FilteredElementCollector collector = new FilteredElementCollector(doc);
                            View3D view3D = collector.OfClass(typeof(View3D)).Cast<View3D>().FirstOrDefault(x => x.Name == "CityJSON 3D");
                            if (view3D == null)
                            {
                                FilteredElementCollector collector0 = new FilteredElementCollector(doc);
                                ViewFamilyType viewFamilyType = collector0.OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                                                          .FirstOrDefault(y => y.ViewFamily == ViewFamily.ThreeDimensional);
                                view3D = View3D.CreateIsometric(
                                                              doc, viewFamilyType.Id);
                                view3D.Name = "CityJSON 3D";
                            }

                            files = files + "$" + filePath;
                            parLoad = projectInfo.GetParameters("loadedFiles").FirstOrDefault(e => e.Definition.Name == "loadedFiles");
                            parLoad.Set(files);
                            trans.Commit();
                            uidoc.RequestViewChange(view3D);
                            IList<UIView> views = uidoc.GetOpenUIViews();
                            foreach (UIView view in views)
                            {
                                if (view.ViewId == view3D.Id)
                                    view.ZoomAndCenterRectangle(minPoint, maxPoint);
                            }
                        }
                    }
                }
            }

            return Result.Succeeded;
        }

        private bool paramMaker(UIApplication uiapp, string param)
        {
            List<DefinitionGroup> m_exdef = new List<DefinitionGroup>();
            var exes = new HashSet<DefinitionGroup>(m_exdef);
            DefinitionFile definitionFile = uiapp.Application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                string AddInPath = typeof(ExternalApplication).Assembly.Location;
                string tempfile = Path.GetDirectoryName(AddInPath) + "\\parameters.txt";
                using (File.OpenWrite(tempfile)) { }
                uiapp.Application.SharedParametersFilename = tempfile;
                definitionFile = uiapp.Application.OpenSharedParameterFile();
            }
            BindingMap bindingMap = uiapp.ActiveUIDocument.Document.ParameterBindings;
            DefinitionGroups myGroups = definitionFile.Groups;
            DefinitionGroup myGroup = null;

            if (myGroups.IsEmpty)
                myGroup = myGroups.Create("CityJSON");
            else
            {
                myGroup = myGroups.FirstOrDefault(e => e.Name == "CityJSON");
            }
            if (myGroup == null)
                myGroup = myGroups.Create("CityJSON");
            Definition myDefinition = myGroup.Definitions.FirstOrDefault(e => e.Name == param);
            if (myDefinition == null)
            {
                ExternalDefinitionCreationOptions option = new ExternalDefinitionCreationOptions(param, SpecTypeId.String.Text);
                option.UserModifiable = false;
                option.HideWhenNoValue = true;
                option.Description = "CityJSON loaded attributes";
                myDefinition = myGroup.Definitions.Create(option);
            }
            CategorySet myCategories = uiapp.Application.Create.NewCategorySet();
            Category myCategory = Category.GetCategory(uiapp.ActiveUIDocument.Document, BuiltInCategory.OST_GenericModel);
            if (param == "loadedFiles")
                myCategory = Category.GetCategory(uiapp.ActiveUIDocument.Document, BuiltInCategory.OST_ProjectInformation);
            myCategories.Insert(myCategory);
            InstanceBinding instanceBinding = uiapp.Application.Create.NewInstanceBinding(myCategories);
            bool instanceBindOK = bindingMap.Insert(myDefinition,
                                                instanceBinding, BuiltInParameterGroup.PG_DATA);
            BindingMap map = uiapp.ActiveUIDocument.Document.ParameterBindings;
            DefinitionBindingMapIterator it = map.ForwardIterator();
            it.Reset();
            while (it.MoveNext())
            {
                InternalDefinition def = it.Key as InternalDefinition;
                if (def.Name == param)
                    def.SetAllowVaryBetweenGroups(uiapp.ActiveUIDocument.Document, true);
            }
            return instanceBindOK;
        }

        private List<string> paramFinder(dynamic jCity)
        {
            List<string> parameters = new List<string>();

            foreach (var objects in jCity.CityObjects)
            {
                foreach (var objProperties in objects)
                {
                    if (objProperties.attributes == null)
                    {
                        continue;
                    }
                    foreach (var attr in objProperties.attributes)
                    {
                        parameters.Add(attr.Name);
                    }
                }
            }
            parameters.Add("Object Name");
            parameters.Add("Object Type");
            parameters.Add("loadedFiles");
            parameters = parameters.Distinct().ToList();
            return parameters;
        }
    }
}

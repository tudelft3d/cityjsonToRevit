using System;
using Autodesk.Revit.UI;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace cityjsonToRevit
{
    public class ExternalApplication : IExternalApplication
    {

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            //create Ribbon Tab
            application.CreateRibbonTab("TU Delft");
            string path = Assembly.GetExecutingAssembly().Location;


            PushButtonData button1 = new PushButtonData("Button1", "Import CityJSON", path, "cityjsonToRevit.Program");
            RibbonPanel panel = application.CreateRibbonPanel("TU Delft", "CityJSON");

            // ExternalCommands assembly path
            string AddInPath = typeof(ExternalApplication).Assembly.Location;
            // Button icons directory
            string ButtonIconsFolder = Path.GetDirectoryName(AddInPath);
            //Add image
            Uri imagepath1 = new Uri(Path.Combine(ButtonIconsFolder, "images/3dgeo.png"), UriKind.Absolute);
            BitmapImage image1 = new BitmapImage(imagepath1);

            PushButton pushButton1 = panel.AddItem(button1) as PushButton;
            pushButton1.LargeImage = image1;

            pushButton1.ToolTip = "Opens Import Manager Form";


            ContextualHelp contextHelp = new ContextualHelp(ContextualHelpType.Url, "https://3d.bk.tudelft.nl/");
            pushButton1.SetContextualHelp(contextHelp);

            return Result.Succeeded;
        }
    }
}

# CityJSON Importer
A plugin for importing CityJSON geometries and attributes to Autodesk Revit 2023

[![GitHub license](https://img.shields.io/github/license/tudelft3d/cityjsonToRevit?style=for-the-badge)](https://github.com/tudelft3d/cityjsonToRevit/blob/main/LICENSE.txt)

![Revit Screenshot](https://github.com/tudelft3d/cityjsonToRevit/blob/main/Images/0.png)


## Introduction

CityJSON (https://www.cityjson.org/) is a JSON-based encoding for 3D city models. It is an official standard of the Open Geospatial Consortium (OGC) and an encoding for a subset of the OGC CityGML data model.

Based on a CityJSON file coordination referencing system (CRS) and metadata, this app reprojects and translates imported data for implementation within Autodesk Revit environment.

It allows users to choose whether to update or keep the Revit site location based on the CityJSON file location.

The app generates CityJSON geometries as generic models, and sets attributes on elements at the child and parent levels as shared parameters.

If a CityJSON file contains multiple LODs (such as 3D BAG), the plugin generates on the user defined level.

Materials assigned to elements based on their CityJSON object types are customizable in Revit's "Material Editor" panel.


![Revit Screenshot](https://github.com/tudelft3d/cityjsonToRevit/blob/main/Images/1.png)


## Installation/Uninstallation

The installer that ran when you downloaded this app/plug-in from Autodesk App Store (https://apps.autodesk.com/RVT/en/Detail/Index?id=7787623024858844510&appLang=en&os=Win64&mode=preview) will start installing the app/plug-in. OR, simply double-click the downloaded installer to install the app/plugin.

To uninstall this plug-in, exit the Autodesk product if you are currently running it, simply rerun the installer, and select the "Uninstall" button. OR, click Control Panel > Programs > Programs and Features (Windows 7/8.1/10/11) and uninstall as you would any other application from your system.


## General Usage Instructions

Users should first import a valid CityJSON file using the open file Windows Form.

If there is a CRS assigned to the CityJSON file, a windows form will popup asking whether to update or keep the Revit site location. Otherwise the CityJSON file will be located at Revit origin after a show dialog.

If the CityJSON file contains multiple LODs, the user identifies the level they want to be generated.

The creation of geometries may take seconds to several minutes, depending on the file size.

After a successful loading of the file, users may alter the assigned materials based on object types using the "material editor” for further uses such as rendering. The materials are all customized generics with a prefix "cj-".

![Revit Screenshot](https://github.com/tudelft3d/cityjsonToRevit/blob/main/Images/3.png)


## YouTube 3D BAG Example

Following you can watch the youtube video for importing a CityJSON file of a 3D BAG model:

[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/WjDxKxvNdto/0.jpg)](https://www.youtube.com/watch?v=WjDxKxvNdto&t=1s)


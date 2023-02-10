using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
namespace cityjsonToRevit
{
    partial class mapViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        GMapOverlay mapOverlay = new GMapOverlay("markers");



        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private RectLatLng rectBypoints(double lat0, double lon0, double lat1, double lon1)
        {
            double latM = (lat0 + lat1) / 2;
            double lonM = (lon0 + lon1) / 2;
            double latDelta = Math.Abs(lat0 - lat1);
            double lonDelta = Math.Abs(lon0 - lon1);
            if (latDelta < 0.0001) { latDelta = 0.0001; }
            if (lonDelta < 0.0001) { lonDelta = 0.0001; }
            double latMax = latM + (0.9 * latDelta);
            double lonMax = lonM - (0.9 * lonDelta);
            RectLatLng rectLatLng = new RectLatLng(latMax, lonMax, 1.8 * lonDelta, 1.8 * latDelta);
            return rectLatLng;
        }
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent(double lat0, double lon0, double lat1, double lon1)
        {
            this.gMap = new GMap.NET.WindowsForms.GMapControl();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();

            this.SuspendLayout();
            // 
            // gMap
            // 
            gMap.DragButton = System.Windows.Forms.MouseButtons.Left;
            this.gMap.MapProvider = GMapProviders.ArcGIS_World_Street_Map;
            GMarkerGoogleType gMarkerGoogleType = GMarkerGoogleType.arrow;
            PointLatLng pp = new PointLatLng(lat0, lon0);
            PointLatLng pp1 = new PointLatLng(lat1, lon1);
            RectLatLng rl = rectBypoints(lat0, lon0, lat1, lon1);


            GMarkerGoogle marker = new GMarkerGoogle(pp, gMarkerGoogleType);
            GMarkerGoogle marker1 = new GMarkerGoogle(pp1, gMarkerGoogleType);
            marker.ToolTipText = "Revit origin";
            marker.ToolTipMode = MarkerTooltipMode.Always;
            marker1.ToolTipText = "CityJSON origin";
            marker1.ToolTipMode = MarkerTooltipMode.Always;
            marker.IsVisible = true;
            marker1.IsVisible = true;

            mapOverlay.Markers.Clear();
            mapOverlay.Markers.Add(marker);
            mapOverlay.Markers.Add(marker1);
            gMap.Overlays.Clear();
            gMap.Overlays.Add(mapOverlay);
            gMap.MinZoom = 0;
            gMap.MaxZoom = 26;
            gMap.ZoomAndCenterMarkers(mapOverlay.Id);
            gMap.SetZoomToFitRect(rl);
            gMap.CanDragMap = false;

            //gMap.Zoom = 10;
            gMap.MouseWheelZoomEnabled = true;
            gMap.AutoScroll = true;
            this.gMap.Bearing = 0F;
            this.gMap.CanDragMap = true;
            this.gMap.EmptyTileColor = System.Drawing.Color.Navy;
            this.gMap.GrayScaleMode = false;
            this.gMap.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            this.gMap.LevelsKeepInMemmory = 2;
            this.gMap.Location = new System.Drawing.Point(12, 12);
            this.gMap.MarkersEnabled = true;
            this.gMap.MouseWheelZoomEnabled = true;
            this.gMap.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.ViewCenter;
            this.gMap.Name = "gMap";
            this.gMap.NegativeMode = false;
            this.gMap.PolygonsEnabled = true;
            this.gMap.RetryLoadTile = 0;
            this.gMap.RoutesEnabled = true;
            this.gMap.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            this.gMap.SelectedAreaFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(65)))), ((int)(((byte)(105)))), ((int)(((byte)(225)))));
            this.gMap.ShowTileGridLines = false;
            this.gMap.Size = new System.Drawing.Size(433, 426);
            this.gMap.TabIndex = 0;
            this.gMap.ShowCenter = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(452, 140);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(210, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Please choose between these two options:";
            this.label1.Enabled = false;

            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(451, 167);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(207, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "Keep";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(451, 234);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(207, 23);
            this.button2.TabIndex = 5;
            this.button2.Text = "Update";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);

            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(452, 193);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(209, 26);
            this.label2.TabIndex = 6;
            this.label2.Text = "Keep existing Revit\'s site location and load \r\nCityJSON file based on the current" +
    " origin.";
            this.label2.Enabled = false;

            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(452, 260);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(202, 26);
            this.label3.TabIndex = 7;
            this.label3.Text = "Change existing site location to CityJSON \r\nfile\'s origin and load the CityJSON f" +
    "ile.";
            this.label3.Enabled = false;



            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(452, 330);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(202, 26);
            this.label4.TabIndex = 8;
            this.label4.Text = "Attention: CityJSON files located too far \r\n" +
                               "from Revit's origin may cause difficulties\r\n" +
                               " with file navigation and graphics.";
            this.label4.Enabled = false;

            // 
            // mapViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(670, 445);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.gMap);
            this.Name = "mapViewer";
            this.ShowIcon = false;
            this.Text = "Select site location";
            this.FormBorderStyle  = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private GMap.NET.WindowsForms.GMapControl gMap;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;

    }
}
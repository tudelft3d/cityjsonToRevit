
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System;
using System.Collections.Generic;
using System.Drawing;
namespace cityjsonToRevit.Command
{
    partial class BagMap
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        GMapOverlay polygons = new GMapOverlay("Polygons");

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

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent(double lat, double lon)
        {
            this.sliderMove = new System.Windows.Forms.TrackBar();
            this.value = new System.Windows.Forms.TextBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.gMap = new GMap.NET.WindowsForms.GMapControl();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.sliderMove)).BeginInit();
            this.SuspendLayout();
            // 
            // sliderMove
            // 
            this.sliderMove.Cursor = System.Windows.Forms.Cursors.Default;
            this.sliderMove.LargeChange = 100;
            this.sliderMove.Location = new System.Drawing.Point(811, 453);
            this.sliderMove.Maximum = 700;
            this.sliderMove.Minimum = 100;
            this.sliderMove.Name = "sliderMove";
            this.sliderMove.Size = new System.Drawing.Size(354, 90);
            this.sliderMove.SmallChange = 50;
            this.sliderMove.TabIndex = 1;
            this.sliderMove.TickFrequency = 50;
            this.sliderMove.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.sliderMove.Value = 300;
            this.sliderMove.ValueChanged += new System.EventHandler(this.sliderMove_ValueChanged);
            // 
            // value
            // 
            this.value.BackColor = System.Drawing.SystemColors.Menu;
            this.value.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.value.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.value.Location = new System.Drawing.Point(842, 536);
            this.value.Multiline = true;
            this.value.Name = "value";
            this.value.Size = new System.Drawing.Size(287, 46);
            this.value.TabIndex = 2;
            this.value.Text = "Side: 300 meters";
            this.value.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.value.ReadOnly = true;
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.Menu;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(811, 68);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(354, 111);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "The plugin will generate geometries and attributes of all the buildings within th" +
    "e specified boundingbox, including those with parts inside it.";
            this.textBox1.ReadOnly = true;

            // 
            // textBox2
            // 
            this.textBox2.BackColor = System.Drawing.SystemColors.Menu;
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox2.Location = new System.Drawing.Point(811, 383);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(354, 38);
            this.textBox2.TabIndex = 2;
            this.textBox2.Text = "Choose the desired dimension:";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBox2.ReadOnly = true;

            // 
            // gMap
            // 
            gMap.DragButton = System.Windows.Forms.MouseButtons.Left;
            this.gMap.MapProvider = GMapProviders.ArcGIS_World_Street_Map;
            gMap.MouseWheelZoomEnabled = true;
            gMap.AutoScroll = true;
            gMap.Position = new GMap.NET.PointLatLng(lat, lon);
            List<PointLatLng> points = Square(lat, lon);
            GMapPolygon polygon = new GMapPolygon(points, "sqr");
            polygon.Fill = new SolidBrush(Color.FromArgb(50, Color.Red));
            polygon.Stroke = new Pen(Color.Red, 1);
            polygons.Polygons.Clear();
            polygons.Polygons.Add(polygon);


            this.gMap.Bearing = 0F;
            this.gMap.CanDragMap = false;
            this.gMap.EmptyTileColor = System.Drawing.Color.Navy;
            this.gMap.GrayScaleMode = false;
            this.gMap.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            this.gMap.LevelsKeepInMemmory = 2;
            this.gMap.Location = new System.Drawing.Point(12, 12);
            this.gMap.MarkersEnabled = true;
            gMap.Overlays.Clear();
            gMap.Overlays.Add(polygons);
            this.gMap.MaxZoom = 18;
            this.gMap.MinZoom = 14;
            this.gMap.Zoom = 16;
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
            this.gMap.Size = new System.Drawing.Size(779, 942);
            this.gMap.TabIndex = 3;
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.SystemColors.Menu;
            this.button1.Location = new System.Drawing.Point(870, 602);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(231, 46);
            this.button1.TabIndex = 10;
            this.button1.Text = "Run";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // BagMap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1177, 966);
            this.Controls.Add(this.gMap);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.value);
            this.Controls.Add(this.sliderMove);
            this.Controls.Add(this.button1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BagMap";
            this.ShowIcon = false;
            this.Text = "BagMap";
            ((System.ComponentModel.ISupportInitialize)(this.sliderMove)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TrackBar sliderMove;
        private System.Windows.Forms.TextBox value;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private GMap.NET.WindowsForms.GMapControl gMap;
        private System.Windows.Forms.Button button1;

    }
}
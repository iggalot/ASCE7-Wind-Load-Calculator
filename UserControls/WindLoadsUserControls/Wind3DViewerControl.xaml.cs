using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Rotatable 3D view of the building with wind zones colored by net pressure.
    /// All geometry, colors and camera math come from ASCE7Wind.Core (WindSceneBuilder, PressureColorScale,
    /// OrbitCamera); this control only converts them to WPF 3D and handles input.
    /// Rebuilds meshes only when results or the view mode change; camera moves just update the camera.
    /// </summary>
    public partial class Wind3DViewerControl : UserControl
    {
        private BuildingData building;
        private WindLoadCalculator_Base cladding, mwfrsLength, mwfrsWidth;

        private WindScene scene;
        private PressureColorScale scale;
        private readonly OrbitCamera cam = new OrbitCamera(new Vec3(0, 0, 0), -125, 28, 100);

        private readonly Dictionary<GeometryModel3D, SceneZone> zoneByModel = new Dictionary<GeometryModel3D, SceneZone>();
        private readonly Dictionary<GeometryModel3D, Material> normalMaterial = new Dictionary<GeometryModel3D, Material>();
        private readonly Dictionary<GeometryModel3D, Material> highlightMaterial = new Dictionary<GeometryModel3D, Material>();
        private GeometryModel3D hovered;
        private GeometryModel3D outlines;
        private Vec3? northLabelAt, windLabelAt;

        private Point lastMouse, downMouse;
        private MouseButton? dragButton;
        private bool dragged;
        private bool suppressModeEvents;

        /// <summary>Raised when the pointer moves onto a different zone (null when it leaves all zones).</summary>
        public event EventHandler<SceneZone> ZoneHovered;

        public Wind3DViewerControl()
        {
            InitializeComponent();
            viewHost.SizeChanged += (s, e) => ApplyCamera();
            UpdateRoofCaseAvailability();
            ApplyCamera();
        }

        public Wind3DViewerControl(BuildingData building, WindLoadCalculator_Base cladding,
            WindLoadCalculator_Base mwfrsAlongLength, WindLoadCalculator_Base mwfrsAlongWidth) : this()
        {
            SetResults(building, cladding, mwfrsAlongLength, mwfrsAlongWidth);
        }

        /// <summary>The current scene (geometry, zones, pressures), or null before results are set.</summary>
        public WindScene Scene => scene;

        public WindViewMode Mode
        {
            get
            {
                if (rbCladPressure.IsChecked == true) return WindViewMode.CladdingPressure;
                if (rbWindWest.IsChecked == true) return WindViewMode.MwfrsAlongLength;
                if (rbWindSouth.IsChecked == true) return WindViewMode.MwfrsAlongWidth;
                return WindViewMode.CladdingSuction;
            }
            set
            {
                suppressModeEvents = true;
                rbCladSuction.IsChecked = value == WindViewMode.CladdingSuction;
                rbCladPressure.IsChecked = value == WindViewMode.CladdingPressure;
                rbWindWest.IsChecked = value == WindViewMode.MwfrsAlongLength;
                rbWindSouth.IsChecked = value == WindViewMode.MwfrsAlongWidth;
                suppressModeEvents = false;
                UpdateRoofCaseAvailability();
                Rebuild(resetCamera: false);
            }
        }

        public MwfrsRoofCase RoofCase => rbCaseB.IsChecked == true ? MwfrsRoofCase.Alternate : MwfrsRoofCase.MaxSuction;

        /// <summary>Supplies (or replaces) the results to show.</summary>
        public void SetResults(BuildingData building, WindLoadCalculator_Base cladding,
            WindLoadCalculator_Base mwfrsAlongLength, WindLoadCalculator_Base mwfrsAlongWidth)
        {
            this.building = building;
            this.cladding = cladding;
            mwfrsLength = mwfrsAlongLength;
            mwfrsWidth = mwfrsAlongWidth;
            Rebuild(resetCamera: true);
        }

        // ---------------------------------------------------------------------------------------
        // Scene construction
        // ---------------------------------------------------------------------------------------

        private void Rebuild(bool resetCamera)
        {
            sceneRoot.Children.Clear();
            zoneByModel.Clear();
            normalMaterial.Clear();
            highlightMaterial.Clear();
            hovered = null;
            outlines = null;
            northLabelAt = windLabelAt = null;
            bdHover.Visibility = Visibility.Collapsed;

            if (building == null)
            {
                scene = null;
                UpdateOverlays();
                return;
            }

            try
            {
                scene = WindSceneBuilder.Build(building, cladding, mwfrsLength, mwfrsWidth, Mode, RoofCase);
            }
            catch (ArgumentException ex)
            {
                // e.g. a zero dimension while the user is still typing
                scene = null;
                UpdateOverlays();
                tbSubtitle.Text = "Cannot draw this building yet: " + ex.Message;
                return;
            }
            scale = new PressureColorScale(scene.MaxAbsPsf);
            var model = scene.Model;
            double size = Math.Max(Math.Max(model.L, model.B), model.RidgeHeight);

            AddLights();
            AddGround(model, size);
            AddEnvelope(model, size);
            AddZones(size);
            AddOutlines(size);
            if (scene.WindDirection.HasValue) AddWindArrow(model, scene.WindDirection.Value, size);

            if (resetCamera)
            {
                FrameModel();
                cam.Apply(OrbitCamera.Preset.Isometric);
            }
            ApplyCamera();
            UpdateOverlays();
        }

        private void AddLights()
        {
            sceneRoot.Children.Add(new AmbientLight(Color.FromRgb(0xB0, 0xB0, 0xB0)));
            sceneRoot.Children.Add(new DirectionalLight(Color.FromRgb(0x50, 0x50, 0x50), new Vector3D(-0.35, -1.0, -0.55)));
            sceneRoot.Children.Add(new DirectionalLight(Color.FromRgb(0x22, 0x22, 0x22), new Vector3D(0.5, -0.3, 0.7)));
        }

        private void AddGround(BuildingModel m, double size)
        {
            double pad = 0.9 * size;
            double x0 = -pad, x1 = m.L + pad, y0 = -pad, y1 = m.B + pad, z = -0.02 * size / 50;

            var plane = new List<Vec3> { new Vec3(x0, y0, z), new Vec3(x1, y0, z), new Vec3(x1, y1, z), new Vec3(x0, y0, z), new Vec3(x1, y1, z), new Vec3(x0, y1, z) };
            sceneRoot.Children.Add(Model(plane, Color.FromRgb(0xF0, 0xEF, 0xEC)));

            // Grid on a "nice" spacing so the scale reads at a glance.
            double step = PressureColorScale.NiceStep(size / 6);
            var lines = new List<Vec3>();
            double w = size / 700;
            for (double x = Math.Ceiling(x0 / step) * step; x <= x1; x += step)
                AddFlatStrip(lines, new Vec3(x, y0, z + 0.001), new Vec3(x, y1, z + 0.001), Vec3.UnitZ, w);
            for (double y = Math.Ceiling(y0 / step) * step; y <= y1; y += step)
                AddFlatStrip(lines, new Vec3(x0, y, z + 0.001), new Vec3(x1, y, z + 0.001), Vec3.UnitZ, w);
            sceneRoot.Children.Add(Model(lines, Color.FromRgb(0xE1, 0xE0, 0xD9)));

            // North arrow on the ground off the north-west corner, labeled N.
            double a = 0.12 * size;
            var cx = -0.3 * size; var cy = m.B + 0.12 * size; var zg = z + 0.002;
            var north = new List<Vec3>();
            AddFlatStrip(north, new Vec3(cx, cy, zg), new Vec3(cx, cy + 0.6 * a, zg), Vec3.UnitZ, 0.12 * a);
            north.Add(new Vec3(cx - 0.3 * a, cy + 0.6 * a, zg)); north.Add(new Vec3(cx + 0.3 * a, cy + 0.6 * a, zg)); north.Add(new Vec3(cx, cy + a, zg));
            sceneRoot.Children.Add(Model(north, Color.FromRgb(0x89, 0x87, 0x81)));
            northLabelAt = new Vec3(cx, cy + 1.25 * a, zg);
        }

        /// <summary>Neutral envelope drawn slightly inside the zones so surfaces without zones still read as solid.</summary>
        private void AddEnvelope(BuildingModel m, double size)
        {
            double inset = size / 2000;
            var tris = new List<Vec3>();
            foreach (var s in m.Surfaces)
            {
                var o = s.Outline;
                var shift = s.Normal * -inset;
                for (int i = 1; i + 1 < o.Count; i++)
                {
                    tris.Add(o[0] + shift); tris.Add(o[i] + shift); tris.Add(o[i + 1] + shift);
                }
            }
            sceneRoot.Children.Add(Model(tris, Color.FromRgb(0xD8, 0xD7, 0xD0)));
        }

        private void AddZones(double size)
        {
            double lift = size / 4000;
            foreach (var z in scene.Zones)
            {
                if (z.Triangles.Count == 0) continue;
                var shift = z.Normal * lift;
                var color = ToColor(scale.ColorFor(z.PressurePsf));
                var gm = Model(z.Triangles.Select(p => p + shift).ToList(), color, freeze: false);

                var hi = new MaterialGroup();
                hi.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
                hi.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF))));
                hi.Freeze();

                zoneByModel[gm] = z;
                normalMaterial[gm] = gm.Material;
                highlightMaterial[gm] = hi;
                sceneRoot.Children.Add(gm);
            }
        }

        private void AddOutlines(double size)
        {
            var strips = new List<Vec3>();
            double w = size / 450, lift = size / 1500;
            foreach (var z in scene.Zones)
                foreach (var (a, b) in z.Edges)
                    AddFlatStrip(strips, a + z.Normal * lift, b + z.Normal * lift, z.Normal, w);
            outlines = Model(strips, Color.FromRgb(0x52, 0x51, 0x4E));
            if (cbOutlines.IsChecked == true) sceneRoot.Children.Add(outlines);
        }

        private void AddWindArrow(BuildingModel m, Vec3 dir, double size)
        {
            // Arrow upwind of the building, at mid eave height, pointing downwind.
            var center = new Vec3(m.L / 2, m.B / 2, m.EaveHeight * 0.5);
            double half = dir.X != 0 ? m.L / 2 : m.B / 2;
            var tip = center - dir * (half + 0.08 * size);
            var tail = center - dir * (half + 0.45 * size);
            var side = dir.Cross(Vec3.UnitZ).Normalized();
            windLabelAt = tail - dir * (0.04 * size) + Vec3.UnitZ * (0.04 * size);
            double r = 0.025 * size, head = 0.09 * size;

            var tris = new List<Vec3>();
            var neck = tip - dir * head;
            AddBox(tris, tail, neck, side, r);
            // pyramid head
            var up = Vec3.UnitZ;
            var c = new[] { neck + side * (2.4 * r) + up * (2.4 * r), neck - side * (2.4 * r) + up * (2.4 * r), neck - side * (2.4 * r) - up * (2.4 * r), neck + side * (2.4 * r) - up * (2.4 * r) };
            for (int i = 0; i < 4; i++) { tris.Add(c[i]); tris.Add(c[(i + 1) % 4]); tris.Add(tip); }
            tris.Add(c[0]); tris.Add(c[2]); tris.Add(c[1]); tris.Add(c[0]); tris.Add(c[3]); tris.Add(c[2]);
            sceneRoot.Children.Add(Model(tris, Color.FromRgb(0x52, 0x51, 0x4E)));
        }

        // ---- mesh helpers -------------------------------------------------------------------------

        private static Point3D P(Vec3 v) => new Point3D(v.X, v.Z, -v.Y);      // model z-up -> WPF y-up
        private static Vector3D D(Vec3 v) => new Vector3D(v.X, v.Z, -v.Y);
        private static Color ToColor(PressureColorScale.Rgb c) => Color.FromRgb(c.R, c.G, c.B);

        private static GeometryModel3D Model(IList<Vec3> triangles, Color color, bool freeze = true)
        {
            var mesh = new MeshGeometry3D();
            var pos = new Point3DCollection(triangles.Count);
            var idx = new Int32Collection(triangles.Count);
            for (int i = 0; i < triangles.Count; i++) { pos.Add(P(triangles[i])); idx.Add(i); }
            mesh.Positions = pos;
            mesh.TriangleIndices = idx;
            mesh.Freeze();

            var mat = new DiffuseMaterial(new SolidColorBrush(color));
            mat.Freeze();
            var gm = new GeometryModel3D(mesh, mat) { BackMaterial = mat };
            if (freeze) gm.Freeze();
            return gm;
        }

        /// <summary>Thin flat strip along segment ab lying in the plane with normal n.</summary>
        private static void AddFlatStrip(List<Vec3> tris, Vec3 a, Vec3 b, Vec3 n, double width)
        {
            var dir = b - a;
            if (dir.Length < 1e-9) return;
            var s = n.Cross(dir).Normalized() * (width / 2);
            var e = dir.Normalized() * (width / 2); // extend so corners close
            Vec3 p0 = a - e - s, p1 = b + e - s, p2 = b + e + s, p3 = a - e + s;
            tris.Add(p0); tris.Add(p1); tris.Add(p2);
            tris.Add(p0); tris.Add(p2); tris.Add(p3);
        }

        private static void AddBox(List<Vec3> tris, Vec3 a, Vec3 b, Vec3 side, double r)
        {
            var dir = (b - a).Normalized();
            var up = side.Cross(dir).Normalized();
            Vec3[] ring(Vec3 c) => new[] { c + side * r + up * r, c - side * r + up * r, c - side * r - up * r, c + side * r - up * r };
            var ra = ring(a); var rb = ring(b);
            for (int i = 0; i < 4; i++)
            {
                int j = (i + 1) % 4;
                tris.Add(ra[i]); tris.Add(rb[i]); tris.Add(rb[j]);
                tris.Add(ra[i]); tris.Add(rb[j]); tris.Add(ra[j]);
            }
            tris.Add(ra[0]); tris.Add(ra[2]); tris.Add(ra[1]); tris.Add(ra[0]); tris.Add(ra[3]); tris.Add(ra[2]);
        }

        // ---------------------------------------------------------------------------------------
        // Camera
        // ---------------------------------------------------------------------------------------

        private void FrameModel()
        {
            if (scene == null) return;
            var m = scene.Model;
            var center = new Vec3(m.L / 2, m.B / 2, m.RidgeHeight / 2);
            double radius = 0.5 * Math.Sqrt(m.L * m.L + m.B * m.B + m.RidgeHeight * m.RidgeHeight);
            if (scene.WindDirection.HasValue) radius *= 1.2; // leave room for the wind arrow
            cam.Frame(center, radius);
        }

        private void ApplyCamera()
        {
            camera.Position = P(cam.Position);
            camera.LookDirection = D(cam.LookDirection);
            camera.UpDirection = D(cam.Up);
            camera.FieldOfView = HorizontalFieldOfView(); // WPF wants the horizontal angle; framing uses the vertical one
            camera.NearPlaneDistance = Math.Max(0.1, cam.Distance / 200);
            camera.FarPlaneDistance = cam.Distance * 50;
            UpdatePinnedLabels();
        }

        /// <summary>Screen position of a model point in the 3D area, or null when it is behind the camera.</summary>
        private Point? Project(Vec3 p)
        {
            double w = viewHost.ActualWidth, h = viewHost.ActualHeight;
            if (w <= 0 || h <= 0) return null;
            var rel = p - cam.Position;
            double zc = rel.Dot(cam.LookDirection.Normalized());
            if (zc <= 1e-6) return null;
            double focal = (h / 2) / Math.Tan(OrbitCamera.FieldOfViewDeg * Math.PI / 360);
            return new Point(w / 2 + rel.Dot(cam.Right) / zc * focal, h / 2 - rel.Dot(cam.ScreenUp) / zc * focal);
        }

        private void UpdatePinnedLabels()
        {
            void Pin(TextBlock label, Vec3? at)
            {
                var s = at.HasValue ? Project(at.Value) : null;
                label.Visibility = s.HasValue ? Visibility.Visible : Visibility.Collapsed;
                if (!s.HasValue) return;
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(label, s.Value.X - label.DesiredSize.Width / 2);
                Canvas.SetTop(label, s.Value.Y - label.DesiredSize.Height / 2);
            }
            Pin(lblNorth, northLabelAt);
            Pin(lblWind, windLabelAt);
        }

        /// <summary>
        /// OrbitCamera frames with a fixed vertical angle; WPF's FieldOfView is horizontal, so convert by
        /// the viewport aspect ratio to keep the model fully in view on wide or tall windows.
        /// </summary>
        private double HorizontalFieldOfView()
        {
            double w = viewHost.ActualWidth, h = viewHost.ActualHeight;
            double aspect = w > 0 && h > 0 ? w / h : 16.0 / 9.0;
            double halfV = OrbitCamera.FieldOfViewDeg * Math.PI / 360;
            double halfH = Math.Atan(Math.Tan(halfV) * Math.Max(aspect, 1e-3));
            return Math.Min(170, 2 * halfH * 180 / Math.PI);
        }

        public void ResetView()
        {
            FrameModel();
            cam.Apply(OrbitCamera.Preset.Isometric);
            ApplyCamera();
        }

        private void ShowPreset(OrbitCamera.Preset p)
        {
            FrameModel();
            cam.Apply(p);
            ApplyCamera();
        }

        private void ViewIso_Click(object sender, RoutedEventArgs e) => ShowPreset(OrbitCamera.Preset.Isometric);
        private void ViewTop_Click(object sender, RoutedEventArgs e) => ShowPreset(OrbitCamera.Preset.Top);
        private void ViewSouth_Click(object sender, RoutedEventArgs e) => ShowPreset(OrbitCamera.Preset.South);
        private void ViewWest_Click(object sender, RoutedEventArgs e) => ShowPreset(OrbitCamera.Preset.West);

        // ---------------------------------------------------------------------------------------
        // Input
        // ---------------------------------------------------------------------------------------

        private void View_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
            {
                ResetView();
                e.Handled = true;
                return;
            }
            dragButton = e.ChangedButton;
            downMouse = lastMouse = e.GetPosition(viewHost);
            dragged = false;
            viewHost.CaptureMouse();
            e.Handled = true;
        }

        private void View_MouseMove(object sender, MouseEventArgs e)
        {
            var p = e.GetPosition(viewHost);
            if (dragButton.HasValue)
            {
                double dx = p.X - lastMouse.X, dy = p.Y - lastMouse.Y;
                if (!dragged && (Math.Abs(p.X - downMouse.X) > 3 || Math.Abs(p.Y - downMouse.Y) > 3)) dragged = true;
                if (dragged)
                {
                    if (dragButton == MouseButton.Left) cam.Orbit(dx, dy);
                    else cam.Pan(dx, dy, viewHost.ActualHeight);
                    ApplyCamera();
                    SetHover(null, p);
                }
                lastMouse = p;
                return;
            }
            SetHover(HitZone(p), p);
        }

        private void View_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dragButton = null;
            viewHost.ReleaseMouseCapture();
            SetHover(HitZone(e.GetPosition(viewHost)), e.GetPosition(viewHost));
        }

        private void View_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cam.Zoom(Math.Pow(0.88, e.Delta / 120.0));
            ApplyCamera();
            e.Handled = true;
        }

        private void View_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!dragButton.HasValue) SetHover(null, default(Point));
        }

        /// <summary>The zone under a point in this control's 3D area (as used for hover), or null.</summary>
        public SceneZone ZoneAt(Point pointInView)
        {
            var gm = HitZone(pointInView);
            return gm != null ? zoneByModel[gm] : null;
        }

        private GeometryModel3D HitZone(Point p)
        {
            GeometryModel3D hit = null;
            var vpPoint = viewHost.TranslatePoint(p, viewport);
            VisualTreeHelper.HitTest(viewport, null, r =>
            {
                if (r is RayMeshGeometry3DHitTestResult mesh && mesh.ModelHit is GeometryModel3D gm && zoneByModel.ContainsKey(gm))
                {
                    hit = gm;
                    return HitTestResultBehavior.Stop;
                }
                return HitTestResultBehavior.Continue;
            }, new PointHitTestParameters(vpPoint));
            return hit;
        }

        private void SetHover(GeometryModel3D gm, Point p)
        {
            if (!ReferenceEquals(gm, hovered))
            {
                if (hovered != null) hovered.Material = hovered.BackMaterial = normalMaterial[hovered];
                hovered = gm;
                if (hovered != null) hovered.Material = hovered.BackMaterial = highlightMaterial[hovered];
                ZoneHovered?.Invoke(this, hovered != null ? zoneByModel[hovered] : null);
            }

            if (hovered == null)
            {
                bdHover.Visibility = Visibility.Collapsed;
                return;
            }

            var z = zoneByModel[hovered];
            tbHoverValue.Text = FormatPressure(z.PressurePsf);
            tbHoverWhat.Text = z.Description;
            tbHoverWhere.Text = $"{z.SurfaceName}  ·  {ShortLabel(z.Label)}  ·  area used {z.AreaFt2:0} ft²";
            bdHover.Visibility = Visibility.Visible;
            bdHover.UpdateLayout();
            double x = Math.Min(p.X + 16, Math.Max(0, viewHost.ActualWidth - bdHover.ActualWidth - 8));
            double y = Math.Min(p.Y + 16, Math.Max(0, viewHost.ActualHeight - bdHover.ActualHeight - 8));
            bdHover.Margin = new Thickness(x, y, 0, 0);
        }

        // ---------------------------------------------------------------------------------------
        // Overlays
        // ---------------------------------------------------------------------------------------

        private void UpdateOverlays()
        {
            spSummary.Children.Clear();
            if (scene == null)
            {
                tbTitle.Text = "Wind pressures";
                tbSubtitle.Text = "Enter building and wind inputs to see results";
                bdWarnings.Visibility = Visibility.Collapsed;
                bdSummary.Visibility = Visibility.Collapsed;
                bdLegend.Visibility = Visibility.Collapsed;
                UpdatePinnedLabels();
                return;
            }

            tbTitle.Text = scene.Title;
            tbSubtitle.Text = scene.Subtitle;

            bdWarnings.Visibility = scene.Messages.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            tbWarnings.Text = string.Join(Environment.NewLine, scene.Messages);

            bool anyPressure = scene.Zones.Any(z => z.HasPressure);
            bdLegend.Visibility = anyPressure ? Visibility.Visible : Visibility.Collapsed;
            if (anyPressure) DrawLegend(scale);
            UpdatePinnedLabels();

            bdSummary.Visibility = Visibility.Visible;
            bool mwfrs = scene.Mode == WindViewMode.MwfrsAlongLength || scene.Mode == WindViewMode.MwfrsAlongWidth;
            if (mwfrs)
            {
                var ww = scene.Zones.FirstOrDefault(z => z.Label == "ZoneWW");
                var lw = scene.Zones.FirstOrDefault(z => z.Label == "ZoneLW");
                var roof = scene.Zones.Where(z => z.Kind == SurfaceKind.Roof && z.HasPressure).OrderBy(z => z.PressurePsf).FirstOrDefault();
                if (ww != null && lw != null) AddSummary("Walls", $"windward {FormatPsf(ww.PressurePsf)},  leeward {FormatPsf(lw.PressurePsf)}");
                if (roof != null) AddSummary("Strongest roof suction", $"{FormatPsf(roof.PressurePsf)}  at the {Short(roof.Description)}");
                if (scene.BaseShearKips.HasValue) AddSummary("Wind force on the walls", $"{scene.BaseShearKips.Value:0.0} kips in the wind direction");
            }
            else
            {
                var roof = scene.Zones.Where(z => z.Kind == SurfaceKind.Roof && z.HasPressure)
                                      .OrderBy(z => scene.Mode == WindViewMode.CladdingSuction ? z.PressurePsf : -z.PressurePsf).FirstOrDefault();
                var wall = scene.Zones.Where(z => z.Kind == SurfaceKind.Wall && z.HasPressure)
                                      .OrderBy(z => scene.Mode == WindViewMode.CladdingSuction ? z.PressurePsf : -z.PressurePsf).FirstOrDefault();
                string what = scene.Mode == WindViewMode.CladdingSuction ? "uplift" : "inward pressure";
                if (roof != null) AddSummary($"Highest roof {what}", $"{FormatPsf(roof.PressurePsf)}  at the {Short(roof.Description)}");
                if (wall != null) AddSummary($"Highest wall {(scene.Mode == WindViewMode.CladdingSuction ? "suction" : "pressure")}", $"{FormatPsf(wall.PressurePsf)}  at the {Short(wall.Description)}");
            }
            if (spSummary.Children.Count == 0) bdSummary.Visibility = Visibility.Collapsed;
        }

        private void AddSummary(string label, string value)
        {
            var tb = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 1) };
            tb.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = (Brush)FindResource("InkSecondary") });
            tb.Inlines.Add(new System.Windows.Documents.Run(value) { Foreground = (Brush)FindResource("InkPrimary"), FontWeight = FontWeights.SemiBold });
            spSummary.Children.Add(tb);
        }

        private void DrawLegend(PressureColorScale s)
        {
            // Top = strongest pressure (blue), bottom = strongest suction (red), gray at zero.
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            const int n = 24;
            for (int i = 0; i <= n; i++)
            {
                double f = (double)i / n;
                brush.GradientStops.Add(new GradientStop(ToColor(s.ColorFor(s.MaxAbs * (1 - 2 * f))), f));
            }
            brush.Freeze();
            legendBar.Background = brush;

            legendTicks.Children.Clear();
            double h = legendTicks.Height;
            foreach (var t in s.Ticks())
            {
                double y = (1 - (t + s.MaxAbs) / (2 * s.MaxAbs)) * h;
                var label = new TextBlock
                {
                    Text = t == 0 ? "0" : t.ToString("+0.#;−0.#", CultureInfo.CurrentCulture),
                    FontSize = 12,
                    Foreground = (Brush)FindResource("InkSecondary"),
                };
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, Math.Max(0, Math.Min(h - 16, y - 8)));
                legendTicks.Children.Add(label);
            }
        }

        private static string FormatPsf(double psf) =>
            double.IsNaN(psf) ? "n/a" : psf.ToString("+0.0;−0.0", CultureInfo.CurrentCulture) + " psf";

        private static string FormatPressure(double psf)
        {
            if (double.IsNaN(psf)) return "No coefficient for this zone";
            return Math.Abs(psf).ToString("0.0", CultureInfo.CurrentCulture) + " psf " + (psf < 0 ? "suction" : "pressure");
        }

        /// <summary>Description without its parenthetical note, lower-cased for use mid-sentence.</summary>
        private static string Short(string description)
        {
            if (string.IsNullOrEmpty(description)) return description;
            int i = description.IndexOf(" (", StringComparison.Ordinal);
            return (i > 0 ? description.Substring(0, i) : description).ToLowerInvariant();
        }

        private static string ShortLabel(string label) =>
            label != null && label.StartsWith("Zone") ? "zone " + label.Substring(4) : label;

        // ---------------------------------------------------------------------------------------
        // Toolbar
        // ---------------------------------------------------------------------------------------

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized || suppressModeEvents || sceneRoot == null) return;
            UpdateRoofCaseAvailability();
            Rebuild(resetCamera: false);
        }

        private void UpdateRoofCaseAvailability()
        {
            if (spRoofCase == null) return;
            var m = Mode;
            spRoofCase.IsEnabled = m == WindViewMode.MwfrsAlongLength || m == WindViewMode.MwfrsAlongWidth;
        }

        private void Outlines_Changed(object sender, RoutedEventArgs e)
        {
            if (outlines == null) return;
            bool show = cbOutlines.IsChecked == true;
            bool present = sceneRoot.Children.Contains(outlines);
            if (show && !present) sceneRoot.Children.Add(outlines);
            if (!show && present) sceneRoot.Children.Remove(outlines);
        }
    }
}

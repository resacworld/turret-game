using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using turret_game.ViewModels;
using System.Diagnostics;
using System.Windows.Threading;

namespace turret_game
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly Dictionary<SceneObjectViewModel, ModelVisual3D> _map = new();

        // Pour coalescer les rafraîchissements : éviter plusieurs RefreshVisual par frame
        private readonly HashSet<SceneObjectViewModel> _pendingRefresh = new();

        // Propriétés qui nécessitent un refresh visuel lorsqu'elles changent
        private static readonly HashSet<string> _transformProps = new(StringComparer.Ordinal)
        {
            "TX","TY","TZ","RX","RY","RZ",
            "OffTX","OffTY","OffTZ","OffRX","OffRY","OffRZ",
            "IsVisible","GetTransform"
        };

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;

            BtnLoad.Click += BtnLoad_Click;
            BtnLink.Click += BtnLink_Click;
            BtnSetParent.Click += BtnSetParent_Click;
            BtnSetChild.Click += BtnSetChild_Click;

            _vm.SceneObjects.CollectionChanged += SceneObjects_CollectionChanged;

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.Loaded();
        }

        private void BtnLoad_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Charger OBJ",
                Filter = "OBJ files (*.obj)|*.obj|All files (*.*)|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog(this) == true)
            {
                // Si plusieurs fichiers choisis -> on exécute la commande avec string[]
                if (dlg.FileNames.Length > 1)
                    _vm.AddFromFileCommand.Execute(dlg.FileNames);
                else if (dlg.FileName != null)
                    _vm.AddFromFileCommand.Execute(dlg.FileName);
            }
        }

        private void BtnSetParent_Click(object? sender, RoutedEventArgs e)
        {
            if (_vm.SelectedObject != null)
                _vm.SelectedParent = _vm.SelectedObject;
        }

        private void BtnSetChild_Click(object? sender, RoutedEventArgs e)
        {
            if (_vm.SelectedObject != null)
                _vm.SelectedChild = _vm.SelectedObject;
        }

        private void BtnLink_Click(object? sender, RoutedEventArgs e)
        {
            if (_vm.SelectedParent == null || _vm.SelectedChild == null)
            {
                MessageBox.Show("Veuillez définir à la fois un parent et un enfant (sélectionner un objet puis 'Définir comme parent/enfant').", "Lien invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ReferenceEquals(_vm.SelectedParent, _vm.SelectedChild))
            {
                MessageBox.Show("Le parent et l'enfant doivent être des objets différents.", "Lien invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _vm.LinkObjects(_vm.SelectedChild, _vm.SelectedParent, offTx: 0, offTy: 0, offTz: 0, offRx: 0, offRy: 0, offRz: 0);
            RefreshVisual(_vm.SelectedChild);
        }

        private void SceneObjects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SceneObjectViewModel so in e.NewItems)
                    AddVisualFor(so);
            }

            if (e.OldItems != null)
            {
                foreach (SceneObjectViewModel so in e.OldItems)
                    RemoveVisualFor(so);
            }
        }

        private void AddVisualFor(SceneObjectViewModel so)
        {
            if (so.Model == null) return;

            // Construire une Transform3DGroup réutilisable (local | offset | parentPlaceholder)
            var localGroup = new Transform3DGroup();
            var rotZ = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), so.RZ));
            var rotY = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), so.RY));
            var rotX = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), so.RX));
            var trans = new TranslateTransform3D(so.TX, so.TY, so.TZ);
            localGroup.Children.Add(rotZ);
            localGroup.Children.Add(rotY);
            localGroup.Children.Add(rotX);
            localGroup.Children.Add(trans);

            var offsetGroup = new Transform3DGroup();
            var offRotZ = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), so.OffRZ));
            var offRotY = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), so.OffRY));
            var offRotX = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), so.OffRX));
            var offTrans = new TranslateTransform3D(so.OffTX, so.OffTY, so.OffTZ);
            offsetGroup.Children.Add(offRotZ);
            offsetGroup.Children.Add(offRotY);
            offsetGroup.Children.Add(offRotX);
            offsetGroup.Children.Add(offTrans);

            // parent placeholder (sera remplacé par Parent.GetTransform() si nécessaire)
            var parentPlaceholder = new MatrixTransform3D();

            var root = new Transform3DGroup();
            root.Children.Add(localGroup);
            root.Children.Add(offsetGroup);
            root.Children.Add(parentPlaceholder);

            var visual = new ModelVisual3D { Content = so.IsVisible ? so.Model : null, Transform = root };
            _map[so] = visual;
            Viewport.Children.Add(visual);

            // réagir uniquement aux changements pertinents et coalescer les rafraîchissements
            so.PropertyChanged += (s, e) =>
            {
                if (e?.PropertyName == null) return;
                if (_transformProps.Contains(e.PropertyName))
                    ScheduleRefresh(so);
            };

            // Petit "warm up" : s'assurer que les transforms sont prêtes (évite la 1ère allocation lourde plus tard)
            // On force ici une assignation minimale (déjà faite) ; garder légère.
        }

        private void RemoveVisualFor(SceneObjectViewModel so)
        {
            if (_map.TryGetValue(so, out var visual))
            {
                Viewport.Children.Remove(visual);
                _map.Remove(so);
            }
            _pendingRefresh.Remove(so);
        }

        private void ScheduleRefresh(SceneObjectViewModel so)
        {
            if (!_pendingRefresh.Add(so)) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _pendingRefresh.Remove(so);
                RefreshVisual(so);
            }), DispatcherPriority.Render);
        }

        private void RefreshVisual(SceneObjectViewModel so)
        {
            if (!_map.TryGetValue(so, out var visual)) return;

            // Mettre à jour la transform existante en mutating les Rotate/Translate pour éviter allocations
            if (visual.Transform is Transform3DGroup root && root.Children.Count >= 3)
            {
                // local group
                if (root.Children[0] is Transform3DGroup local && local.Children.Count >= 4)
                {
                    if (local.Children[0] is RotateTransform3D rotZ && rotZ.Rotation is AxisAngleRotation3D rz)
                        rz.Angle = so.RZ;
                    if (local.Children[1] is RotateTransform3D rotY && rotY.Rotation is AxisAngleRotation3D ry)
                        ry.Angle = so.RY;
                    if (local.Children[2] is RotateTransform3D rotX && rotX.Rotation is AxisAngleRotation3D rx)
                        rx.Angle = so.RX;
                    if (local.Children[3] is TranslateTransform3D t)
                    {
                        t.OffsetX = so.TX;
                        t.OffsetY = so.TY;
                        t.OffsetZ = so.TZ;
                    }
                }

                // offset group
                if (root.Children[1] is Transform3DGroup offset && offset.Children.Count >= 4)
                {
                    if (offset.Children[0] is RotateTransform3D orz && orz.Rotation is AxisAngleRotation3D orzR)
                        orzR.Angle = so.OffRZ;
                    if (offset.Children[1] is RotateTransform3D ory && ory.Rotation is AxisAngleRotation3D oryR)
                        oryR.Angle = so.OffRY;
                    if (offset.Children[2] is RotateTransform3D orx && orx.Rotation is AxisAngleRotation3D orxR)
                        orxR.Angle = so.OffRX;
                    if (offset.Children[3] is TranslateTransform3D ot)
                    {
                        ot.OffsetX = so.OffTX;
                        ot.OffsetY = so.OffTY;
                        ot.OffsetZ = so.OffTZ;
                    }
                }

                // mettre à jour le parent (on remplace la référence, cheap)
                if (root.Children[2] == null || !(so.Parent == null ? root.Children[2] is MatrixTransform3D : ReferenceEquals(root.Children[2], so.Parent.GetTransform())))
                {
                    root.Children[2] = so.Parent != null ? so.Parent.GetTransform() : new MatrixTransform3D();
                }
            }
            else
            {
                // fallback (rare) : assigner le transform complet
                visual.Transform = so.GetTransform();
            }

            // Affiche/masque le contenu (peu coûteux)
            visual.Content = so.IsVisible ? so.Model : null;
        }
    }
}
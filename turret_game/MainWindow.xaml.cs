using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using turret_game.ViewModels;
using System.Diagnostics;

namespace turret_game
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;
        private readonly Dictionary<SceneObjectViewModel, ModelVisual3D> _map = new();

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
            var visual = new ModelVisual3D { Content = so.IsVisible ? so.Model : null, Transform = so.GetTransform() };
            _map[so] = visual;
            Viewport.Children.Add(visual);
            // réagir aux changements de transform ou visibilité
            so.PropertyChanged += (s, e) => RefreshVisual(so);
        }

        private void RemoveVisualFor(SceneObjectViewModel so)
        {
            if (_map.TryGetValue(so, out var visual))
            {
                Viewport.Children.Remove(visual);
                _map.Remove(so);
            }
        }

        private void RefreshVisual(SceneObjectViewModel so)
        {
            if (_map.TryGetValue(so, out var visual))
            {
                // Met à jour la transform (recalcule parent si nécessaire)
                visual.Transform = so.GetTransform();
                // Affiche/masque le contenu
                visual.Content = so.IsVisible ? so.Model : null;
            }
        }
    }
}
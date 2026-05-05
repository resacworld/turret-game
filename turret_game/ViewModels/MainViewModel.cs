using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using turret_game.Services;
using System.Windows.Input;

namespace turret_game.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ObjLoaderService _loader = new ObjLoaderService();

        public ObservableCollection<SceneObjectViewModel> SceneObjects { get; } = new();

        private SceneObjectViewModel? _selected;
        public SceneObjectViewModel? SelectedObject
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        private SceneObjectViewModel? _selectedParent;
        public SceneObjectViewModel? SelectedParent
        {
            get => _selectedParent;
            set { _selectedParent = value; OnPropertyChanged(); }
        }

        private SceneObjectViewModel? _selectedChild;
        public SceneObjectViewModel? SelectedChild
        {
            get => _selectedChild;
            set { _selectedChild = value; OnPropertyChanged(); }
        }

        public ICommand AddFromFileCommand { get; }

        public MainViewModel()
        {
            AddFromFileCommand = new RelayCommand(p =>
            {
                // Supporte un seul chemin (string) ou plusieurs (string[])
                if (p is string path && !string.IsNullOrWhiteSpace(path))
                {
                    AddFile(path);
                }
                else if (p is string[] paths)
                {
                    foreach (var fp in paths)
                        AddFile(fp);
                }
            });
        }

        private void AddFile(string path)
        {
            var model = _loader.Load(path);
            var obj = new SceneObjectViewModel { Model = model, Name = System.IO.Path.GetFileNameWithoutExtension(path) };
            SceneObjects.Add(obj);
            SelectedObject = obj;
        }

        // Lier child à parent en utilisant des offsets (en degrés et unités) :
        public void LinkObjects(SceneObjectViewModel child, SceneObjectViewModel parent,
            double offTx = 0, double offTy = 0, double offTz = 0,
            double offRx = 0, double offRy = 0, double offRz = 0)
        {
            child.OffTX = offTx;
            child.OffTY = offTy;
            child.OffTZ = offTz;
            child.OffRX = offRx;
            child.OffRY = offRy;
            child.OffRZ = offRz;
            child.LinkToParent(parent);
            // notifier
            child.OnPropertyChanged(nameof(child));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
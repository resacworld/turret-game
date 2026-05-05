using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Media3D;

namespace turret_game.ViewModels
{
    public class SceneObjectViewModel : INotifyPropertyChanged
    {
        public string Name { get; set; } = "Object";

        private double _tx, _ty, _tz;
        private double _rx, _ry, _rz;

        // Offset applied when linked to a parent (relative pose)
        private double _offTx, _offTy, _offTz;
        private double _offRx, _offRy, _offRz;

        public Model3D Model { get; set; }

        public SceneObjectViewModel? Parent { get; private set; }

        public double TX { get => _tx; set { if (SetField(ref _tx, value)) NotifyTransformChanged(); } }
        public double TY { get => _ty; set { if (SetField(ref _ty, value)) NotifyTransformChanged(); } }
        public double TZ { get => _tz; set { if (SetField(ref _tz, value)) NotifyTransformChanged(); } }
        public double RX { get => _rx; set { if (SetField(ref _rx, value)) NotifyTransformChanged(); } }
        public double RY { get => _ry; set { if (SetField(ref _ry, value)) NotifyTransformChanged(); } }
        public double RZ { get => _rz; set { if (SetField(ref _rz, value)) NotifyTransformChanged(); } }

        public double OffTX { get => _offTx; set { if (SetField(ref _offTx, value)) NotifyTransformChanged(); } }
        public double OffTY { get => _offTy; set { if (SetField(ref _offTy, value)) NotifyTransformChanged(); } }
        public double OffTZ { get => _offTz; set { if (SetField(ref _offTz, value)) NotifyTransformChanged(); } }
        public double OffRX { get => _offRx; set { if (SetField(ref _offRx, value)) NotifyTransformChanged(); } }
        public double OffRY { get => _offRy; set { if (SetField(ref _offRy, value)) NotifyTransformChanged(); } }
        public double OffRZ { get => _offRz; set { if (SetField(ref _offRz, value)) NotifyTransformChanged(); } }

        private void ParentListener(object? sender, PropertyChangedEventArgs e)
        {
            // Quand le parent change de pose, invalide la transform du child
            NotifyTransformChanged();
        }

        public void LinkToParent(SceneObjectViewModel parent)
        {
            // detach existing parent listener si nécessaire
            if (Parent != null)
                Parent.PropertyChanged -= ParentListener;

            Parent = parent;

            if (Parent != null)
                Parent.PropertyChanged += ParentListener;

            NotifyTransformChanged();
        }

        public void Unlink()
        {
            if (Parent != null)
                Parent.PropertyChanged -= ParentListener;

            Parent = null;
            NotifyTransformChanged();
        }

        // Calcule la transform appliquée à ce modèle (parent puis offset puis local)
        public Transform3D GetTransform()
        {
            // Local transform from TX/TY/TZ and RX/RY/RZ (degrees)
            var localGroup = new Transform3DGroup();
            // rotations (order RZ, RY, RX typical)
            localGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), RZ)));
            localGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), RY)));
            localGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), RX)));
            localGroup.Children.Add(new TranslateTransform3D(TX, TY, TZ));

            // Offset transform (applied relative to parent)
            var offsetGroup = new Transform3DGroup();
            offsetGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), OffRZ)));
            offsetGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), OffRY)));
            offsetGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), OffRX)));
            offsetGroup.Children.Add(new TranslateTransform3D(OffTX, OffTY, OffTZ));

            var result = new Transform3DGroup();

            if (Parent != null)
            {
                // IMPORTANT: add local then offset then parent so que l'application réelle soit
                // parent * offset * local  (local appliqué en premier, parent en dernier)
                result.Children.Add(localGroup);
                result.Children.Add(offsetGroup);
                result.Children.Add(Parent.GetTransform());
            }
            else
            {
                // sans parent: offset * local => local appliqué en premier
                result.Children.Add(localGroup);
                result.Children.Add(offsetGroup);
            }

            return result;
        }

        private void NotifyTransformChanged()
        {
            OnPropertyChanged(nameof(GetTransform));
            OnPropertyChanged(nameof(TX));
            OnPropertyChanged(nameof(TY));
            OnPropertyChanged(nameof(TZ));
            OnPropertyChanged(nameof(RX));
            OnPropertyChanged(nameof(RY));
            OnPropertyChanged(nameof(RZ));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
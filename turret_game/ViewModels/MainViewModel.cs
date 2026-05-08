using HelixToolkit.Maths;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using turret_game.Objects;
using turret_game.Services;

namespace turret_game.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
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

        public Cannon Cannon { get; } = new Cannon();
        public System.Windows.Media.Media3D.Model3D Rect { get; } = ObjLoaderService.GetBox(1, 1, 10, 10, 10, 5);

        public ICommand AddFromFileCommand { get; }

        // game state
        private readonly List<Enemy> _enemies = new();
        private readonly DispatcherTimer _gameTimer;
        private readonly Random _rnd = new();

        public MainViewModel()
        {
            // timer pour update simple (UI thread)
            _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5) };
            _gameTimer.Tick += (s, e) => GameTick(_gameTimer.Interval.TotalSeconds);

            Cannon.PropertyChanged += Cannon_PropertyChanged;
        }

        private void Cannon_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            foreach(var enemy in _enemies)
            {
                if (!enemy.IsDead)
                {
                    // mettre à jour la cible de chaque ennemi vers la nouvelle position du canon
                    enemy.SetTarget(new Point3D(Cannon.TX, Cannon.TY, Cannon.TZ));
                }
            }
        }

        public void Loaded()
        {
            // ajouter la tourelle à la scène
            AddObjectsToScene(Cannon.SceneObjects);

            // positionner la tourelle au centre (exemple)
            Cannon.TX = 0;
            Cannon.TY = 0;
            Cannon.TZ = 1.0;

            _gameTimer.Start();
        }

        private void SpawnEnemiesAroundCenter(int count, double radius, double elevation)
        {
            for (int i = 0; i < count; i++)
            {
                // angle en radians réparti uniformément + petite variation aléatoire
                double angle = 2.0 * Math.PI * i / count + (_rnd.NextDouble() * 0.15 - 0.075);
                double x = Math.Cos(angle) * radius;
                double y = Math.Sin(angle) * radius;
                var start = new Point3D(x, y, elevation + _rnd.NextFloat(-85, 85));

                // cible : centre de la map (0,0, hauteur du canon)
                var target = new Point3D(0, 0, Cannon.TZ);

                var enemy = new Enemy(start, target, speed: 15.0 + _rnd.NextDouble() * 15.0, health: 100.0);
                _enemies.Add(enemy);
                AddObjectToScene(enemy.Body);

                // orienter visuellement l'enemy vers le centre si besoin
                var dx = target.Tx - start.Tx;
                var dy = target.Ty - start.Ty;
                var yawDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                enemy.Body.RZ = yawDeg;
            }
        }

        private void GameTick(double dt)
        {
            if (dt <= 0) return;

            // update ennemis (avancent vers le centre)
            foreach (var e in _enemies.ToList())
            {
                e.Update(dt);
            }

            // tourelle vise & tire automatiquement
            Cannon.Update(dt, _enemies);

            // retirer morts
            var dead = _enemies.Where(x => x.IsDead).ToList();
            foreach (var d in dead)
            {
                _enemies.Remove(d);
                if (SceneObjects.Contains(d.Body))
                    SceneObjects.Remove(d.Body);
            }

            if(_enemies.Count <= 2)
            {
                SpawnEnemiesAroundCenter(count: 12, radius: 80.0, elevation: 2.0);
            }
        }

        private void AddObjectToScene(SceneObjectViewModel obj)
        {
            if (!SceneObjects.Contains(obj))
                SceneObjects.Add(obj);
        }

        private void AddObjectsToScene(List<SceneObjectViewModel> objs)
        {
            foreach (var obj in objs)
            {
                if (!SceneObjects.Contains(obj))
                    SceneObjects.Add(obj);
            }
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
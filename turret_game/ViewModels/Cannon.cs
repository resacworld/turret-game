using turret_game.Objects;
using turret_game.Services;

namespace turret_game.ViewModels
{
    public class Cannon
    {
        public SceneObjectViewModel Head { get; } = new SceneObjectViewModel
        {
            Model = ObjLoaderService.Load("C:\\Users\\Victor - User\\github\\Turret_game\\turret_game\\3Dmodels\\center.obj"),
            Name = "Head"
        };

        public SceneObjectViewModel Cannons { get; } = new SceneObjectViewModel
        {
            Model = ObjLoaderService.Load("C:\\Users\\Victor - User\\github\\Turret_game\\turret_game\\3Dmodels\\cannons.obj"),
            Name = "Cannons"
        };

        public SceneObjectViewModel Fire { get; } = new SceneObjectViewModel
        {
            Model = ObjLoaderService.GetBox(-3, (float)0.2, (float)0.2, -2, 0, (float)0),
            Name = "Cannons"
        };

        public List<SceneObjectViewModel> SceneObjects => new() { Head, Cannons, Fire };

        public double Axis1 { get => Head.RZ; set { Head.RZ = value; } }
        public double Axis2 { get => Cannons.RX; set { Cannons.RX = value; } }

        public double TX { get => Head.TX; set { Head.TX = value; } }
        public double TY { get => Head.TY; set { Head.TY = value; } }
        public double TZ { get => Head.TZ; set { Head.TZ = value; } }

        // Gameplay
        public double Range { get; set; } = 50.0;
        public double FireRate { get; set; } = 2.0; // tirs par seconde
        public double Damage { get; set; } = 50.0;

        private double _cooldown = 0.0;
        private int _fireToken = 0;

        public Cannon()
        {
            Cannons.LinkToParent(Head);
            Fire.LinkToParent(Cannons);

            // si nécessaire, ajuste Head.OffRZ ou Head.OffTZ pour aligner le mesh
            Head.OffTZ = 0.0;
            Fire.IsVisible = false;
        }

        private static double RadToDeg(double rad) => rad * (180.0 / Math.PI);

        private static double NormalizeAngleDeg(double deg)
        {
            double a = deg % 360.0;
            if (a <= -180.0) a += 360.0;
            else if (a > 180.0) a -= 360.0;
            return a;
        }

        /// <summary>
        /// Oriente la tourelle vers un point 3D (Z = up).
        /// </summary>
        public void OrientTo(Point3D point)
        {
            var dx = -(point.Tx - Head.TX);
            var dy = -(point.Ty - Head.TY);
            var dz = point.Tz - Head.TZ;

            // yaw autour de Z (angle dans le plan X/Y)
            var yawDeg = RadToDeg(Math.Atan2(dy, dx));
            Head.RZ = NormalizeAngleDeg(yawDeg);

            // pitch (élévation) : angle entre ligne de visée et plan horizontal
            var horiz = Math.Sqrt(dx * dx + dy * dy);
            var pitchDeg = RadToDeg(Math.Atan2(dz, horiz));
            // inversion possible selon orientation du mesh ; ici on applique -pitch si nécessaire
            Cannons.RY = pitchDeg;
        }

        /// <summary>
        /// Update appelé chaque frame par MainViewModel : vise le plus proche ennemi et tire.
        /// </summary>
        public void Update(double dt, IEnumerable<Enemy> enemies)
        {
            if (dt <= 0) return;
            _cooldown -= dt;

            var alive = enemies?.Where(e => !e.IsDead).ToList() ?? new List<Enemy>();
            if (!alive.Any()) return;

            Enemy? target = null;
            double bestDistSq = double.MaxValue;
            foreach (var e in alive)
            {
                var dx = e.Position.Tx - Head.TX;
                var dy = e.Position.Ty - Head.TY;
                var dz = e.Position.Tz - Head.TZ;
                var distSq = dx * dx + dy * dy + dz * dz;
                if (distSq < bestDistSq && Math.Sqrt(distSq) <= Range)
                {
                    bestDistSq = distSq;
                    target = e;
                }
            }

            if (target == null) return;

            // Viser la cible
            OrientTo(target.Position);

            // Tir automatique (instant hit)
            if (_cooldown <= 0)
            {
                Fire.IsVisible = true;
                target.Hit(Damage);
                _cooldown = 1.0 / Math.Max(1e-4, FireRate);

                // Incrémente un token atomiquement pour identifier ce tir.
                var token = System.Threading.Interlocked.Increment(ref _fireToken);

                // Tâche non bloquante : attend 200 ms puis cache Fire si le token est toujours le dernier.
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(200);

                    // Si un tir plus récent a eu lieu, ne pas cacher l'effet du tir récent.
                    if (token != _fireToken) return;

                    var app = System.Windows.Application.Current;
                    if (app?.Dispatcher != null)
                    {
                        // Mettre à jour sur le thread UI
                        app.Dispatcher.BeginInvoke(new System.Action(() => Fire.IsVisible = false));
                    }
                    else
                    {
                        // Fallback si pas d'Application disponible
                        Fire.IsVisible = false;
                    }
                });
            }
        }
    }
}

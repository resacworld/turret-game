using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using turret_game.Objects;
using turret_game.Services;

namespace turret_game.ViewModels
{
    // Plan (pseudocode) :
    // 1) Calculer l'angle actuel "continu" de la tête pendant une transition (éviter de se baser
    //    uniquement sur Head.RZ normalisé) pour éviter des sauts lors d'un nouvel objectif.
    // 2) Lorsqu'on démarre une nouvelle orientation (OrientTo), définir _startRZ à l'angle continu
    //    actuel, calculer le delta angulaire le plus court via NormalizeAngleDeg, et appliquer
    //    _targetRZ = _startRZ + delta (target en espace continu). Faire de même pour RY (pitch).
    // 3) Interpoler linéairement entre _start et _target en espace continu pendant Update.
    //    Ne normaliser que pour l'affichage Head.RZ = NormalizeAngleDeg(...).
    // 4) Quand la transition est terminée, réinitialiser _start/_target en valeurs normalisées
    //    pour maintenir la cohérence pour les futures orientations.
    // 5) Conserver la logique de tir et visibilité, mais s'assurer que IsOrientedToTarget compare
    //    correctement en utilisant NormalizeAngleDeg sur la cible continue.
    //
    // Ces changements évitent les "saccades" provoquées par le wrapping angulaire et par des
    // réinitialisations brutales des angles de départ/arrivée lors de transitions successives.

    public class Cannon : INotifyPropertyChanged
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

        public double Axis1 { get => Head.RZ; set { Head.RZ = value; OnPropertyChanged(nameof(Axis1)); } }
        public double Axis2 { get => Cannons.RY; set { Cannons.RY = value; OnPropertyChanged(nameof(Axis2)); } }

        public double TX { get => Head.TX; set { Head.TX = value; } }
        public double TY { get => Head.TY; set { Head.TY = value; } }
        public double TZ { get => Head.TZ; set { Head.TZ = value; } }

        // Gameplay
        public double Range { get; set; } = 50.0;
        public double FireRate { get; set; } = 7.0; // tirs par seconde
        public double Damage { get; set; } = 50.0;

        private double _cooldown = 0.0;
        private int _fireToken = 0;

        // Orientation transition fields
        private readonly double _orientTime = 0.05; // durée de transition en secondes
        private double _orientElapsed = 10.0;
        private double _startRZ;
        private double _targetRZ;
        private double _startRY;
        private double _targetRY;
        private const double OrientEpsilon = 0.1; // seuil en degrés pour considérer un changement

        // Nouvelle tolérance pour considérer que la tourelle est "orientée" vers la cible pour tirer
        private const double AimEpsilonDeg = 1.0;

        public Cannon()
        {
            Cannons.LinkToParent(Head);
            Fire.LinkToParent(Cannons);

            // si nécessaire, ajuste Head.OffRZ ou Head.OffTZ pour aligner le mesh
            Head.OffTZ = 0.0;
            Fire.IsVisible = false;

            // initialiser les valeurs d'orientation pour la transition
            _startRZ = Head.RZ;
            _targetRZ = Head.RZ;
            _startRY = Cannons.RY;
            _targetRY = Cannons.RY;
            _orientElapsed = _orientTime;
        }

        private static double RadToDeg(double rad) => rad * (180.0 / Math.PI);

        private static double NormalizeAngleDeg(double deg)
        {
            double a = deg % 360.0;
            if (a <= -180.0) a += 360.0;
            else if (a > 180.0) a -= 360.0;
            return a;
        }

        // Retourne l'angle de la tête en espace "continu" en prenant en compte la progression actuelle
        // si une transition est en cours. Evite de brusques sauts quand on interrompt/re-définit la cible.
        private double GetCurrentHeadAngleContinuous()
        {
            if (_orientElapsed < _orientTime)
            {
                var t = Math.Max(0.0, Math.Min(1.0, _orientElapsed / _orientTime));
                return _startRZ + (_targetRZ - _startRZ) * t;
            }

            return _targetRZ;
        }

        // Même principe pour le pitch (RY), mais sans wrapping angulaire important.
        private double GetCurrentCannonsPitch()
        {
            if (_orientElapsed < _orientTime)
            {
                var t = Math.Max(0.0, Math.Min(1.0, _orientElapsed / _orientTime));
                return _startRY + (_targetRY - _startRY) * t;
            }

            return _targetRY;
        }

        /// <summary>
        /// Oriente la tourelle vers un point 3D (Z = up).
        /// Cette méthode définit la cible d'orientation ; la transition sera interpolée sur 1s dans Update.
        /// Pour éviter les saccades, on travaille en espace d'angles "continus" et on applique le plus court
        /// chemin angulaire en ajustant _targetRZ relatif à _startRZ.
        /// </summary>
        public void OrientTo(Point3D point)
        {
            var dx = -(point.Tx - Head.TX);
            var dy = -(point.Ty - Head.TY);
            var dz = point.Tz - Head.TZ;

            // yaw autour de Z (angle dans le plan X/Y)
            var yawDeg = RadToDeg(Math.Atan2(dy, dx));
            yawDeg = NormalizeAngleDeg(yawDeg);

            // pitch (élévation) : angle entre ligne de visée et plan horizontal
            var horiz = Math.Sqrt(dx * dx + dy * dy);
            var pitchDeg = RadToDeg(Math.Atan2(dz, horiz));

            // Calculer différences par rapport à la cible connue (_targetRZ/_targetRY)
            var yawDiff = Math.Abs(NormalizeAngleDeg(yawDeg - NormalizeAngleDeg(_targetRZ)));
            var pitchDiff = Math.Abs(pitchDeg - _targetRY);

            if (yawDiff > OrientEpsilon || pitchDiff > OrientEpsilon)
            {
                // récupérer l'angle actuel continu (évite discontinuités si on est en pleine interpolation)
                var currentHead = GetCurrentHeadAngleContinuous();
                var currentPitch = GetCurrentCannonsPitch();

                // définir le départ aux angles actuels continus
                _startRZ = currentHead;
                _startRY = currentPitch;

                // calculer le delta angulaire le plus court et appliquer la cible en espace continu
                var deltaYaw = NormalizeAngleDeg(yawDeg - _startRZ);
                _targetRZ = _startRZ + deltaYaw;

                // pitch n'a pas de wrapping complexe : target = pitchDeg directement
                _targetRY = pitchDeg;

                _orientElapsed = 0.0;
            }

            // L'interpolation réelle s'effectue dans Update().
        }

        /// <summary>
        /// Retourne vrai si l'orientation actuelle est suffisamment proche de la cible pour autoriser le tir.
        /// On compare les angles en prenant en compte le wrapping via NormalizeAngleDeg.
        /// </summary>
        private bool IsOrientedToTarget()
        {
            var yawError = Math.Abs(NormalizeAngleDeg(Head.RZ - NormalizeAngleDeg(_targetRZ)));
            var pitchError = Math.Abs(Cannons.RY - _targetRY);
            return yawError <= AimEpsilonDeg && pitchError <= AimEpsilonDeg;
        }

        /// <summary>
        /// Update appelé chaque frame par MainViewModel : vise le plus proche ennemi et tire.
        /// Ne tire que si la tourelle est orientée vers la cible.
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

            // Définir la nouvelle orientation cible (ne change pas l'orientation instantanément)
            OrientTo(target.Position);

            // Avancer la transition d'orientation
            if (_orientElapsed < _orientTime)
            {
                _orientElapsed = Math.Min(_orientTime, _orientElapsed + dt);
                var t = Math.Max(0.0, Math.Min(1.0, _orientElapsed / _orientTime));

                // interpolation en espace continu (évite wrapping brusque)
                var newRZ = _startRZ + (_targetRZ - _startRZ) * t;
                Axis1 = NormalizeAngleDeg(newRZ); 

                var newRY = _startRY + (_targetRY - _startRY) * t;
                Axis2 = newRY;

                // si la transition vient de se terminer, normaliser les endpoints pour la prochaine opération
                if (_orientElapsed >= _orientTime)
                {
                    _startRZ = _targetRZ = NormalizeAngleDeg(_targetRZ);
                    _startRY = _targetRY = _targetRY;
                    _orientElapsed = _orientTime;
                }
            }
            else
            {
                // s'assurer que la valeur finale est exactement la cible (normalisée pour RZ)
                Axis1 = NormalizeAngleDeg(_targetRZ);
                Axis2 = _targetRY;

                // garder les start/target cohérents pour la prochaine OrientTo
                _startRZ = _targetRZ = NormalizeAngleDeg(_targetRZ);
                _startRY = _targetRY = _targetRY;
            }

            // Tir automatique (instant hit) : autorisé seulement si la tourelle est orientée vers la cible
            if (_cooldown <= 0 && IsOrientedToTarget())
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

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

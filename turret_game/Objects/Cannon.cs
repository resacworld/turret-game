using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using turret_game.Services;
using turret_game.ViewModels;

namespace turret_game.Objects
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

        public List<SceneObjectViewModel> SceneObjects
        {
            get
            {
                return new List<SceneObjectViewModel> { Head, Cannons };
            }
        }

        // Axis1 -> rotation autour de l'axe vertical (Z)
        public double Axis1 { get => Head.RZ; set { Head.RZ = value;  } }
        // Axis2 -> élévation des canons (rotation autour de Y)
        public double Axis2 { get => Cannons.RX; set { Cannons.RX = value; } }

        // Gameplay
        public double Range { get; set; } = 40.0;
        public double FireRate { get; set; } = 1.0; // shots per second
        public double Damage { get; set; } = 100.0;

        private double _cooldown = 0.0;

        public Cannon()
        {
            Cannons.LinkToParent(Head);
            Cannons.OffTX = 0;
            Cannons.OffTY = 0;
            Cannons.OffTZ = 0;
            Cannons.OffRX = 0;
            Cannons.OffRY = 0;
            Cannons.OffRZ = 0;

            Head.OffRZ = 0;
            Head.OffTZ = 1.5;
        }

        private static double NormalizeAngle(double degrees)
        {
            double angle = degrees % 360.0;
            if (angle > 180.0) angle -= 360.0;
            if (angle <= -180.0) angle += 360.0;
            return angle;
        }

        // dt en secondes ; vise et tire sur la liste d'ennemis (turret_game.Objects.Enemy)
        public void Update(double dt, IEnumerable<turret_game.Objects.Enemy> enemies)
        {
            if (dt <= 0) return;
            _cooldown -= dt;

            var alive = enemies?.Where(e => !e.IsDead).ToList() ?? new List<turret_game.Objects.Enemy>();
            if (!alive.Any()) return;

            turret_game.Objects.Enemy target = null;
            double bestDistSq = double.MaxValue;
            foreach (var e in alive)
            {
                var dx = e.Position.Tx - Head.TX;
                var dy = e.Position.Ty - Head.TY; // plan horizontal
                var dz = e.Position.Tz - Head.TZ; // vertical
                var distSq = dx * dx + dy * dy + dz * dz;
                if (distSq < bestDistSq && Math.Sqrt(distSq) <= Range)
                {
                    bestDistSq = distSq;
                    target = e;
                }
            }

            if (target == null) return;

            // Yaw: angle dans le plan X/Y. Atan2(y, x) fournit l'angle depuis l'axe X.
            var dxH = target.Position.Tx - Head.TX;
            var dyH = target.Position.Ty - Head.TY;
            var yawDeg = Math.Atan2(dyH, dxH) * 180.0 / Math.PI;

            // Le modèle a son "forward" inversé -> ajouter 180° pour corriger l'opposé.
            Head.RZ = NormalizeAngle(yawDeg + 180.0);

            // Elevation: angle vertical (Z = up)
            var horizontalDist = Math.Sqrt(dxH * dxH + dyH * dyH);
            var dzTarget = target.Position.Tz - Head.TZ;
            var pitchDeg = Math.Atan2(dzTarget, horizontalDist) * 180.0 / Math.PI;
            // Application commune : inversion possible selon orientation du mesh; conserver -pitch si nécessaire.
            Cannons.RX = -pitchDeg;

            if (_cooldown <= 0)
            {
                // Tire sur la cible
                target.Hit(Damage);
                _cooldown = 1.0 / Math.Max(0.0001, FireRate);
            }
        }

        /// <summary>
        /// Oriente la tourelle vers un point donné (utilitaire)
        /// </summary>
        public void OrientTo(Point3D point)
        {
            var dy = point.Tx - Head.TX;
            var dx = point.Ty - Head.TY;
            var dz = point.Tz - Head.TZ;

            var yawDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            Head.RZ = NormalizeAngle(yawDeg + 180.0);

            var horiz = Math.Sqrt(dx * dx + dy * dy);
            var pitchDeg = Math.Atan2(dz, horiz) * 180.0 / Math.PI;
            Cannons.RX = -pitchDeg;
        }
    }
}

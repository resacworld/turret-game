using System.Windows.Media.Media3D;
using turret_game.ViewModels;
using turret_game.Services;

namespace turret_game.Objects
{
    public class Enemy
    {
        public SceneObjectViewModel Body { get; }

        public double Health { get; private set; } = 100.0;
        public double Speed { get; set; } = 6.0; // units per second
        public bool IsDead => Health <= 0.0;

        private Point3D _target;

        public Enemy(Point3D start, Point3D target, double speed = 6.0, double health = 100.0)
        {
            Body = new SceneObjectViewModel
            {
                Model = ObjLoaderService.GetSphere(0.8f),
                Name = "Enemy"
            };

            Speed = speed;
            Health = health;
            _target = target;
            SetPosition(start);
        }

        public Point3D Position => new Point3D(Body.TX, Body.TY, Body.TZ);

        public void SetTarget(Point3D target) => _target = target;

        public void Hit(double damage)
        {
            if (IsDead) return;
            Health -= damage;
            if (Health <= 0) Health = 0;
        }

        // dt en secondes : avance vers _target
        public void Update(double dt)
        {
            if (IsDead || dt <= 0) return;

            var dir = new Vector3D(_target.Tx - Body.TX, _target.Ty - Body.TY, _target.Tz - Body.TZ);
            var dist = dir.Length;
            if (dist <= 0.0001) return;

            dir.Normalize();
            var travel = Math.Min(Speed * dt, dist);
            var newPos = new Point3D(Body.TX + dir.X * travel, Body.TY + dir.Y * travel, Body.TZ + dir.Z * travel);
            SetPosition(newPos);
        }

        private void SetPosition(Point3D p)
        {
            Body.TX = p.Tx;
            Body.TY = p.Ty;
            Body.TZ = p.Tz;
        }
    }
}
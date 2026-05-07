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

        public List<SceneObjectViewModel> SceneObjects
        {
            get
            {
                return new List<SceneObjectViewModel> { Head, Cannons };
            }
        }

        public double Axis1 { get => Head.RZ; set { Head.RZ = value;  } }
        public double Axis2 { get => Cannons.RY; set { Cannons.RY = value; } }

        public Cannon()
        {
            Cannons.LinkToParent(Head);
            Cannons.OffTX = 0;
            Cannons.OffTY = 0;
            Cannons.OffTZ = 0;
            Cannons.OffRX = 0;
            Cannons.OffRY = 0;
            Cannons.OffRZ = 0;
        }
    }
}

using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using System;
using System.Windows.Media.Media3D;

namespace turret_game.Services
{
    public static class ObjLoaderService
    {
        public static Model3D Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path");
            ModelImporter importer = new ModelImporter();
            // Vous pouvez configurer importer.DefaultMaterial ici si besoin.
            return importer.Load(path);
        }

        public static Model3D GetSphere(float radius)
        {
            var builder = new MeshBuilder();
            builder.AddSphere(new  System.Numerics.Vector3(0, 0, 0), radius);
            var mesh = builder.ToMesh();
            var material = new DiffuseMaterial(System.Windows.Media.Brushes.Red);
            return new GeometryModel3D(mesh.ToWndMeshGeometry3D(), material);
        }

        public static Model3D GetBox(float xlength, float ylength, float zlength, float posx=0, float posy=0, float posz=0)
        {
            var builder = new MeshBuilder();
            builder.AddBox(new System.Numerics.Vector3(posx, posy, posz), xlength, ylength, zlength);
            var mesh = builder.ToMesh();
            var material = new DiffuseMaterial(System.Windows.Media.Brushes.Red);
            return new GeometryModel3D(mesh.ToWndMeshGeometry3D(), material);
        }
    }
}
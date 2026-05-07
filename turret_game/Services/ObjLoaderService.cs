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
    }
}
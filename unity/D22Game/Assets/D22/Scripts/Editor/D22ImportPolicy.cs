using UnityEditor;

namespace D22.Editor
{
    public sealed class D22ImportPolicy : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/D22/Art/") || !assetPath.Contains("/Models/")) return;
            var model = (ModelImporter)assetImporter;
            model.globalScale = 1;
            model.useFileScale = true;
            model.importCameras = false;
            model.importLights = false;
            model.importAnimation = false;
            model.importBlendShapes = false;
            model.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            model.materialLocation = ModelImporterMaterialLocation.InPrefab;
            model.importNormals = ModelImporterNormals.Import;
            model.importTangents = ModelImporterTangents.CalculateMikk;
            model.generateSecondaryUV = true;
            model.secondaryUVPackMargin = 8;
            model.isReadable = false;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/D22/Art/") || !assetPath.Contains("/Textures/")) return;
            var texture = (TextureImporter)assetImporter;
            texture.maxTextureSize = 2048;
            texture.mipmapEnabled = true;
            texture.textureCompression = TextureImporterCompression.CompressedHQ;
            texture.alphaSource = TextureImporterAlphaSource.FromInput;
            if (assetPath.Contains("MetallicSmoothness")) texture.sRGBTexture = false;
        }
    }
}

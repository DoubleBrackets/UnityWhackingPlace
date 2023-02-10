using System.IO;
using MushiCore.Editor;
using UnityEditor;
using UnityEngine;

namespace MushiURPTools.PostProcessing
{
    public class PostProcessScriptTemplates
    {
        private static string path = "Assets/MushiStuff/MushiURPTools/PostProcessing/Editor";

        private static string volumePostfix = "VolumeComponent";

        [MenuItem("Assets/Create/MushiStuff/MushiURPTools/Custom Post Process Component", false, 0)]
        public static void CreateVolumeComponentScript()
        {
            AssetTemplateUtility.CreateNamedAssetFromTemplate(
                $"{path}/VolumeComponentTemplate.txt",
                "CustomVolumeComponent.cs",
                "#FILENAME#",
                appendPostfix: volumePostfix,
                onAssetCreated: OnVolumeScriptGenerated
            );

            void OnVolumeScriptGenerated(string volumeScriptPath)
            {
                string name = Path.GetFileNameWithoutExtension(volumeScriptPath);
                name = name.Substring(0, name.Length - volumePostfix.Length);

                string directory = Path.GetDirectoryName(volumeScriptPath);

                var replace = new AssetTemplateUtility.SimpleReplacement("#FILENAME#", name);
                var feature = new AssetTemplateUtility.AddPostfix("RenderFeature");
                var pass = new AssetTemplateUtility.AddPostfix("RenderPass");

                var processors = new AssetTemplateUtility.TemplateStringProcessor[] { replace};
                var nameProcessor = new AssetTemplateUtility.TemplateStringProcessor[] { feature };
                
                AssetTemplateUtility.CreateAssetFromTemplate(
                    $"{path}/PPRenderFeatureTemplate.txt",
                    $"{directory}/{name}.cs",
                    processors,
                    nameProcessor
                );

                nameProcessor[0] = pass;
                
                AssetTemplateUtility.CreateAssetFromTemplate(
                    $"{path}/PPRenderPassTemplate.txt",
                    $"{directory}/{name}.cs",
                    processors,
                    nameProcessor
                );
            }
        }
    }
}
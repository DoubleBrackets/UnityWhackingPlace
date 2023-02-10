#region

using Febucci.UI.Core;
using UnityEngine;
using UnityEngine.Assertions;

#endregion

namespace Febucci.UI.Examples
{
    [AddComponentMenu("")]
    public class EffectsTesting : MonoBehaviour
    {
        public TextAnimatorPlayer textAnimatorPlayer;

        private void Awake()
        {
            Assert.IsNotNull(textAnimatorPlayer, $"Text Animator Player component is null in {gameObject.name}");

            TAnimBuilder.InitializeGlobalDatabase();
        }

        private void Start()
        {
            ShowText();
        }

        public static string AddEffect(string tag)
        {
            return $"<{tag}><noparse><{tag}></noparse></{tag}>, ";
        }

        public static string AddAppearanceEffect(string tag)
        {
            return "{" + tag + "}" + "<noparse>{" + tag + "}</noparse>{/" + tag + "}, "; //todo optimize
        }

        public void ShowText()
        {
            string builtText = "Detected Behavior effects:\n";

            string[] behaviors = TAnimBuilder.GetAllBehaviorsTags();
            string[] appearances = TAnimBuilder.GetAllApppearancesTags();

            for (int i = 0; i < behaviors.Length; i++)
            {
                builtText += AddEffect(behaviors[i]);
            }

            builtText += "\n\nDetected Appearance effects:\n";

            for (int i = 0; i < appearances.Length; i++)
            {
                builtText += AddAppearanceEffect(appearances[i]);
            }

            textAnimatorPlayer.ShowText(builtText);
        }
    }
}
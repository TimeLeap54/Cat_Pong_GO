using UnityEditor;
using UnityEngine;

namespace CatTennis.Rebuild.Editor
{
    public static class RallyScorePrefsEditor
    {
        private const string BestRallyPrefsKey = "CatTennis.Rally.BestCount";

        [MenuItem("Cat Tennis/Rebuild/Reset Rally Best Score")]
        public static void ResetRallyBestScore()
        {
            PlayerPrefs.DeleteKey(BestRallyPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("Rally best score reset to 0.");
        }
    }
}

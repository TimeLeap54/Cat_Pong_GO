#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CatTennis.Rebuild.Cat;

namespace CatTennis.Rebuild.Editor
{
    [CustomEditor(typeof(OpponentAIController))]
    public sealed class OpponentAIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            OpponentAIController controller = (OpponentAIController)target;

            GUILayout.Space(15);
            if (GUILayout.Button("Setup Opponent Manual Hitboxes", GUILayout.Height(35)))
            {
                SetupHitboxes(controller);
            }
        }

        private void SetupHitboxes(OpponentAIController controller)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find 'Player' GameObject in the scene.", "OK");
                return;
            }

            GameObject opponentObj = controller.gameObject;

            if (opponentObj.transform.Find("ManualHitboxes") != null)
            {
                EditorUtility.DisplayDialog("Info", "ManualHitboxes already exist under Opponent.", "OK");
                return;
            }

            Transform playerManualHitboxes = playerObj.transform.Find("ManualHitboxes");
            if (playerManualHitboxes != null)
            {
                GameObject newManualHitboxes = Instantiate(playerManualHitboxes.gameObject, opponentObj.transform);
                newManualHitboxes.name = "ManualHitboxes";
                
                PlayerManualHitboxController oldCtrl = opponentObj.GetComponent<PlayerManualHitboxController>();
                if (oldCtrl != null) DestroyImmediate(oldCtrl);

                OpponentManualHitboxController newCtrl = opponentObj.AddComponent<OpponentManualHitboxController>();

                var triggers = newManualHitboxes.GetComponentsInChildren<ManualHitboxTrigger>(true);
                OpponentManualHitboxTrigger normalTrigger = null;
                OpponentManualHitboxTrigger smashTrigger = null;

                foreach (var trigger in triggers)
                {
                    GameObject trigObj = trigger.gameObject;
                    ManualHitboxKind kind = trigger.Kind;
                    DestroyImmediate(trigger);

                    var newTrigger = trigObj.AddComponent<OpponentManualHitboxTrigger>();
                    if (kind == ManualHitboxKind.Normal) normalTrigger = newTrigger;
                    else if (kind == ManualHitboxKind.Smash) smashTrigger = newTrigger;
                }

                OpponentHitDetector hitDetector = opponentObj.GetComponent<OpponentHitDetector>();
                if (hitDetector == null)
                {
                    hitDetector = opponentObj.AddComponent<OpponentHitDetector>();
                }

                newCtrl.Configure(normalTrigger, smashTrigger, hitDetector, newManualHitboxes.transform);
                hitDetector.SetManualHitboxController(newCtrl);

                Undo.RegisterCreatedObjectUndo(newManualHitboxes, "Setup Opponent Manual Hitboxes");
            }

            Transform playerServeAnchors = playerObj.transform.Find("ServeAnchors");
            if (playerServeAnchors != null && opponentObj.transform.Find("ServeAnchors") == null)
            {
                GameObject newServeAnchors = Instantiate(playerServeAnchors.gameObject, opponentObj.transform);
                newServeAnchors.name = "ServeAnchors";
                newServeAnchors.transform.localPosition = new Vector3(-playerServeAnchors.transform.localPosition.x, playerServeAnchors.transform.localPosition.y, playerServeAnchors.transform.localPosition.z);
                
                foreach (Transform child in newServeAnchors.transform)
                {
                    child.localPosition = new Vector3(-child.localPosition.x, child.localPosition.y, child.localPosition.z);
                }

                Undo.RegisterCreatedObjectUndo(newServeAnchors, "Setup Opponent Serve Anchors");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorUtility.DisplayDialog("Success", "Opponent Manual Hitboxes and Serve Anchors setups are completed successfully!", "OK");
        }
    }
}
#endif

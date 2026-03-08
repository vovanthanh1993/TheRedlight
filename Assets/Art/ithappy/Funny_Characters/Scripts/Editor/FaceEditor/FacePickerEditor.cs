using System.Linq;
using CharacterCustomizationTool.FaceManagement;
using UnityEditor;

namespace CharacterCustomizationTool.Editor.FaceEditor
{
    [CustomEditor(typeof(FacePicker))]
    public class FacePickerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var facePicker = (FacePicker)target;

            var previousFaceIndex = facePicker.ActiveFaceIndex;

            var newFaceIndex = EditorGUILayout.Popup("Face", previousFaceIndex, facePicker.Faces.Select(f => f.Replace("_emotion", "")).ToArray());

            if (newFaceIndex != previousFaceIndex)
            {
                facePicker.PickFace(newFaceIndex);

                EditorUtility.SetDirty(facePicker.gameObject);
                AssetDatabase.SaveAssetIfDirty(facePicker.gameObject);
            }
        }
    }
}
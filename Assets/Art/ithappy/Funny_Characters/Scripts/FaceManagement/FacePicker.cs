using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CharacterCustomizationTool.FaceManagement
{
    public class FacePicker : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        private FaceMesh[] _faceMeshes;

        private SkinnedMeshRenderer _faceRenderer;

        public IEnumerable<string> Faces => _faceMeshes.Select(m => m.Mesh.name);
        public int ActiveFaceIndex => Array.FindIndex(_faceMeshes, m => m.Mesh.name.Equals(_faceRenderer.sharedMesh.name, StringComparison.InvariantCultureIgnoreCase));

        public void SetFaces(Mesh[] faceMeshes)
        {
            _faceMeshes = faceMeshes.Select(m => new FaceMesh(m)).ToArray();
        }

        public void PickFace(int index)
        {
            _faceRenderer.sharedMesh = _faceMeshes[index].Mesh;
        }

        private void Start()
        {
            ValidateFields();
        }

        private void OnValidate()
        {
            ValidateFields();
        }

        private void ValidateFields()
        {
            var children = transform
                .Cast<Transform>().ToArray();

            _faceRenderer = children
                .First(t => t.name.Contains("emotion"))
                .GetComponent<SkinnedMeshRenderer>();
        }
    }
}
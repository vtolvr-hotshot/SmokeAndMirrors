using System;

using UnityEngine;

namespace SmokeAndMirrors
{
    [DisallowMultipleComponent]
    class SideMirror : MonoBehaviour
    {
#pragma warning disable CS0649
        public float nearClippingPlaneOffset;
        public Transform mirrorTransform;
        public Transform cameraTransform;
#pragma warning restore CS0649

        private Camera camera;
        private Transform topMirror;

        public Transform TopMirror {
            get => topMirror;
            set
            {
                topMirror = value;
                CopyMeshAndMaterial();
                CreateNewRenderTexture();
            }
        }

        private void Start()
        {
            camera = cameraTransform.GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            var mirrorViewVector =
                Vector3.Reflect(mirrorTransform.position - VRHead.position, mirrorTransform.forward) * 0.33f;
            cameraTransform.position = mirrorTransform.position - mirrorViewVector;
            cameraTransform.rotation = Quaternion.LookRotation(mirrorViewVector, mirrorTransform.up);
            camera.nearClipPlane = mirrorViewVector.magnitude + nearClippingPlaneOffset;
        }

        private void CopyMeshAndMaterial()
        {
            var topMirrorMesh = TopMirror.Find("mirrorMesh");
            var mirrorFrame = transform.Find("canopyFrame.001");
            var mirrorMesh = mirrorFrame.Find("mirrorMesh");

            try
            {
                mirrorFrame.GetComponent<MeshFilter>().mesh = TopMirror.GetComponent<MeshFilter>().mesh;
                mirrorFrame.GetComponent<MeshRenderer>().material = TopMirror.GetComponent<MeshRenderer>().material;
                mirrorMesh.GetComponent<MeshFilter>().mesh = topMirrorMesh.GetComponent<MeshFilter>().mesh;
            }
            catch (NullReferenceException)
            {
                Main.LogError($"Could not copy mesh and materials from {TopMirror} to {mirrorFrame}");
            }
        }

        private void CreateNewRenderTexture()
        {
            try
            {
                // Get references to the existing Material and RenderTexture, to copy their settings
                var topMirrorMaterial = TopMirror.Find("mirrorMesh").GetComponent<MeshRenderer>().material;
                var topMirrorRenderTexture = TopMirror.GetComponentInChildren<Camera>(true).targetTexture;

                // Create and connect the new RenderTexture and Material
                var renderTexture = new RenderTexture(topMirrorRenderTexture);
                var material = new Material(topMirrorMaterial);
                material.SetTexture("_EmissionMap", renderTexture);
                transform.Find("canopyFrame.001/mirrorMesh").GetComponent<MeshRenderer>().material = material;
                transform.GetComponentInChildren<Camera>(true).targetTexture = renderTexture;
            }
            catch (NullReferenceException)
            {
                Main.LogError($"Failed to configure RenderTexture for {transform}");
            }
        }
    }
}

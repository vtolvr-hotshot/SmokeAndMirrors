using System;

using UnityEngine;

namespace SmokeAndMirrors
{
    [DisallowMultipleComponent]
    class SideMirrorLoader : MonoBehaviour
    {
        private void Awake()
        {
            // Define the local position and rotation for the side mirrors
            var leftPosition = new Vector3(0.004662f, 0.035816f, 0.010366f);
            var leftRotation = Quaternion.Euler(6.003f, 58.425f, 0.985f);
            var rightPosition = new Vector3(-0.004662f, 0.035816f, 0.010366f);
            var rightRotation = Quaternion.Euler(6.003f, -58.425f, 0.985f);

            Main.Log("Instantiating side mirrors");

            try
            {
                // Get references to the canopy frame and existing mirror
                var canopyFrame = transform.Find("aFighter2/Canopy/canopyParent/canopy/canopyFrame");
                var topMirror = canopyFrame.Find("canopyFrame.001");

                // Instantiate the side mirrors
                if (Main.Prefabs.TryGetValue("SideMirror", out var sideMirrorPrefab))
                {
                    var leftMirrorObj = Instantiate(sideMirrorPrefab, canopyFrame);
                    leftMirrorObj.transform.localPosition = leftPosition;
                    leftMirrorObj.transform.localRotation = leftRotation;
                    leftMirrorObj.transform.localScale = Vector3.one;
                    leftMirrorObj.GetComponent<SideMirror>().TopMirror = topMirror;
                    leftMirrorObj.SetActive(true);

                    var rightMirrorObj = Instantiate(sideMirrorPrefab, canopyFrame);
                    rightMirrorObj.transform.localPosition = rightPosition;
                    rightMirrorObj.transform.localRotation = rightRotation;
                    rightMirrorObj.transform.localScale = Vector3.one;
                    rightMirrorObj.GetComponent<SideMirror>().TopMirror = topMirror;
                    rightMirrorObj.SetActive(true);
                }
                else
                {
                    Main.LogError("Could not find SideMirror prefab!");
                }
            }
            catch (NullReferenceException)
            {
                Main.LogError("Could not instantiate side mirrors!");
            }
        }
    }

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
